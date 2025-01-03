/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.DnSpy.Metadata;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Documents {
	sealed class AssemblyResolver : IAssemblyResolver {
		readonly DsDocumentService documentService;
		readonly Lazy<IRuntimeAssemblyResolver, IRuntimeAssemblyResolverMetadata>[] runtimeAsmResolvers;
		readonly FailedAssemblyResolveCache failedAssemblyResolveCache;
		readonly DotNetPathProvider dotNetPathProvider;

		static readonly UTF8String mscorlibName = new UTF8String("mscorlib");
		static readonly UTF8String systemRuntimeName = new UTF8String("System.Runtime");
		static readonly UTF8String netstandardName = new UTF8String("netstandard");
		static readonly UTF8String aspNetCoreName = new UTF8String("Microsoft.AspNetCore");
		// netstandard1.5 also uses this version number, but assume it's .NET
		static readonly Version minSystemRuntimeNetCoreVersion = new Version(4, 1, 0, 0);

		const string TFM_netframework = ".NETFramework";
		const string TFM_uwp = ".NETCore";
		const string TFM_netcoreapp = ".NETCoreApp";
		const string TFM_netstandard = ".NETStandard";
		const string UnityEngineFilename = "UnityEngine.dll";
		const string SelfContainedDotNetFilename = "System.Private.CoreLib.dll";

		public AssemblyResolver(DsDocumentService documentService, Lazy<IRuntimeAssemblyResolver, IRuntimeAssemblyResolverMetadata>[] runtimeAsmResolvers) {
			this.documentService = documentService;
			this.runtimeAsmResolvers = runtimeAsmResolvers;
			failedAssemblyResolveCache = new FailedAssemblyResolveCache();
			dotNetPathProvider = new DotNetPathProvider();
		}

		// PERF: Sometimes various pieces of code tries to resolve the same assembly and this
		// assembly isn't found. This class caches these failed resolves so null is returned
		// without searching for the assembly. It forgets about it after some number of seconds
		// in case the user adds the assembly to one of the search paths or loads it in dnSpy.
		sealed class FailedAssemblyResolveCache {
			const int MAX_CACHE_TIME_SECONDS = 10;
			readonly HashSet<IAssembly> failedAsms = new HashSet<IAssembly>(AssemblyNameComparer.CompareAll);
			readonly object lockObj = new object();
			volatile bool isEmpty = true;
			DateTime lastTime = DateTime.UtcNow;

			public bool IsFailed(IAssembly asm) {
				if (isEmpty)
					return false;
				lock (lockObj) {
					if (failedAsms.Count == 0)
						return false;
					var now = DateTime.UtcNow;
					bool isOld = (now - lastTime).TotalSeconds > MAX_CACHE_TIME_SECONDS;
					if (isOld) {
						isEmpty = true;
						failedAsms.Clear();
						return false;
					}
					return failedAsms.Contains(asm);
				}
			}

			public void MarkFailed(IAssembly asm) {
				// Use ToAssemblyRef() to prevent storing a reference to an AssemblyDef
				var asmKey = asm.ToAssemblyRef();
				lock (lockObj) {
					if (failedAsms.Count == 0)
						lastTime = DateTime.UtcNow;
					isEmpty = false;
					failedAsms.Add(asmKey);
				}
			}
		}

		AssemblyDef? IAssemblyResolver.Resolve(IAssembly assembly, ModuleDef? sourceModule) =>
			Resolve(assembly, sourceModule)?.AssemblyDef;

		IDsDocument? Resolve(IAssembly assembly, ModuleDef? sourceModule) {
			if (assembly.IsContentTypeWindowsRuntime) {
				if (failedAssemblyResolveCache.IsFailed(assembly))
					return null;
				var document = ResolveWinMD(assembly, sourceModule);
				if (document is null)
					failedAssemblyResolveCache.MarkFailed(assembly);
				return document;
			}
			else {
				if (failedAssemblyResolveCache.IsFailed(assembly))
					return null;
				var document = ResolveNormal(assembly, sourceModule);
				if (document is null)
					failedAssemblyResolveCache.MarkFailed(assembly);
				return document;
			}
		}

		IDsDocument? TryRuntimeAssemblyResolvers(IAssembly assembly, ModuleDef? sourceModule) {
			foreach (var lz in runtimeAsmResolvers) {
				var result = lz.Value.Resolve(assembly, sourceModule);
				if (!result.IsDefault) {
					if (!string2.IsNullOrEmpty(result.Filename)) {
						var file = documentService.Find(FilenameKey.CreateFullPath(result.Filename), checkTempCache: true);
						if (file is not null)
							return file;
					}

					if (result.GetFileData is not null)
						return documentService.TryGetOrCreateInternal(DsDocumentInfo.CreateInMemory(result.GetFileData, result.Filename), true, true);
					if (!string2.IsNullOrEmpty(result.Filename))
						return documentService.TryGetOrCreateInternal(DsDocumentInfo.CreateDocument(result.Filename), true, true);

					Debug.Fail("Shouldn't be reached");
					return null;
				}
			}

			return null;
		}

		enum FrameworkKind {
			Unknown,
			// This is .NET Framework 1.0-3.5. Search in V2 GAC, not V4 GAC.
			DotNetFramework2,
			// This is .NET Framework 4.0 and later. Search in V4 GAC, not V2 GAC.
			DotNetFramework4,
			DotNet,
			SelfContainedDotNet,
			Unity,
			WindowsUniversal,
			DotNetStandard,
		}

		sealed class FrameworkPathInfo {
			public readonly string Directory;
			public volatile FrameworkKind FrameworkKind;
			public volatile Version? FrameworkVersion;
			public volatile bool Frozen;
			public FrameworkPathInfo(string directory) {
				Directory = directory ?? throw new ArgumentNullException(nameof(directory));
				FrameworkKind = FrameworkKind.Unknown;
			}
		}

		// An array (instead of a dict) is used because it's expected to be small. We can also
		// iterate over it without a lock. Since we use an array we don't need a lock and just
		// overwrite the field (we risk losing a new element but we'll survive if that happens).
		volatile FrameworkPathInfo[] frameworkInfos = Array.Empty<FrameworkPathInfo>();
		FrameworkPathInfo Add(FrameworkPathInfo info) {
			var current = frameworkInfos;
			var newInfos = new FrameworkPathInfo[current.Length + 1];
			for (int i = 0; i < current.Length; i++) {
				var item = current[i];
				if (item.Directory == info.Directory)
					return item;
				newInfos[i] = item;
			}
			newInfos[newInfos.Length - 1] = info;
			frameworkInfos = newInfos;
			return info;
		}
		internal void OnAssembliesCleared() => frameworkInfos = Array.Empty<FrameworkPathInfo>();

		FrameworkKind GetFrameworkKind(ModuleDef? module, out Version? netVersion, out string? sourceModuleDirectoryHint) {
			if (module is null) {
				netVersion = null;
				sourceModuleDirectoryHint = null;
				return FrameworkKind.Unknown;
			}

			var sourceFilename = module.Location;
			if (!string2.IsNullOrEmpty(sourceFilename)) {
				bool isExe = (module.Characteristics & Characteristics.Dll) == 0;
				Version? fwkVersion;
				foreach (var info in frameworkInfos) {
					if (FileUtils.IsFileInDir(info.Directory, sourceFilename)) {
						// The same 'module' could be passed in here multiple times, but we can't save the module instance
						// anywhere so only update info if it's an EXE and then mark it as frozen.
						if (isExe && !info.Frozen) {
							info.Frozen = true;
							var newFwkKind = GetFrameworkKind_TargetFrameworkAttribute(module, out var frameworkName, out fwkVersion);
							if (newFwkKind == FrameworkKind.Unknown)
								newFwkKind = GetFrameworkKind_AssemblyRefs(module, frameworkName, out fwkVersion);
							if (newFwkKind != FrameworkKind.Unknown) {
								info.FrameworkKind = Best(info.FrameworkKind, newFwkKind);
								if (info.FrameworkKind == FrameworkKind.DotNet && newFwkKind == FrameworkKind.DotNet)
									info.FrameworkVersion = fwkVersion;
								if (info.FrameworkKind == FrameworkKind.DotNetStandard && newFwkKind == FrameworkKind.DotNetStandard)
									info.FrameworkVersion = fwkVersion;
							}
						}
						if (info.FrameworkKind == FrameworkKind.DotNet || info.FrameworkKind == FrameworkKind.DotNetStandard)
							netVersion = info.FrameworkVersion;
						else
							netVersion = null;
						sourceModuleDirectoryHint = info.Directory;
						return info.FrameworkKind;
					}
				}

				var fwkKind = GetRuntimeFrameworkKind(sourceFilename, out var frameworkVersion);
				if (fwkKind != FrameworkKind.Unknown) {
					if (fwkKind == FrameworkKind.DotNet || fwkKind == FrameworkKind.DotNetStandard)
						netVersion = frameworkVersion;
					else
						netVersion = null;
					sourceModuleDirectoryHint = null;
					return fwkKind;
				}

				var fwkInfo = new FrameworkPathInfo(Path.GetDirectoryName(sourceFilename)!);
				fwkInfo.FrameworkKind = GetFrameworkKind_Directory(fwkInfo.Directory, out fwkVersion);
				fwkInfo.FrameworkVersion = fwkVersion;
				if (fwkInfo.FrameworkKind == FrameworkKind.Unknown) {
					fwkInfo.FrameworkKind = GetFrameworkKind_TargetFrameworkAttribute(module, out var frameworkName, out fwkVersion);
					fwkInfo.FrameworkVersion = fwkVersion;
					if (fwkInfo.FrameworkKind == FrameworkKind.Unknown) {
						fwkInfo.FrameworkKind = GetFrameworkKind_AssemblyRefs(module, frameworkName, out fwkVersion);
						fwkInfo.FrameworkVersion = fwkVersion;
					}
				}
				if (fwkInfo.FrameworkKind == FrameworkKind.Unknown)
					fwkInfo.FrameworkVersion = null;
				fwkInfo.Frozen = isExe;
				fwkInfo = Add(fwkInfo);
				if (fwkInfo.FrameworkKind == FrameworkKind.DotNet || fwkInfo.FrameworkKind == FrameworkKind.DotNetStandard)
					netVersion = fwkInfo.FrameworkVersion;
				else
					netVersion = null;
				sourceModuleDirectoryHint = fwkInfo.Directory;
				return fwkInfo.FrameworkKind;
			}

			netVersion = null;
			sourceModuleDirectoryHint = null;
			return FrameworkKind.Unknown;
		}

		static FrameworkKind Best(FrameworkKind a, FrameworkKind b) {
			if (a == FrameworkKind.SelfContainedDotNet || b == FrameworkKind.SelfContainedDotNet)
				return FrameworkKind.SelfContainedDotNet;
			if (a == FrameworkKind.DotNet || b == FrameworkKind.DotNet)
				return FrameworkKind.DotNet;
			if (a == FrameworkKind.Unity || b == FrameworkKind.Unity)
				return FrameworkKind.Unity;
			if (a == FrameworkKind.WindowsUniversal || b == FrameworkKind.WindowsUniversal)
				return FrameworkKind.WindowsUniversal;
			if (a == FrameworkKind.DotNetFramework4 || b == FrameworkKind.DotNetFramework4)
				return FrameworkKind.DotNetFramework4;
			if (a == FrameworkKind.DotNetFramework2 || b == FrameworkKind.DotNetFramework2)
				return FrameworkKind.DotNetFramework2;
			if (a == FrameworkKind.DotNetStandard || b == FrameworkKind.DotNetStandard)
				return FrameworkKind.DotNetStandard;
			Debug.Assert(a == FrameworkKind.Unknown && b == FrameworkKind.Unknown);
			return FrameworkKind.Unknown;
		}

		FrameworkKind GetRuntimeFrameworkKind(string filename, out Version? netVersion) {
			foreach (var gacPath in GacInfo.GacPaths) {
				if (FileUtils.IsFileInDir(gacPath.Path, filename)) {
					netVersion = null;
					Debug.Assert(gacPath.Version == GacVersion.V2 || gacPath.Version == GacVersion.V4);
					return gacPath.Version == GacVersion.V2 ? FrameworkKind.DotNetFramework2 : FrameworkKind.DotNetFramework4;
				}
			}

			netVersion = dotNetPathProvider.TryGetDotNetVersion(filename);
			if (netVersion is not null)
				return FrameworkKind.DotNet;

			netVersion = null;
			return FrameworkKind.Unknown;
		}

		static FrameworkKind GetFrameworkKind_Directory(string directory, out Version? version) {
			if (File.Exists(Path.Combine(directory, UnityEngineFilename))) {
				version = null;
				return FrameworkKind.Unity;
			}
			if (File.Exists(Path.Combine(directory, SelfContainedDotNetFilename))) {
				version = null;
				return FrameworkKind.SelfContainedDotNet;
			}

			// Could be a runtime sub dir, eg. "<basedir>\runtimes\unix\lib\netcoreapp2.0". These assemblies
			// don't always have a TFM attribute.
			// Could also be the compilation output directory.
			var dirName = Path.GetFileName(directory);
			if (TryParseVersion("netcoreapp", dirName, out var fwkVersion)) {
				version = fwkVersion;
				return FrameworkKind.DotNet;
			}
			if (TryParseVersion("netstandard", dirName, out fwkVersion)) {
				version = fwkVersion;
				return FrameworkKind.DotNetStandard;
			}
			if (TryParseNetFrameworkVersion("net", dirName, out fwkVersion)) {
				version = fwkVersion;
				// Versions greater or eqaal to 5 should be treated as .NET Core.
				if (version.Major >= 5)
					return FrameworkKind.DotNet;
				return version.Major < 4 ? FrameworkKind.DotNetFramework2 : FrameworkKind.DotNetFramework4;
			}

			version = null;
			return FrameworkKind.Unknown;
		}

		static bool TryParseVersion(string prefix, string tfm, [NotNullWhen(true)] out Version? version) {
			if (!tfm.StartsWith(prefix)) {
				version = null;
				return false;
			}

			var verStr = tfm.Substring(prefix.Length);
			if (Version.TryParse(verStr, out var v)) {
				version = new Version(v.Major, v.Minor, v.Build < 0 ? 0 : v.Build, v.Revision < 0 ? 0 : v.Revision);
				return true;
			}

			version = null;
			return false;
		}

		static bool TryParseNetFrameworkVersion(string prefix, string tfm, [NotNullWhen(true)] out Version? version) {
			if (!tfm.StartsWith(prefix)) {
				version = null;
				return false;
			}

			var verStr = tfm.Substring(prefix.Length);
			if (uint.TryParse(verStr, out uint ver)) {
				if (ver <= 9) {
					version = new Version((int)ver, 0, 0, 0);
					return true;
				}
				if (ver <= 99) {
					version = new Version((int)(ver / 10), (int)(ver % 10), 0, 0);
					return true;
				}
				if (ver <= 999) {
					version = new Version((int)(ver / 100), (int)((ver / 10) % 10), (int)(ver % 10), 0);
					return true;
				}
			}

			version = null;
			return false;
		}

		FrameworkKind GetFrameworkKind_TargetFrameworkAttribute(ModuleDef module, out string? frameworkName, out Version? version) {
			var asm = module.Assembly;
			if (asm is not null && asm.TryGetOriginalTargetFrameworkAttribute(out frameworkName, out version, out _)) {
				if (frameworkName == TFM_netframework)
					return version.Major < 4 ? FrameworkKind.DotNetFramework2 : FrameworkKind.DotNetFramework4;
				if (frameworkName == TFM_netcoreapp)
					return FrameworkKind.DotNet;
				if (frameworkName == TFM_uwp)
					return FrameworkKind.WindowsUniversal;
				if (frameworkName == TFM_netstandard)
					return FrameworkKind.DotNetStandard;
				return FrameworkKind.Unknown;
			}

			frameworkName = null;
			version = null;
			return FrameworkKind.Unknown;
		}

		FrameworkKind GetFrameworkKind_AssemblyRefs(ModuleDef module, string? frameworkName, out Version? version) {
			AssemblyRef? mscorlibRef = null;
			AssemblyRef? systemRuntimeRef = null;
			// ASP.NET Core *.Views assemblies don't have a TFM attribute, so grab the .NET version from an ASP.NET Core asm ref
			AssemblyRef? aspNetCoreRef = null;
			foreach (var asmRef in module.GetAssemblyRefs()) {
				var name = asmRef.Name;
				if (name == mscorlibName) {
					if (IsValidMscorlibVersion(asmRef.Version)) {
						if (mscorlibRef is null || asmRef.Version > mscorlibRef.Version)
							mscorlibRef = asmRef;
					}
				}
				else if (name == systemRuntimeName) {
					if (systemRuntimeRef is null || asmRef.Version > systemRuntimeRef.Version)
						systemRuntimeRef = asmRef;
				}
				else if (name == netstandardName) {
					version = asmRef.Version;
					return FrameworkKind.DotNetStandard;
				}
				else if (StartsWith(name, aspNetCoreName)) {
					if (aspNetCoreRef is null || asmRef.Version > aspNetCoreRef.Version)
						aspNetCoreRef = asmRef;
				}
			}

			if (systemRuntimeRef is not null) {
				// - .NET Core:
				//		1.0: System.Runtime, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		1.1: System.Runtime, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		2.0: System.Runtime, Version=4.2.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		2.1: System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		2.2: System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		3.0: System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		3.1: System.Runtime, Version=4.2.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				// - .NET Standard:
				//		1.0: System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		1.1: System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		1.2: System.Runtime, Version=4.0.10.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		1.3: System.Runtime, Version=4.0.20.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		1.4: System.Runtime, Version=4.0.20.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		1.5: System.Runtime, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		2.0: <it has no System.Runtime ref, just a netstandard.dll ref>
				//		2.1: <it has no System.Runtime ref, just a netstandard.dll ref>
				// - .NET:
				//		5.0: System.Runtime, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		6.0: System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				//		7.0: System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				if (frameworkName != TFM_netstandard) {
					if (module.IsClr40Exactly && systemRuntimeRef.Version >= minSystemRuntimeNetCoreVersion) {
						version = aspNetCoreRef?.Version;
						if (version is null) {
							// .NET Core 1.0 or 1.1
							if (systemRuntimeRef.Version == version_4_1_0_0)
								version = version_1_0_0_0;
							// .NET Core 2.0
							else if (systemRuntimeRef.Version == version_4_2_0_0)
								version = version_2_0_0_0;
							// .NET Core 2.1, 2.2 or 3.0
							else if (systemRuntimeRef.Version == version_4_2_1_0)
								version = version_2_1_0_0;
							// .NET Core 3.1
							else if (systemRuntimeRef.Version == version_4_2_2_0)
								version = version_3_1_0_0;
							// .NET 5
							else if (systemRuntimeRef.Version == version_5_0_0_0)
								version = version_5_0_0_0;
							// .NET 6
							else if (systemRuntimeRef.Version == version_6_0_0_0)
								version = version_6_0_0_0;
							// .NET 7
							else if (systemRuntimeRef.Version == version_7_0_0_0)
								version = version_7_0_0_0;
							else
								Debug.Fail("Unknown .NET Core version");
						}
						return FrameworkKind.DotNet;
					}
				}
			}

			version = null;
			if (mscorlibRef is not null) {
				// It can't be Unity since we checked that before this method was called.
				// It can't be .NET Core since it uses System.Runtime.

				if (mscorlibRef.Version.Major >= 4)
					return FrameworkKind.DotNetFramework4;

				// If it's an exe and it's net20-net35, return that
				if ((module.Characteristics & Characteristics.Dll) == 0)
					return FrameworkKind.DotNetFramework2;

				// It's a net20-net35 dll, but it could be referenced by a net4x asm so we
				// can't return net20-net35.
			}

			return FrameworkKind.Unknown;
		}

		// Cached version instances to prevent allocations
		static readonly Version version_1_0_0_0 = new Version(1, 0, 0, 0);
		static readonly Version version_2_0_0_0 = new Version(2, 0, 0, 0);
		static readonly Version version_2_1_0_0 = new Version(2, 1, 0, 0);
		static readonly Version version_3_1_0_0 = new Version(3, 1, 0, 0);
		static readonly Version version_4_1_0_0 = new Version(4, 1, 0, 0);
		static readonly Version version_4_2_0_0 = new Version(4, 2, 0, 0);
		static readonly Version version_4_2_1_0 = new Version(4, 2, 1, 0);
		static readonly Version version_4_2_2_0 = new Version(4, 2, 2, 0);
		static readonly Version version_5_0_0_0 = new Version(5, 0, 0, 0);
		static readonly Version version_6_0_0_0 = new Version(6, 0, 0, 0);
		static readonly Version version_7_0_0_0 = new Version(7, 0, 0, 0);

		// Silverlight uses 5.0.5.0
		static bool IsValidMscorlibVersion(Version? version) => version is not null && (uint)version.Major <= 5;

		static bool StartsWith(UTF8String? s, UTF8String? value) {
			var d = s?.Data;
			var vd = value?.Data;
			if (d is null || vd is null)
				return false;
			if (d.Length < vd.Length)
				return false;
			for (int i = 0; i < vd.Length; i++) {
				if (d[i] != vd[i])
					return false;
			}
			return true;
		}

		IDsDocument? ResolveNormal(IAssembly assembly, ModuleDef? sourceModule) {
			var fwkKind = GetFrameworkKind(sourceModule, out var netVersion, out var sourceModuleDirectoryHint);
			if (fwkKind == FrameworkKind.DotNetStandard) {
				if (netVersion is not null &&
					dotNetPathProvider.TryGetLatestNetStandardCompatibleVersion(netVersion, out var coreVersion))
					netVersion = coreVersion;
				else {
					fwkKind = FrameworkKind.DotNetFramework4;
					netVersion = null;
				}
			}
			if (fwkKind == FrameworkKind.DotNet && !dotNetPathProvider.HasDotNet)
				fwkKind = FrameworkKind.DotNetFramework4;
			bool loaded;
			IDsDocument? document;
			IDsDocument? existingDocument;
			FindAssemblyOptions options;
			switch (fwkKind) {
			case FrameworkKind.Unknown:
			case FrameworkKind.DotNetFramework2:
			case FrameworkKind.DotNetFramework4:
			case FrameworkKind.DotNetStandard:
				int gacVersion;
				if (!GacInfo.HasGAC2)
					fwkKind = FrameworkKind.DotNetFramework4;
				bool redirected;
				IAssembly tempAsm;
				if (fwkKind == FrameworkKind.DotNetFramework4) {
					redirected = FrameworkRedirect.TryApplyFrameworkRedirectV4(assembly, out tempAsm);
					if (redirected)
						assembly = tempAsm;
					gacVersion = 4;
				}
				else if (fwkKind == FrameworkKind.DotNetFramework2) {
					redirected = FrameworkRedirect.TryApplyFrameworkRedirectV2(assembly, out tempAsm);
					if (redirected)
						assembly = tempAsm;
					gacVersion = 2;
				}
				else {
					Debug.Assert(fwkKind == FrameworkKind.Unknown || fwkKind == FrameworkKind.DotNetStandard);
					redirected = FrameworkRedirect.TryApplyFrameworkRedirect(assembly, sourceModule, out tempAsm);
					// OK : System.Runtime 4.0.20.0 => 4.0.0.0
					// KO : System 4.0.0.0 => 2.0.0.0
					if (redirected && tempAsm.Version.Major >= assembly.Version.Major)
						assembly = tempAsm;
					else
						redirected = false;
					gacVersion = -1;
				}

				document = TryRuntimeAssemblyResolvers(assembly, sourceModule);
				if (document is not null)
					return document;

				options = DsDocumentService.DefaultOptions;
				// If the assembly was redirected, always compare the version number. This prevents resolving
				// mscorlib 2.0 when a .NET 4 app references a .NET 2.0-3.5 dll. We should get mscorlib 4.0.
				if (redirected)
					options |= FindAssemblyOptions.Version;
				existingDocument = documentService.FindAssembly(assembly, options);
				if (existingDocument is not null)
					return existingDocument;

				(document, loaded) = LookupFromSearchPaths(assembly, sourceModule, sourceModuleDirectoryHint, netVersion);
				if (document is not null)
					return documentService.GetOrAddCanDispose(document, assembly, loaded);

				var gacFile = GacInfo.FindInGac(assembly, gacVersion);
				if (gacFile is not null)
					return documentService.TryGetOrCreateInternal(DsDocumentInfo.CreateDocument(gacFile), true, true);
				foreach (var gacPath in GacInfo.OtherGacPaths) {
					if (gacVersion == 4) {
						if (gacPath.Version != GacVersion.V4)
							continue;
					}
					else if (gacVersion == 2) {
						if (gacPath.Version != GacVersion.V2)
							continue;
					}
					else
						Debug.Assert(gacVersion == -1);
					document = TryLoadFromDir(assembly, checkVersion: true, checkPublicKeyToken: true, gacPath.Path);
					if (document is not null)
						return documentService.GetOrAddCanDispose(document, assembly, isAutoLoaded: true);
				}
				break;

			case FrameworkKind.DotNet:
			case FrameworkKind.Unity:
			case FrameworkKind.SelfContainedDotNet:
			case FrameworkKind.WindowsUniversal:
				document = TryRuntimeAssemblyResolvers(assembly, sourceModule);
				if (document is not null)
					return document;

				// If it's a self-contained .NET app, we don't need the version since we must only search
				// the current directory.
				Debug2.Assert(fwkKind == FrameworkKind.DotNet || netVersion is null);
				(document, loaded) = LookupFromSearchPaths(assembly, sourceModule, sourceModuleDirectoryHint, netVersion);
				if (document is not null)
					return documentService.GetOrAddCanDispose(document, assembly, loaded);

				// If it already exists in assembly explorer, use it
				options = DsDocumentService.DefaultOptions;
				if (IgnorePublicKey(fwkKind))
					options &= ~FindAssemblyOptions.PublicKeyToken;
				existingDocument = documentService.FindAssembly(assembly, options);
				if (existingDocument is not null)
					return existingDocument;

				break;

			default:
				throw new InvalidOperationException();
			}

			return null;
		}

		static bool IgnorePublicKey(FrameworkKind fwkKind) {
			switch (fwkKind) {
			case FrameworkKind.Unknown:
			case FrameworkKind.DotNetFramework2:
			case FrameworkKind.DotNetFramework4:
			case FrameworkKind.DotNetStandard:
				return false;

			case FrameworkKind.DotNet:
			case FrameworkKind.SelfContainedDotNet:
			case FrameworkKind.Unity:
			case FrameworkKind.WindowsUniversal:
				return true;

			default:
				throw new InvalidOperationException();
			}
		}

		(IDsDocument? document, bool loaded) LookupFromSearchPaths(IAssembly asmName, ModuleDef? sourceModule, string? sourceModuleDir, Version? dotNetCoreAppVersion) {
			IDsDocument? document;
			if (sourceModuleDir is null && sourceModule is not null && !string2.IsNullOrEmpty(sourceModule.Location)) {
				try {
					sourceModuleDir = Path.GetDirectoryName(sourceModule.Location);
				}
				catch (ArgumentException) {
				}
				catch (PathTooLongException) {
				}
			}

			if (sourceModuleDir is not null) {
				document = TryFindFromDir(asmName, dirPath: sourceModuleDir);
				if (document is not null)
					return (document, false);
			}

			var configProbePaths = GetConfigProbePaths(sourceModule, sourceModuleDir);
			if (configProbePaths is not null) {
				foreach (var path in configProbePaths) {
					document = TryFindFromDir(asmName, dirPath: path);
					if (document is not null)
						return (document, false);
				}
			}

			string[]? dotNetPaths;
			if (dotNetCoreAppVersion is not null) {
				int bitness = (sourceModule?.GetPointerSize(IntPtr.Size) ?? IntPtr.Size) * 8;
				dotNetPaths = dotNetPathProvider.TryGetSharedDotNetPaths(dotNetCoreAppVersion, bitness);
			}
			else
				dotNetPaths = null;
			if (dotNetPaths is not null) {
				foreach (var path in dotNetPaths) {
					document = TryFindFromDir(asmName, dirPath: path);
					if (document is not null)
						return (document, false);
				}
			}

			if (sourceModuleDir is not null) {
				document = TryLoadFromDir(asmName, checkVersion: false, checkPublicKeyToken: false, dirPath: sourceModuleDir);
				if (document is not null)
					return (document, true);
			}

			if (configProbePaths is not null) {
				foreach (var path in configProbePaths) {
					document = TryLoadFromDir(asmName, checkVersion: false, checkPublicKeyToken: false, dirPath: path);
					if (document is not null)
						return (document, true);
				}
			}

			if (dotNetPaths is not null) {
				foreach (var path in dotNetPaths) {
					document = TryLoadFromDir(asmName, checkVersion: false, checkPublicKeyToken: false, dirPath: path);
					if (document is not null)
						return (document, true);
				}
			}

			return default;
		}

		static IList<string>? GetConfigProbePaths(ModuleDef? module, string? sourceModuleDir) {
			var imageName = module?.Assembly?.ManifestModule?.Location;
			if (string2.IsNullOrEmpty(imageName) || string2.IsNullOrEmpty(sourceModuleDir))
				return null;

			var configName = imageName + ".config";
			if (!File.Exists(configName))
				return null;

			try {
				using (var xmlStream = new FileStream(configName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					var doc = new XmlDocument();
					doc.Load(XmlReader.Create(xmlStream));

					var searchPaths = new List<string>();

					foreach (var tmp in doc.GetElementsByTagName("probing")) {
						if (tmp is not XmlElement probingElem)
							continue;
						var privatePath = probingElem.GetAttribute("privatePath");
						if (string2.IsNullOrEmpty(privatePath))
							continue;
						foreach (var tmp2 in privatePath.Split(';')) {
							var path = tmp2.Trim();
							if (string2.IsNullOrEmpty(path))
								continue;
							var newPath = Path.GetFullPath(Path.Combine(sourceModuleDir, path));
							if (Directory.Exists(newPath) && FileUtils.IsFileInDir(sourceModuleDir, newPath))
								searchPaths.Add(newPath);
						}
					}

					return searchPaths;
				}
			}
			catch (ArgumentException) {
			}
			catch (IOException) {
			}
			catch (XmlException) {
			}

			return null;
		}

		IDsDocument? TryFindFromDir(IAssembly asmName, string dirPath) {
			string baseName;
			try {
				baseName = Path.Combine(dirPath, asmName.Name);
			}
			catch (ArgumentException) { // eg. invalid chars in asmName.Name
				return null;
			}
			return TryFindFromDir2(baseName + ".dll") ??
				   TryFindFromDir2(baseName + ".exe");
		}

		IDsDocument? TryFindFromDir2(string filename) => documentService.Find(FilenameKey.CreateFullPath(filename), checkTempCache: true);

		IDsDocument? TryLoadFromDir(IAssembly asmName, bool checkVersion, bool checkPublicKeyToken, string dirPath) {
			string baseName;
			try {
				baseName = Path.Combine(dirPath, asmName.Name);
			}
			catch (ArgumentException) { // eg. invalid chars in asmName.Name
				return null;
			}
			return TryLoadFromDir2(asmName, checkVersion, checkPublicKeyToken, baseName + ".dll") ??
				   TryLoadFromDir2(asmName, checkVersion, checkPublicKeyToken, baseName + ".exe");
		}

		IDsDocument? TryLoadFromDir2(IAssembly asmName, bool checkVersion, bool checkPublicKeyToken, string filename) {
			if (!File.Exists(filename))
				return null;

			IDsDocument? document = null;
			bool error = true;
			try {
				document = documentService.TryCreateDocument(DsDocumentInfo.CreateDocument(filename));
				if (document is null)
					return null;
				document.IsAutoLoaded = true;
				var asm = document.AssemblyDef;
				if (asm is null)
					return null;
				var flags = AssemblyNameComparerFlags.All & ~(AssemblyNameComparerFlags.Version | AssemblyNameComparerFlags.PublicKeyToken);
				if (checkVersion)
					flags |= AssemblyNameComparerFlags.Version;
				if (checkPublicKeyToken)
					flags |= AssemblyNameComparerFlags.PublicKeyToken;
				bool b = new AssemblyNameComparer(flags).Equals(asmName, asm);
				if (!b)
					return null;

				error = false;
				return document;
			}
			finally {
				if (error) {
					if (document is IDisposable)
						((IDisposable)document).Dispose();
				}
			}
		}

		IDsDocument? ResolveWinMD(IAssembly assembly, ModuleDef? sourceModule) {
			IDsDocument? document;

			document = TryRuntimeAssemblyResolvers(assembly, sourceModule);
			if (document is not null)
				return document;

			document = documentService.FindAssembly(assembly, DsDocumentService.DefaultOptions);
			if (document is not null)
				return document;

			foreach (var winmdPath in GacInfo.WinmdPaths) {
				string file;
				try {
					file = Path.Combine(winmdPath, assembly.Name + ".winmd");
				}
				catch (ArgumentException) {
					continue;
				}
				if (File.Exists(file))
					return documentService.TryGetOrCreateInternal(DsDocumentInfo.CreateDocument(file), true, true);
			}
			return null;
		}
	}
}
