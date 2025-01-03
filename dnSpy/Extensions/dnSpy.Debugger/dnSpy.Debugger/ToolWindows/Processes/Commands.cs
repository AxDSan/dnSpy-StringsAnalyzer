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
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.ToolWindows.Processes {
	[ExportAutoLoaded]
	sealed class ProcessesCommandsLoader : IAutoLoaded {
		[ImportingConstructor]
		ProcessesCommandsLoader(IWpfCommandService wpfCommandService, Lazy<IProcessesContent> processesContent) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_PROCESSES_LISTVIEW);
			cmds.Add(new RelayCommand(a => processesContent.Value.Operations.Copy(), a => processesContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.C);
			cmds.Add(new RelayCommand(a => processesContent.Value.Operations.Copy(), a => processesContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.Insert);
			cmds.Add(new RelayCommand(a => processesContent.Value.Operations.AttachToProcess(), a => processesContent.Value.Operations.CanAttachToProcess), ModifierKeys.Control | ModifierKeys.Alt, Key.P);
			cmds.Add(new RelayCommand(a => processesContent.Value.Operations.SetCurrentProcess(newTab: false), a => processesContent.Value.Operations.CanSetCurrentProcess), ModifierKeys.None, Key.Enter);
			cmds.Add(new RelayCommand(a => processesContent.Value.Operations.SetCurrentProcess(newTab: true), a => processesContent.Value.Operations.CanSetCurrentProcess), ModifierKeys.Control, Key.Enter);
			cmds.Add(new RelayCommand(a => processesContent.Value.Operations.SetCurrentProcess(newTab: true), a => processesContent.Value.Operations.CanSetCurrentProcess), ModifierKeys.Shift, Key.Enter);

			cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_PROCESSES_CONTROL);
			cmds.Add(new RelayCommand(a => processesContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.F);
			cmds.Add(new RelayCommand(a => processesContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.E);
		}
	}

	sealed class ProcessesCtxMenuContext {
		public ProcessesOperations Operations { get; }
		public ProcessesCtxMenuContext(ProcessesOperations operations) => Operations = operations;
	}

	abstract class ProcessesCtxMenuCommand : MenuItemBase<ProcessesCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected readonly Lazy<IProcessesContent> processesContent;

		protected ProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent) => this.processesContent = processesContent;

		protected sealed override ProcessesCtxMenuContext? CreateContext(IMenuItemContext context) {
			if (!(context.CreatorObject.Object is ListView))
				return null;
			if (context.CreatorObject.Object != processesContent.Value.ListView)
				return null;
			return Create();
		}

		ProcessesCtxMenuContext Create() => new ProcessesCtxMenuContext(processesContent.Value.Operations);
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DBG_PROCESSES_COPY, Order = 0)]
	sealed class CopyProcessesCtxMenuCommand : ProcessesCtxMenuCommand {
		[ImportingConstructor]
		CopyProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent)
			: base(processesContent) {
		}

		public override void Execute(ProcessesCtxMenuContext context) => context.Operations.Copy();
		public override bool IsEnabled(ProcessesCtxMenuContext context) => context.Operations.CanCopy;
	}

	[ExportMenuItem(Header = "res:SelectAllCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_PROCESSES_COPY, Order = 10)]
	sealed class SelectAllProcessesCtxMenuCommand : ProcessesCtxMenuCommand {
		[ImportingConstructor]
		SelectAllProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent)
			: base(processesContent) {
		}

		public override void Execute(ProcessesCtxMenuContext context) => context.Operations.SelectAll();
		public override bool IsEnabled(ProcessesCtxMenuContext context) => context.Operations.CanSelectAll;
	}

	[ExportMenuItem(Header = "res:ContinueProcessCommand", Icon = DsImagesAttribute.Run, Group = MenuConstants.GROUP_CTX_DBG_PROCESSES_CONTINUE, Order = 0)]
	sealed class ContinueProcessProcessesCtxMenuCommand : ProcessesCtxMenuCommand {
		[ImportingConstructor]
		ContinueProcessProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent)
			: base(processesContent) {
		}

		public override void Execute(ProcessesCtxMenuContext context) => context.Operations.ContinueProcess();
		public override bool IsEnabled(ProcessesCtxMenuContext context) => context.Operations.CanContinueProcess;
	}

	[ExportMenuItem(Header = "res:BreakProcessCommand", Icon = DsImagesAttribute.Pause, Group = MenuConstants.GROUP_CTX_DBG_PROCESSES_CONTINUE, Order = 10)]
	sealed class BreakProcessProcessesCtxMenuCommand : ProcessesCtxMenuCommand {
		[ImportingConstructor]
		BreakProcessProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent)
			: base(processesContent) {
		}

		public override void Execute(ProcessesCtxMenuContext context) => context.Operations.BreakProcess();
		public override bool IsEnabled(ProcessesCtxMenuContext context) => context.Operations.CanBreakProcess;
	}

	[ExportMenuItem(Header = "res:DetachProcessCommand", Icon = DsImagesAttribute.Cancel, Group = MenuConstants.GROUP_CTX_DBG_PROCESSES_TERMINATE, Order = 0)]
	sealed class DetachProcessProcessesCtxMenuCommand : ProcessesCtxMenuCommand {
		[ImportingConstructor]
		DetachProcessProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent)
			: base(processesContent) {
		}

		public override void Execute(ProcessesCtxMenuContext context) => context.Operations.DetachProcess();
		public override bool IsEnabled(ProcessesCtxMenuContext context) => context.Operations.CanDetachProcess;
	}

	[ExportMenuItem(Header = "res:TerminateProcessCommand", Icon = DsImagesAttribute.TerminateProcess, Group = MenuConstants.GROUP_CTX_DBG_PROCESSES_TERMINATE, Order = 10)]
	sealed class TerminateProcessProcessesCtxMenuCommand : ProcessesCtxMenuCommand {
		[ImportingConstructor]
		TerminateProcessProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent)
			: base(processesContent) {
		}

		public override void Execute(ProcessesCtxMenuContext context) => context.Operations.TerminateProcess();
		public override bool IsEnabled(ProcessesCtxMenuContext context) => context.Operations.CanTerminateProcess;
	}

	[ExportMenuItem(Header = "res:DetachWhenDebuggingStoppedCommand", Group = MenuConstants.GROUP_CTX_DBG_PROCESSES_OPTIONS, Order = 0)]
	sealed class ToggleDetachWhenDebuggingStoppedProcessesCtxMenuCommand : ProcessesCtxMenuCommand {
		[ImportingConstructor]
		ToggleDetachWhenDebuggingStoppedProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent)
			: base(processesContent) {
		}

		public override void Execute(ProcessesCtxMenuContext context) => context.Operations.ToggleDetachWhenDebuggingStopped();
		public override bool IsEnabled(ProcessesCtxMenuContext context) => context.Operations.CanToggleDetachWhenDebuggingStopped;
		public override bool IsChecked(ProcessesCtxMenuContext context) => context.Operations.DetachWhenDebuggingStopped;
	}

	[ExportMenuItem(Header = "res:HexDisplayCommand", Group = MenuConstants.GROUP_CTX_DBG_PROCESSES_OPTIONS, Order = 10)]
	sealed class UseHexadecimalProcessesCtxMenuCommand : ProcessesCtxMenuCommand {
		[ImportingConstructor]
		UseHexadecimalProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent)
			: base(processesContent) {
		}

		public override void Execute(ProcessesCtxMenuContext context) => context.Operations.ToggleUseHexadecimal();
		public override bool IsEnabled(ProcessesCtxMenuContext context) => context.Operations.CanToggleUseHexadecimal;
		public override bool IsChecked(ProcessesCtxMenuContext context) => context.Operations.UseHexadecimal;
	}

	[ExportMenuItem(Header = "res:AttachToProcessCommand", InputGestureText = "res:ShortCutKeyCtrlAltP", Icon = DsImagesAttribute.Process, Group = MenuConstants.GROUP_CTX_DBG_PROCESSES_ATTACH, Order = 0)]
	sealed class AttachToProcessProcessesCtxMenuCommand : ProcessesCtxMenuCommand {
		[ImportingConstructor]
		AttachToProcessProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent)
			: base(processesContent) {
		}

		public override void Execute(ProcessesCtxMenuContext context) => context.Operations.AttachToProcess();
		public override bool IsEnabled(ProcessesCtxMenuContext context) => context.Operations.CanAttachToProcess;
	}
}
