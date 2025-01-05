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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Interpreter;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter {
	sealed class NullableILValue : TypeILValueImpl {
		readonly DmdFieldInfo hasValueField;
		readonly DmdFieldInfo valueField;

		public NullableILValue(DebuggerRuntimeImpl runtime, DbgDotNetValue nullableValue) : base(runtime, nullableValue) {
			var nullableFields = NullableTypeUtils.TryGetNullableFields(nullableValue.Type);
			if (nullableFields.valueField is null || nullableFields.hasValueField is null)
				throw new InvalidOperationException();
			(hasValueField, valueField) = nullableFields;
		}

		public override ILValue? Box(DmdType type) {
			if (type.IsNullable && type.Equals(Type)) {
				var hasValueResult = runtime.LoadInstanceField2(ObjValue, hasValueField).GetRawValue();
				if (!hasValueResult.HasRawValue || hasValueResult.ValueType != DbgSimpleValueType.Boolean)
					return null;
				if ((bool)hasValueResult.RawValue!)
					return runtime.Box(runtime.LoadInstanceField(ObjValue, valueField), Type!.GetNullableElementType());
				return new NullObjectRefILValue();
			}
			return base.Box(type);
		}
	}
}
