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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.Modules {
	[Export(typeof(IToolWindowContentProvider))]
	sealed class ModulesToolWindowContentProvider : IToolWindowContentProvider {
		readonly Lazy<IModulesContent> modulesContent;

		public ModulesToolWindowContent ModulesToolWindowContent => modulesToolWindowContent ??= new ModulesToolWindowContent(modulesContent);
		ModulesToolWindowContent? modulesToolWindowContent;

		[ImportingConstructor]
		ModulesToolWindowContentProvider(Lazy<IModulesContent> modulesContent) => this.modulesContent = modulesContent;

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(ModulesToolWindowContent.THE_GUID, ModulesToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_MODULES, false); }
		}

		public ToolWindowContent? GetOrCreate(Guid guid) => guid == ModulesToolWindowContent.THE_GUID ? ModulesToolWindowContent : null;
	}

	sealed class ModulesToolWindowContent : ToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("8C95EB2E-25F4-4D2F-A00D-A303754990DF");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public override IInputElement? FocusedElement => modulesContent.Value.FocusedElement;
		public override FrameworkElement? ZoomElement => modulesContent.Value.ZoomElement;
		public override Guid Guid => THE_GUID;
		public override string Title => dnSpy_Debugger_Resources.Window_Modules;
		public override object? UIObject => modulesContent.Value.UIObject;
		public bool CanFocus => true;

		readonly Lazy<IModulesContent> modulesContent;

		public ModulesToolWindowContent(Lazy<IModulesContent> modulesContent) => this.modulesContent = modulesContent;

		public override void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				modulesContent.Value.OnShow();
				break;
			case ToolWindowContentVisibilityEvent.Removed:
				modulesContent.Value.OnClose();
				break;
			case ToolWindowContentVisibilityEvent.Visible:
				modulesContent.Value.OnVisible();
				break;
			case ToolWindowContentVisibilityEvent.Hidden:
				modulesContent.Value.OnHidden();
				break;
			}
		}

		public void Focus() => modulesContent.Value.Focus();
	}
}
