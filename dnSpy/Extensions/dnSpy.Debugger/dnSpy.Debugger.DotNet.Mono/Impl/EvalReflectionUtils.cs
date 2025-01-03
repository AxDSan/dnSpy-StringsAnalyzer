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

using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Mono.Properties;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	static class EvalReflectionUtils {
		public static string? TryGetExceptionMessage(ObjectMirror exObj) {
			var field = GetField(exObj.Type, KnownMemberNames.Exception_Message_FieldName, KnownMemberNames.Exception_Message_FieldName_Mono);
			if (field is null)
				return null;
			var value = exObj.GetValue(field);
			if (value is StringMirror sm)
				return sm.Value ?? dnSpy_Debugger_DotNet_Mono_Resources.ExceptionMessageIsNull;
			if (value is null || (value is PrimitiveValue pv && pv.Value is null))
				return dnSpy_Debugger_DotNet_Mono_Resources.ExceptionMessageIsNull;
			return null;
		}

		public static int? TryGetExceptionHResult(ObjectMirror exObj) {
			var field = GetField(exObj.Type, KnownMemberNames.Exception_HResult_FieldName);
			if (field is null)
				return null;
			var value = exObj.GetValue(field);
			if (value is PrimitiveValue primitive && primitive.Value is int hResult)
				return hResult;
			return null;
		}

		static FieldInfoMirror? GetField(TypeMirror type, string name1, string? name2 = null) {
			while (type is not null) {
				foreach (var field in type.GetFields()) {
					if (field.Name == name1)
						return field;
					if (name2 is not null && field.Name == name2)
						return field;
				}
				type = type.BaseType;
			}
			return null;
		}
	}
}
