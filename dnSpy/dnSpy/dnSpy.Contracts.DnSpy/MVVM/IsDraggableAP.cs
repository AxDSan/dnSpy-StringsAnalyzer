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

namespace dnSpy.Contracts.MVVM {
	/// <summary>
	/// Is-draggable attached property
	/// </summary>
	public sealed class IsDraggableAP : DependencyObject {
		/// <summary>
		/// Is draggable property
		/// </summary>
		public static readonly DependencyProperty IsDraggableProperty = DependencyProperty.RegisterAttached(
			"IsDraggable", typeof(bool), typeof(IsDraggableAP), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

		/// <summary>
		/// Writes a new is-draggable value
		/// </summary>
		/// <param name="element">Target</param>
		/// <param name="value">New value</param>
		public static void SetIsDraggable(FrameworkElement element, bool value) => element.SetValue(IsDraggableProperty, value);

		/// <summary>
		/// Gets the is-draggable value
		/// </summary>
		/// <param name="element">Element</param>
		/// <returns></returns>
		public static bool GetIsDraggable(FrameworkElement? element) => element is not null && (bool)element.GetValue(IsDraggableProperty);
	}
}
