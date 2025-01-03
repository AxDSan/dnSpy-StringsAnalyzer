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

using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;

namespace dnSpy.Decompiler.MSBuild {
	static class DotNetUtils {
		static bool IsType(TypeDef type, string typeFullName) {
			while (type is not null) {
				var bt = type.BaseType;
				if (bt is null)
					break;
				if (bt.FullName == typeFullName)
					return true;
				type = bt.ResolveTypeDef();
			}
			return false;
		}

		public static bool IsWinForm(TypeDef type) => IsType(type, "System.Windows.Forms.Form");
		public static bool IsSystemWindowsApplication(TypeDef type) => IsType(type, "System.Windows.Application");
		public static bool IsStartUpClass(TypeDef type) => type.Module.EntryPoint is not null && type.Module.EntryPoint.DeclaringType == type;
		public static bool IsUnsafe(ModuleDef module) => module.CustomAttributes.IsDefined("System.Security.UnverifiableCodeAttribute");
		public static IEnumerable<FieldDef> GetFields(MethodDef method) => GetDefs(method).OfType<FieldDef>();

		public static IEnumerable<IMemberDef> GetDefs(MethodDef method) {
			var body = method.Body;
			if (body is not null) {
				foreach (var instr in body.Instructions) {
					if (instr.Operand is IMemberDef def && def.DeclaringType == method.DeclaringType)
						yield return def;
				}
			}
		}

		public static IEnumerable<IMemberDef> GetDefs(PropertyDef prop) {
			foreach (var g in prop.GetMethods) {
				foreach (var d in GetDefs(g))
					yield return d;
			}
		}

		public static IEnumerable<IMemberDef> GetMethodsAndSelf(PropertyDef p) {
			yield return p;
			foreach (var m in p.GetMethods)
				yield return m;
			foreach (var m in p.SetMethods)
				yield return m;
			foreach (var m in p.OtherMethods)
				yield return m;
		}
	}
}
