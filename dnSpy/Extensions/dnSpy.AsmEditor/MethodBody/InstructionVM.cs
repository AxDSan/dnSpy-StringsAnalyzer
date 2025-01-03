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
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using dnSpy.AsmEditor.Commands;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class InstructionVM : ViewModelBase, IIndexedItem {
		public static readonly InstructionVM Null = new InstructionVM(false);
		InstructionOptions? origOptions;

		static readonly Code[] codeList;

		static InstructionVM() {
			var hash = new HashSet<OpCode>();
			foreach (var c in typeof(Code).GetFields()) {
				if (!c.IsLiteral)
					continue;
				hash.Add(((Code)c.GetValue(null)!).ToOpCode());
			}
			var remove = new List<OpCode> { OpCodes.UNKNOWN1, OpCodes.UNKNOWN2 };
			foreach (var r in remove)
				hash.Remove(r);
			var list = new List<OpCode>(hash);
			list.Sort((a, b) => StringComparer.InvariantCultureIgnoreCase.Compare(a.Name, b.Name));
			list.AddRange(remove);
			codeList = list.Select(a => a.Code).ToArray();
		}

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());

		public int Index {
			get => index;
			set {
				if (index != value) {
					index = value;
					OnPropertyChanged(nameof(Index));
				}
			}
		}
		int index;

		public uint Offset {
			get => offset;
			set {
				if (offset != value) {
					offset = value;
					OnPropertyChanged(nameof(Offset));
				}
			}
		}
		uint offset;

		public ListVM<Code> CodeVM { get; }

		public Code Code {
			get => CodeVM.SelectedItem;
			set => CodeVM.SelectedItem = value;
		}

		public InstructionOperandVM InstructionOperandVM { get; }

		public SequencePoint? SequencePoint {
			get => sequencePoint;
			set {
				if (sequencePoint != value) {
					sequencePoint = value;
					OnPropertyChanged(nameof(SequencePoint));
				}
			}
		}
		SequencePoint? sequencePoint;

		InstructionVM(bool dummy) {
			InstructionOperandVM = null!;
			CodeVM = null!;
		}

		public InstructionVM() {
			InstructionOperandVM = new InstructionOperandVM();
			InstructionOperandVM.PropertyChanged += (a, b) => HasErrorUpdated();
			CodeVM = new ListVM<Code>(codeList, (a, b) => OnCodeUpdated());
		}

		void OnCodeUpdated() {
			InstructionOperandVM.UpdateOperandType(Code);
			OnPropertyChanged(nameof(Code));
			HasErrorUpdated();
		}

		// Create an Instruction that we can use to call useful Instruction methods
		Instruction GetTempInstruction() {
			var opCode = Code.ToOpCode();
			switch (InstructionOperandVM.InstructionOperandType) {
			case InstructionOperandType.None:
				return new Instruction(opCode);

			case InstructionOperandType.SByte:
			case InstructionOperandType.Byte:
			case InstructionOperandType.Int32:
			case InstructionOperandType.Int64:
			case InstructionOperandType.Single:
			case InstructionOperandType.Double:
			case InstructionOperandType.String:
				if (!InstructionOperandVM.Text!.HasError)
					return new Instruction(opCode, InstructionOperandVM.Text.ObjectValue);
				return new Instruction(opCode);

			case InstructionOperandType.Field:
			case InstructionOperandType.Method:
			case InstructionOperandType.Token:
			case InstructionOperandType.Type:
			case InstructionOperandType.MethodSig:
				return new Instruction(opCode, InstructionOperandVM.Other);

			case InstructionOperandType.SwitchTargets:
				var list = InstructionOperandVM.Other as IList<InstructionVM>;
				if (list is not null)
					return new Instruction(opCode, new Instruction[list.Count]);
				return new Instruction(opCode, Array.Empty<Instruction>());

			case InstructionOperandType.BranchTarget:
				return new Instruction(opCode, new Instruction());

			case InstructionOperandType.Local:
				return new Instruction(opCode, new Local(null));

			case InstructionOperandType.Parameter:
				return new Instruction(opCode, new Parameter(0));

			default:
				throw new InvalidOperationException();
			}
		}

		public void CalculateStackUsage(out int pushes, out int pops) =>
			GetTempInstruction().CalculateStackUsage(out pushes, out pops);

		public int GetSize() {
			var opCode = Code.ToOpCode();
			switch (opCode.OperandType) {
			case OperandType.InlineBrTarget:
			case OperandType.InlineField:
			case OperandType.InlineI:
			case OperandType.InlineMethod:
			case OperandType.InlineSig:
			case OperandType.InlineString:
			case OperandType.InlineTok:
			case OperandType.InlineType:
			case OperandType.ShortInlineR:
				return opCode.Size + 4;

			case OperandType.InlineI8:
			case OperandType.InlineR:
				return opCode.Size + 8;

			case OperandType.InlineNone:
			case OperandType.InlinePhi:
			default:
				return opCode.Size;

			case OperandType.InlineSwitch:
				var targets = InstructionOperandVM.Value as System.Collections.IList;
				return opCode.Size + 4 + (targets is null ? 0 : targets.Count * 4);

			case OperandType.InlineVar:
				return opCode.Size + 2;

			case OperandType.ShortInlineBrTarget:
			case OperandType.ShortInlineI:
			case OperandType.ShortInlineVar:
				return opCode.Size + 1;
			}
		}

		public void Initialize(InstructionOptions options) {
			origOptions = options;
			Reinitialize();
		}

		void Reinitialize() => InitializeFrom(origOptions!);
		public InstructionOptions CreateInstructionOptions() => CopyTo(new InstructionOptions());

		public void InitializeFrom(InstructionOptions options) {
			Code = options.Code;
			InstructionOperandVM.WriteValue(Code, options.Operand);
			SequencePoint = options.SequencePoint;
		}

		public InstructionOptions CopyTo(InstructionOptions options) {
			options.Code = Code;
			options.Operand = InstructionOperandVM.Value;
			options.SequencePoint = SequencePoint;
			return options;
		}

		public override bool HasError => InstructionOperandVM.HasError;

		public IIndexedItem Clone() {
			var instr = new InstructionVM();

			instr.Code = Code;
			instr.InstructionOperandVM.InitializeFrom(InstructionOperandVM);
			instr.SequencePoint = SequencePoint;
			instr.Offset = offset;

			return instr;
		}

		public InstructionVM Import(ModuleDef ownerModule) {
			var instr = new InstructionVM();
			instr.Code = Code;
			instr.InstructionOperandVM.ImportFrom(ownerModule, InstructionOperandVM);
			instr.Offset = offset;
			return instr;
		}
	}
}
