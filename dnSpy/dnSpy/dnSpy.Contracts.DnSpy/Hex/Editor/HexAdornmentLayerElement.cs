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

using System.Windows;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Adornment layer element
	/// </summary>
	public abstract class HexAdornmentLayerElement {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexAdornmentLayerElement() { }

		/// <summary>
		/// Gets the adornment
		/// </summary>
		public abstract UIElement Adornment { get; }

		/// <summary>
		/// Gets the positioning behavior
		/// </summary>
		public abstract VSTE.AdornmentPositioningBehavior Behavior { get; }

		/// <summary>
		/// Called when the adornment is removed
		/// </summary>
		public abstract VSTE.AdornmentRemovedCallback? RemovedCallback { get; }

		/// <summary>
		/// Gets the tag
		/// </summary>
		public abstract object? Tag { get; }

		/// <summary>
		/// Buffer span
		/// </summary>
		public abstract HexBufferSpan? VisualSpan { get; }
	}
}
