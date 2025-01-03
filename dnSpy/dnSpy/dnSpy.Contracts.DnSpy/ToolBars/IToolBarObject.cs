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
using System.Windows;
using System.Windows.Controls;

namespace dnSpy.Contracts.ToolBars {
	/// <summary>
	/// A toolbar object
	/// </summary>
	public interface IToolBarObject : IToolBarItem {
		/// <summary>
		/// Gets the UI object to place in the <see cref="ToolBar"/>
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="commandTarget">Command target for toolbar items, eg. the owner window, or null</param>
		/// <returns></returns>
		object GetUIObject(IToolBarItemContext context, IInputElement? commandTarget);
	}

	/// <summary>Metadata</summary>
	public interface IToolBarObjectMetadata : IToolBarItemMetadata {
	}

	/// <summary>
	/// Exports a toolbar object (<see cref="IToolBarObject"/>)
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportToolBarObjectAttribute : ExportToolBarItemAttribute, IToolBarObjectMetadata {
		/// <summary>Constructor</summary>
		public ExportToolBarObjectAttribute()
			: base(typeof(IToolBarObject)) {
		}
	}
}
