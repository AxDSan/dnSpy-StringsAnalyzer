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

namespace dnSpy.Contracts.Documents.Tabs {
	/// <summary>
	/// Saves tabs
	/// </summary>
	public interface ISaveService {
		/// <summary>
		/// Returns true if the tab can be saved
		/// </summary>
		/// <param name="tab">Tab</param>
		/// <returns></returns>
		bool CanSave(IDocumentTab? tab);

		/// <summary>
		/// Saves the tab. See also <see cref="CanSave(IDocumentTab)"/>
		/// </summary>
		/// <param name="tab">Tab</param>
		void Save(IDocumentTab? tab);

		/// <summary>
		/// Returns the menu header content, eg. "_Save..."
		/// </summary>
		/// <param name="tab">Tab</param>
		/// <returns></returns>
		string GetMenuHeader(IDocumentTab? tab);
	}
}
