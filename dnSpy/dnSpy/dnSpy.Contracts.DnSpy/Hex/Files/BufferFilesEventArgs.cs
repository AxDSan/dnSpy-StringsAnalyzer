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

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// Buffer files event args base class
	/// </summary>
	public abstract class BufferFilesEventArgs : EventArgs {
		/// <summary>
		/// Gets the files
		/// </summary>
		public HexBufferFile[] Files { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="files">Files</param>
		protected BufferFilesEventArgs(HexBufferFile[] files) => Files = files ?? throw new ArgumentNullException(nameof(files));
	}

	/// <summary>
	/// <see cref="HexBufferFile"/>s added event args
	/// </summary>
	public sealed class BufferFilesAddedEventArgs : BufferFilesEventArgs {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="files">Added files</param>
		public BufferFilesAddedEventArgs(HexBufferFile[] files)
			: base(files) {
		}
	}

	/// <summary>
	/// <see cref="HexBufferFile"/>s removed event args
	/// </summary>
	public sealed class BufferFilesRemovedEventArgs : BufferFilesEventArgs {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="files">Removed files</param>
		public BufferFilesRemovedEventArgs(HexBufferFile[] files)
			: base(files) {
		}
	}
}
