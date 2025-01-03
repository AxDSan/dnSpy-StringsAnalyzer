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
using dnlib.PE;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.SaveModule;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.Module {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CommandLoader(IWpfCommandService wpfCommandService, IDocumentTabService documentTabService, RemoveNetModuleFromAssemblyCommand.EditMenuCommand removeCmd, ModuleSettingsCommand.EditMenuCommand settingsCmd) {
			wpfCommandService.AddRemoveCommand(removeCmd);
			wpfCommandService.AddSettingsCommand(documentTabService, settingsCmd, null);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class CreateNetModuleCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:CreateNetModuleCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 30)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateNetModuleCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateNetModuleCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateNetModuleCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 30)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateNetModuleCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateNetModuleCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => nodes is not null &&
	(nodes.Length == 0 || nodes.Any(a => a is DsDocumentNode));

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var win = new NetModuleOptionsDlg();
			var data = new NetModuleOptionsVM();
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreateNetModuleCommand(undoCommandService, appService.DocumentTreeView, data.CreateNetModuleOptions());
			undoCommandService.Value.Add(cmd);
			appService.DocumentTabService.FollowReference(cmd.fileNodeCreator.DocumentNode);
		}

		readonly RootDocumentNodeCreator fileNodeCreator;
		readonly Lazy<IUndoCommandService> undoCommandService;

		CreateNetModuleCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView, NetModuleOptions options) {
			this.undoCommandService = undoCommandService;
			var module = ModuleUtils.CreateNetModule(options.Name, options.Mvid, options.ClrVersion);
			var file = DsDotNetDocument.CreateModule(DsDocumentInfo.CreateDocument(string.Empty), module, true);
			fileNodeCreator = RootDocumentNodeCreator.CreateModule(documentTreeView, file);
		}

		public string Description => dnSpy_AsmEditor_Resources.CreateNetModuleCommand2;

		public void Execute() {
			fileNodeCreator.Add();
			undoCommandService.Value.MarkAsModified(undoCommandService.Value.GetUndoObject(fileNodeCreator.DocumentNode.Document)!);
		}

		public void Undo() => fileNodeCreator.Remove();

		public IEnumerable<object> ModifiedObjects {
			get { yield return fileNodeCreator.DocumentNode; }
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class ConvertNetModuleToAssemblyCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:ConvNetModuleToAssemblyCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_MISC, Order = 20)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => ConvertNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ConvertNetModuleToAssemblyCommand.Execute(undoCommandService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:ConvNetModuleToAssemblyCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_MISC, Order = 20)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => ConvertNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ConvertNetModuleToAssemblyCommand.Execute(undoCommandService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes is not null &&
			nodes.Length > 0 &&
			nodes.All(n => n is ModuleDocumentNode &&
					n.TreeNode.Parent is not null &&
					!(n.TreeNode.Parent.Data is AssemblyDocumentNode));

		static void Execute(Lazy<IUndoCommandService> undoCommandService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			undoCommandService.Value.Add(new ConvertNetModuleToAssemblyCommand(nodes));
		}

		readonly ModuleDocumentNode[] nodes;
		SavedState[] savedStates;
		bool hasExecuted;
		sealed class SavedState {
			public ModuleKind ModuleKind;
			public Characteristics Characteristics;
			public AssemblyDocumentNode? AssemblyFileNode;
		}

		ConvertNetModuleToAssemblyCommand(DocumentTreeNodeData[] nodes) {
			this.nodes = nodes.Cast<ModuleDocumentNode>().ToArray();
			savedStates = new SavedState[this.nodes.Length];
			for (int i = 0; i < savedStates.Length; i++)
				savedStates[i] = new SavedState();
		}

		public string Description => dnSpy_AsmEditor_Resources.ConvNetModuleToAssemblyCommand;

		public void Execute() {
			Debug.Assert(!hasExecuted);
			if (hasExecuted)
				throw new InvalidOperationException();
			for (int i = 0; i < nodes.Length; i++) {
				var node = nodes[i];
				var savedState = savedStates[i];

				var module = node.Document.ModuleDef;
				bool b = module is not null && module.Assembly is null;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				Debug2.Assert(module is not null);

				savedState.ModuleKind = module.Kind;
				ModuleUtils.AddToNewAssemblyDef(module, ModuleKind.Dll, out savedState.Characteristics);

				if (savedState.AssemblyFileNode is null) {
					var asmFile = DsDotNetDocument.CreateAssembly(node.Document);
					savedState.AssemblyFileNode = node.Context.DocumentTreeView.CreateAssembly(asmFile);
				}

				lock (savedState.AssemblyFileNode.Document.Children.SyncRoot) {
					Debug.Assert(savedState.AssemblyFileNode.Document.Children.Count == 1);
					savedState.AssemblyFileNode.Document.Children.Remove(node.Document);
					Debug.Assert(savedState.AssemblyFileNode.Document.Children.Count == 0);
					savedState.AssemblyFileNode.TreeNode.EnsureChildrenLoaded();
					Debug.Assert(savedState.AssemblyFileNode.TreeNode.Children.Count == 0);
					savedState.AssemblyFileNode.Document.Children.Add(node.Document);
				}

				int index = node.Context.DocumentTreeView.TreeView.Root.DataChildren.ToList().IndexOf(node);
				b = index >= 0;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				node.Context.DocumentTreeView.TreeView.Root.Children[index] = savedState.AssemblyFileNode.TreeNode;
				savedState.AssemblyFileNode.TreeNode.AddChild(node.TreeNode);
			}
			hasExecuted = true;
		}

		public void Undo() {
			Debug.Assert(hasExecuted);
			if (!hasExecuted)
				throw new InvalidOperationException();

			for (int i = nodes.Length - 1; i >= 0; i--) {
				var node = nodes[i];
				var savedState = savedStates[i];

				int index = node.Context.DocumentTreeView.TreeView.Root.DataChildren.ToList().IndexOf(savedState.AssemblyFileNode!);
				bool b = index >= 0;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				savedState.AssemblyFileNode!.TreeNode.Children.Remove(node.TreeNode);
				node.Context.DocumentTreeView.TreeView.Root.Children[index] = node.TreeNode;

				var module = node.Document.ModuleDef;
				b = module is not null && module.Assembly is not null &&
					module.Assembly.Modules.Count == 1 &&
					module.Assembly.ManifestModule == module;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				module!.Assembly!.Modules.Remove(module);
				module.Kind = savedState.ModuleKind;
				module.Characteristics = savedState.Characteristics;
			}
			hasExecuted = false;
		}

		public IEnumerable<object> ModifiedObjects => nodes;
	}

	[DebuggerDisplay("{Description}")]
	sealed class ConvertAssemblyToNetModuleCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:ConvAssemblyToNetModuleCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_MISC, Order = 30)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => ConvertAssemblyToNetModuleCommand.IsVisible(context.Nodes);
			public override bool IsEnabled(AsmEditorContext context) => ConvertAssemblyToNetModuleCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ConvertAssemblyToNetModuleCommand.Execute(undoCommandService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:ConvAssemblyToNetModuleCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_MISC, Order = 30)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => ConvertAssemblyToNetModuleCommand.IsVisible(context.Nodes);
			public override bool IsEnabled(AsmEditorContext context) => ConvertAssemblyToNetModuleCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ConvertAssemblyToNetModuleCommand.Execute(undoCommandService, context.Nodes);
		}

		static bool IsVisible(DocumentTreeNodeData[] nodes) =>
			nodes is not null &&
			nodes.Length > 0 &&
			nodes.All(n => n is AssemblyDocumentNode);

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes is not null &&
			nodes.Length > 0 &&
			nodes.All(n => n is AssemblyDocumentNode &&
					((AssemblyDocumentNode)n).Document.AssemblyDef is not null &&
					((AssemblyDocumentNode)n).Document.AssemblyDef!.Modules.Count == 1);

		static void Execute(Lazy<IUndoCommandService> undoCommandService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			undoCommandService.Value.Add(new ConvertAssemblyToNetModuleCommand(nodes));
		}

		readonly AssemblyDocumentNode[] nodes;
		SavedState[]? savedStates;
		sealed class SavedState {
			public AssemblyDef? AssemblyDef;
			public ModuleKind ModuleKind;
			public Characteristics Characteristics;
			public ModuleDocumentNode? ModuleNode;
		}

		ConvertAssemblyToNetModuleCommand(DocumentTreeNodeData[] nodes) => this.nodes = nodes.Cast<AssemblyDocumentNode>().ToArray();

		public string Description => dnSpy_AsmEditor_Resources.ConvAssemblyToNetModuleCommand;

		public void Execute() {
			Debug2.Assert(savedStates is null);
			if (savedStates is not null)
				throw new InvalidOperationException();
			savedStates = new SavedState[nodes.Length];
			for (int i = 0; i < nodes.Length; i++) {
				var node = nodes[i];
				savedStates[i] = new SavedState();
				var savedState = savedStates[i];

				int index = node.TreeNode.Parent!.Children.IndexOf(node.TreeNode);
				bool b = index >= 0;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				savedState.ModuleNode = (ModuleDocumentNode)node.TreeNode.Children.First(a => a.Data is ModuleDocumentNode).Data;
				node.TreeNode.Children.Remove(savedState.ModuleNode.TreeNode);
				node.TreeNode.Parent.Children[index] = savedState.ModuleNode.TreeNode;

				var module = node.Document.ModuleDef;
				b = module is not null && module.Assembly is not null &&
					module.Assembly.Modules.Count == 1 &&
					module.Assembly.ManifestModule == module;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				node.TreeNode.EnsureChildrenLoaded();
				savedState.AssemblyDef = module!.Assembly;
				module.Assembly!.Modules.Remove(module);
				savedState.ModuleKind = module.Kind;
				ModuleUtils.WriteNewModuleKind(module, ModuleKind.NetModule, out savedState.Characteristics);
			}
		}

		public void Undo() {
			Debug2.Assert(savedStates is not null);
			if (savedStates is null)
				throw new InvalidOperationException();

			for (int i = nodes.Length - 1; i >= 0; i--) {
				var node = nodes[i];
				var savedState = savedStates[i];

				var module = node.Document.ModuleDef;
				bool b = module is not null && module.Assembly is null;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				module!.Kind = savedState.ModuleKind;
				module.Characteristics = savedState.Characteristics;
				savedState.AssemblyDef!.Modules.Add(module);

				var parent = savedState.ModuleNode!.TreeNode.Parent!;
				int index = parent.Children.IndexOf(savedState.ModuleNode.TreeNode);
				b = index >= 0;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				parent.Children[index] = node.TreeNode;
				node.TreeNode.AddChild(savedState.ModuleNode.TreeNode);
			}
			savedStates = null;
		}

		public IEnumerable<object> ModifiedObjects => nodes;
	}

	abstract class AddNetModuleToAssemblyCommand : IUndoCommand2 {
		internal static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes is not null &&
			nodes.Length == 1 &&
			(nodes[0] is AssemblyDocumentNode || nodes[0] is ModuleDocumentNode);

		readonly IUndoCommandService undoCommandService;
		readonly AssemblyDocumentNode asmNode;
		internal readonly ModuleDocumentNode modNode;
		readonly bool modNodeWasCreated;

		protected AddNetModuleToAssemblyCommand(IUndoCommandService undoCommandService, DsDocumentNode asmNode, ModuleDocumentNode modNode, bool modNodeWasCreated) {
			this.undoCommandService = undoCommandService;
			if (!(asmNode is AssemblyDocumentNode))
				asmNode = (AssemblyDocumentNode)asmNode.TreeNode.Parent!.Data;
			this.asmNode = (AssemblyDocumentNode)asmNode;
			this.modNode = modNode;
			this.modNodeWasCreated = modNodeWasCreated;
		}

		public abstract string Description { get; }

		public void Execute() {
			Debug2.Assert(modNode.TreeNode.Parent is null);
			if (modNode.TreeNode.Parent is not null)
				throw new InvalidOperationException();
			asmNode.TreeNode.EnsureChildrenLoaded();
			asmNode.Document.AssemblyDef!.Modules.Add(modNode.Document.ModuleDef);
			asmNode.Document.Children.Add(modNode.Document);
			asmNode.TreeNode.AddChild(modNode.TreeNode);
			if (modNodeWasCreated)
				undoCommandService.MarkAsModified(undoCommandService.GetUndoObject(modNode.Document)!);
		}

		public void Undo() {
			Debug2.Assert(modNode.TreeNode.Parent is not null);
			if (modNode.TreeNode.Parent is null)
				throw new InvalidOperationException();
			asmNode.Document.AssemblyDef!.Modules.Remove(modNode.Document.ModuleDef);
			asmNode.Document.Children.Remove(modNode.Document);
			bool b = asmNode.TreeNode.Children.Remove(modNode.TreeNode);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return asmNode; }
		}

		public IEnumerable<object> NonModifiedObjects {
			get {
				if (modNodeWasCreated)
					yield return modNode;
			}
		}
	}

	sealed class AddNewNetModuleToAssemblyCommand : AddNetModuleToAssemblyCommand {
		[ExportMenuItem(Header = "res:AddNewNetModuleToAssemblyCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 10)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => AddNewNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => AddNewNetModuleToAssemblyCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:AddNewNetModuleToAssemblyCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 10)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => AddNewNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => AddNewNetModuleToAssemblyCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!AddNetModuleToAssemblyCommand.CanExecute(nodes))
				return;

			var asmNode = (DsDocumentNode)nodes[0];
			if (asmNode is ModuleDocumentNode)
				asmNode = (AssemblyDocumentNode)asmNode.TreeNode.Parent!.Data;

			var win = new NetModuleOptionsDlg();
			var data = new NetModuleOptionsVM(asmNode.Document.ModuleDef!);
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var options = data.CreateNetModuleOptions();
			var newModule = ModuleUtils.CreateNetModule(options.Name, options.Mvid, options.ClrVersion);
			var newFile = DsDotNetDocument.CreateModule(DsDocumentInfo.CreateDocument(string.Empty), newModule, true);
			var newModNode = asmNode.Context.DocumentTreeView.CreateModule(newFile);
			var cmd = new AddNewNetModuleToAssemblyCommand(undoCommandService.Value, (DsDocumentNode)nodes[0], newModNode);
			undoCommandService.Value.Add(cmd);
			appService.DocumentTabService.FollowReference(cmd.modNode);
		}

		AddNewNetModuleToAssemblyCommand(IUndoCommandService undoCommandService, DsDocumentNode asmNode, ModuleDocumentNode modNode)
			: base(undoCommandService, asmNode, modNode, true) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.AddNewNetModuleToAssemblyCommand2;
	}

	sealed class AddExistingNetModuleToAssemblyCommand : AddNetModuleToAssemblyCommand {
		[ExportMenuItem(Header = "res:AddExistingNetModuleToAssemblyCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 20)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => AddExistingNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => AddExistingNetModuleToAssemblyCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:AddExistingNetModuleToAssemblyCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 20)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => AddExistingNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => AddExistingNetModuleToAssemblyCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!AddNetModuleToAssemblyCommand.CanExecute(nodes))
				return;

			var dialog = new System.Windows.Forms.OpenFileDialog() {
				Filter = PickFilenameConstants.NetModuleFilter,
				RestoreDirectory = true,
			};
			if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return;
			if (string.IsNullOrEmpty(dialog.FileName))
				return;

			var fm = appService.DocumentTreeView.DocumentService;
			var file = fm.CreateDocument(DsDocumentInfo.CreateDocument(dialog.FileName), dialog.FileName, true);
			if (file.ModuleDef is null || file.AssemblyDef is not null || !(file is IDsDotNetDocument)) {
				MsgBox.Instance.Show(string.Format(dnSpy_AsmEditor_Resources.Error_NotNetModule, file.Filename), MsgBoxButton.OK);
				if (file is IDisposable id)
					id.Dispose();
				return;
			}

			var node = (DsDocumentNode)nodes[0];
			var newModNode = node.Context.DocumentTreeView.CreateModule((IDsDotNetDocument)file);
			var cmd = new AddExistingNetModuleToAssemblyCommand(undoCommandService.Value, node, newModNode);
			undoCommandService.Value.Add(cmd);
			appService.DocumentTabService.FollowReference(cmd.modNode);
		}

		AddExistingNetModuleToAssemblyCommand(IUndoCommandService undoCommandService, DsDocumentNode asmNode, ModuleDocumentNode modNode)
			: base(undoCommandService, asmNode, modNode, false) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.AddExistingNetModuleToAssemblyCommand2;
	}

	sealed class RemoveNetModuleFromAssemblyCommand : IUndoCommand2 {
		[ExportMenuItem(Header = "res:RemoveNetModuleCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_DELETE, Order = 10)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IDocumentSaver> documentSaver;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IDocumentSaver> documentSaver, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.documentSaver = documentSaver;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => RemoveNetModuleFromAssemblyCommand.IsVisible(context.Nodes);
			public override bool IsEnabled(AsmEditorContext context) => RemoveNetModuleFromAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => RemoveNetModuleFromAssemblyCommand.Execute(undoCommandService, documentSaver, appService, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:RemoveNetModuleCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 10)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IDocumentSaver> documentSaver;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IDocumentSaver> documentSaver, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.documentSaver = documentSaver;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => RemoveNetModuleFromAssemblyCommand.IsVisible(context.Nodes);
			public override bool IsEnabled(AsmEditorContext context) => RemoveNetModuleFromAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => RemoveNetModuleFromAssemblyCommand.Execute(undoCommandService, documentSaver, appService, context.Nodes);
		}

		static bool IsVisible(DocumentTreeNodeData[] nodes) =>
			nodes is not null &&
			nodes.Length == 1 &&
			nodes[0] is ModuleDocumentNode &&
			nodes[0].TreeNode.Parent is not null &&
			nodes[0].TreeNode.Parent!.Data is AssemblyDocumentNode;

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes is not null &&
			nodes.Length == 1 &&
			nodes[0] is ModuleDocumentNode &&
			nodes[0].TreeNode.Parent is not null &&
			nodes[0].TreeNode.Parent!.DataChildren.ToList().IndexOf(nodes[0]) > 0;

		static void Execute(Lazy<IUndoCommandService> undoCommandService, Lazy<IDocumentSaver> documentSaver, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var modNode = (ModuleDocumentNode)nodes[0];
			if (!documentSaver.Value.AskUserToSaveIfModified(new[] { modNode.Document }))
				return;

			undoCommandService.Value.Add(new RemoveNetModuleFromAssemblyCommand(undoCommandService, modNode));
		}

		readonly AssemblyDocumentNode asmNode;
		readonly ModuleDocumentNode modNode;
		readonly int removeIndex, removeIndexDocument;
		readonly Lazy<IUndoCommandService> undoCommandService;

		RemoveNetModuleFromAssemblyCommand(Lazy<IUndoCommandService> undoCommandService, ModuleDocumentNode modNode) {
			this.undoCommandService = undoCommandService;
			asmNode = (AssemblyDocumentNode)modNode.TreeNode.Parent!.Data;
			Debug2.Assert(asmNode is not null);
			this.modNode = modNode;
			removeIndex = asmNode.TreeNode.DataChildren.ToList().IndexOf(modNode);
			Debug.Assert(removeIndex > 0);
			Debug2.Assert(asmNode.Document.AssemblyDef is not null &&
				asmNode.Document.AssemblyDef.Modules.IndexOf(modNode.Document.ModuleDef) == removeIndex);
			removeIndexDocument = asmNode.Document.Children.IndexOf(modNode.Document);
			Debug.Assert(removeIndexDocument >= 0);
		}

		public string Description => dnSpy_AsmEditor_Resources.RemoveNetModuleCommand;

		public void Execute() {
			Debug2.Assert(modNode.TreeNode.Parent is not null);
			if (modNode.TreeNode.Parent is null)
				throw new InvalidOperationException();

			var children = asmNode.TreeNode.DataChildren.ToArray();
			lock (asmNode.Document.Children.SyncRoot) {
				bool b = removeIndex < children.Length && children[removeIndex] == modNode &&
					removeIndex < asmNode.Document.AssemblyDef!.Modules.Count &&
					asmNode.Document.AssemblyDef.Modules[removeIndex] == modNode.Document.ModuleDef &&
					removeIndexDocument < asmNode.Document.Children.Count &&
					asmNode.Document.Children[removeIndexDocument] == modNode.Document;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				asmNode.Document.AssemblyDef!.Modules.RemoveAt(removeIndex);
				asmNode.Document.Children.RemoveAt(removeIndexDocument);
				asmNode.TreeNode.Children.RemoveAt(removeIndex);
			}
			undoCommandService.Value.MarkAsModified(undoCommandService.Value.GetUndoObject(asmNode.Document)!);
		}

		public void Undo() {
			Debug2.Assert(modNode.TreeNode.Parent is null);
			if (modNode.TreeNode.Parent is not null)
				throw new InvalidOperationException();
			asmNode.Document.AssemblyDef!.Modules.Insert(removeIndex, modNode.Document.ModuleDef);
			asmNode.Document.Children.Insert(removeIndexDocument, modNode.Document);
			asmNode.TreeNode.Children.Insert(removeIndex, modNode.TreeNode);
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return asmNode; }
		}

		public IEnumerable<object> NonModifiedObjects {
			get { yield return modNode; }
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class ModuleSettingsCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:EditModuleCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_SETTINGS, Order = 10)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => ModuleSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ModuleSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditModuleCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 10)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => ModuleSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ModuleSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes is not null &&
			nodes.Length == 1 &&
			nodes[0] is ModuleDocumentNode;

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var asmNode = (ModuleDocumentNode)nodes[0];

			var module = asmNode.Document.ModuleDef!;
			var data = new ModuleOptionsVM(module, new ModuleOptions(module), appService.DecompilerService);
			var win = new ModuleOptionsDlg();
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			undoCommandService.Value.Add(new ModuleSettingsCommand(asmNode, data.CreateModuleOptions()));
		}

		readonly ModuleDocumentNode modNode;
		readonly ModuleOptions newOptions;
		readonly ModuleOptions origOptions;

		ModuleSettingsCommand(ModuleDocumentNode modNode, ModuleOptions newOptions) {
			this.modNode = modNode;
			this.newOptions = newOptions;
			origOptions = new ModuleOptions(modNode.Document.ModuleDef!);
		}

		public string Description => dnSpy_AsmEditor_Resources.EditModuleCommand2;

		void WriteModuleOptions(ModuleOptions theOptions) {
			theOptions.CopyTo(modNode.Document.ModuleDef!);
			modNode.TreeNode.RefreshUI();
		}

		public void Execute() => WriteModuleOptions(newOptions);
		public void Undo() => WriteModuleOptions(origOptions);

		public IEnumerable<object> ModifiedObjects {
			get { yield return modNode; }
		}
	}
}
