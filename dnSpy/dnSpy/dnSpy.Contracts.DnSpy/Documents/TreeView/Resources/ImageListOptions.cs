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

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media;

namespace dnSpy.Contracts.Documents.TreeView.Resources {
	/// <summary>
	/// <see cref="ImageList"/> options
	/// </summary>
	public sealed class ImageListOptions {
		/// <summary>
		/// Gets/sets the name
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets/sets the color depth
		/// </summary>
		public ColorDepth ColorDepth { get; set; }

		/// <summary>
		/// Gets/sets the image size
		/// </summary>
		public Size ImageSize { get; set; }

		/// <summary>
		/// Gets/sets the transparent color
		/// </summary>
		public System.Drawing.Color TransparentColor { get; set; }

		/// <summary>
		/// Gets the images
		/// </summary>
		public List<ImageSource> ImageSources => imageSources;
		readonly List<ImageSource> imageSources = new List<ImageSource>();

		/// <summary>
		/// Constructor
		/// </summary>
		public ImageListOptions() {
			Name = string.Empty;
			ColorDepth = ColorDepth.Depth32Bit;
			ImageSize = new Size(16, 16);
			TransparentColor = System.Drawing.Color.Transparent;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="other">Other instance</param>
		public ImageListOptions(ImageListOptions other) {
			Name = other.Name ?? string.Empty;
			ColorDepth = other.ColorDepth;
			ImageSize = other.ImageSize;
			TransparentColor = other.TransparentColor;
			ImageSources.AddRange(other.ImageSources);
		}
	}
}
