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

namespace dnSpy.Settings.Dialog {
	/// <summary>
	/// Converts objects to other objects that get shown in the UI. Eg. it could highlight
	/// the searched text by converting a label's text to highlighted text.
	/// </summary>
	interface IContentConverter {
		/// <summary>
		/// Converts <paramref name="content"/> to a new object or returns the input
		/// </summary>
		/// <param name="content">Current content, usually a string</param>
		/// <param name="ownerControl">Owner control</param>
		/// <returns></returns>
		object Convert(object content, object ownerControl);
	}
}
