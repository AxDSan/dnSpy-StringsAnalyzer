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

namespace dnSpy.Contracts.Hex.Files.PE {
	/// <summary>
	/// PE headers
	/// </summary>
	public abstract class PeHeaders : IBufferFileHeaders {
		/// <summary>
		/// Constructor
		/// </summary>
		protected PeHeaders() { }

		/// <summary>
		/// Gets the DOS header
		/// </summary>
		public abstract PeDosHeaderData DosHeader { get; }

		/// <summary>
		/// Gets the file header
		/// </summary>
		public abstract PeFileHeaderData FileHeader { get; }

		/// <summary>
		/// Gets the optional header
		/// </summary>
		public abstract PeOptionalHeaderData OptionalHeader { get; }

		/// <summary>
		/// Gets the sections
		/// </summary>
		public abstract PeSectionsData Sections { get; }

		/// <summary>
		/// true if file layout, false if memory layout
		/// </summary>
		public abstract bool IsFileLayout { get; }

		/// <summary>
		/// Converts <paramref name="rva"/> to a buffer position. This method uses data
		/// from cached section headers.
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		public abstract HexPosition RvaToBufferPosition(uint rva);

		/// <summary>
		/// Converts a buffer position to an RVA. If the input is invalid, 0 is returned.
		/// This method uses data from cached section headers.
		/// </summary>
		/// <param name="position">Buffer position</param>
		/// <returns></returns>
		public abstract uint BufferPositionToRva(HexPosition position);

		/// <summary>
		/// Converts a buffer position to a file position in the PE file. If the input is invalid, 0 is returned.
		/// This method uses data from cached section headers.
		/// </summary>
		/// <param name="position">Buffer position</param>
		/// <returns></returns>
		public abstract ulong BufferPositionToFilePosition(HexPosition position);

		/// <summary>
		/// Converts a file position in the PE file to a buffer position. If the input is invalid, 0 is returned.
		/// This method uses data from cached section headers.
		/// </summary>
		/// <param name="position">File position</param>
		/// <returns></returns>
		public abstract HexPosition FilePositionToBufferPosition(ulong position);
	}
}
