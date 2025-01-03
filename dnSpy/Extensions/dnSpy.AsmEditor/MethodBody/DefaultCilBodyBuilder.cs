/*
    Copyright (C) 2022 ElektroKill

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
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.AsmEditor.MethodBody {
	readonly struct DefaultCilBodyBuilder {
		readonly MethodDef _methodDef;
		readonly MethodBodyOptions _options;

		public DefaultCilBodyBuilder(MethodDef methodDef) {
			_methodDef = methodDef;
			_options = new MethodBodyOptions {
				CodeType = MethodImplAttributes.IL,
				BodyType = MethodBodyType.Cil
			};
		}

		internal MethodBodyOptions CreateDefaultCilMethodBody() {
			if (_methodDef.IsInstanceConstructor) {
				if (!_methodDef.DeclaringType.IsValueType) {
					// Call base constructor for reference type constructors.
					var baseCtor = GetBaseConstructorForEmptyBody(_methodDef);
					if (baseCtor is not null) {
						var methodSig = GetMethodBaseSig(_methodDef.DeclaringType.BaseType, baseCtor.MethodSig);
						_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));

						foreach (var methodSigParam in methodSig.Params)
							PushDefaultValue(methodSigParam, false);

						_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Call, baseCtor));
					}
				}
				else if (!_methodDef.DeclaringType.IsEnum) {
					// Initialize instance fields of a value type.
					foreach (var field in _methodDef.DeclaringType.Fields) {
						if (field.IsStatic || field.IsLiteral)
							continue;
						var useLdflda = AcceptsAddressOnStack(field.FieldType);
						_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
						if (useLdflda)
							_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Ldflda, field));
						PushDefaultValue(field.FieldType, useLdflda);
						if (!useLdflda)
							_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Stfld, field));
					}
				}
			}

			// Assign default values to 'out' parameters so that the decompilation produces valid C# code.
			foreach (var parameter in _methodDef.Parameters) {
				if (!parameter.HasParamDef || !parameter.ParamDef.IsOut)
					continue;

				var unwrapped = UnwrapFirstByRef(parameter.Type);
				_options.CilBodyOptions.Instructions.Add(GetLdarg(parameter));

				PushDefaultValue(unwrapped, true);
				if (!AcceptsAddressOnStack(unwrapped))
					_options.CilBodyOptions.Instructions.Add(CreateAddressStore(unwrapped));
			}

			// Return default value if necessary.
			PushDefaultValue(_methodDef.ReturnType, false);

			_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Ret));
			return _options;
		}

		static TypeSig UnwrapFirstByRef(TypeSig sig) {
			sig = sig.RemovePinnedAndModifiers();
			if (sig is ByRefSig byRefSig)
				return byRefSig.Next;
			return sig;
		}

		static bool AcceptsAddressOnStack(TypeSig type) {
			while (true) {
				switch (type.ElementType) {
				case ElementType.GenericInst:
					var genericInst = (GenericInstSig)type;
					if (genericInst.GenericType.RemovePinnedAndModifiers() is ClassSig)
						return false;
					goto case ElementType.ValueType;
				case ElementType.ValueType:
					var resolved = ((ValueTypeSig)type).TypeDefOrRef.ResolveTypeDef();
					if (resolved.IsEnum) {
						type = resolved.GetEnumUnderlyingType();
						continue;
					}
					goto case ElementType.Var;
				case ElementType.Var:
				case ElementType.TypedByRef:
				case ElementType.I:
				case ElementType.U:
				case ElementType.MVar:
					return true;
				case ElementType.CModOpt:
				case ElementType.CModReqd:
					type = type.Next;
					continue;
				default:
					return false;
				}
			}
		}

		void PushDefaultValue(TypeSig type, bool addressOnStack, bool pushAsRef = false) {
			switch (type.ElementType) {
			case ElementType.Void:
				return;
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
				_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
				break;
			case ElementType.I8:
			case ElementType.U8:
				_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Ldc_I8, 0L));
				break;
			case ElementType.R4:
				_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Ldc_R4, 0F));
				break;
			case ElementType.R8:
				_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Ldc_R8, 0D));
				break;
			case ElementType.String:
			case ElementType.Class:
			case ElementType.Array:
			case ElementType.Object:
			case ElementType.SZArray:
				_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
				break;
			case ElementType.Ptr:
			case ElementType.FnPtr:
				_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
				_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Conv_U));
				break;
			case ElementType.ValueType:
				var resolved = ((ValueTypeSig)type).TypeDefOrRef.ResolveTypeDef();
				if (resolved.IsEnum) {
					PushDefaultValue(resolved.GetEnumUnderlyingType(), addressOnStack, pushAsRef);
					return;
				}
				goto case ElementType.Var;
			case ElementType.Var:
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.MVar:
				if (addressOnStack && !pushAsRef) {
					_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Initobj, type.ToTypeDefOrRef()));
					return;
				}
				var local = new Local(type);
				_options.CilBodyOptions.Locals.Add(local);
				_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Ldloca, local));
				_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Initobj, type.ToTypeDefOrRef()));
				_options.CilBodyOptions.Instructions.Add(Instruction.Create(pushAsRef ? OpCodes.Ldloca : OpCodes.Ldloc, local));
				return;
			case ElementType.GenericInst:
				var genericInst = (GenericInstSig)type;
				if (genericInst.GenericType.RemovePinnedAndModifiers() is ClassSig)
					goto case ElementType.Class;
				goto case ElementType.ValueType;
			case ElementType.ByRef:
				PushDefaultValue(type.Next, addressOnStack, true);
				return;
			case ElementType.CModOpt:
			case ElementType.CModReqd:
				PushDefaultValue(type.Next, addressOnStack, pushAsRef);
				return;
			default:
				throw new ArgumentOutOfRangeException();
			}

			if (pushAsRef) {
				var local = new Local(type);
				_options.CilBodyOptions.Locals.Add(local);
				_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Stloc, local));
				_options.CilBodyOptions.Instructions.Add(Instruction.Create(OpCodes.Ldloca, local));
			}
		}

		static Instruction GetLdarg(Parameter parameter) {
			if (parameter.Index == 0)
				return Instruction.Create(OpCodes.Ldarg_0);
			if (parameter.Index == 1)
				return Instruction.Create(OpCodes.Ldarg_1);
			if (parameter.Index == 2)
				return Instruction.Create(OpCodes.Ldarg_2);
			if (parameter.Index == 3)
				return Instruction.Create(OpCodes.Ldarg_3);
			if (byte.MinValue <= parameter.Index && parameter.Index <= byte.MaxValue)
				return Instruction.Create(OpCodes.Ldarg_S, parameter);
			return Instruction.Create(OpCodes.Ldarg, parameter);
		}

		static Instruction CreateAddressStore(TypeSig sig) {
			switch (sig.RemovePinnedAndModifiers().ElementType) {
			case ElementType.Boolean:
			case ElementType.I1:
			case ElementType.U1:
				return Instruction.Create(OpCodes.Stind_I1);
			case ElementType.Char:
			case ElementType.I2:
			case ElementType.U2:
				return Instruction.Create(OpCodes.Stind_I2);
			case ElementType.I4:
			case ElementType.U4:
				return Instruction.Create(OpCodes.Stind_I4);
			case ElementType.I8:
			case ElementType.U8:
				return Instruction.Create(OpCodes.Stind_I8);
			case ElementType.R4:
				return Instruction.Create(OpCodes.Stind_R4);
			case ElementType.R8:
				return Instruction.Create(OpCodes.Stind_R8);
			case ElementType.String:
			case ElementType.Class:
			case ElementType.Array:
			case ElementType.Object:
			case ElementType.SZArray:
				return Instruction.Create(OpCodes.Stind_Ref);
			case ElementType.I:
			case ElementType.U:
				return Instruction.Create(OpCodes.Stind_I);
			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.ValueType:
			case ElementType.Var:
			case ElementType.TypedByRef:
			case ElementType.FnPtr:
			case ElementType.MVar:
				return Instruction.Create(OpCodes.Stobj, sig.ToTypeDefOrRef());
			case ElementType.GenericInst:
				var genericInst = (GenericInstSig)sig;
				if (genericInst.GenericType.RemovePinnedAndModifiers() is ClassSig)
					goto case ElementType.Class;
				goto case ElementType.ValueType;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		static MethodDef? GetBaseConstructorForEmptyBody(MethodDef method) {
			var baseType = method.DeclaringType.BaseType.ResolveTypeDef();
			return baseType is null ? null : GetAccessibleConstructorForEmptyBody(baseType, method.DeclaringType);
		}

		static MethodDef? GetAccessibleConstructorForEmptyBody(TypeDef baseType, TypeDef type) {
			var list = new List<MethodDef>(baseType.FindConstructors());
			if (list.Count == 0)
				return null;
			bool isAssem = baseType.Module.Assembly == type.Module.Assembly || type.Module.Assembly.IsFriendAssemblyOf(baseType.Module.Assembly);
			list.Sort((a, b) => {
				int c = GetAccessForEmptyBody(a, isAssem) - GetAccessForEmptyBody(b, isAssem);
				if (c != 0)
					return c;
				// Don't prefer ref/out ctors
				c = GetParamTypeOrderForEmtpyBody(a) - GetParamTypeOrderForEmtpyBody(b);
				if (c != 0)
					return c;
				return a.Parameters.Count - b.Parameters.Count;
			});
			return list[0];
		}

		static int GetParamTypeOrderForEmtpyBody(MethodDef m) =>
			m.MethodSig.Params.Any(a => a.RemovePinnedAndModifiers() is ByRefSig) ? 1 : 0;

		static int GetAccessForEmptyBody(MethodDef m, bool isAssem) => m.Access switch {
			MethodAttributes.Public => 0,
			MethodAttributes.FamORAssem => 0,
			MethodAttributes.Family => 0,
			MethodAttributes.Assembly => isAssem ? 0 : 1,
			MethodAttributes.FamANDAssem => isAssem ? 0 : 1,
			MethodAttributes.Private => 2,
			MethodAttributes.PrivateScope => 3,
			_ => 3
		};

		static MethodBaseSig GetMethodBaseSig(ITypeDefOrRef type, MethodBaseSig msig) {
			IList<TypeSig>? typeGenArgs = null;
			if (type is TypeSpec ts) {
				var genSig = ts.TypeSig.ToGenericInstSig();
				if (genSig is not null)
					typeGenArgs = genSig.GenericArguments;
			}
			if (typeGenArgs is null)
				return msig;
			return GenericArgumentResolver.Resolve(msig, typeGenArgs, null) ?? msig;
		}
	}
}
