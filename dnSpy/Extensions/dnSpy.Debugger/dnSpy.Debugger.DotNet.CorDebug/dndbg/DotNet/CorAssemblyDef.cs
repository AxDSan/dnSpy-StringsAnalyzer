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
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using dndbg.Engine;
using dnlib.DotNet;

namespace dndbg.DotNet {
	sealed class CorAssemblyDef : AssemblyDef, ICorHasCustomAttribute, ICorHasDeclSecurity {
		readonly CorModuleDef readerModule;
		readonly uint origRid;
		volatile bool hasInitdTFA;
		string? tfaFramework;
		Version? tfaVersion;
		string? tfaProfile;
		bool tfaReturnValue;

		public MDToken OriginalToken => new MDToken(MDToken.Table, origRid);

		public CorAssemblyDef(CorModuleDef readerModule, uint rid) {
			this.readerModule = readerModule;
			this.rid = rid;
			origRid = rid;
			Initialize_NoLock();
		}

		void Initialize_NoLock() => InitAssemblyName_NoLock();
		protected override void InitializeCustomAttributes() =>
			readerModule.InitCustomAttributes(this, ref customAttributes, new GenericParamContext());
		protected override void InitializeDeclSecurities() =>
			readerModule.InitDeclSecurities(this, ref declSecurities);

		unsafe void InitAssemblyName_NoLock() {
			var mdai = readerModule.MetaDataAssemblyImport;
			uint token = OriginalToken.Raw;

			Name = MDAPI.GetAssemblySimpleName(mdai, token) ?? string.Empty;
			Version = MDAPI.GetAssemblyVersionAndLocale(mdai, token, out var locale) ?? new Version(0, 0, 0, 0);
			Culture = locale ?? string.Empty;
			HashAlgorithm = MDAPI.GetAssemblyHashAlgorithm(mdai, token) ?? AssemblyHashAlgorithm.SHA1;
			Attributes = MDAPI.GetAssemblyAttributes(mdai, token) ?? AssemblyAttributes.None;
			PublicKey = MDAPI.GetAssemblyPublicKey(mdai, token) ?? new PublicKey((byte[]?)null);
		}

		public override bool TryGetOriginalTargetFrameworkAttribute(out string? framework, out Version? version, out string? profile) {
			if (!hasInitdTFA)
				InitializeTargetFrameworkAttribute();
			framework = tfaFramework;
			version = tfaVersion;
			profile = tfaProfile;
			return tfaReturnValue;
		}

		void InitializeTargetFrameworkAttribute() {
			if (hasInitdTFA)
				return;

			uint[] tokens = MDAPI.GetCustomAttributeTokens(readerModule.MetaDataImport, OriginalToken.Raw);
			var gpContext = new GenericParamContext();
			for (int i = 0; i < tokens.Length; i++) {
				var caBlob = MDAPI.GetCustomAttributeBlob(readerModule.MetaDataImport, tokens[i], out uint typeToken);
				var cat = readerModule.ResolveToken(typeToken, gpContext) as ICustomAttributeType;
				if (!TryGetName(cat, out var ns, out var name))
					continue;
				if (ns != nameSystemRuntimeVersioning || name != nameTargetFrameworkAttribute)
					continue;
				var ca = CustomAttributeReader.Read(readerModule, caBlob, cat, gpContext);
				if (ca is null || ca.ConstructorArguments.Count != 1)
					continue;
				if (ca.ConstructorArguments[0].Value is not UTF8String s)
					continue;
				if (TryCreateTargetFrameworkInfo(s, out var tmpFramework, out var tmpVersion, out var tmpProfile)) {
					tfaFramework = tmpFramework;
					tfaVersion = tmpVersion;
					tfaProfile = tmpProfile;
					tfaReturnValue = true;
					break;
				}
			}

			hasInitdTFA = true;
		}

		static readonly UTF8String nameSystemRuntimeVersioning = new UTF8String("System.Runtime.Versioning");
		static readonly UTF8String nameTargetFrameworkAttribute = new UTF8String("TargetFrameworkAttribute");

		static bool TryGetName(ICustomAttributeType? caType, out UTF8String? ns, out UTF8String? name) {
			ITypeDefOrRef? type;
			if (caType is MemberRef mr)
				type = mr.DeclaringType;
			else
				type = (caType as MethodDef)?.DeclaringType;
			if (type is TypeRef tr) {
				ns = tr.Namespace;
				name = tr.Name;
				return true;
			}
			if (type is TypeDef td) {
				ns = td.Namespace;
				name = td.Name;
				return true;
			}
			ns = null;
			name = null;
			return false;
		}

		static bool TryCreateTargetFrameworkInfo(string attrString, out string? framework, out Version? version, out string? profile) {
			framework = null;
			version = null;
			profile = null;

			// See corclr/src/mscorlib/src/System/Runtime/Versioning/BinaryCompatibility.cs
			var values = attrString.Split(',');
			if (values.Length < 2 || values.Length > 3)
				return false;
			var frameworkRes = values[0].Trim();
			if (frameworkRes.Length == 0)
				return false;

			Version? versionRes = null;
			string? profileRes = null;
			for (int i = 1; i < values.Length; i++) {
				var kvp = values[i].Split('=');
				if (kvp.Length != 2)
					return false;

				var key = kvp[0].Trim();
				var value = kvp[1].Trim();

				if (key.Equals("Version", StringComparison.OrdinalIgnoreCase)) {
					if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
						value = value.Substring(1);
					if (!TryParse(value, out versionRes))
						return false;
					versionRes = new Version(versionRes.Major, versionRes.Minor, versionRes.Build == -1 ? 0 : versionRes.Build, 0);
				}
				else if (key.Equals("Profile", StringComparison.OrdinalIgnoreCase)) {
					if (!string.IsNullOrEmpty(value))
						profileRes = value;
				}
			}
			if (versionRes is null)
				return false;

			framework = frameworkRes;
			version = versionRes;
			profile = profileRes;
			return true;
		}

		static int ParseInt32(string s) => int.TryParse(s, out int res) ? res : 0;

		static bool TryParse(string s, [NotNullWhen(true)] out Version? version) {
			var m = Regex.Match(s, @"^(\d+)\.(\d+)$");
			if (m.Groups.Count == 3) {
				version = new Version(ParseInt32(m.Groups[1].Value), ParseInt32(m.Groups[2].Value));
				return true;
			}

			m = Regex.Match(s, @"^(\d+)\.(\d+)\.(\d+)$");
			if (m.Groups.Count == 4) {
				version = new Version(ParseInt32(m.Groups[1].Value), ParseInt32(m.Groups[2].Value), ParseInt32(m.Groups[3].Value));
				return true;
			}

			m = Regex.Match(s, @"^(\d+)\.(\d+)\.(\d+)\.(\d+)$");
			if (m.Groups.Count == 5) {
				version = new Version(ParseInt32(m.Groups[1].Value), ParseInt32(m.Groups[2].Value), ParseInt32(m.Groups[3].Value), ParseInt32(m.Groups[4].Value));
				return true;
			}

			version = null;
			return false;
		}
	}
}
