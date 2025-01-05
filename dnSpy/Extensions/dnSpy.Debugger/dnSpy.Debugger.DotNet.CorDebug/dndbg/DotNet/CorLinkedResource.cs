/*
    Copyright (C) 2023 ElektroKill

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

using dnlib.DotNet;

namespace dndbg.DotNet {
	sealed class CorLinkedResource : LinkedResource, ICorHasCustomAttribute {
		readonly CorModuleDef readerModule;
		readonly uint origRid;

		public MDToken OriginalToken => new MDToken(MDToken.Table, origRid);

		protected override void InitializeCustomAttributes() => readerModule.InitCustomAttributes(this, ref customAttributes, new GenericParamContext());

		public CorLinkedResource(CorModuleDef readerModule, ManifestResource mr, FileDef file) : base(mr.Name, file, mr.Flags) {
			this.readerModule = readerModule;
			Rid = origRid = mr.Rid;
			Offset = mr.Offset;
		}
	}
}
