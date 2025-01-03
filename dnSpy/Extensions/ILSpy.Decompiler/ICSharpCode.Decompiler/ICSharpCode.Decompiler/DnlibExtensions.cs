﻿// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.PE;
using dnSpy.Contracts.Decompiler;

namespace ICSharpCode.Decompiler {
	/// <summary>
	/// dnlib helper methods.
	/// </summary>
	static class DnlibExtensions
	{
		sealed class InterfaceImplComparer : IComparer<InterfaceImpl> {
			public static readonly InterfaceImplComparer Instance = new InterfaceImplComparer();

			public int Compare(InterfaceImpl x, InterfaceImpl y) {
				int c = StringComparer.OrdinalIgnoreCase.Compare(x.Interface.Name, y.Interface.Name);
				if (c != 0)
					return c;
				c = x.MDToken.Raw.CompareTo(y.MDToken.Raw);
				if (c != 0)
					return c;
				return x.GetHashCode().CompareTo(y.GetHashCode());
			}
		}

		public static IList<InterfaceImpl> GetInterfaceImpls(this TypeDef type, bool sortMembers)
		{
			if (!sortMembers)
				return type.Interfaces;
			var ary = type.Interfaces.ToArray();
			Array.Sort(ary, InterfaceImplComparer.Instance);
			return ary;
		}

		public static IList<TypeDef> GetNestedTypes(this TypeDef type, bool sortMembers)
		{
			if (!sortMembers)
				return type.NestedTypes;
			var ary = type.NestedTypes.ToArray();
			Array.Sort(ary, TypeDefComparer.Instance);
			return ary;
		}

		public static IList<FieldDef> GetFields(this TypeDef type, bool sortMembers)
		{
			if (!sortMembers || !type.CanSortFields())
				return type.Fields;
			var ary = type.Fields.ToArray();
			Array.Sort(ary, FieldDefComparer.Instance);
			return ary;
		}

		public static IList<EventDef> GetEvents(this TypeDef type, bool sortMembers)
		{
			if (!sortMembers || !type.CanSortMethods())
				return type.Events;
			var ary = type.Events.ToArray();
			Array.Sort(ary, EventDefComparer.Instance);
			return ary;
		}

		public static IList<PropertyDef> GetProperties(this TypeDef type, bool sortMembers)
		{
			if (!sortMembers || !type.CanSortMethods())
				return type.Properties;
			var ary = type.Properties.ToArray();
			Array.Sort(ary, PropertyDefComparer.Instance);
			return ary;
		}

		public static IList<MethodDef> GetMethods(this TypeDef type, bool sortMembers)
		{
			if (!sortMembers || !type.CanSortMethods())
				return type.Methods;
			var ary = type.Methods.ToArray();
			Array.Sort(ary, MethodDefComparer.Instance);
			return ary;
		}

		public static bool SupportsPrefix(this Instruction instr, Code prefix) {
			switch (instr.OpCode.Code) {
			case Code.Cpblk:
			case Code.Initblk:
			case Code.Ldobj:
			case Code.Ldind_I:
			case Code.Ldind_I1:
			case Code.Ldind_I2:
			case Code.Ldind_I4:
			case Code.Ldind_I8:
			case Code.Ldind_R4:
			case Code.Ldind_R8:
			case Code.Ldind_Ref:
			case Code.Ldind_U1:
			case Code.Ldind_U2:
			case Code.Ldind_U4:
			case Code.Stobj:
			case Code.Stind_I:
			case Code.Stind_I1:
			case Code.Stind_I2:
			case Code.Stind_I4:
			case Code.Stind_I8:
			case Code.Stind_R4:
			case Code.Stind_R8:
			case Code.Stind_Ref:
			case Code.Ldfld:
			case Code.Stfld:
				return prefix == Code.Volatile || prefix == Code.Unaligned;
			case Code.Ldsfld:
			case Code.Stsfld:
				return prefix == Code.Volatile;
			case Code.Callvirt:
				return prefix == Code.Constrained || prefix == Code.Tailcall;
			case Code.Call:
				if (prefix == Code.Readonly) {
					return instr.Operand is MemberRef memberRef && memberRef.Name == "Address" &&
						   memberRef.DeclaringType is TypeSpec typeSpec && typeSpec.TypeSig.RemoveModifiers() is ArraySigBase;
				}
				return prefix == Code.Tailcall;
			case Code.Calli:
				return prefix == Code.Tailcall;
			case Code.Ldelema:
				return prefix == Code.Readonly;
			default:
				return false;
			}
		}

