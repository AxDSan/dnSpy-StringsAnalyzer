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
using dnlib.DotNet;

namespace ICSharpCode.Decompiler.ILAst {
	static class MethodUtils {
		static bool IsMethod(MethodDef method, UTF8String name) {
			if (method.Name == name)
				return true;
			foreach (var o in method.Overrides) {
				if (o.MethodDeclaration.Name == name)
					return true;
			}
			return false;
		}

		public static IEnumerable<MethodDef> GetMethod_get_Current(TypeDef type) {
			foreach (var method in type.Methods) {
				if (!method.IsVirtual || method.MethodSig.GetParamCount() != 0)
					continue;
				if (IsMethod(method, name_get_Current))
					yield return method;
			}
		}
		static readonly UTF8String name_get_Current = new UTF8String("get_Current");

		public static IEnumerable<MethodDef> GetMethod_GetEnumerator(TypeDef type) {
			foreach (var method in type.Methods) {
				if (!method.IsVirtual || method.MethodSig.GetParamCount() != 0)
					continue;
				if (IsMethod(method, name_GetEnumerator))
					yield return method;
			}
		}
		static readonly UTF8String name_GetEnumerator = new UTF8String("GetEnumerator");

		public static IEnumerable<MethodDef> GetMethod_Dispose(TypeDef type) {
			foreach (var method in type.Methods) {
				if (!method.IsVirtual || method.MethodSig.GetParamCount() != 0)
					continue;
				if (method.MethodSig.GetRetType().RemovePinnedAndModifiers().GetElementType() != ElementType.Void)
					continue;
				if (IsMethod(method, name_Dispose))
					yield return method;
			}
		}
		static readonly UTF8String name_Dispose = new UTF8String("Dispose");

		public static IEnumerable<MethodDef> GetMethod_MoveNext(TypeDef type) {
			foreach (var method in type.Methods) {
				if (!method.IsVirtual || method.MethodSig.GetParamCount() != 0)
					continue;
				if (method.MethodSig.GetRetType().RemovePinnedAndModifiers().GetElementType() != ElementType.Boolean)
					continue;
				if (IsMethod(method, name_MoveNext))
					yield return method;
			}
		}
		static readonly UTF8String name_MoveNext = new UTF8String("MoveNext");
	}
}
