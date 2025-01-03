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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Debugger.DotNet.Interpreter;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter {
	interface IDebuggerRuntime {
		void CreateVariable(DmdType type, string name, Guid customTypeInfoPayloadTypeId, byte[] customTypeInfoPayload);
		DbgDotNetValue CreateValue(object? value, DmdType targetType);
		DbgDotNetValue GetException();
		DbgDotNetValue GetStowedException();
		DbgDotNetValue GetReturnValue(int index);
		DbgDotNetValue GetObjectByAlias(string name);
		DbgDotNetValue GetObjectAtAddress(ulong address);
		DbgDotNetValue GetVariableAddress(DmdType type, string name);
		char ToChar(ILValue value);
		int ToInt32(ILValue value);
		ulong ToUInt64(ILValue value);
		string ToString(ILValue value);
		DmdType ToType(ILValue value);
		Guid ToGuid(ILValue value);
		byte[] ToByteArray(ILValue value);
		DbgDotNetValue ToDotNetValue(ILValue value);
	}
}
