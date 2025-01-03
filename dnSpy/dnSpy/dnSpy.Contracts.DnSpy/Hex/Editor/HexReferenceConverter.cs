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

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Converts a reference
	/// </summary>
	public abstract class HexReferenceConverter {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexReferenceConverter() { }

		/// <summary>
		/// Converts the reference to a new reference or returns the original value
		/// </summary>
		/// <param name="hexView">Hex view</param>
		/// <param name="reference">Reference</param>
		/// <returns></returns>
		public abstract object? Convert(HexView hexView, object reference);
	}
}