		/// <summary>
		/// checks if the given TypeReference is one of the following types:
		/// [sbyte, short, int, long, IntPtr]
		/// </summary>
		public static bool IsSignedIntegralType(this TypeSig type)
		{
			if (type == null)
				return false;
			return type.ElementType == ElementType.I1 ||
				   type.ElementType == ElementType.I2 ||
				   type.ElementType == ElementType.I4 ||
				   type.ElementType == ElementType.I8 ||
				   type.ElementType == ElementType.I;
		}

		public static bool IsZero(this object value)
		{
			if (value == null)
				return false;
			var type = value.GetType();
			switch (Type.GetTypeCode(type)) {
			case TypeCode.Empty:		return false;
			case TypeCode.DBNull:		return false;
			case TypeCode.Boolean:		return false;
			case TypeCode.Char:			return (char)value == 0;
			case TypeCode.SByte:		return (sbyte)value == 0;
			case TypeCode.Byte:			return (byte)value == 0;
			case TypeCode.Int16:		return (short)value == 0;
			case TypeCode.UInt16:		return (ushort)value == 0;
			case TypeCode.Int32:		return (int)value == 0;
			case TypeCode.UInt32:		return (uint)value == 0;
			case TypeCode.Int64:		return (long)value == 0;
			case TypeCode.UInt64:		return (ulong)value == 0;
			case TypeCode.Single:		return (float)value == 0;
			case TypeCode.Double:		return (double)value == 0;
			case TypeCode.Decimal:		return (decimal)value == 0;
			case TypeCode.DateTime:		return false;
			case TypeCode.String:		return false;
			default:					return false;
			case TypeCode.Object:
				var ip = value as IntPtr?;
				if (ip != null)
					return ip.Value == IntPtr.Zero;
				var uip = value as UIntPtr?;
				if (uip != null)
					return uip.Value == UIntPtr.Zero;
				return false;
			}
		}

		/// <summary>
		/// Gets the (exclusive) end offset of this instruction.
		/// </summary>
		public static int GetEndOffset(this Instruction inst)
		{
			if (inst == null)
				return 0;
			return (int)inst.Offset + inst.GetSize();
		}

		public static string OffsetToString(uint offset)
		{
			return string.Format("IL_{0:X4}", offset);
		}

		public static TypeDef ResolveWithinSameModule(this ITypeDefOrRef type)
		{
			if (type != null && type.Scope == type.Module)
				return type.ResolveTypeDef();
			else
				return null;
		}

		public static FieldDef ResolveFieldWithinSameModule(this MemberRef field)
		{
			if (field != null && field.DeclaringType != null && field.DeclaringType.Scope == field.Module)
				return field.ResolveField();
			else
				return null;
		}

		public static FieldDef ResolveFieldWithinSameModule(this IField field)
		{
			if (field != null && field.DeclaringType != null && field.DeclaringType.Scope == field.Module)
				return field is FieldDef ? (FieldDef)field : ((MemberRef)field).ResolveField();
			else
				return null;
		}

		public static MethodDef ResolveMethodWithinSameModule(this IMethod method)
		{
			if (method is MethodSpec)
				method = ((MethodSpec)method).Method;
			if (method != null && method.DeclaringType != null && method.DeclaringType.Scope == method.Module)
				return method is MethodDef ? (MethodDef)method : ((MemberRef)method).ResolveMethod();
			else
				return null;
		}

		public static MethodDef Resolve(this IMethod method)
		{
			if (method is MethodSpec)
				method = ((MethodSpec)method).Method;
			if (method is MemberRef)
				return ((MemberRef)method).ResolveMethod();
			else
				return (MethodDef)method;
		}

		public static FieldDef Resolve(this IField field)
		{
			if (field is MemberRef)
				return ((MemberRef)field).ResolveField();
			else
				return (FieldDef)field;
		}

		public static TypeDef Resolve(this IType type)
		{
			return type == null ? null : type.GetScopeTypeDefOrRef().ResolveTypeDef();
		}

		public static bool IsCompilerGenerated(this IHasCustomAttribute provider)
		{
			return provider.IsDefined(systemRuntimeCompilerServicesString, compilerGeneratedAttributeString);
		}
		static readonly UTF8String systemRuntimeCompilerServicesString = new UTF8String("System.Runtime.CompilerServices");
		static readonly UTF8String compilerGeneratedAttributeString = new UTF8String("CompilerGeneratedAttribute");

		public static bool IsCompilerGeneratedOrIsInCompilerGeneratedClass(this IMemberDef member)
		{
			for (int i = 0; i < 50; i++) {
				if (member == null)
					break;
				if (member.IsCompilerGenerated())
					return true;
				member = member.DeclaringType;
			}
			return false;
		}

