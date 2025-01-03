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

using System.ComponentModel;
using System.Windows;
using dnSpy.Contracts.Controls;

namespace dnSpy.Contracts.Tabs {
	/// <summary>
	/// Tab content
	/// </summary>
	public interface ITabContent {
		/// <summary>
		/// Title. If this property can change, you must implement <see cref="INotifyPropertyChanged"/>
		/// </summary>
		string? Title { get; }

		/// <summary>
		/// ToolTip or null. If this property can change, you must implement <see cref="INotifyPropertyChanged"/>
		/// </summary>
		object? ToolTip { get; }

		/// <summary>
		/// The UI object. If this property can change, you must implement <see cref="INotifyPropertyChanged"/>
		/// </summary>
		object? UIObject { get; }

		/// <summary>
		/// Gets the element that should get focus when the tab is selected or null to use <see cref="UIObject"/>.
		/// Implement <see cref="IFocusable"/> to set focus yourself.
		/// </summary>
		IInputElement? FocusedElement { get; }

		/// <summary>
		/// Called when the visibility changes
		/// </summary>
		/// <param name="visEvent">Event</param>
		void OnVisibilityChanged(TabContentVisibilityEvent visEvent);
	}
}
