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
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;

namespace dnSpy.Analyzer {
	[Export(typeof(IToolWindowContentProvider))]
	sealed class AnalyzerToolWindowContentProvider : IToolWindowContentProvider {
		readonly Lazy<IAnalyzerService> analyzerService;

		public AnalyzerToolWindowContent DocumentTreeViewWindowContent => analyzerToolWindowContent ??= new AnalyzerToolWindowContent(analyzerService);
		AnalyzerToolWindowContent? analyzerToolWindowContent;

		[ImportingConstructor]
		AnalyzerToolWindowContentProvider(Lazy<IAnalyzerService> analyzerService) => this.analyzerService = analyzerService;

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(AnalyzerToolWindowContent.THE_GUID, AnalyzerToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_ANALYZER, false); }
		}

		public ToolWindowContent? GetOrCreate(Guid guid) => guid == AnalyzerToolWindowContent.THE_GUID ? DocumentTreeViewWindowContent : null;
	}

	sealed class AnalyzerToolWindowContent : ToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("5827D693-A5DF-4D65-B1F8-ACF249508A96");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public override IInputElement? FocusedElement => null;
		public override FrameworkElement? ZoomElement => analyzerService.Value.TreeView.UIObject;
		public override Guid Guid => THE_GUID;
		public override string Title => dnSpy_Analyzer_Resources.AnalyzerWindowTitle;
		public override object? UIObject => analyzerService.Value.TreeView.UIObject;
		public bool CanFocus => true;

		readonly Lazy<IAnalyzerService> analyzerService;

		public AnalyzerToolWindowContent(Lazy<IAnalyzerService> analyzerService) => this.analyzerService = analyzerService;

		public override void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			if (visEvent == ToolWindowContentVisibilityEvent.Removed)
				analyzerService.Value.OnClose();
		}

		public void Focus() => analyzerService.Value.TreeView.Focus();
	}
}
