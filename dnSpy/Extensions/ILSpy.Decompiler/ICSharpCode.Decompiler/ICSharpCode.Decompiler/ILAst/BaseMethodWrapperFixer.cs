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
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace ICSharpCode.Decompiler.ILAst {
	static class BaseMethodWrapperFixer {
		public static void FixBaseCalls(TypeDef type, ILBlock block, List<ILExpression> listExpr) {
			foreach (var expr in block.GetSelfAndChildrenRecursive(listExpr)) {
				switch (expr.Code) {
				case ILCode.Call:
				case ILCode.CallGetter:
				case ILCode.CallReadOnlySetter:
				case ILCode.CallSetter:
				case ILCode.Callvirt:
				case ILCode.CallvirtGetter:
				case ILCode.CallvirtSetter:
					break;
				default:
					continue;
				}
				var method = (expr.Operand as IMethod).ResolveMethodWithinSameModule();
				if (method?.DeclaringType != type)
					continue;
				if (!TryGetBaseMethod(method, out var realMethod))
					continue;
				expr.Operand = realMethod;
				switch (expr.Code) {
				case ILCode.Callvirt:
					expr.Code = ILCode.Call;
					break;
				case ILCode.CallvirtGetter:
					expr.Code = ILCode.CallGetter;
					break;
				case ILCode.CallvirtSetter:
					expr.Code = ILCode.CallSetter;
					break;
				default:
					continue;
				}
			}
		}

		static bool TryGetBaseMethod(MethodDef method, out IMethod realMethod) {
			realMethod = null;
			if (method == null)
				return false;
			if (!IsBaseWrapperMethod(method))
				return false;
			return IsBaseWrapperMethodBody(method.Body, out realMethod);
		}

		static bool IsBaseWrapperMethodBody(CilBody body, out IMethod calledMethod) {
			calledMethod = null;
			if (body == null)
				return false;
			var instrs = body.Instructions;
			if (instrs.Count < 2)
				return false;
			var callInstr = instrs[instrs.Count - 2];
			var retInstr = instrs[instrs.Count - 1];
			if (retInstr.OpCode.Code != Code.Ret)
				return false;
			if (callInstr.OpCode.Code != Code.Call)
				return false;
			calledMethod = callInstr.Operand as IMethod;
			return calledMethod != null;
		}

		static bool IsBaseWrapperMethod(MethodDef method) {
			if (!method.IsPrivate || method.IsStatic || method.IsAbstract || method.IsVirtual)
				return false;
			var name = UTF8String.ToSystemStringOrEmpty(method.Name);
			if (name.Length == 0)
				return false;

			bool okName = false;
			var c = name[0];
			if (c == '<') {
				// Roslyn C#, eg. "<>n__2"
				if (name.StartsWith("<>n__", StringComparison.Ordinal))
					okName = true;
				// mcs, eg. "<GetString>__BaseCallProxy1"
				else if (name.IndexOf(">__BaseCallProxy", StringComparison.Ordinal) >= 0)
					okName = true;
			}
			// VB, eg. "$VB$ClosureStub_GetString_MyBase"
			else if (c == '$' && name.StartsWith("$VB$ClosureStub_", StringComparison.Ordinal) && name.EndsWith("_MyBase", StringComparison.Ordinal))
				okName = true;

			if (!okName)
				return false;

			return method.CustomAttributes.IsDefined("System.Runtime.CompilerServices.CompilerGeneratedAttribute");
		}
	}
}
