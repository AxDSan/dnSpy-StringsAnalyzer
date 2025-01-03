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
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace dnSpy.Contracts.Documents.TreeView.Resources {
	/// <summary>
	/// Resource image utils
	/// </summary>
	public static class ImageResourceUtilities {
		/// <summary>
		/// Creates a <see cref="ImageSource"/>
		/// </summary>
		/// <param name="data">Data</param>
		/// <returns></returns>
		public static ImageSource CreateImageSource(byte[] data) {
			// Check if CUR
			if (data.Length >= 4 && BitConverter.ToUInt32(data, 0) == 0x00020000) {
				try {
					data[2] = 1;
					return CreateImageSource2(data);
				}
				finally {
					data[2] = 2;
				}
			}

			return CreateImageSource2(data);
		}

		static ImageSource CreateImageSource2(byte[] data) {
			var bimg = new BitmapImage();
			bimg.BeginInit();
			bimg.StreamSource = new MemoryStream(data);
			bimg.EndInit();
			return bimg;
		}
	}
}
