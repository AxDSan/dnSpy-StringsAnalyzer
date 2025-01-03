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
using DDN = dnlib.DotNet;

namespace dnSpy.Debugger.DotNet.Metadata {
	abstract partial class DmdType {
		readonly struct GenericArgsStack {
			readonly List<IReadOnlyList<DmdType>> argsStack;

			public GenericArgsStack() => argsStack = new List<IReadOnlyList<DmdType>>();

			public void Push(IReadOnlyList<DmdType> args) => argsStack.Add(args);

			public void Pop() => argsStack.RemoveAt(argsStack.Count - 1);

			public DmdType Resolve(DmdType type) {
				if (!type.IsGenericTypeParameter)
					return type;
				var newType = Resolve(type.GenericParameterPosition);
				return newType is null || newType == type ? type : newType;
			}

			DmdType? Resolve(int number) {
				DmdType? result = null;
				for (int i = argsStack.Count - 1; i >= 0; i--) {
					var args = argsStack[i];
					if (number >= args.Count)
						return null;
					var type = args[number];
					if (type.IsGenericTypeParameter) {
						result = type;
						number = type.GenericParameterPosition;
					}
					else
						return type;
				}
				return result;
			}
		}

		static int? __ComputeTypeAllignement(DmdType type, GenericArgsStack genericArgs) {
			bool isGenericInst = type.IsConstructedGenericType;
			if (isGenericInst)
				genericArgs.Push(type.GenericTypeArguments);

			type = genericArgs.Resolve(type);

			if (type.IsGenericParameter)
				return null;

			var elementType = type.__GetVerifierCorElementType();

			int alignment;
			if (elementType == DDN.ElementType.ValueType) {
				alignment = 1;
				for (int i = 0; i < type.DeclaredFields.Count; i++) {
					var field = type.DeclaredFields[i];
					if (field.IsStatic)
						continue;
					var fieldAlignment = __ComputeTypeAllignement(field.FieldType, genericArgs);
					if (fieldAlignment is null)
						return null;
					alignment = Math.Max(alignment, fieldAlignment.Value);
				}

				if (type.StructLayoutAttribute is { } layoutAttribute) {
					int packingSize = layoutAttribute.Pack == 0 ? type.AppDomain.Runtime.PointerSize : layoutAttribute.Pack;

					if (layoutAttribute.Size == 0 || packingSize <= layoutAttribute.Size)
						alignment = Math.Min(alignment, packingSize);
				}
			}
			else
				alignment = type.__Size(elementType);

			if (isGenericInst)
				genericArgs.Pop();

			if (alignment <= 0)
				return null;
			return alignment;
		}

		static int? __ComputeTypeSize(DmdType type, GenericArgsStack genericArgs) {
			bool isGenericInst = type.IsConstructedGenericType;
			if (isGenericInst)
				genericArgs.Push(type.GenericTypeArguments);

			type = genericArgs.Resolve(type);

			if (type.IsGenericParameter)
				return null;

			var elementType = type.__GetVerifierCorElementType();

			int size = 0;
			if (elementType == DDN.ElementType.ValueType) {
				var alignment = __ComputeTypeAllignement(type, genericArgs);
				if (alignment is null)
					return null;

				if (type.IsExplicitLayout) {
					for (int i = 0; i < type.DeclaredFields.Count; i++) {
						var field = type.DeclaredFields[i];
						if (field.IsStatic)
							continue;
						var fieldSize = __ComputeTypeSize(field.FieldType, genericArgs);
						if (fieldSize is null)
							return null;
						var fieldOffset = __GetFieldOffset(field);
						if (fieldOffset is null)
							return null;
						size = Math.Max(size, fieldOffset.Value + fieldSize.Value);
					}
				}
				else {
					for (int i = 0; i < type.DeclaredFields.Count; i++) {
						var field = type.DeclaredFields[i];
						if (field.IsStatic)
							continue;
						var fieldSize = __ComputeTypeSize(field.FieldType, genericArgs);
						if (fieldSize is null)
							return null;
						size = AlignUp(size, Math.Min(fieldSize.Value, alignment.Value)) + fieldSize.Value;
					}
				}

				size = Math.Max(1, AlignUp(size, alignment.Value));

				if (type.StructLayoutAttribute is { } layoutAttribute)
					size = Math.Max(layoutAttribute.Size, size);
			}
			else
				size = type.__Size(elementType);

			if (isGenericInst)
				genericArgs.Pop();

			if (size <= 0)
				return null;
			return size;
		}

		static int? __GetFieldOffset(DmdFieldInfo field) {
			var caInfo = field.FindCustomAttribute(
				field.AppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_FieldOffsetAttribute), false);
			if (caInfo is null || caInfo.ConstructorArguments.Count != 1)
				return null;
			var firstArg = caInfo.ConstructorArguments[0];
			if (firstArg.ArgumentType != field.AppDomain.System_Int32 || firstArg.Value is not int i)
				return null;
			return i;
		}

		static int AlignUp(int v, int alignment) => (v + alignment - 1) & ~(alignment - 1);
	}
}
