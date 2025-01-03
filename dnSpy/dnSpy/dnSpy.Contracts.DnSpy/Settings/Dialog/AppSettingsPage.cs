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
using System.ComponentModel;
using System.Windows;
using dnSpy.Contracts.Images;

namespace dnSpy.Contracts.Settings.Dialog {
	/// <summary>
	/// Content shown in the options dialog box
	/// </summary>
	public abstract class AppSettingsPage : INotifyPropertyChanged {
		/// <summary>
		/// Parent <see cref="System.Guid"/> or <see cref="System.Guid.Empty"/> if the root element is the parent
		/// </summary>
		public virtual Guid ParentGuid => Guid.Empty;

		/// <summary>
		/// Gets the <see cref="System.Guid"/>
		/// </summary>
		public abstract Guid Guid { get; }

		/// <summary>
		/// Gets the order, eg. <see cref="AppSettingsConstants.ORDER_DECOMPILER"/>
		/// </summary>
		public abstract double Order { get; }

		/// <summary>
		/// Gets the title shown in the UI
		/// </summary>
		public abstract string Title { get; }

		/// <summary>
		/// Gets the icon shown in the UI (eg. <see cref="DsImages.Assembly"/>) or <see cref="ImageReference.None"/>
		/// </summary>
		public virtual ImageReference Icon => ImageReference.None;

		/// <summary>
		/// Gets the UI object. This property is only loaded if the user clicks on the page
		/// title in the dialog box.
		/// </summary>
		public abstract object? UIObject { get; }

		/// <summary>
		/// Called when all settings should be saved
		/// </summary>
		public abstract void OnApply();

		/// <summary>
		/// Called when the dialog box has been closed
		/// </summary>
		public virtual void OnClosed() {
		}

		/// <summary>
		/// Returns the UI object that contains strings. This can be a <see cref="UIElement"/>,
		/// an object with a <see cref="DataTemplate"/> or the <see cref="Type"/> of an object
		/// with a <see cref="DataTemplate"/>. The caller will find all strings in it.
		/// 
		/// By default, it returns <see cref="UIObject"/>. Return null if <see cref="UIObject"/>
		/// takes too long to create and override <see cref="GetSearchStrings"/> instead.
		/// 
		/// See also <see cref="GetSearchStrings"/>.
		/// </summary>
		/// <returns></returns>
		public virtual object? GetStringsObject() => UIObject;

		/// <summary>
		/// Returns an array of strings shown in the UI that can be searched. This method
		/// isn't needed if <see cref="GetStringsObject"/> returns a non-null value (default
		/// behavior).
		/// </summary>
		/// <returns></returns>
		public virtual string[]? GetSearchStrings() => null;

		/// <summary>
		/// Raised when a property is changed
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Raises <see cref="PropertyChanged"/>
		/// </summary>
		/// <param name="propName">Name of property that changed</param>
		protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		/// <summary>
		/// Constructor
		/// </summary>
		protected AppSettingsPage() {
		}
	}

	/// <summary>
	/// Content shown in the options dialog box
	/// </summary>
	public interface IAppSettingsPage2 {
		/// <summary>
		/// Called when all settings should be saved. <see cref="AppSettingsPage.OnApply"/> is
		/// never called.
		/// </summary>
		/// <param name="appRefreshSettings">Add anything that needs to be refreshed, eg. re-decompile code</param>
		void OnApply(IAppRefreshSettings appRefreshSettings);
	}
}