		static readonly UTF8String dynamicCallSiteTypeName = new UTF8String("<>o");

		public static bool IsDynamicCallSiteContainerType(this ITypeDefOrRef type)
		{
			if (type == null)
				return false;
			return type.Name == dynamicCallSiteTypeName || type.Name.StartsWith("<>o__");
		}

		public static bool IsAnonymousType(this ITypeDefOrRef type)
		{
			if (type == null)
				return false;
			if (!string.IsNullOrEmpty(type.GetNamespaceInternal()))
				return false;
			string name = type.Name;
			if (name.StartsWith("VB$AnonymousType_", StringComparison.Ordinal) || (type.HasGeneratedName() && (name.Contains("AnonType") || name.Contains("AnonymousType")))) {
				TypeDef td = type.ResolveTypeDef();
				return td != null && td.IsCompilerGenerated();
			}
			return false;
		}

		public static bool HasGeneratedName(this IMemberRef member)
		{
			if (member == null)
				return false;
			var u = member.Name;
			return (object)u != null && u.Data != null && u.Data.Length > 0 && (u.Data[0] == '<' || (u.Data[0] == '$' && u.StartsWith("$VB", StringComparison.Ordinal)));
		}

		public static bool IsLocalFunction(this MethodDef method) {
			var name = method.Name.String;
			return name.StartsWith("<", StringComparison.Ordinal) && name.Contains(">g__");
		}

		public static bool ContainsAnonymousType(this TypeSig type)
		{
			return type.ContainsAnonymousType(0);
		}

		static bool ContainsAnonymousType(this TypeSig type, int depth)
		{
			if (depth >= 30)
				return false;
			GenericInstSig git = type as GenericInstSig;
			if (git != null && git.GenericType != null) {
				if (IsAnonymousType(git.GenericType.TypeDefOrRef))
					return true;
				for (int i = 0; i < git.GenericArguments.Count; i++) {
					if (git.GenericArguments[i].ContainsAnonymousType(depth + 1))
						return true;
				}
				return false;
			}
			if (type != null && type.Next != null)
				return type.Next.ContainsAnonymousType(depth + 1);
			return false;
		}

		public static string GetDefaultMemberName(this TypeDef type)
		{
			if (type != null) {
				foreach (CustomAttribute ca in type.CustomAttributes.FindAll("System.Reflection.DefaultMemberAttribute")) {
					if (ca.Constructor != null && ca.Constructor.FullName == @"System.Void System.Reflection.DefaultMemberAttribute::.ctor(System.String)" &&
					    ca.ConstructorArguments.Count == 1) {
						var value = ca.ConstructorArguments[0].Value;
						var memberName = (value as UTF8String)?.String ?? value as string;
						if (memberName is not null) {
							return memberName;
						}
					}
				}
			}
			return null;
		}

		public static bool IsIndexer(this PropertyDef property)
		{
			if (property != null && property.PropertySig.GetParamCount() > 0) {
				var accessor = property.GetMethod ?? property.SetMethod;
				PropertyDef basePropDef = property;
				if (accessor != null && accessor.HasOverrides) {
					// if the property is explicitly implementing an interface, look up the property in the interface:
					MethodDef baseAccessor = accessor.Overrides.First().MethodDeclaration.Resolve();
					if (baseAccessor != null) {
						for (int i = 0; i < baseAccessor.DeclaringType.Properties.Count; i++) {
							var baseProp = baseAccessor.DeclaringType.Properties[i];
							if (baseProp.GetMethod == baseAccessor || baseProp.SetMethod == baseAccessor) {
								basePropDef = baseProp;
								break;
							}
						}
					} else
						return false;
				}
				var defaultMemberName = basePropDef.DeclaringType.GetDefaultMemberName();
				if (defaultMemberName == basePropDef.Name) {
					return true;
				}
			}
			return false;
		}

		public static Instruction GetPrevious(this CilBody body, Instruction instr)
		{
			int index = body.Instructions.IndexOf(instr);
			if (index <= 0)
				return null;
			return body.Instructions[index - 1];
		}

		public static IList<TypeSig> GetParameters(this MethodBaseSig methodSig)
		{
			if (methodSig == null)
				return new List<TypeSig>();
			if (methodSig.ParamsAfterSentinel != null)
				return methodSig.Params
					.Concat(new TypeSig[] { new SentinelSig() })
					.Concat(methodSig.ParamsAfterSentinel)
					.ToList();
			else
				return methodSig.Params;
		}

