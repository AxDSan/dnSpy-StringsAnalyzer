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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using dnSpy.Contracts.Debugger;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	readonly struct ObjectConstantsFactory {
		const int FuncEvalTimeoutMilliseconds = 5000;
		readonly DbgProcess process;
		readonly FuncEvalFactory funcEvalFactory;
		readonly ThreadMirror thread;

		public ObjectConstantsFactory(DbgProcess process, FuncEvalFactory funcEvalFactory, ThreadMirror thread) {
			this.process = process;
			this.funcEvalFactory = funcEvalFactory;
			this.thread = thread;
		}

		public bool TryCreate([NotNullWhen(true)] out ObjectConstants? objectConstants) {
			try {
				var offsetToStringData = GetOffsetToStringData();
				if (offsetToStringData is not null) {
					var offsetToArrayData = GetOffsetToArrayData();
					if (offsetToArrayData is not null) {
						objectConstants = new ObjectConstants(offsetToStringData, offsetToArrayData);
						return true;
					}
				}
			}
			catch {
			}
			objectConstants = null;
			return false;
		}

		Value Call(MethodMirror method, IList<Value> arguments) {
			using (var funcEval = funcEvalFactory.CreateFuncEval(OnFuncEvalComplete, thread, TimeSpan.FromMilliseconds(FuncEvalTimeoutMilliseconds), true, CancellationToken.None))
				return funcEval.CallMethod(method, null, arguments, FuncEvalOptions.Virtual).Result;
		}

		static void OnFuncEvalComplete(FuncEval funcEval) {
		}

		int? GetOffsetToStringData() {
			var type = thread.Domain.Corlib.GetType("System.Runtime.CompilerServices.RuntimeHelpers");
			if (type is null)
				return null;
#pragma warning disable CS0618
			var method = type.GetMethod("get_" + nameof(System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData));
#pragma warning restore CS0618
			if (method is null)
				return null;
			var res = Call(method, Array.Empty<Value>());
			return (res as PrimitiveValue)?.Value as int?;
		}

		int? GetOffsetToArrayData() {
			ArrayMirror? arrayMirror;
			// Later versions of Mono allow easier byte array creation.
			if (thread.VirtualMachine.Version.AtLeast(2, 52)) {
				arrayMirror = thread.Domain.CreateByteArray(randomData);
				if (arrayMirror is null)
					return null;
			}
			else {
				var byteType = thread.Domain.Corlib.GetType("System.Byte");
				var arrayType = thread.Domain.Corlib.GetType("System.Array");
				var typeType = thread.Domain.Corlib.GetType("System.Type");
				if (byteType is null || arrayType is null || typeType is null)
					return null;
				var createInstanceMethod = GetCreateInstance(arrayType);
				if (createInstanceMethod is null)
					return null;
				var args = new Value[] {
					byteType.GetTypeObject(),
					new PrimitiveValue(thread.VirtualMachine, ElementType.I4, randomData.Length),
				};
				arrayMirror = Call(createInstanceMethod, args) as ArrayMirror;
				if (arrayMirror is null)
					return null;
				arrayMirror.SetByteValues(randomData);
			}

			var arrayData = process.ReadMemory((ulong)arrayMirror.Address, randomData.Length + 0x80);
			var res = GetIndex(arrayData, randomData);
			arrayMirror.SetByteValues(emptyData);
			return res;
		}
		static readonly byte[] randomData = { 0x4A, 0xA3, 0x6F, 0x96, 0xB3, 0x8F, 0x4A, 0x5A, 0xB5, 0xD1, 0x8B, 0x76, 0x06, 0x37, 0xB6, 0x67 };
		static readonly byte[] emptyData = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

		static int? GetIndex(byte[] data, byte[] pattern) {
			for (int i = 0; i + pattern.Length <= data.Length; i++) {
				bool match = true;
				for (int j = 0; j < pattern.Length; j++) {
					if (data[i + j] != pattern[j]) {
						match = false;
						break;
					}
				}
				if (match)
					return i;
			}
			return null;
		}

		MethodMirror? GetCreateInstance(TypeMirror arrayType) {
			foreach (var method in arrayType.GetMethods()) {
				if (method.Name != nameof(Array.CreateInstance))
					continue;
				var ps = method.GetParameters();
				if (ps.Length != 2)
					continue;
				if (ps[0].ParameterType.FullName != "System.Type" || ps[1].ParameterType.FullName != "System.Int32")
					continue;
				return method;
			}
			return null;
		}
	}

	sealed class ObjectConstants {
		public int? OffsetToStringData { get; }
		public int? OffsetToArrayData { get; }

		public ObjectConstants(int? offsetToStringData, int? offsetToArrayData) {
			OffsetToStringData = offsetToStringData;
			OffsetToArrayData = offsetToArrayData;
		}
	}
}
