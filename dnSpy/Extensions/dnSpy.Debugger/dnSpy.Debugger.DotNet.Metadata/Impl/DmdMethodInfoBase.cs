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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdMethodInfoBase : DmdMethodInfo {
		sealed private protected override void YouCantDeriveFromThisClass() => throw new InvalidOperationException();

		public sealed override DmdModule Module => DeclaringType!.Module;
		public sealed override DmdType ReturnType => GetMethodSignature().ReturnType;

		public sealed override DmdMethodSignature GetMethodSignature(IList<DmdType> genericMethodArguments) {
			if (genericMethodArguments is null)
				throw new ArgumentNullException(nameof(genericMethodArguments));
			if (!IsGenericMethodDefinition)
				throw new ArgumentException();
			var sig = GetMethodSignature();
			if (genericMethodArguments.Count != sig.GenericParameterCount)
				throw new ArgumentException();
			return GetMethodSignatureCore(genericMethodArguments);
		}

		// Only overridden by DmdMethodDef and DmdMethodRef
		private protected virtual DmdMethodSignature GetMethodSignatureCore(IList<DmdType> genericMethodArguments) => throw new InvalidOperationException();

		public sealed override object? Invoke(object? context, object? obj, DmdBindingFlags invokeAttr, object?[]? parameters) =>
			AppDomain.Invoke(context, this, obj, parameters);

		internal abstract DmdMethodBody? GetMethodBody(IList<DmdType> genericMethodArguments);
	}
}