		public static IList<TypeSig> GetParametersWithoutSentinel(this MethodBaseSig methodSig)
		{
			if (methodSig is null)
				return new List<TypeSig>();
			if (methodSig.ParamsAfterSentinel is not null)
				return methodSig.Params.Concat(methodSig.ParamsAfterSentinel).ToList();
			return methodSig.Params;
		}

		public static ITypeDefOrRef GetTypeDefOrRef(this TypeSig type)
		{
			type = type.RemovePinnedAndModifiers();
			if (type == null)
				return null;
			if (type.IsGenericInstanceType)
				return ((GenericInstSig)type).GenericType?.TypeDefOrRef;
			else if (type.IsTypeDefOrRef)
				return ((TypeDefOrRefSig)type).TypeDefOrRef;
			else
				return null;
		}

		public static bool IsSystemBoolean(this ITypeDefOrRef type)
		{
			if (type == null)
				return false;
			if (!type.DefinitionAssembly.IsCorLib())
				return false;

			var tr = type as TypeRef;
			if (tr != null)
				return tr.Namespace == systemString && tr.Name == booleanString;
			var td = type as TypeDef;
			if (td != null)
				return td.Namespace == systemString && td.Name == booleanString;

			return false;
		}
		static readonly UTF8String systemString = new UTF8String("System");
		static readonly UTF8String booleanString = new UTF8String("Boolean");
		static readonly UTF8String objectString = new UTF8String("Object");
		static readonly UTF8String nullableString = new UTF8String("Nullable`1");

		public static bool IsSystemObject(this ITypeDefOrRef type)
		{
			if (type == null)
				return false;
			if (!type.DefinitionAssembly.IsCorLib())
				return false;

			var tr = type as TypeRef;
			if (tr != null)
				return tr.Namespace == systemString && tr.Name == objectString;
			var td = type as TypeDef;
			if (td != null)
				return td.Namespace == systemString && td.Name == objectString;

			return false;
		}

		public static IEnumerable<Parameter> GetParameters(this PropertyDef property)
		{
			if (property == null)
				yield break;
			if (property.GetMethod != null) {
				for (int i = 0; i < property.GetMethod.Parameters.Count; i++)
					yield return property.GetMethod.Parameters[i];
				yield break;
			}
			if (property.SetMethod != null)
			{
				int last = property.SetMethod.Parameters.Count - 1;
				for (int i = 0; i < property.SetMethod.Parameters.Count; i++) {
					var param = property.SetMethod.Parameters[i];
					if (param.Index != last)
						yield return param;
				}
				yield break;
			}

			var sigs = property.PropertySig.GetParameters();
			for (int i = 0; i < sigs.Count; i++)
				yield return new Parameter(i, i, sigs[i]);
		}

		public static string GetScopeName(this IScope scope)
		{
			if (scope == null)
				return string.Empty;
			if (scope is IFullName)
				return ((IFullName)scope).Name;
			else
				return scope.ScopeName;	// Shouldn't be reached
		}

		public static int GetParametersSkip(this IList<Parameter> parameters)
		{
			if (parameters == null || parameters.Count == 0)
				return 0;
			if (parameters[0].IsHiddenThisParameter)
				return 1;
			return 0;
		}

		public static IEnumerable<Parameter> SkipNonNormal(this IList<Parameter> parameters) {
			if (parameters == null)
				yield break;
			for (int i = 0; i < parameters.Count; i++) {
				var p = parameters[i];
				if (p.IsNormalMethodParameter)
					yield return p;
			}
		}

		public static int GetNumberOfNormalParameters(this IList<Parameter> parameters)
		{
			if (parameters == null)
				return 0;
			return parameters.Count - GetParametersSkip(parameters);
		}

		public static IEnumerable<int> GetLengths(this ArraySigBase ary)
		{
			var sizes = ary.GetSizes();
			for (int i = 0; i < (int)ary.Rank; i++)
				yield return i < sizes.Count ? (int)sizes[i] - 1 : 0;
		}

		public static string GetFnPtrFullName(FnPtrSig sig)
		{
			if (sig == null)
				return string.Empty;
			var methodSig = sig.MethodSig;
			if (methodSig == null)
				return GetFnPtrName(sig);

			var sb = new StringBuilder();

			sb.Append("method ");
			FullNameFactory.FullNameSB(methodSig.RetType, false, null, null, null, sb);
			sb.Append(" *(");
			PrintArgs(sb, methodSig.Params, true);
			if (methodSig.ParamsAfterSentinel != null) {
				if (methodSig.Params.Count > 0)
					sb.Append(',');
				sb.Append("...,");
				PrintArgs(sb, methodSig.ParamsAfterSentinel, false);
			}
			sb.Append(')');

			return sb.ToString();
		}

