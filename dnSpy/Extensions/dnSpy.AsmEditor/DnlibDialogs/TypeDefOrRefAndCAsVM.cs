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
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class TypeDefOrRefAndCAsVM<TModel> : ListVM<TypeDefOrRefAndCAVM, TModel> where TModel : class {
		static TypeDefOrRefAndCAsVM() {
			if (typeof(TModel) != typeof(GenericParamConstraint) && typeof(TModel) != typeof(InterfaceImpl))
				throw new InvalidOperationException("TModel is an invalid type");
		}

		public TypeDefOrRefAndCAsVM(string editString, string createString, ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef? ownerType, MethodDef? ownerMethod)
			: base(editString, createString, ownerModule, decompilerService, ownerType, ownerMethod) {
		}

		protected override TypeDefOrRefAndCAVM Create(TModel model) {
			var gpc = model as GenericParamConstraint;
			if (gpc is not null)
				return new TypeDefOrRefAndCAVM(new TypeDefOrRefAndCAOptions(gpc), OwnerModule, decompilerService, ownerType, ownerMethod);
			return new TypeDefOrRefAndCAVM(new TypeDefOrRefAndCAOptions((InterfaceImpl)(object)model), OwnerModule, decompilerService, ownerType, ownerMethod);
		}

		protected override TypeDefOrRefAndCAVM Clone(TypeDefOrRefAndCAVM obj) =>
			new TypeDefOrRefAndCAVM(obj.CreateTypeDefOrRefAndCAOptions(), OwnerModule, decompilerService, ownerType, ownerMethod);
		protected override TypeDefOrRefAndCAVM Create() =>
			new TypeDefOrRefAndCAVM(new TypeDefOrRefAndCAOptions(), OwnerModule, decompilerService, ownerType, ownerMethod);
	}
}
