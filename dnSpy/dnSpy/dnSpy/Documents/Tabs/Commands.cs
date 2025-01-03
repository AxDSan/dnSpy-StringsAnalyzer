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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.ToolBars;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Utilities;
using dnSpy.Documents.Tabs.Dialogs;
using dnSpy.Documents.TreeView;
using dnSpy.Properties;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.Win32;

namespace dnSpy.Documents.Tabs {
	[ExportAutoLoaded]
	sealed class OpenFileInit : IAutoLoaded {
		readonly IDocumentTreeView documentTreeView;
		readonly IAppWindow appWindow;
		readonly AssemblyExplorerMostRecentlyUsedList mruList;

		[ImportingConstructor]
		OpenFileInit(IDocumentTreeView documentTreeView, IAppWindow appWindow, AssemblyExplorerMostRecentlyUsedList mruList) {
			this.documentTreeView = documentTreeView;
			this.appWindow = appWindow;
			this.mruList = mruList;
			appWindow.MainWindowCommands.Add(ApplicationCommands.Open, (s, e) => { Open(); e.Handled = true; }, (s, e) => e.CanExecute = true);
		}

		static readonly string DotNetAssemblyOrModuleFilter = $"{dnSpy_Resources.DotNetExes} (*.exe, *.dll, *.netmodule, *.winmd)|*.exe;*.dll;*.netmodule;*.winmd|{dnSpy_Resources.AllFiles} (*.*)|*.*";