		public static string GetMethodSigFullName(MethodSig methodSig)
		{
			if (methodSig == null)
				return string.Empty;
			var sb = new StringBuilder();

			FullNameFactory.FullNameSB(methodSig.RetType, false, null, null, null, sb);
			sb.Append('(');
			PrintArgs(sb, methodSig.Params, true);
			if (methodSig.ParamsAfterSentinel != null) {
				if (methodSig.Params.Count > 0)
					sb.Append(',');
				sb.Append("...,");
				PrintArgs(sb, methodSig.ParamsAfterSentinel, false);
			}
			sb.Append(')');

			return sb.ToString();
		}

		static void PrintArgs(StringBuilder sb, IList<TypeSig> args, bool isFirst) {
			for (int i = 0; i < args.Count; i++) {
				if (!isFirst)
					sb.Append(',');
				isFirst = false;
				FullNameFactory.FullNameSB(args[i], false, null, null, null, sb);
			}
		}

		public static string GetFnPtrName(FnPtrSig sig)
		{
			return "method";
		}

		public static bool IsValueType(ITypeDefOrRef tdr)
		{
			if (tdr == null)
				return false;
			var ts = tdr as TypeSpec;
			if (ts != null)
				return IsValueType(ts.TypeSig);
			return tdr.IsValueType;
		}

		public static bool IsValueType(TypeSig ts) => ts?.IsValueType ?? false;

		static string GetNamespaceInternal(this ITypeDefOrRef tdr) {
			var tr = tdr as TypeRef;
			if (tr != null)
				return tr.Namespace;
			var td = tdr as TypeDef;
			if (td != null)
				return td.Namespace;
			return tdr.Namespace;
		}

		public static string GetNamespace(this IType type, StringBuilder sb) {
			var td = type as TypeDef;
			if (td != null)
				return td.Namespace;
			var tr = type as TypeRef;
			if (tr != null)
				return tr.Namespace;
			sb.Length = 0;
			return FullNameFactory.Namespace(type, false, sb);
		}

		public static string GetName(this IType type, StringBuilder sb) {
			var td = type as TypeDef;
			if (td != null)
				return td.Name;
			var tr = type as TypeRef;
			if (tr != null)
				return tr.Name;
			sb.Length = 0;
			return FullNameFactory.Name(type, false, sb);
		}

		public static bool Compare(this ITypeDefOrRef type, UTF8String expNs, UTF8String expName) {
			if (type == null)
				return false;

			var tr = type as TypeRef;
			if (tr != null)
				return tr.Namespace == expNs && tr.Name == expName;
			var td = type as TypeDef;
			if (td != null)
				return td.Namespace == expNs && td.Name == expName;

			return false;
		}

		public static bool IsSystemNullable(this ClassOrValueTypeSig sig) {
			return sig is ValueTypeSig && sig.TypeDefOrRef.Compare(systemString, nullableString);
		}

		public static bool HasIsReadOnlyAttribute(IHasCustomAttribute hca) {
			if (hca == null)
				return false;
			for (int i = 0; i < hca.CustomAttributes.Count; i++) {
				if (hca.CustomAttributes[i].AttributeType.Compare(systemRuntimeCompilerServicesString, isReadOnlyAttributeString))
					return true;
			}
			return false;
		}
		static readonly UTF8String isReadOnlyAttributeString = new UTF8String("IsReadOnlyAttribute");

		public static bool HasIsByRefLikeAttribute(IHasCustomAttribute hca) {
			if (hca == null)
				return false;
			for (int i = 0; i < hca.CustomAttributes.Count; i++) {
				if (hca.CustomAttributes[i].AttributeType.Compare(systemRuntimeCompilerServicesString, isByRefLikeAttributeString))
					return true;
			}
			return false;
		}
		static readonly UTF8String isByRefLikeAttributeString = new UTF8String("IsByRefLikeAttribute");

		public static ImageSectionHeader GetContainingSection(this ModuleDef mod, RVA rva) {
			if (mod is not ModuleDefMD mdMod)
				return null;
			var image = mdMod.Metadata.PEImage;
			for (int i = 0; i < image.ImageSectionHeaders.Count; i++) {
				var section = image.ImageSectionHeaders[i];
				if (rva >= section.VirtualAddress &&
				    rva < section.VirtualAddress + Math.Max(section.VirtualSize, section.SizeOfRawData))
					return section;
			}
			return null;
		}
	}
}
