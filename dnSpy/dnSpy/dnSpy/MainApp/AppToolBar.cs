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
using System.Windows.Controls;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.ToolBars;
using dnSpy.Controls;

namespace dnSpy.MainApp {
	[Export]
	sealed class AppToolBar : IStackedContentChild {
		public object? UIObject => toolBar;
		readonly ToolBar toolBar;

		readonly IToolBarService toolBarService;

		[ImportingConstructor]
		public AppToolBar(IToolBarService toolBarService) {
			this.toolBarService = toolBarService;
			toolBar = new ToolBar();
		}

		internal void Initialize(MetroWindow window) =>
			toolBarService.InitializeToolBar(toolBar, new Guid(ToolBarConstants.APP_TB_GUID), window);
	}
}
