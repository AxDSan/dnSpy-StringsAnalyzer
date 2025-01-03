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

namespace dnSpy.Contracts.ToolWindows {
	/// <summary>
	/// Tab selected event args
	/// </summary>
	public sealed class ToolWindowSelectedEventArgs : EventArgs {
		/// <summary>
		/// Tab group
		/// </summary>
		public IToolWindowGroup TabGroup { get; }

		/// <summary>
		/// Selected tab content or null
		/// </summary>
		public ToolWindowContent? Selected { get; }

		/// <summary>
		/// Unselected tab content or null
		/// </summary>
		public ToolWindowContent? Unselected { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="tabGroup">Tab group</param>
		/// <param name="selected">Selected content or null</param>
		/// <param name="unselected">Unselected content or null</param>
		public ToolWindowSelectedEventArgs(IToolWindowGroup tabGroup, ToolWindowContent? selected, ToolWindowContent? unselected) {
			TabGroup = tabGroup;
			Selected = selected;
			Unselected = unselected;
		}
	}
}
