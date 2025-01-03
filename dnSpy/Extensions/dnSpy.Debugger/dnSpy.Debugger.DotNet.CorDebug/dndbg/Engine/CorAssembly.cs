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
using System.Text;
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dndbg.Engine {
	sealed class CorAssembly : COMObject<ICorDebugAssembly>, IEquatable<CorAssembly?> {
		public IEnumerable<CorModule> Modules {
			get {
				int hr = obj.EnumerateModules(out var moduleEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					hr = moduleEnum.Next(1, out var module, out uint count);
					if (hr != 0 || module is null)
						break;
					yield return new CorModule(module);
				}
			}
		}

		/// <summary>
		/// Assembly name, and is usually the full path to the manifest (first) module on disk
		/// (the EXE or DLL file).
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the full name, identical to the dnlib assembly full name
		/// </summary>
		public string FullName {
			get {
				var module = ManifestModule;
				Debug2.Assert(module is not null);
				if (module is null)
					return Name;
				return CalculateFullName(module);
			}
		}

		static string CalculateFullName(CorModule manifestModule) {
			var mdai = manifestModule.GetMetaDataInterface<IMetaDataAssemblyImport>();
			uint token = new MDToken(Table.Assembly, 1).Raw;

			var asm = new AssemblyNameInfo();
			asm.Name = MDAPI.GetAssemblySimpleName(mdai, token) ?? string.Empty;
			asm.Version = MDAPI.GetAssemblyVersionAndLocale(mdai, token, out var locale) ?? new Version(0, 0, 0, 0);
			asm.Culture = locale ?? string.Empty;
			asm.HashAlgId = MDAPI.GetAssemblyHashAlgorithm(mdai, token) ?? AssemblyHashAlgorithm.SHA1;
			asm.Attributes = MDAPI.GetAssemblyAttributes(mdai, token) ?? AssemblyAttributes.None;
			asm.PublicKeyOrToken = MDAPI.GetAssemblyPublicKey(mdai, token) ?? new PublicKey((byte[]?)null);
			return asm.FullName;
		}

		public CorModule? ManifestModule {
			get {
				CorModule? moduleWithAssemblyRow = null;
				foreach (var module in Modules) {
					if (module.HasAssemblyRow) {
						if (moduleWithAssemblyRow is null || (!IsFile(moduleWithAssemblyRow) && IsFile(module)))
							moduleWithAssemblyRow = module;
					}
				}
				return moduleWithAssemblyRow;
			}
		}

		bool IsFile(CorModule module) => !module.IsDynamic && !module.IsInMemory;

		public CorAssembly(ICorDebugAssembly assembly)
			: base(assembly) => Name = GetName(assembly) ?? string.Empty;

		static string? GetName(ICorDebugAssembly assembly) {
			int hr = assembly.GetName(0, out uint cchName, null);
			if (hr < 0)
				return null;
			var sb = new StringBuilder((int)cchName);
			hr = assembly.GetName(cchName, out cchName, sb);
			if (hr < 0)
				return null;
			return sb.ToString();
		}

		public bool Equals(CorAssembly? other) => other is not null && RawObject == other.RawObject;
		public override bool Equals(object? obj) => Equals(obj as CorAssembly);
		public override int GetHashCode() => RawObject.GetHashCode();
		public override string ToString() => $"[Assembly] {Name}";
	}
}
