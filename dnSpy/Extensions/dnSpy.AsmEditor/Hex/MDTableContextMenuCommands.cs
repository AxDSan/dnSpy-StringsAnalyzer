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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.AsmEditor.Hex.Nodes;
using dnSpy.AsmEditor.Hex.PE;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.Utilities;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.Hex {
	[Export(typeof(IInitializeDataTemplate))]
	sealed class InitializeMDTableKeyboardShortcuts : IInitializeDataTemplate {
		readonly IDocumentTabService documentTabService;

		[ImportingConstructor]
		InitializeMDTableKeyboardShortcuts(IDocumentTabService documentTabService) => this.documentTabService = documentTabService;

		public void Initialize(DependencyObject d) {
			var lv = d as ListView;
			if (lv is null)
				return;
			if (!(lv.DataContext is MetadataTableVM))
				return;

			lv.InputBindings.Add(new KeyBinding(new CtxMenuMDTableCommandProxy(documentTabService, new SortMDTableCommand.TheMenuMDTableCommand()), Key.T, ModifierKeys.Shift | ModifierKeys.Control));
			lv.InputBindings.Add(new KeyBinding(new CtxMenuMDTableCommandProxy(documentTabService, new CopyAsTextMDTableCommand.TheMenuMDTableCommand()), Key.C, ModifierKeys.Shift | ModifierKeys.Control));
			lv.InputBindings.Add(new KeyBinding(new CtxMenuMDTableCommandProxy(documentTabService, new GoToRidMDTableCommand.TheMenuMDTableCommand()), Key.G, ModifierKeys.Control));
			lv.InputBindings.Add(new KeyBinding(new CtxMenuMDTableCommandProxy(documentTabService, new ShowInHexEditorMDTableCommand.TheMenuMDTableCommand(documentTabService)), Key.X, ModifierKeys.Control));
			Add(lv, ApplicationCommands.Copy, new CtxMenuMDTableCommandProxy(documentTabService, new CopyMDTableCommand.TheMenuMDTableCommand()));
			Add(lv, ApplicationCommands.Paste, new CtxMenuMDTableCommandProxy(documentTabService, new PasteMDTableCommand.TheMenuMDTableCommand()));
		}

		static void Add(UIElement elem, ICommand cmd, ICommand realCmd) => elem.CommandBindings.Add(new CommandBinding(cmd, (s, e) => realCmd.Execute(e.Parameter), (s, e) => e.CanExecute = realCmd.CanExecute(e.Parameter)));
	}

	sealed class MDTableContext {
		public ListView ListView { get; }
		public MetadataTableVM MetadataTableVM { get; }
		public MetadataTableNode Node { get; }
		public MetadataTableRecordVM[] Records { get; }
		public bool IsContextMenu { get; }

		public MDTableContext(ListView listView, MetadataTableVM mdVM, MetadataTableNode mdNode, bool isContextMenu) {
			ListView = listView;
			MetadataTableVM = mdVM;
			Node = mdNode;
			Records = listView.SelectedItems.Cast<MetadataTableRecordVM>().OrderBy(a => a.Span.Start).ToArray();
			IsContextMenu = isContextMenu;
		}

		public bool ContiguousRecords() {
			if (Records.Length <= 1)
				return true;
			for (int i = 1; i < Records.Length; i++) {
				if (Records[i - 1].Span.End != Records[i].Span.Start)
					return false;
			}
			return true;
		}
	}

	sealed class CtxMenuMDTableCommandProxy : ICommand {
		readonly IDocumentTabService documentTabService;
		readonly MenuItemBase<MDTableContext> cmd;

		public CtxMenuMDTableCommandProxy(IDocumentTabService documentTabService, MenuItemBase<MDTableContext> cmd) {
			this.documentTabService = documentTabService;
			this.cmd = cmd;
		}

		MDTableContext? CreateMDTableContext() {
			var tab = documentTabService.ActiveTab;
			if (tab is not null) {
				var listView = FindListView(tab);
				if (listView is not null && UIUtils.HasSelectedChildrenFocus(listView))
					return MenuMDTableCommand.ToMDTableContext(listView, false);
			}

			return null;
		}

		static ListView? FindListView(IDocumentTab tab) {
			var o = tab.UIContext.UIObject as DependencyObject;
			while (o is not null) {
				if (o is ListView lv && InitDataTemplateAP.GetInitialize(lv))
					return lv;
				var children = UIUtils.GetChildren(o).ToArray();
				if (children.Length != 1)
					return null;
				o = children[0];
			}

			return null;
		}

		event EventHandler? ICommand.CanExecuteChanged {
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		bool ICommand.CanExecute(object? parameter) {
			var ctx = CreateMDTableContext();
			return ctx is not null && cmd.IsVisible(ctx) && cmd.IsEnabled(ctx);
		}

		void ICommand.Execute(object? parameter) {
			var ctx = CreateMDTableContext();
			if (ctx is not null)
				cmd.Execute(ctx);
		}
	}

	abstract class CtxMenuMDTableCommand : MenuItemBase<MDTableContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected sealed override MDTableContext? CreateContext(IMenuItemContext context) => MenuMDTableCommand.ToMDTableContext(context.CreatorObject.Object, true);
	}

	abstract class MenuMDTableCommand : MenuItemBase<MDTableContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected sealed override MDTableContext? CreateContext(IMenuItemContext context) => ToMDTableContext(context.CreatorObject.Object, false);
		internal static MDTableContext? ToMDTableContext(object? obj, bool isContextMenu) => ToMDTableContext(obj as ListView, isContextMenu);

		internal static MDTableContext? ToMDTableContext(ListView? listView, bool isContextMenu) {
			if (listView is null)
				return null;
			var mdVM = listView.DataContext as MetadataTableVM;
			if (mdVM is null)
				return null;

			return new MDTableContext(listView, mdVM, (MetadataTableNode)mdVM.Owner!, isContextMenu);
		}
	}

	static class SortMDTableCommand {
		[ExportMenuItem(Header = "res:SortMetadataTableCommand", InputGestureText = "res:ShortCutKeyCtrlShiftT", Group = MenuConstants.GROUP_CTX_DOCVIEWER_HEX_MD, Order = 0)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:SortMetadataTableCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_MD, InputGestureText = "res:ShortCutKeyCtrlShiftT", Order = 0)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		static void ExecuteInternal(MDTableContext context) =>
			SortTable(context.MetadataTableVM, 1, context.MetadataTableVM.Rows);

		internal static void SortTable(MetadataTableVM mdTblVM, uint rid, uint count) {
			var buffer = mdTblVM.Buffer;
			int len = (int)count * mdTblVM.TableInfo.RowSize;
			var data = new byte[len];
			var startOffset = mdTblVM.Span.Start + (rid - 1) * (ulong)mdTblVM.TableInfo.RowSize;
			buffer.ReadBytes(startOffset, data);
			TableSorter.Sort(mdTblVM.TableInfo, data);
			HexBufferWriterHelper.Write(buffer, startOffset, data);
		}

		static bool IsEnabledInternal(MDTableContext context) => TableSorter.CanSort(context.MetadataTableVM.TableInfo);
	}

	static class SortSelectionMDTableCommand {
		[ExportMenuItem(Header = "res:SortSelectionCommand", Group = MenuConstants.GROUP_CTX_DOCVIEWER_HEX_MD, Order = 10)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:SortSelectionCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_MD, Order = 10)]
		sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		static void ExecuteInternal(MDTableContext context) {
			uint rid = context.Records[0].Token.Rid;
			uint count = (uint)context.Records.Length;
			SortMDTableCommand.SortTable(context.MetadataTableVM, rid, count);
		}

		static bool IsEnabledInternal(MDTableContext context) =>
			TableSorter.CanSort(context.MetadataTableVM.TableInfo) &&
			context.Records.Length > 1 &&
			context.ContiguousRecords();
	}

	static class GoToRidMDTableCommand {
		[ExportMenuItem(Header = "res:GoToRowIdentifierCommand", InputGestureText = "res:ShortCutKeyCtrlG", Group = MenuConstants.GROUP_CTX_DOCVIEWER_HEX_MD, Order = 20)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:GoToRowIdentifierCommand", InputGestureText = "res:ShortCutKeyCtrlG", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_MD, Order = 20)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
		}

		static void ExecuteInternal(MDTableContext context) {
			var recVM = Ask(dnSpy_AsmEditor_Resources.GoToRowIdentifier_Title, context);
			if (recVM is not null)
				UIUtils.ScrollSelectAndSetFocus(context.ListView, recVM);
		}

		static MetadataTableRecordVM? Ask(string title, MDTableContext context) => MsgBox.Instance.Ask(dnSpy_AsmEditor_Resources.GoToMetaDataTableRow_RID, null, title, s => {
			uint rid = SimpleTypeConverter.ParseUInt32(s, 1, context.MetadataTableVM.Rows, out var error);
			if (!string.IsNullOrEmpty(error))
				return null;
			return context.MetadataTableVM.Get((int)(rid - 1));
		}, s => {
			uint rid = SimpleTypeConverter.ParseUInt32(s, 1, context.MetadataTableVM.Rows, out var error);
			if (!string.IsNullOrEmpty(error))
				return error;
			if (rid == 0 || rid > context.MetadataTableVM.Rows)
				return string.Format(dnSpy_AsmEditor_Resources.GoToRowIdentifier_InvalidRowIdentifier, rid);
			return string.Empty;
		});
	}

	static class ShowInHexEditorMDTableCommand {
		[ExportMenuItem(Header = "res:ShowInHexEditorCommand", Icon = DsImagesAttribute.Binary, InputGestureText = "res:ShortCutKeyCtrlX", Group = MenuConstants.GROUP_CTX_DOCVIEWER_HEX_MD, Order = 30)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			readonly IDocumentTabService documentTabService;

			[ImportingConstructor]
			TheCtxMenuMDTableCommand(IDocumentTabService documentTabService) => this.documentTabService = documentTabService;

			public override void Execute(MDTableContext context) => ExecuteInternal(documentTabService, context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:ShowInHexEditorCommand", Icon = DsImagesAttribute.Binary, InputGestureText = "res:ShortCutKeyCtrlX", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_MD, Order = 30)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			readonly IDocumentTabService documentTabService;

			[ImportingConstructor]
			public TheMenuMDTableCommand(IDocumentTabService documentTabService) => this.documentTabService = documentTabService;

			public override void Execute(MDTableContext context) => ExecuteInternal(documentTabService, context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		static void ExecuteInternal(IDocumentTabService documentTabService, MDTableContext context) {
			var @ref = GetAddressReference(context);
			if (@ref is not null)
				documentTabService.FollowReference(@ref);
		}

		static bool IsEnabledInternal(MDTableContext context) => GetAddressReference(context) is not null;

		static AddressReference? GetAddressReference(MDTableContext context) {
			if (context.Records.Length == 0)
				return null;
			if (!context.ContiguousRecords())
				return null;

			var start = context.Records[0].Span.Start;
			var end = context.Records[context.Records.Length - 1].Span.End;
			return new AddressReference(context.MetadataTableVM.Buffer.Name, false, start.ToUInt64(), (end - start).ToUInt64());
		}
	}

	static class CopyAsTextMDTableCommand {
		[ExportMenuItem(Header = "res:CopyAsTextCommand2", InputGestureText = "res:ShortCutKeyCtrlShiftC", Group = MenuConstants.GROUP_CTX_DOCVIEWER_HEX_COPY, Order = 0)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CopyAsTextCommand2", InputGestureText = "res:ShortCutKeyCtrlShiftC", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_COPY, Order = 0)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		static void ExecuteInternal(MDTableContext context) {
			var output = new StringBuilderTextColorOutput();
			var output2 = TextColorWriterToDecompilerOutput.Create(output);
			context.Node.WriteHeader(output2);
			foreach (var rec in context.Records)
				context.Node.Write(output2, rec);
			var s = output.ToString();
			if (s.Length > 0) {
				try {
					Clipboard.SetText(s);
				}
				catch (ExternalException) { }
			}
		}

		static bool IsEnabledInternal(MDTableContext context) => context.Records.Length > 0;
	}

	static class CopyMDTableCommand {
		[ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DOCVIEWER_HEX_COPY, Order = 10)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_COPY, Order = 10)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		static void ExecuteInternal(MDTableContext context) {
			var buffer = context.MetadataTableVM.Buffer;
			ulong totalSize = (ulong)context.MetadataTableVM.TableInfo.RowSize * (ulong)context.Records.Length * 2;
			if (totalSize >= int.MaxValue) {
				MsgBox.Instance.Show(dnSpy_AsmEditor_Resources.TooManyBytesSelected);
				return;
			}
			var sb = new StringBuilder((int)totalSize);
			var recData = new byte[context.MetadataTableVM.TableInfo.RowSize];
			foreach (var rec in context.Records) {
				buffer.ReadBytes(rec.Span.Start, recData);
				foreach (var b in recData)
					sb.Append(b.ToString("X2"));
			}
			var s = sb.ToString();
			if (s.Length > 0) {
				try {
					Clipboard.SetText(s);
				}
				catch (ExternalException) { }
			}
		}

		static bool IsEnabledInternal(MDTableContext context) => context.Records.Length > 0;
	}

	static class PasteMDTableCommand {
		[ExportMenuItem(Header = "res:PasteCommand", Icon = DsImagesAttribute.Paste, InputGestureText = "res:ShortCutKeyCtrlV", Group = MenuConstants.GROUP_CTX_DOCVIEWER_HEX_COPY, Order = 20)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
			public override string? GetHeader(MDTableContext context) => GetHeaderInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:PasteCommand", Icon = DsImagesAttribute.Paste, InputGestureText = "res:ShortCutKeyCtrlV", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_COPY, Order = 20)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
			public override string? GetHeader(MDTableContext context) => GetHeaderInternal(context);
		}

		static void ExecuteInternal(MDTableContext context) {
			var data = GetPasteData(context);
			if (data is null)
				return;

			var buffer = context.MetadataTableVM.Buffer;
			int recs = data.Length / context.MetadataTableVM.TableInfo.RowSize;
			HexBufferWriterHelper.Write(buffer, context.Records[0].Span.Start, data);
		}

		static bool IsEnabledInternal(MDTableContext context) => GetPasteData(context) is not null;

		static byte[]? GetPasteData(MDTableContext context) {
			if (context.Records.Length == 0)
				return null;

			var data = ClipboardUtils.GetData(canBeEmpty: false);
			if (data is null || data.Length == 0)
				return null;

			if (data.Length % context.MetadataTableVM.TableInfo.RowSize != 0)
				return null;

			int recs = data.Length / context.MetadataTableVM.TableInfo.RowSize;
			if ((uint)context.Records[0].Index + (uint)recs > context.MetadataTableVM.Rows)
				return null;

			return data;
		}

		static string? GetHeaderInternal(MDTableContext context) {
			var data = GetPasteData(context);
			if (data is null)
				return null;
			int recs = data.Length / context.MetadataTableVM.TableInfo.RowSize;
			if (recs <= 1)
				return null;
			return string.Format(dnSpy_AsmEditor_Resources.PasteRecordsCommand, recs, context.Records[0].Span.Start.ToUInt64(), context.Records[0].Token.Rid);
		}
	}
}
