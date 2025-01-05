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
using dnlib.IO;

namespace dndbg.DotNet {
	sealed class CorEmbeddedResource : EmbeddedResource, ICorHasCustomAttribute {
		readonly CorModuleDef readerModule;
		readonly uint origRid;

		public MDToken OriginalToken => new MDToken(MDToken.Table, origRid);

		protected override void InitializeCustomAttributes() => readerModule.InitCustomAttributes(this, ref customAttributes, new GenericParamContext());

		public CorEmbeddedResource(CorModuleDef readerModule, ManifestResource mr, byte[] data)
			: this(readerModule, mr, ByteArrayDataReaderFactory.Create(data, filename: null), 0, (uint)data.Length) {
		}

		public CorEmbeddedResource(CorModuleDef readerModule, ManifestResource mr, DataReaderFactory dataReaderFactory, uint offset, uint length)
			: base(mr.Name, dataReaderFactory, offset, length, mr.Flags) {
			this.readerModule = readerModule;
			Rid = origRid = mr.Rid;
			Offset = mr.Offset;
		}
	}
}
