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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using dndbg.DotNet;
using dnlib.IO;

namespace dndbg.Engine {
	sealed class ProcessDataStream : DataStream {
		readonly ProcessBinaryReader reader;
		readonly long basePos;

		public ProcessDataStream(ProcessBinaryReader reader) {
			this.reader = reader;
			basePos = reader.Position;
		}

		public override unsafe void ReadBytes(uint offset, void* destination, int length) {
			reader.Position = basePos + offset;
			byte[] dest = new byte[length];
			reader.Read(dest, 0, length);
			Marshal.Copy(dest, 0, (IntPtr)destination, length);
		}

		public override void ReadBytes(uint offset, byte[] destination, int destinationIndex, int length) {
			reader.Position = basePos + offset;
			reader.Read(destination, destinationIndex, length);
		}

		public override byte ReadByte(uint offset) {
			reader.Position = basePos + offset;
			return reader.ReadByte();
		}

		public override ushort ReadUInt16(uint offset) {
			reader.Position = basePos + offset;
			return reader.ReadUInt16();
		}

		public override uint ReadUInt32(uint offset) {
			reader.Position = basePos + offset;
			return reader.ReadUInt32();
		}

		public override ulong ReadUInt64(uint offset) {
			reader.Position = basePos + offset;
			return reader.ReadUInt64();
		}

		public override float ReadSingle(uint offset) {
			reader.Position = basePos + offset;
			return reader.ReadSingle();
		}

		public override double ReadDouble(uint offset) {
			reader.Position = basePos + offset;
			return reader.ReadDouble();
		}

		public override string ReadUtf16String(uint offset, int chars) {
			reader.Position = basePos + offset;
			byte[] dest = new byte[chars * 2];
			reader.Read(dest, 0, chars * 2);
			return Encoding.Unicode.GetString(dest);
		}

		public override string ReadString(uint offset, int length, Encoding encoding) {
			reader.Position = basePos + offset;
			byte[] dest = new byte[length];
			reader.Read(dest, 0, length);
			return encoding.GetString(dest);
		}

		public override bool TryGetOffsetOf(uint offset, uint endOffset, byte value, out uint valueOffset) {
			Debug.Fail("NYI");
			throw new NotImplementedException();
		}
	}
}
