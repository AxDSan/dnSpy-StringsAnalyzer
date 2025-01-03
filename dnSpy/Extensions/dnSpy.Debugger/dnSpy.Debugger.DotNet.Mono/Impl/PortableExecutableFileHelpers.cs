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

using System.IO;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	static class PortableExecutableFileHelpers {
		public static bool IsGuiApp(string? file) {
			if (!File.Exists(file))
				return false;
			try {
				using (var f = File.OpenRead(file!)) {
					var r = new BinaryReader(f);
					if (r.ReadUInt16() != 0x5A4D)
						return false;
					f.Position = 0x3C;
					f.Position = r.ReadUInt32();
					// Mono only checks the low 2 bytes
					if ((ushort)r.ReadUInt32() != 0x4550)
						return false;
					f.Position += 0x14;
					ushort magic = r.ReadUInt16();
					if (magic != 0x010B && magic != 0x020B)
						return false;
					r.BaseStream.Position += 0x42;
					const ushort WindowsGui = 2;
					return r.ReadUInt16() == WindowsGui;
				}
			}
			catch {
			}
			return false;
		}
	}
}
