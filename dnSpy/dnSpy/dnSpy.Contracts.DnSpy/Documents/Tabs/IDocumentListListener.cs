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

namespace dnSpy.Contracts.Documents.Tabs {
	/// <summary>
	/// Can cancel loading document lists. Use <see cref="ExportDocumentListListenerAttribute"/> to export
	/// an instance.
	/// </summary>
	public interface IDocumentListListener {
		/// <summary>
		/// true if we can load a new document list
		/// </summary>
		bool CanLoad { get; }

		/// <summary>
		/// true if we can reload the current document list
		/// </summary>
		bool CanReload { get; }

		/// <summary>
		/// Called before a new document list is loaded
		/// </summary>
		/// <param name="isReload">true if it's reload-list, false if it's load-list</param>
		void BeforeLoad(bool isReload);

		/// <summary>
		/// Called after a new document list has been loaded
		/// </summary>
		/// <param name="isReload">true if it's reload-list, false if it's load-list</param>
		void AfterLoad(bool isReload);

		/// <summary>
		/// Returns true if the list can be loaded. It's called before <see cref="BeforeLoad(bool)"/>
		/// and can be used to show a message box to the user. If false is returned, the list isn't
		/// loaded.
		/// </summary>
		/// <param name="isReload">true if it's reload-list, false if it's load-list</param>
		/// <returns></returns>
		bool CheckCanLoad(bool isReload);
	}

	/// <summary>Metadata</summary>
	public interface IDocumentListListenerMetadata {
		/// <summary>See <see cref="ExportDocumentListListenerAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IDocumentListListener"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDocumentListListenerAttribute : ExportAttribute, IDocumentListListenerMetadata {
		/// <summary>Constructor</summary>
		public ExportDocumentListListenerAttribute()
			: base(typeof(IDocumentListListener)) {
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
