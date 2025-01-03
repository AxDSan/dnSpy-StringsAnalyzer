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
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.DnlibDialogs;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.Text;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class InstructionsListHelper : ListBoxHelperBase<InstructionVM>, IEditOperand, ISelectItems<InstructionVM> {
		CilBodyVM cilBodyVM;

		protected override string AddNewBeforeSelectionMessage => dnSpy_AsmEditor_Resources.Instr_Command1;
		protected override string AddNewAfterSelectionMessage => dnSpy_AsmEditor_Resources.Instr_Command2;
		protected override string AppendNewMessage => dnSpy_AsmEditor_Resources.Instr_Command3;
		protected override string RemoveSingularMessage => dnSpy_AsmEditor_Resources.Instr_Command4;
		protected override string RemovePluralMessage => dnSpy_AsmEditor_Resources.Instr_Command5;
		protected override string RemoveAllMessage => dnSpy_AsmEditor_Resources.Instr_Command6;

		public InstructionsListHelper(ListView listView, Window ownerWindow)
			: base(listView) {
			cilBodyVM = null!;
		}

		protected override InstructionVM[] GetSelectedItems() => listBox.SelectedItems.Cast<InstructionVM>().ToArray();

		protected override void OnDataContextChangedInternal(object dataContext) {
			cilBodyVM = ((MethodBodyVM)dataContext).CilBodyVM;
			cilBodyVM.SelectItems = this;
			coll = cilBodyVM.InstructionsListVM;
			coll.CollectionChanged += coll_CollectionChanged;
			InitializeInstructions(coll);

			Add(new ContextMenuHandler {
				Header = "res:NopInstrCommand",
				HeaderPlural = "res:NopInstrsCommand",
				Command = cilBodyVM.ReplaceInstructionWithNopCommand,
				Icon = null,
				InputGestureText = "res:ShortCutKeyN",
				Modifiers = ModifierKeys.None,
				Key = Key.N,
			});
			Add(new ContextMenuHandler {
				Header = "res:ReplaceNopsInstrCommand",
				HeaderPlural = "res:ReplaceNopsInstrsCommand",
				Command = cilBodyVM.ReplaceInstructionWithMultipleNopsCommand,
				Icon = null,
				InputGestureText = "res:ShortCutKeyR",
				Modifiers = ModifierKeys.None,
				Key = Key.R,
			});
			Add(new ContextMenuHandler {
				Header = "res:InvertBranchCommand",
				HeaderPlural = "res:InvertBranchesCommand",
				Command = cilBodyVM.InvertBranchCommand,
				Icon = DsImages.Branch,
				InputGestureText = "res:ShortCutKeyI",
				Modifiers = ModifierKeys.None,
				Key = Key.I,
			});
			Add(new ContextMenuHandler {
				Header = "res:ConvertUncondBranchCommand",
				HeaderPlural = "res:ConvertUncondBranchesCommand",
				Command = cilBodyVM.ConvertBranchToUnconditionalBranchCommand,
				Icon = null,
				InputGestureText = "res:ShortCutKeyB",
				Modifiers = ModifierKeys.None,
				Key = Key.B,
			});
			Add(new ContextMenuHandler {
				Header = "res:RemoveAndAddPopsCommand",
				Command = cilBodyVM.RemoveInstructionAndAddPopsCommand,
				InputGestureText = "res:ShortCutKeyP",
				Modifiers = ModifierKeys.None,
				Key = Key.P,
			});
			AddSeparator();
			Add(new ContextMenuHandler {
				Header = "res:SimplifyAllInstructionsCommand",
				Command = cilBodyVM.SimplifyAllInstructionsCommand,
				InputGestureText = "res:ShortCutKeyS",
				Modifiers = ModifierKeys.None,
				Key = Key.S,
			});
			Add(new ContextMenuHandler {
				Header = "res:OptimizeAllInstructionsCommand",
				Command = cilBodyVM.OptimizeAllInstructionsCommand,
				InputGestureText = "res:ShortCutKeyO",
				Modifiers = ModifierKeys.None,
				Key = Key.O,
			});
			AddSeparator();
			AddStandardMenuHandlers();
			Add(new ContextMenuHandler {
				Header = "res:CopyMetaDataToken",
				HeaderPlural = "res:CopyMetaDataTokens",
				Command = new RelayCommand(a => CopyOperandMDTokens((InstructionVM[])a!), a => CopyOperandMDTokensCanExecute((InstructionVM[])a!)),
				InputGestureText = "res:ShortCutKeyCtrlM",
				Modifiers = ModifierKeys.Control,
				Key = Key.M,
			});
			Add(new ContextMenuHandler {
				Header = "res:CopyRVACommand",
				HeaderPlural = "res:CopyRVAsCommand",
				Command = new RelayCommand(a => CopyInstructionRVA((InstructionVM[])a!), a => CopyInstructionRVACanExecute((InstructionVM[])a!)),
				InputGestureText = "res:ShortCutKeyCtrlR",
				Modifiers = ModifierKeys.Control,
				Key = Key.R,
			});
			Add(new ContextMenuHandler {
				Header = "res:CopyFileOffsetCommand",
				HeaderPlural = "res:CopyFileOffsetsCommand",
				Command = new RelayCommand(a => CopyInstructionFileOffset((InstructionVM[])a!), a => CopyInstructionFileOffsetCanExecute((InstructionVM[])a!)),
				InputGestureText = "res:ShortCutKeyCtrlF",
				Modifiers = ModifierKeys.Control,
				Key = Key.F,
			});
		}

		void CopyOffsets(ulong baseOffset, InstructionVM[] instrs) {
			var sb = new StringBuilder();

			int lines = 0;
			for (int i = 0; i < instrs.Length; i++) {
				if (lines++ > 0)
					sb.AppendLine();
				sb.Append($"0x{baseOffset + instrs[i].Offset:X8}");
			}
			if (lines > 1)
				sb.AppendLine();

			var text = sb.ToString();
			if (text.Length > 0) {
				try {
					Clipboard.SetText(text);
				}
				catch (ExternalException) { }
			}
		}

		void CopyInstructionRVA(InstructionVM[] instrs) => CopyOffsets(cilBodyVM.RVA.Value, instrs);
		bool CopyInstructionRVACanExecute(InstructionVM[] instrs) => !cilBodyVM.RVA.HasError && instrs.Length > 0;
		void CopyInstructionFileOffset(InstructionVM[] instrs) => CopyOffsets(cilBodyVM.FileOffset.Value, instrs);
		bool CopyInstructionFileOffsetCanExecute(InstructionVM[] instrs) => !cilBodyVM.FileOffset.HasError && instrs.Length > 0;

		void CopyOperandMDTokens(InstructionVM[] instrs) {
			var sb = new StringBuilder();

			int lines = 0;
			for (int i = 0; i < instrs.Length; i++) {
				uint? token = GetOperandMDToken(instrs[i].InstructionOperandVM);
				if (token is null)
					continue;

				if (lines++ > 0)
					sb.AppendLine();
				sb.Append($"0x{token.Value:X8}");
			}
			if (lines > 1)
				sb.AppendLine();

			var text = sb.ToString();
			if (text.Length > 0) {
				try {
					Clipboard.SetText(text);
				}
				catch (ExternalException) { }
			}
		}

		bool CopyOperandMDTokensCanExecute(InstructionVM[] instrs) => instrs.Any(a => GetOperandMDToken(a.InstructionOperandVM) is not null);

		static uint? GetOperandMDToken(InstructionOperandVM op) {
			switch (op.InstructionOperandType) {
			case InstructionOperandType.None:
			case InstructionOperandType.SByte:
			case InstructionOperandType.Byte:
			case InstructionOperandType.Int32:
			case InstructionOperandType.Int64:
			case InstructionOperandType.Single:
			case InstructionOperandType.Double:
			case InstructionOperandType.String:
			case InstructionOperandType.BranchTarget:
			case InstructionOperandType.SwitchTargets:
			case InstructionOperandType.Local:
			case InstructionOperandType.Parameter:
				return null;

			case InstructionOperandType.Field:
			case InstructionOperandType.Method:
			case InstructionOperandType.Token:
			case InstructionOperandType.Type:
				var token = op.Other as IMDTokenProvider;
				return token is null ? (uint?)null : token.MDToken.ToUInt32();

			case InstructionOperandType.MethodSig:
				var msig = op.Other as MethodSig;
				return msig is null ? (uint?)null : msig.OriginalToken;

			default:
				throw new InvalidOperationException();
			}
		}

		void coll_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
			if (e.NewItems is not null)
				InitializeInstructions(e.NewItems);
		}

		void InitializeInstructions(System.Collections.IList list) {
			foreach (InstructionVM? instr in list)
				instr!.InstructionOperandVM.EditOperand = this;
		}

		protected override void CopyItemsAsText(InstructionVM[] instrs) {
			Array.Sort(instrs, (a, b) => a.Index.CompareTo(b.Index));
			CopyItemsAsTextToClipboard(instrs);
		}

		public static void CopyItemsAsTextToClipboard(InstructionVM[] instrs) {
			var output = new StringBuilderTextColorOutput();

			for (int i = 0; i < instrs.Length; i++) {
				if (i > 0)
					output.WriteLine();

				var instr = instrs[i];
				output.Write(BoxedTextColor.Number, instr.Index.ToString());
				output.Write(BoxedTextColor.Text, "\t");
				output.Write(BoxedTextColor.Label, instr.Offset.ToString("X4"));
				output.Write(BoxedTextColor.Text, "\t");
				output.Write(BoxedTextColor.OpCode, instr.Code.ToOpCode().ToString());

				switch (instr.InstructionOperandVM.InstructionOperandType) {
				case InstructionOperandType.None:
					break;

				case InstructionOperandType.SByte:
					output.Write(BoxedTextColor.Text, "\t");
					output.Write(BoxedTextColor.Number, instr.InstructionOperandVM.SByte.StringValue);
					break;

				case InstructionOperandType.Byte:
					output.Write(BoxedTextColor.Text, "\t");
					output.Write(BoxedTextColor.Number, instr.InstructionOperandVM.Byte.StringValue);
					break;

				case InstructionOperandType.Int32:
					output.Write(BoxedTextColor.Text, "\t");
					output.Write(BoxedTextColor.Number, instr.InstructionOperandVM.Int32.StringValue);
					break;

				case InstructionOperandType.Int64:
					output.Write(BoxedTextColor.Text, "\t");
					output.Write(BoxedTextColor.Number, instr.InstructionOperandVM.Int64.StringValue);
					break;

				case InstructionOperandType.Single:
					output.Write(BoxedTextColor.Text, "\t");
					output.Write(BoxedTextColor.Number, instr.InstructionOperandVM.Single.StringValue);
					break;

				case InstructionOperandType.Double:
					output.Write(BoxedTextColor.Text, "\t");
					output.Write(BoxedTextColor.Number, instr.InstructionOperandVM.Double.StringValue);
					break;

				case InstructionOperandType.String:
					output.Write(BoxedTextColor.Text, "\t");
					output.Write(BoxedTextColor.String, instr.InstructionOperandVM.String.StringValue);
					break;

				case InstructionOperandType.Field:
				case InstructionOperandType.Method:
				case InstructionOperandType.Token:
				case InstructionOperandType.Type:
				case InstructionOperandType.MethodSig:
				case InstructionOperandType.BranchTarget:
				case InstructionOperandType.SwitchTargets:
				case InstructionOperandType.Local:
				case InstructionOperandType.Parameter:
					output.Write(BoxedTextColor.Text, "\t");
					BodyUtils.WriteObject(output, instr.InstructionOperandVM.Value);
					break;

				default:
					throw new InvalidOperationException();
				}
			}
			if (instrs.Length > 1)
				output.WriteLine();

			try {
				Clipboard.SetText(output.ToString());
			}
			catch (ExternalException) { }
		}

		[Flags]
		enum MenuCommandFlags {
			FieldDef		= 0x00000001,
			FieldMemberRef	= 0x00000002,
			MethodDef		= 0x00000004,
			MethodMemberRef	= 0x00000008,
			MethodSpec		= 0x00000010,
			TypeDef			= 0x00000020,
			TypeRef			= 0x00000040,
			TypeSpec		= 0x00000080,
		}

		void ShowMenu(object? parameter, InstructionOperandVM opvm, MenuCommandFlags flags) {
			var ctxMenu = new ContextMenu();
			ctxMenu.SetResourceReference(DsImage.BackgroundBrushProperty, "ContextMenuRectangleFill");

			MenuItem menuItem;
			if ((flags & (MenuCommandFlags.TypeDef | MenuCommandFlags.TypeRef)) != 0) {
				ctxMenu.Items.Add(menuItem = new MenuItem() {
					Header = dnSpy_AsmEditor_Resources.EditOperand_Type,
					Command = new RelayCommand(a => AddType(opvm)),
				});
				Add16x16Image(menuItem, DsImages.ClassPublic, true);
			}
			if ((flags & MenuCommandFlags.TypeSpec) != 0) {
				ctxMenu.Items.Add(menuItem = new MenuItem() {
					Header = dnSpy_AsmEditor_Resources.EditOperand_TypeSpec,
					Command = new RelayCommand(a => AddTypeSpec(opvm)),
				});
				Add16x16Image(menuItem, DsImages.Template, true);
			}
			if ((flags & MenuCommandFlags.MethodDef) != 0) {
				ctxMenu.Items.Add(menuItem = new MenuItem() {
					Header = dnSpy_AsmEditor_Resources.EditOperand_Method,
					Command = new RelayCommand(a => AddMethodDef(opvm)),
				});
				Add16x16Image(menuItem, DsImages.MethodPublic, true);
			}
			if ((flags & MenuCommandFlags.MethodMemberRef) != 0) {
				ctxMenu.Items.Add(new MenuItem() {
					Header = dnSpy_AsmEditor_Resources.EditOperand_Method_MemberRef,
					Command = new RelayCommand(a => AddMethodMemberRef(opvm)),
				});
			}
			if ((flags & MenuCommandFlags.MethodSpec) != 0) {
				ctxMenu.Items.Add(new MenuItem() {
					Header = dnSpy_AsmEditor_Resources.EditOperand_MethodSpec,
					Command = new RelayCommand(a => AddMethodSpec(opvm)),
				});
			}
			if ((flags & MenuCommandFlags.FieldDef) != 0) {
				ctxMenu.Items.Add(menuItem = new MenuItem() {
					Header = dnSpy_AsmEditor_Resources.EditOperand_Field,
					Command = new RelayCommand(a => AddFieldDef(opvm)),
				});
				Add16x16Image(menuItem, DsImages.FieldPublic, true);
			}
			if ((flags & MenuCommandFlags.FieldMemberRef) != 0) {
				ctxMenu.Items.Add(new MenuItem() {
					Header = dnSpy_AsmEditor_Resources.EditOperand_Field_MemberRef,
					Command = new RelayCommand(a => AddFieldMemberRef(opvm)),
				});
			}

			ctxMenu.Placement = PlacementMode.Bottom;
			ctxMenu.PlacementTarget = parameter as UIElement;
			ctxMenu.IsOpen = true;
		}

		void AddFieldDef(InstructionOperandVM opvm) {
			var picker = new DnlibTypePicker(Window.GetWindow(listBox));
			var op = opvm.Other as IField ?? (object?)cilBodyVM.TypeSigCreatorOptions.OwnerType;
			if (picker.GetDnlibType(dnSpy_AsmEditor_Resources.Pick_Field, new FlagsDocumentTreeNodeFilter(VisibleMembersFlags.FieldDef), op, cilBodyVM.OwnerModule) is IField field)
				opvm.Other = field;
		}

		void AddFieldMemberRef(InstructionOperandVM opvm) {
			MemberRef? mr = opvm.Other as MemberRef;
			if (opvm.Other is FieldDef fd)
				mr = cilBodyVM.OwnerModule.Import(fd);
			if (mr is not null && mr.FieldSig is null)
				mr = null;
			AddMemberRef(opvm, mr, true);
		}

		void AddMethodDef(InstructionOperandVM opvm) {
			var picker = new DnlibTypePicker(Window.GetWindow(listBox));
			var op = opvm.Other as IMethod ?? (object?)cilBodyVM.TypeSigCreatorOptions.OwnerType;
			if (picker.GetDnlibType(dnSpy_AsmEditor_Resources.Pick_Method, new FlagsDocumentTreeNodeFilter(VisibleMembersFlags.MethodDef), op, cilBodyVM.OwnerModule) is IMethod method)
				opvm.Other = method;
		}

		void AddMethodMemberRef(InstructionOperandVM opvm) {
			MemberRef? mr = opvm.Other as MemberRef;
			var md = opvm.Other as MethodDef;
			if (opvm.Other is MethodSpec ms) {
				mr = ms.Method as MemberRef;
				md = ms.Method as MethodDef;
			}
			if (md is not null)
				mr = cilBodyVM.OwnerModule.Import(md);
			if (mr is not null && mr.MethodSig is null)
				mr = null;
			AddMemberRef(opvm, mr, false);
		}

		void AddMemberRef(InstructionOperandVM opvm, MemberRef? mr, bool isField) {
			var opts = mr is null ? new MemberRefOptions() : new MemberRefOptions(mr);
			MemberRefVM? vm = new MemberRefVM(opts, cilBodyVM.TypeSigCreatorOptions, isField);
			var creator = new EditMemberRef(Window.GetWindow(listBox));
			var title = isField ? dnSpy_AsmEditor_Resources.EditFieldMemberRef : dnSpy_AsmEditor_Resources.EditMethodMemberRef;
			vm = creator.Edit(title, vm);
			if (vm is null)
				return;

			opvm.Other = vm.CreateMemberRefOptions().Create(cilBodyVM.OwnerModule);
		}

		void AddMethodSpec(InstructionOperandVM opvm) {
			var ms = opvm.Other as MethodSpec;
			var opts = ms is null ? new MethodSpecOptions() : new MethodSpecOptions(ms);
			MethodSpecVM? vm = new MethodSpecVM(opts, cilBodyVM.TypeSigCreatorOptions);
			var creator = new EditMethodSpec(Window.GetWindow(listBox));
			vm = creator.Edit(dnSpy_AsmEditor_Resources.EditMethodSpec, vm);
			if (vm is null)
				return;

			opvm.Other = vm.CreateMethodSpecOptions().Create(cilBodyVM.OwnerModule);
		}

		void AddType(InstructionOperandVM opvm) {
			var picker = new DnlibTypePicker(Window.GetWindow(listBox));
			var op = opvm.Other as ITypeDefOrRef ?? (object?)cilBodyVM.TypeSigCreatorOptions.OwnerType;
			if (picker.GetDnlibType(dnSpy_AsmEditor_Resources.Pick_Type, new FlagsDocumentTreeNodeFilter(VisibleMembersFlags.TypeDef), op, cilBodyVM.OwnerModule) is ITypeDefOrRef type)
				opvm.Other = type;
		}

		void AddTypeSpec(InstructionOperandVM opvm) {
			var creator = new TypeSigCreator(Window.GetWindow(listBox));
			var opts = cilBodyVM.TypeSigCreatorOptions.Clone(dnSpy_AsmEditor_Resources.CreateTypeSpec);
			var newSig = creator.Create(opts, (opvm.Other as ITypeDefOrRef).ToTypeSig(), out bool canceled);
			if (canceled)
				return;

			opvm.Other = newSig.ToTypeDefOrRef();
		}

		void EditMethodSig(InstructionOperandVM opvm) {
			var creator = new CreateMethodPropertySig(Window.GetWindow(listBox));
			var opts = new MethodSigCreatorOptions(cilBodyVM.TypeSigCreatorOptions.Clone(dnSpy_AsmEditor_Resources.CreateMethodSig));
			opts.CanHaveSentinel = true;
			var sig = (MethodSig?)creator.Create(opts, opvm.Other as MethodSig);
			if (sig is not null)
				opvm.Other = sig;
		}

		void EditSwitchOperand(InstructionOperandVM opvm) {
			var data = new SwitchOperandVM(cilBodyVM.InstructionsListVM, opvm.Other as InstructionVM[]);
			var win = new SwitchOperandDlg();
			win.DataContext = data;
			win.Owner = Window.GetWindow(listBox) ?? Application.Current.MainWindow;
			if (win.ShowDialog() != true)
				return;

			opvm.Other = data.GetSwitchList();
		}

		void IEditOperand.Edit(object? parameter, InstructionOperandVM opvm) {
			MenuCommandFlags flags;
			switch (opvm.InstructionOperandType) {
			case InstructionOperandType.Field:
				flags = MenuCommandFlags.FieldDef | MenuCommandFlags.FieldMemberRef;
				ShowMenu(parameter, opvm, flags);
				break;

			case InstructionOperandType.Method:
				flags = MenuCommandFlags.MethodDef | MenuCommandFlags.MethodMemberRef | MenuCommandFlags.MethodSpec;
				ShowMenu(parameter, opvm, flags);
				break;

			case InstructionOperandType.Token:
				flags = MenuCommandFlags.FieldDef | MenuCommandFlags.FieldMemberRef |
						MenuCommandFlags.MethodDef | MenuCommandFlags.MethodMemberRef | MenuCommandFlags.MethodSpec |
						MenuCommandFlags.TypeDef | MenuCommandFlags.TypeRef | MenuCommandFlags.TypeSpec;
				ShowMenu(parameter, opvm, flags);
				break;

			case InstructionOperandType.Type:
				flags = MenuCommandFlags.TypeDef | MenuCommandFlags.TypeRef | MenuCommandFlags.TypeSpec;
				ShowMenu(parameter, opvm, flags);
				break;

			case InstructionOperandType.MethodSig:
				EditMethodSig(opvm);
				break;

			case InstructionOperandType.SwitchTargets:
				EditSwitchOperand(opvm);
				break;

			default:
				throw new InvalidOperationException();
			}
		}

		public void Select(IEnumerable<InstructionVM> items) {
			var instrs = items.ToArray();
			if (instrs.Length == 0)
				return;
			listBox.SelectedItems.Clear();
			foreach (var instr in instrs)
				listBox.SelectedItems.Add(instr);

			// Select the last one because the selected item is usually the last visible item in the view.
			listBox.ScrollIntoView(instrs[instrs.Length - 1]);
		}

		protected override bool CanUseClipboardData(InstructionVM[] data, bool fromThisInstance) => true;

		protected override InstructionVM[] BeforeCopyingData(InstructionVM[] data, bool fromThisInstance) {
			if (fromThisInstance)
				return data;
			var newData = new InstructionVM[data.Length];
			for (int i = 0; i < data.Length; i++)
				newData[i] = data[i].Import(cilBodyVM.OwnerModule);
			return newData;
		}

		protected override void AfterCopyingData(InstructionVM[] data, InstructionVM[] origData, bool fromThisInstance) {
			var dict = new Dictionary<uint, InstructionVM>();
			for (int i = 0; i < data.Length; i++) {
				if (origData[i] == InstructionVM.Null)
					continue;
				Debug.Assert(!dict.ContainsKey(origData[i].Offset));
				dict[origData[i].Offset] = data[i];
			}
			var createdLocals = new Dictionary<LocalVM, LocalVM>();
			createdLocals[LocalVM.Null] = LocalVM.Null;

			// Need to fix references to instructions and locals
			InstructionVM oldInstr;
			InstructionVM? newInstr;
			for (int i = 0; i < data.Length; i++) {
				var instr = data[i];
				var origInstr = origData[i];
				var op = instr.InstructionOperandVM;
				switch (op.InstructionOperandType) {
				case MethodBody.InstructionOperandType.BranchTarget:
					oldInstr = (origInstr.InstructionOperandVM.OperandListItem as InstructionVM) ?? InstructionVM.Null;
					if (oldInstr == InstructionVM.Null || !dict.TryGetValue(oldInstr.Offset, out newInstr))
						newInstr = fromThisInstance ? oldInstr : InstructionVM.Null;
					op.OperandListItem = newInstr;
					break;

				case MethodBody.InstructionOperandType.SwitchTargets:
					var oldInstrs = (origInstr.InstructionOperandVM.Other as InstructionVM[]) ?? Array.Empty<InstructionVM>();
					var newInstrs = new InstructionVM[oldInstrs.Length];
					for (int j = 0; j < oldInstrs.Length; j++) {
						oldInstr = oldInstrs[j] ?? InstructionVM.Null;
						if (oldInstr == InstructionVM.Null || !dict.TryGetValue(oldInstr.Offset, out newInstr))
							newInstr = fromThisInstance ? oldInstr : InstructionVM.Null;
						newInstrs[j] = newInstr;
					}
					op.Other = newInstrs;
					break;

				case MethodBody.InstructionOperandType.Local:
					if (!fromThisInstance) {
						var oldLocal = (origInstr.InstructionOperandVM.OperandListItem as LocalVM) ?? LocalVM.Null;
						if (!createdLocals.TryGetValue(oldLocal, out var newLocal)) {
							newLocal = oldLocal.Import(cilBodyVM.TypeSigCreatorOptions, cilBodyVM.OwnerModule);
							cilBodyVM.LocalsListVM.Add(newLocal);
							createdLocals.Add(oldLocal, newLocal);
						}
						op.OperandListItem = newLocal;
					}
					break;

				case MethodBody.InstructionOperandType.Parameter:
					if (!fromThisInstance) {
						// Can't reference a parameter in another method
						op.OperandListItem = BodyUtils.NullParameter;
					}
					break;
				}
			}
		}
	}
}
