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
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;

namespace dnSpy.AsmEditor.Compiler {
	[DebuggerDisplay("{Description}")]
	sealed class AddClassMembersCommand : EditCodeCommandBase {
		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_ILED, Order = 13)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider;
			readonly IAppService appService;
			readonly EditCodeVMCreator editCodeVMCreator;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, IAppService appService, EditCodeVMCreator editCodeVMCreator) {
				this.undoCommandService = undoCommandService;
				this.addUpdatedNodesHelperProvider = addUpdatedNodesHelperProvider;
				this.appService = appService;
				this.editCodeVMCreator = editCodeVMCreator;
			}

			public override ImageReference? GetIcon(AsmEditorContext context) => editCodeVMCreator.GetIcon(CompilationKind.AddMembers);
			public override string? GetHeader(AsmEditorContext context) => editCodeVMCreator.GetHeader(CompilationKind.AddMembers);
			public override bool IsVisible(AsmEditorContext context) => AddClassMembersCommand.CanExecute(editCodeVMCreator, context.Nodes);
			public override void Execute(AsmEditorContext context) => AddClassMembersCommand.Execute(editCodeVMCreator, addUpdatedNodesHelperProvider, undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 43)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider;
			readonly IAppService appService;
			readonly EditCodeVMCreator editCodeVMCreator;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, IAppService appService, EditCodeVMCreator editCodeVMCreator)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.addUpdatedNodesHelperProvider = addUpdatedNodesHelperProvider;
				this.appService = appService;
				this.editCodeVMCreator = editCodeVMCreator;
			}

			public override ImageReference? GetIcon(AsmEditorContext context) => editCodeVMCreator.GetIcon(CompilationKind.AddMembers);
			public override string? GetHeader(AsmEditorContext context) => editCodeVMCreator.GetHeader(CompilationKind.AddMembers);
			public override bool IsVisible(AsmEditorContext context) => AddClassMembersCommand.CanExecute(editCodeVMCreator, context.Nodes);
			public override void Execute(AsmEditorContext context) => AddClassMembersCommand.Execute(editCodeVMCreator, addUpdatedNodesHelperProvider, undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_ILED, Order = 13)]
		sealed class CodeCommand : NodesCodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider;
			readonly IAppService appService;
			readonly EditCodeVMCreator editCodeVMCreator;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, IAppService appService, EditCodeVMCreator editCodeVMCreator)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.addUpdatedNodesHelperProvider = addUpdatedNodesHelperProvider;
				this.appService = appService;
				this.editCodeVMCreator = editCodeVMCreator;
			}

			public override ImageReference? GetIcon(CodeContext context) => editCodeVMCreator.GetIcon(CompilationKind.AddMembers);
			public override string? GetHeader(CodeContext context) => editCodeVMCreator.GetHeader(CompilationKind.AddMembers);
			public override bool IsEnabled(CodeContext context) => AddClassMembersCommand.CanExecute(editCodeVMCreator, context.Nodes);
			public override void Execute(CodeContext context) => AddClassMembersCommand.Execute(editCodeVMCreator, addUpdatedNodesHelperProvider, undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(EditCodeVMCreator editCodeVMCreator, DocumentTreeNodeData[] nodes) =>
			editCodeVMCreator.CanCreate(CompilationKind.AddMembers) &&
			nodes.Length == 1 &&
			(nodes[0] as IMDTokenNode)?.Reference is IMemberDef;

		internal static void Execute(EditCodeVMCreator editCodeVMCreator, Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(editCodeVMCreator, nodes))
				return;

			var node = nodes[0];
			var tokNode = node as IMDTokenNode;
			var defToEdit = tokNode?.Reference as IMemberDef;
			if (defToEdit is null)
				return;

			TypeNode? typeNode = null;
			for (TreeNodeData? n = node; n is not null;) {
				if (n is TypeNode t)
					typeNode = t;
				n = n.TreeNode.Parent?.Data;
			}
			if (typeNode is null)
				return;

			var asmNode = nodes[0] as AssemblyDocumentNode;
			ModuleDocumentNode? modNode;
			if (asmNode is not null) {
				asmNode.TreeNode.EnsureChildrenLoaded();
				modNode = asmNode.TreeNode.DataChildren.FirstOrDefault() as ModuleDocumentNode;
			}
			else
				modNode = nodes[0].GetModuleNode();
			Debug2.Assert(modNode is not null);
			if (modNode is null)
				return;

			var vm = editCodeVMCreator.CreateAddMembers(defToEdit);
			var win = new EditCodeDlg();
			win.DataContext = vm;
			win.Owner = appService.MainWindow;
			win.Title = $"{dnSpy_AsmEditor_Resources.EditCodeAddClassMembers} - {typeNode.ToString()}";

			if (win.ShowDialog() != true) {
				vm.Dispose();
				return;
			}
			Debug2.Assert(vm.Result is not null);

			undoCommandService.Value.Add(new AddClassMembersCommand(addUpdatedNodesHelperProvider, modNode, vm.Result));
			vm.Dispose();
		}

		AddClassMembersCommand(Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, ModuleDocumentNode modNode, ModuleImporter importer)
			: base(addUpdatedNodesHelperProvider, modNode, importer) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.EditCodeAddClassMembers;
	}
}
