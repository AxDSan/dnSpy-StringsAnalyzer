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
using System.ComponentModel.Composition;

namespace dnSpy.Contracts.Settings.Dialog {
	/// <summary>
	/// Gets notified when the settings dialog box has closed. Use <see cref="ExportAppSettingsModifiedListenerAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IAppSettingsModifiedListener {
		/// <summary>
		/// Called when the settings have been updated
		/// </summary>
		/// <param name="appRefreshSettings">Stuff that must be refreshed</param>
		void OnSettingsModified(IAppRefreshSettings appRefreshSettings);
	}

	/// <summary>Metadata</summary>
	public interface IAppSettingsModifiedListenerMetadata {
		/// <summary>See <see cref="ExportAppSettingsModifiedListenerAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IAppSettingsModifiedListener"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportAppSettingsModifiedListenerAttribute : ExportAttribute, IAppSettingsModifiedListenerMetadata {
		/// <summary>Constructor</summary>
		public ExportAppSettingsModifiedListenerAttribute()
			: base(typeof(IAppSettingsModifiedListener)) => Order = double.MaxValue;

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
