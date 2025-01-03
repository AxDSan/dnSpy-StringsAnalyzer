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
using dnlib.DotNet.Emit;

namespace ICSharpCode.Decompiler.ILAst {
	sealed class AutoPropertyInfo {
		public TypeDef Type => type;
		readonly Dictionary<FieldDef, PropertyDef> toProp;
		TypeDef type;
		IMemberRefParent memberRefClass;

		public AutoPropertyInfo() {
			toProp = new Dictionary<FieldDef, PropertyDef>();
		}

		public void Initialize(TypeDef type) {
			this.type = type;
			foreach (var prop in type.Properties) {
				FieldDef backingField = null;

				var getter = prop.GetMethod;
				if (getter != null) {
					var field = GetGetterBackingField(getter);
					if (field == null || (backingField != null && backingField != field))
						continue;
					backingField = field;
				}

				var setter = prop.SetMethod;
				if (setter != null) {
					var field = GetSetterBackingField(setter);
					if (field == null || (backingField != null && backingField != field))
						continue;
					backingField = field;
				}

				if (backingField == null)
					continue;
				if (!backingField.IsCompilerGenerated())
					continue;
				toProp[backingField] = prop;
			}
		}

		static FieldDef GetGetterBackingField(MethodDef getter) {
			var body = getter.Body;
			if (body == null)
				return null;
			int pos = 0;
			var instrs = body.Instructions;
			IField field;
			if (getter.IsStatic) {
				if (instrs.Count == 2) {
					if (instrs[pos].OpCode.Code != Code.Ldsfld)
						return null;
					field = instrs[pos++].Operand as IField;
				}
				else if (instrs.Count == 5) {
					if (instrs[pos].OpCode.Code != Code.Ldsfld)
						return null;
					field = instrs[pos++].Operand as IField;
					if (instrs[pos++].OpCode.Code != Code.Stloc_0)
						return null;
					var brTarget = instrs[pos];
					if (instrs[pos].OpCode.Code != Code.Br && instrs[pos].OpCode.Code != Code.Br_S)
						return null;
					if (brTarget != instrs[pos++])
						return null;
					if (instrs[pos++].OpCode.Code != Code.Ldloc_0)
						return null;
				}
				else
					return null;
			}
			else {
				if (instrs.Count == 3) {
					if (instrs[pos++].OpCode.Code != Code.Ldarg_0)
						return null;
					if (instrs[pos].OpCode.Code != Code.Ldfld)
						return null;
					field = instrs[pos++].Operand as IField;
				}
				else if (instrs.Count == 6) {
					if (instrs[pos++].OpCode.Code != Code.Ldarg_0)
						return null;
					if (instrs[pos].OpCode.Code != Code.Ldfld)
						return null;
					field = instrs[pos++].Operand as IField;
					if (instrs[pos++].OpCode.Code != Code.Stloc_0)
						return null;
					var brTarget = instrs[pos];
					if (instrs[pos].OpCode.Code != Code.Br && instrs[pos].OpCode.Code != Code.Br_S)
						return null;
					if (brTarget != instrs[pos++])
						return null;
					if (instrs[pos++].OpCode.Code != Code.Ldloc_0)
						return null;
				}
				else
					return null;
			}
			var fd = field.ResolveFieldWithinSameModule();
			if (fd?.DeclaringType != getter.DeclaringType)
				return null;
			if (instrs[pos++].OpCode.Code != Code.Ret)
				return null;
			return fd;
		}

		static FieldDef GetSetterBackingField(MethodDef getter) {
			var body = getter.Body;
			if (body == null)
				return null;
			int pos = 0;
			var instrs = body.Instructions;
			if (getter.IsStatic) {
				if (instrs.Count != 3)
					return null;
				if (instrs[pos++].OpCode.Code != Code.Ldarg_0)
					return null;
				if (instrs[pos].OpCode.Code != Code.Stsfld)
					return null;
			}
			else {
				if (instrs.Count != 4)
					return null;
				if (instrs[pos++].OpCode.Code != Code.Ldarg_0)
					return null;
				if (instrs[pos++].OpCode.Code != Code.Ldarg_1)
					return null;
				if (instrs[pos].OpCode.Code != Code.Stfld)
					return null;
			}
			var field = (instrs[pos++].Operand as IField).ResolveFieldWithinSameModule() as FieldDef;
			if (field?.DeclaringType != getter.DeclaringType)
				return null;
			if (instrs[pos++].OpCode.Code != Code.Ret)
				return null;
			return field;
		}

		public IMethod TryGetGetter(FieldDef field) {
			if (field?.DeclaringType != type)
				return null;
			PropertyDef prop;
			if (!toProp.TryGetValue(field, out prop))
				return null;
			return CreateMethodRef(prop.GetMethod);
		}

		public IMethod TryGetSetter(FieldDef field) {
			if (field?.DeclaringType != type)
				return null;
			PropertyDef prop;
			if (!toProp.TryGetValue(field, out prop))
				return null;
			return CreateMethodRef(prop.SetMethod);
		}

		IMethod CreateMethodRef(MethodDef method) {
			if (method == null)
				return null;
			if (!type.HasGenericParameters)
				return method;
			if (memberRefClass == null) {
				var gis = new GenericInstSig(type.IsValueType ? (ClassOrValueTypeSig)new ValueTypeSig(type) : new ClassSig(type));
				for (int i = 0; i < type.GenericParameters.Count; i++)
					gis.GenericArguments.Add(new GenericVar(i, type));
				memberRefClass = new TypeSpecUser(gis);
			}
			return new MemberRefUser(type.Module, method.Name, method.MethodSig, memberRefClass);
		}

		public void Reset() {
			toProp.Clear();
			type = null;
			memberRefClass = null;
		}
	}
}
