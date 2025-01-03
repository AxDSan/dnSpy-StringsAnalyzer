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
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.DnlibDialogs;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.Field {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CommandLoader(IWpfCommandService wpfCommandService, IDocumentTabService documentTabService, DeleteFieldDefCommand.EditMenuCommand removeCmd, DeleteFieldDefCommand.CodeCommand removeCmd2, FieldDefSettingsCommand.EditMenuCommand settingsCmd, FieldDefSettingsCommand.CodeCommand settingsCmd2) {
			wpfCommandService.AddRemoveCommand(removeCmd);
			wpfCommandService.AddRemoveCommand(removeCmd2, documentTabService);
			wpfCommandService.AddSettingsCommand(documentTabService, settingsCmd, settingsCmd2);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteFieldDefCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:DeleteFieldCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_DELETE, Order = 40)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => DeleteFieldDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeleteFieldDefCommand.Execute(undoCommandService, context.Nodes);
			public override string? GetHeader(AsmEditorContext context) => DeleteFieldDefCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:DeleteFieldCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 40)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => DeleteFieldDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeleteFieldDefCommand.Execute(undoCommandService, context.Nodes);
			public override string? GetHeader(AsmEditorContext context) => DeleteFieldDefCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:DeleteFieldCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_DELETE, Order = 40)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) => this.undoCommandService = undoCommandService;

			public override bool IsEnabled(CodeContext context) => context.IsDefinition && DeleteFieldDefCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => DeleteFieldDefCommand.Execute(undoCommandService, context.Nodes);
			public override string? GetHeader(CodeContext context) => DeleteFieldDefCommand.GetHeader(context.Nodes);
		}

		static string GetHeader(DocumentTreeNodeData[] nodes) {
			if (nodes.Length == 1)
				return string.Format(dnSpy_AsmEditor_Resources.DeleteX, UIUtilities.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format(dnSpy_AsmEditor_Resources.DeleteFieldsCommand, nodes.Length);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => nodes.Length > 0 && nodes.All(n => n is FieldNode);

		static void Execute(Lazy<IUndoCommandService> undoCommandService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			if (!Method.DeleteMethodDefCommand.AskDeleteDef(dnSpy_AsmEditor_Resources.AskDeleteField))
				return;

			var fieldNodes = nodes.Cast<FieldNode>().ToArray();
			undoCommandService.Value.Add(new DeleteFieldDefCommand(fieldNodes));
		}

		struct DeleteModelNodes {
			ModelInfo[]? infos;

			readonly struct ModelInfo {
				public readonly TypeDef OwnerType;
				public readonly int FieldIndex;

				public ModelInfo(FieldDef field) {
					OwnerType = field.DeclaringType;
					FieldIndex = OwnerType.Fields.IndexOf(field);
					Debug.Assert(FieldIndex >= 0);
				}
			}

			public void Delete(FieldNode[] nodes) {
				Debug2.Assert(infos is null);
				if (infos is not null)
					throw new InvalidOperationException();

				infos = new ModelInfo[nodes.Length];

				for (int i = 0; i < infos.Length; i++) {
					var node = nodes[i];

					var info = new ModelInfo(node.FieldDef);
					infos[i] = info;
					info.OwnerType.Fields.RemoveAt(info.FieldIndex);
				}
			}

			public void Restore(FieldNode[] nodes) {
				Debug2.Assert(infos is not null);
				if (infos is null)
					throw new InvalidOperationException();
				Debug.Assert(infos.Length == nodes.Length);
				if (infos.Length != nodes.Length)
					throw new InvalidOperationException();

				for (int i = infos.Length - 1; i >= 0; i--) {
					var node = nodes[i];
					ref readonly var info = ref infos[i];
					info.OwnerType.Fields.Insert(info.FieldIndex, node.FieldDef);
				}

				infos = null;
			}
		}

		DeletableNodes<FieldNode> nodes;
		DeleteModelNodes modelNodes;

		DeleteFieldDefCommand(FieldNode[] fieldNodes) => nodes = new DeletableNodes<FieldNode>(fieldNodes);

		public string Description => dnSpy_AsmEditor_Resources.DeleteFieldCommand;

		public void Execute() {
			nodes.Delete();
			modelNodes.Delete(nodes.Nodes);
		}

		public void Undo() {
			modelNodes.Restore(nodes.Nodes);
			nodes.Restore();
		}

		public IEnumerable<object> ModifiedObjects => nodes.Nodes;
	}

	[DebuggerDisplay("{Description}")]
	sealed class CreateFieldDefCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:CreateFieldCommand", Icon = DsImagesAttribute.NewField, Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 70)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateFieldDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateFieldDefCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateFieldCommand", Icon = DsImagesAttribute.NewField, Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 70)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateFieldDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateFieldDefCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(Header = "res:CreateFieldCommand", Icon = DsImagesAttribute.NewField, Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_NEW, Order = 70)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsEnabled(CodeContext context) =>
				context.IsDefinition &&
				context.Nodes.Length == 1 &&
				context.Nodes[0] is TypeNode;

			public override void Execute(CodeContext context) => CreateFieldDefCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes.Length == 1 &&
			(nodes[0] is TypeNode || (nodes[0].TreeNode.Parent is not null && nodes[0].TreeNode.Parent!.Data is TypeNode));

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var ownerNode = nodes[0];
			if (!(ownerNode is TypeNode))
				ownerNode = (DocumentTreeNodeData)ownerNode.TreeNode.Parent!.Data;
			var typeNode = ownerNode as TypeNode;
			Debug2.Assert(typeNode is not null);
			if (typeNode is null)
				throw new InvalidOperationException();

			var module = typeNode.GetModule();
			Debug2.Assert(module is not null);
			if (module is null)
				throw new InvalidOperationException();

			FieldDefOptions options;
			var type = typeNode.TypeDef;
			if (type.IsEnum) {
				var ts = type.GetEnumUnderlyingType();
				if (ts is not null) {
					options = FieldDefOptions.Create("MyField", new FieldSig(new ValueTypeSig(typeNode.TypeDef)));
					options.Constant = module.UpdateRowId(new ConstantUser(ModelUtils.GetDefaultValue(ts), ts.RemovePinnedAndModifiers().GetElementType()));
					options.Attributes |= FieldAttributes.Literal | FieldAttributes.Static | FieldAttributes.HasDefault;
				}
				else {
					options = FieldDefOptions.Create("value__", new FieldSig(module.CorLibTypes.Int32));
					options.Attributes |= FieldAttributes.SpecialName | FieldAttributes.RTSpecialName;
				}
			}
			else if (type.IsAbstract && type.IsSealed) {
				options = FieldDefOptions.Create("MyField", new FieldSig(module.CorLibTypes.Int32));
				options.Attributes |= FieldAttributes.Static;
			}
			else
				options = FieldDefOptions.Create("MyField", new FieldSig(module.CorLibTypes.Int32));

			var data = new FieldOptionsVM(options, module, appService.DecompilerService, type);
			var win = new FieldOptionsDlg();
			win.Title = dnSpy_AsmEditor_Resources.CreateFieldCommand2;
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreateFieldDefCommand(typeNode, data.CreateFieldDefOptions());
			undoCommandService.Value.Add(cmd);
			appService.DocumentTabService.FollowReference(cmd.fieldNode);
		}

		readonly TypeNode ownerNode;
		readonly FieldNode fieldNode;

		CreateFieldDefCommand(TypeNode ownerNode, FieldDefOptions options) {
			this.ownerNode = ownerNode;
			fieldNode = ownerNode.Create(options.CreateFieldDef(ownerNode.TypeDef.Module));
		}

		public string Description => dnSpy_AsmEditor_Resources.CreateFieldCommand2;

		public void Execute() {
			ownerNode.TreeNode.EnsureChildrenLoaded();
			ownerNode.TypeDef.Fields.Add(fieldNode.FieldDef);
			ownerNode.TreeNode.AddChild(fieldNode.TreeNode);
		}

		public void Undo() {
			bool b = ownerNode.TreeNode.Children.Remove(fieldNode.TreeNode) &&
					ownerNode.TypeDef.Fields.Remove(fieldNode.FieldDef);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return ownerNode; }
		}
	}

	readonly struct MemberRefInfo {
		public readonly MemberRef MemberRef;
		public readonly UTF8String OrigName;

		public MemberRefInfo(MemberRef mr) {
			MemberRef = mr;
			OrigName = mr.Name;
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class FieldDefSettingsCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:EditFieldCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_SETTINGS, Order = 50)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => FieldDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => FieldDefSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditFieldCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 50)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => FieldDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => FieldDefSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:EditFieldCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_SETTINGS, Order = 50)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsEnabled(CodeContext context) => FieldDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => FieldDefSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => nodes.Length == 1 && nodes[0] is FieldNode;

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var fieldNode = (FieldNode)nodes[0];

			var module = nodes[0].GetModule();
			Debug2.Assert(module is not null);
			if (module is null)
				throw new InvalidOperationException();

			var data = new FieldOptionsVM(new FieldDefOptions(fieldNode.FieldDef), module, appService.DecompilerService, fieldNode.FieldDef.DeclaringType);
			var win = new FieldOptionsDlg();
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			undoCommandService.Value.Add(new FieldDefSettingsCommand(fieldNode, data.CreateFieldDefOptions()));
		}

		readonly FieldNode fieldNode;
		readonly FieldDefOptions newOptions;
		readonly FieldDefOptions origOptions;
		readonly DocumentTreeNodeData origParentNode;
		readonly int origParentChildIndex;
		readonly bool nameChanged;
		readonly MemberRefInfo[]? memberRefInfos;

		FieldDefSettingsCommand(FieldNode fieldNode, FieldDefOptions options) {
			this.fieldNode = fieldNode;
			newOptions = options;
			origOptions = new FieldDefOptions(fieldNode.FieldDef);

			origParentNode = (DocumentTreeNodeData)fieldNode.TreeNode.Parent!.Data;
			origParentChildIndex = origParentNode.TreeNode.Children.IndexOf(fieldNode.TreeNode);
			Debug.Assert(origParentChildIndex >= 0);
			if (origParentChildIndex < 0)
				throw new InvalidOperationException();

			nameChanged = origOptions.Name != newOptions.Name;
			if (nameChanged)
				memberRefInfos = RefFinder.FindMemberRefsToThisModule(fieldNode.GetModule()!).Where(a => RefFinder.Equals(a, fieldNode.FieldDef)).Select(a => new MemberRefInfo(a)).ToArray();
		}

		public string Description => dnSpy_AsmEditor_Resources.EditFieldCommand2;

		public void Execute() {
			if (nameChanged) {
				bool b = origParentChildIndex < origParentNode.TreeNode.Children.Count && origParentNode.TreeNode.Children[origParentChildIndex] == fieldNode.TreeNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				var isNodeSelected = fieldNode.TreeNode.TreeView.SelectedItem == fieldNode;

				origParentNode.TreeNode.Children.RemoveAt(origParentChildIndex);
				newOptions.CopyTo(fieldNode.FieldDef);
				origParentNode.TreeNode.AddChild(fieldNode.TreeNode);

				if (isNodeSelected)
					origParentNode.TreeNode.TreeView.SelectItems(new[] { fieldNode });
			}
			else
				newOptions.CopyTo(fieldNode.FieldDef);
			if (memberRefInfos is not null) {
				foreach (var info in memberRefInfos)
					info.MemberRef.Name = fieldNode.FieldDef.Name;
			}
			fieldNode.TreeNode.RefreshUI();
		}

		public void Undo() {
			if (nameChanged) {
				bool b = origParentNode.TreeNode.Children.Remove(fieldNode.TreeNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				origOptions.CopyTo(fieldNode.FieldDef);
				origParentNode.TreeNode.Children.Insert(origParentChildIndex, fieldNode.TreeNode);
				origParentNode.TreeNode.TreeView.SelectItems(new[] { fieldNode });
			}
			else
				origOptions.CopyTo(fieldNode.FieldDef);
			if (memberRefInfos is not null) {
				foreach (var info in memberRefInfos)
					info.MemberRef.Name = info.OrigName;
			}
			fieldNode.TreeNode.RefreshUI();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return fieldNode; }
		}
	}
}
