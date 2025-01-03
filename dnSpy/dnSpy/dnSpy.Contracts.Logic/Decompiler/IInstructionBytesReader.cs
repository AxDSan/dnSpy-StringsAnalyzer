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

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Instruction bytes reader
	/// </summary>
	public interface IInstructionBytesReader {
		/// <summary>
		/// true if it's reading the original bytes, false if the method has been modified
		/// </summary>
		bool IsOriginalBytes { get; }

		/// <summary>
		/// Reads the next byte or returns a value less than 0 if the byte is unknown
		/// </summary>
		/// <returns></returns>
		int ReadByte();

		/// <summary>
		/// Initializes the next instruction that should be read
		/// </summary>
		/// <param name="index">Index of the instruction in the method body</param>
		/// <param name="offset">Offset of the instruction in the stream</param>
		void SetInstruction(int index, uint offset);
	}
}