		void Open() {
			var openDlg = new OpenFileDialog {
				Filter = DotNetAssemblyOrModuleFilter,
				RestoreDirectory = true,
				Multiselect = true,
			};
			if (openDlg.ShowDialog() != true)
				return;
			OpenDocumentsHelper.OpenDocuments(documentTreeView, appWindow.MainWindow, mruList, openDlg.FileNames);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Header = "res:OpenCommand", InputGestureText = "res:OpenKey", Icon = DsImagesAttribute.OpenFolder, Group = MenuConstants.GROUP_APP_MENU_FILE_OPEN, Order = 0)]
	sealed class MenuFileOpenCommand : MenuItemCommand {
		public MenuFileOpenCommand()
			: base(ApplicationCommands.Open) {
		}
	}

	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, Icon = DsImagesAttribute.OpenFolder, Group = ToolBarConstants.GROUP_APP_TB_MAIN_OPEN, Order = 0)]
	sealed class ToolbarFileOpenCommand : ToolBarButtonCommand {
		public ToolbarFileOpenCommand()
			: base(ApplicationCommands.Open) {
		}
		public override string? GetToolTip(IToolBarItemContext context) => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.OpenToolBarToolTip, dnSpy_Resources.ShortCutKeyCtrlO);
	}

	[ExportAutoLoaded]
	sealed class OpenFromGacCommandLoader : IAutoLoaded {
		public static readonly RoutedCommand OpenFromGac = new RoutedCommand("OpenFromGac", typeof(OpenFromGacCommandLoader));

		readonly IOpenFromGAC openFromGAC;

		[ImportingConstructor]
		OpenFromGacCommandLoader(IOpenFromGAC openFromGAC, IWpfCommandService wpfCommandService) {
			this.openFromGAC = openFromGAC;

			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);
			cmds.Add(OpenFromGac, (s, e) => Execute(), (s, e) => e.CanExecute = true, ModifierKeys.Control | ModifierKeys.Shift, Key.O);
		}

		void Execute() => openFromGAC.OpenAssemblies(true);
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Header = "res:OpenGACCommand", InputGestureText = "res:ShortCutKeyCtrlShiftO", Icon = DsImagesAttribute.Library, Group = MenuConstants.GROUP_APP_MENU_FILE_OPEN, Order = 10)]
	sealed class OpenFromGacCommand : MenuItemCommand {
		OpenFromGacCommand()
			: base(OpenFromGacCommandLoader.OpenFromGac) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Header = "res:OpenListCommand", Group = MenuConstants.GROUP_APP_MENU_FILE_OPEN, Order = 20)]
	sealed class OpenListCommand : MenuItemBase {
		readonly IAppWindow appWindow;
		readonly IDocumentListLoader documentListLoader;
		readonly DocumentListService documentListService;
		readonly IMessageBoxService messageBoxService;
		readonly IDsDocumentService documentService;
		readonly IClassificationFormatMap classificationFormatMap;
		readonly ITextElementProvider textElementProvider;

		[ImportingConstructor]
		OpenListCommand(IAppWindow appWindow, IDocumentListLoader documentListLoader, DocumentListService documentListService, IMessageBoxService messageBoxService, IDsDocumentService documentService, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider) {
			this.appWindow = appWindow;
			this.documentListLoader = documentListLoader;
			this.documentListService = documentListService;
			this.messageBoxService = messageBoxService;
			this.documentService = documentService;
			classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			this.textElementProvider = textElementProvider;
		}

		public override bool IsEnabled(IMenuItemContext context) => documentListLoader.CanLoad;

		public override void Execute(IMenuItemContext context) {
			if (!documentListLoader.CanLoad)
				return;

			documentListLoader.SaveCurrentDocumentsToList();

			var win = new OpenDocumentListDlg();
			const bool syntaxHighlight = true;
			var vm = new OpenDocumentListVM(syntaxHighlight, classificationFormatMap, textElementProvider, documentListService, labelMsg => messageBoxService.Ask<string>(labelMsg, ownerWindow: win, verifier: s => string.IsNullOrEmpty(s) ? dnSpy_Resources.OpenList_MissingName : string.Empty));
			win.DataContext = vm;
			win.Owner = appWindow.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var flvm = win.SelectedItems.FirstOrDefault();
			var oldSelected = documentListService.SelectedDocumentList;
			if (flvm is not null) {
				documentListLoader.SaveCurrentDocumentsToList();
				documentListService.Add(flvm.DocumentList);
				documentListService.SelectedDocumentList = flvm.DocumentList;
			}

			vm.Save();

			if (flvm is null)
				return;
			var documentList = flvm.DocumentList;
			if (documentList == oldSelected)
				return;

			documentListLoader.Load(documentList, new DsDocumentLoader(documentService, appWindow.MainWindow, null));
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Guid = Guid, Header = "res:RecentFilesCommand", Group = MenuConstants.GROUP_APP_MENU_FILE_OPEN, Order = 30)]
	sealed class RecentFilesCommandItem : MenuItemBase {
		public const string Guid = "2C57024A-6226-486B-8DD7-202333BC848A";
		readonly AssemblyExplorerMostRecentlyUsedList mruList;
		[ImportingConstructor]
		RecentFilesCommandItem(AssemblyExplorerMostRecentlyUsedList mruList) => this.mruList = mruList;
		public override void Execute(IMenuItemContext context) => Debug.Fail("Shouldn't execute");
		public override bool IsEnabled(IMenuItemContext context) => mruList.RecentFiles.Length > 0;
	}

	[ExportMenuItem(OwnerGuid = RecentFilesCommandItem.Guid, Group = "0,A8109840-1340-42BB-85C5-D753FD41AA86", Order = 0)]
	sealed class RecentFilesCommand : MenuItemBase, IMenuItemProvider {
		const int MaxFilenameLength = 100;
		readonly IDocumentTreeView documentTreeView;
		readonly AssemblyExplorerMostRecentlyUsedList mruList;

		[ImportingConstructor]
		RecentFilesCommand(IDocumentTreeView documentTreeView, AssemblyExplorerMostRecentlyUsedList mruList) {
			this.documentTreeView = documentTreeView;
			this.mruList = mruList;
		}

		public override void Execute(IMenuItemContext context) { }

		sealed class MyMenuItem : MenuItemBase {
			readonly Action<IMenuItemContext> action;
			public MyMenuItem(Action<IMenuItemContext> action) => this.action = action;
			public override void Execute(IMenuItemContext context) => action(context);
		}

		static string GetHeader(int i, string filename) {
			string s;
			if (i == 10)
				s = "1_0";
			else if (i > 10)
				s = i.ToString();
			else
				s = $"_{i}";
			return $"{s} {UIUtilities.EscapeMenuItemHeader(GetShortFilename(filename))}";
		}

		static string GetShortFilename(string filename) {
			if (filename.Length > MaxFilenameLength)
				filename = "[...]" + filename.Substring(filename.Length - MaxFilenameLength);
			return filename;
		}

		public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
			var files = mruList.RecentFiles;
			for (int i = 0; i < files.Length; i++) {
				var filename = files[i];
				var attr = new ExportMenuItemAttribute { Header = GetHeader(i + 1, filename) };
				var item = new MyMenuItem(ctx => OpenFile(filename));
				yield return new CreatedMenuItem(attr, item);
			}
		}

		void OpenFile(string filename) {
			var document = documentTreeView.DocumentService.TryGetOrCreate(DsDocumentInfo.CreateDocument(filename));
			if (document is null)
				return;
			mruList.Add(document.Filename);
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				var node = documentTreeView.FindNode(document);
				if (node is not null)
					documentTreeView.TreeView.SelectItems(new[] { node });
			}));
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Header = "res:ReloadAsmsCommand", Group = MenuConstants.GROUP_APP_MENU_FILE_OPEN, Order = 40)]
	sealed class ReloadCommand : MenuItemBase {
		readonly IDocumentListLoader documentListLoader;

		[ImportingConstructor]
		ReloadCommand(IDocumentListLoader documentListLoader) => this.documentListLoader = documentListLoader;

		public override bool IsEnabled(IMenuItemContext context) => documentListLoader.CanReload;
		public override void Execute(IMenuItemContext context) => documentListLoader.Reload();
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Header = "res:CloseAllCommand", Icon = DsImagesAttribute.CloseAll, Group = MenuConstants.GROUP_APP_MENU_FILE_OPEN, Order = 50)]
	sealed class CloseAllDocumentsCommand : MenuItemBase {
		readonly IDocumentListLoader documentListLoader;

		[ImportingConstructor]
		CloseAllDocumentsCommand(IDocumentListLoader documentListLoader) => this.documentListLoader = documentListLoader;

		public override bool IsEnabled(IMenuItemContext context) => documentListLoader.CanCloseAll;
		public override void Execute(IMenuItemContext context) => documentListLoader.CloseAll();
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Header = "res:SortAsmsCommand", Icon = DsImagesAttribute.SortAscending, Group = MenuConstants.GROUP_APP_MENU_FILE_OPEN, Order = 100)]
	sealed class SortAssembliesCommand : MenuItemBase {
		readonly IDocumentTreeView documentTreeView;

		[ImportingConstructor]
		SortAssembliesCommand(IDocumentTreeView documentTreeView) => this.documentTreeView = documentTreeView;

		public override bool IsEnabled(IMenuItemContext context) => documentTreeView.CanSortTopNodes;
		public override void Execute(IMenuItemContext context) => documentTreeView.SortTopNodes();
	}

	[ExportMenuItem(Header = "res:SortAsmsCommand", Icon = DsImagesAttribute.SortAscending, Group = MenuConstants.GROUP_CTX_DOCUMENTS_OTHER, Order = 40)]
	sealed class SortAssembliesCtxMenuCommand : MenuItemBase {
		readonly IDocumentTreeView documentTreeView;

		[ImportingConstructor]
		SortAssembliesCtxMenuCommand(IDocumentTreeView documentTreeView) => this.documentTreeView = documentTreeView;

		public override bool IsVisible(IMenuItemContext context) => context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID);
		public override bool IsEnabled(IMenuItemContext context) => documentTreeView.CanSortTopNodes;
		public override void Execute(IMenuItemContext context) => documentTreeView.SortTopNodes();
	}

	[ExportAutoLoaded]
	sealed class ShowCodeEditorCommandLoader : IAutoLoaded {
		public static readonly RoutedCommand ShowCodeEditorRoutedCommand = new RoutedCommand("ShowCodeEditorRoutedCommand", typeof(ShowCodeEditorCommandLoader));

		[ImportingConstructor]
		ShowCodeEditorCommandLoader(IWpfCommandService wpfCommandService, IDocumentTabService documentTabService) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);
			cmds.Add(ShowCodeEditorRoutedCommand,
				(s, e) => documentTabService.ActiveTab?.TrySetFocus(),
				(s, e) => e.CanExecute = documentTabService.ActiveTab is not null,
				ModifierKeys.Control | ModifierKeys.Alt, Key.D0,
				ModifierKeys.Control | ModifierKeys.Alt, Key.NumPad0,
				ModifierKeys.None, Key.F7);
			cmds.Add(ShowCodeEditorRoutedCommand, ModifierKeys.None, Key.Escape);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "res:ShowCodeCommand", InputGestureText = "res:ShowCodeKey", Icon = DsImagesAttribute.MarkupTag, Group = MenuConstants.GROUP_APP_MENU_VIEW_WINDOWS, Order = 0)]
	sealed class ShowCodeEditorCommand : MenuItemCommand {
		ShowCodeEditorCommand()
			: base(ShowCodeEditorCommandLoader.ShowCodeEditorRoutedCommand) {
		}
	}

	[ExportMenuItem(Header = "res:OpenContainingFolderCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_OTHER, Order = 30)]
	sealed class OpenContainingFolderCtxMenuCommand : MenuItemBase {
		public override bool IsVisible(IMenuItemContext context) => GetFilename(context) is not null;

		static string? GetFilename(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID))
				return null;
			var nodes = context.Find<TreeNodeData[]>();
			if (nodes?.Length != 1)
				return null;
			var documentNode = nodes[0] as DsDocumentNode;
			var filename = documentNode?.Document?.Filename;
			if (!File.Exists(filename))
				return null;
			return filename;
		}

		public override void Execute(IMenuItemContext context) {
			// Known problem: explorer can't show files in the .NET 2.0 GAC.
			var filename = GetFilename(context);
			if (filename is null)
				return;
			var args = $"/select,{filename}";
			try {
				Process.Start(new ProcessStartInfo("explorer.exe", args) { UseShellExecute = false });
			}
			catch (IOException) {
			}
			catch (Win32Exception) {
			}
		}
	}
}
