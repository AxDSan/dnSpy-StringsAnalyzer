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
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.DnlibDialogs;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class CilBodyVM : ViewModelBase {
		readonly CilBodyOptions origOptions;

		public ISelectItems<InstructionVM> SelectItems {
			set => selectItems = value;
		}
		ISelectItems<InstructionVM>? selectItems;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public ICommand SimplifyAllInstructionsCommand => new RelayCommand(a => SimplifyAllInstructions(), a => SimplifyAllInstructionsCanExecute());
		public ICommand OptimizeAllInstructionsCommand => new RelayCommand(a => OptimizeAllInstructions(), a => OptimizeAllInstructionsCanExecute());
		public ICommand ReplaceInstructionWithNopCommand => new RelayCommand(a => ReplaceInstructionWithNop((InstructionVM[])a!), a => ReplaceInstructionWithNopCanExecute((InstructionVM[])a!));
		public ICommand ReplaceInstructionWithMultipleNopsCommand => new RelayCommand(a => ReplaceInstructionWithMultipleNops((InstructionVM[])a!), a => ReplaceInstructionWithMultipleNopsCanExecute((InstructionVM[])a!));
		public ICommand InvertBranchCommand => new RelayCommand(a => InvertBranch((InstructionVM[])a!), a => InvertBranchCanExecute((InstructionVM[])a!));
		public ICommand ConvertBranchToUnconditionalBranchCommand => new RelayCommand(a => ConvertBranchToUnconditionalBranch((InstructionVM[])a!), a => ConvertBranchToUnconditionalBranchCanExecute((InstructionVM[])a!));
		public ICommand RemoveInstructionAndAddPopsCommand => new RelayCommand(a => RemoveInstructionAndAddPops((InstructionVM[])a!), a => RemoveInstructionAndAddPopsCanExecute((InstructionVM[])a!));

		public bool KeepOldMaxStack {
			get => keepOldMaxStack;
			set {
				if (keepOldMaxStack != value) {
					keepOldMaxStack = value;
					OnPropertyChanged(nameof(KeepOldMaxStack));
				}
			}
		}
		bool keepOldMaxStack;

		public bool InitLocals {
			get => initLocals;
			set {
				if (initLocals != value) {
					initLocals = value;
					OnPropertyChanged(nameof(InitLocals));
				}
			}
		}
		bool initLocals;

		public UInt16VM MaxStack { get; }
		public UInt32VM LocalVarSigTok { get; }
		public ByteVM HeaderSize { get; }
		public UInt32VM HeaderRVA { get; }
		public UInt64VM HeaderFileOffset { get; }
		public UInt32VM RVA { get; }
		public UInt64VM FileOffset { get; }

		sealed class LocalsIndexObservableCollection : IndexObservableCollection<LocalVM> {
			readonly CilBodyVM owner;

			public LocalsIndexObservableCollection(CilBodyVM owner, Func<LocalVM> createNewItem)
				: base(createNewItem) => this.owner = owner;

			protected override void ClearItems() {
				var old_disable_UpdateLocalOperands = owner.disable_UpdateLocalOperands;
				var old_disable_UpdateBranchOperands = owner.disable_UpdateBranchOperands;
				try {
					owner.disable_UpdateLocalOperands = true;
					owner.disable_UpdateBranchOperands = true;

					base.ClearItems();
				}
				finally {
					owner.disable_UpdateLocalOperands = old_disable_UpdateLocalOperands;
					owner.disable_UpdateBranchOperands = old_disable_UpdateBranchOperands;
				}
				owner.UpdateLocalOperands();
				owner.UpdateBranchOperands();
			}
		}

		public IndexObservableCollection<LocalVM> LocalsListVM { get; }
		public IndexObservableCollection<InstructionVM> InstructionsListVM { get; }
		public IndexObservableCollection<ExceptionHandlerVM> ExceptionHandlersListVM { get; }
		internal ModuleDef OwnerModule { get; }
		internal TypeSigCreatorOptions TypeSigCreatorOptions { get; }

		readonly MethodDef ownerMethod;

		public CilBodyVM(CilBodyOptions options, ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef ownerType, MethodDef ownerMethod, bool initialize) {
			OwnerModule = ownerModule;
			this.ownerMethod = ownerMethod;
			origOptions = options;

			TypeSigCreatorOptions = new TypeSigCreatorOptions(ownerModule, decompilerService) {
				CanAddGenericTypeVar = ownerType.HasGenericParameters,
				CanAddGenericMethodVar = ownerMethod.MethodSig.GetGenParamCount() > 0,
				OwnerType = ownerType,
				OwnerMethod = ownerMethod,
			};

			LocalsListVM = new LocalsIndexObservableCollection(this, () => new LocalVM(TypeSigCreatorOptions, new LocalOptions(new Local(ownerModule.CorLibTypes.Int32))));
			InstructionsListVM = new IndexObservableCollection<InstructionVM>(() => CreateInstructionVM());
			ExceptionHandlersListVM = new IndexObservableCollection<ExceptionHandlerVM>(() => new ExceptionHandlerVM(TypeSigCreatorOptions, new ExceptionHandlerOptions()));
			LocalsListVM.UpdateIndexesDelegate = LocalsUpdateIndexes;
			InstructionsListVM.UpdateIndexesDelegate = InstructionsUpdateIndexes;
			ExceptionHandlersListVM.UpdateIndexesDelegate = ExceptionHandlersUpdateIndexes;
			InstructionsListVM.CollectionChanged += InstructionsListVM_CollectionChanged;
			LocalsListVM.CollectionChanged += LocalsListVM_CollectionChanged;
			ExceptionHandlersListVM.CollectionChanged += ExceptionHandlersListVM_CollectionChanged;
			MaxStack = new UInt16VM(a => CallHasErrorUpdated());
			LocalVarSigTok = new UInt32VM(a => CallHasErrorUpdated());
			HeaderSize = new ByteVM(a => CallHasErrorUpdated());
			HeaderRVA = new UInt32VM(a => CallHasErrorUpdated());
			HeaderFileOffset = new UInt64VM(a => CallHasErrorUpdated());
			RVA = new UInt32VM(a => CallHasErrorUpdated());
			FileOffset = new UInt64VM(a => CallHasErrorUpdated());

			if (initialize)
				Reinitialize();
		}

		public void Select(uint[] offsets) {
			if (selectItems is null)
				throw new InvalidOperationException();
			if (offsets is null || offsets.Length == 0)
				return;

			var dict = InstructionsListVM.ToDictionary(a => a.Offset);
			var instrs = offsets.Select(a => {
				InstructionVM? instr;
				dict.TryGetValue(a, out instr);
				return instr;
			}).OfType<InstructionVM>().Distinct().ToArray();
			if (instrs.Length == 0)
				return;

			selectItems.Select(instrs);
		}

		void LocalsUpdateIndexes(int i) {
			var old_disable_UpdateLocalOperands = disable_UpdateLocalOperands;
			var old_disable_UpdateBranchOperands = disable_UpdateBranchOperands;
			var old = DisableHasError();
			try {
				disable_UpdateLocalOperands = true;
				disable_UpdateBranchOperands = true;
				LocalsListVM.DefaultUpdateIndexes(i);
				disable_UpdateLocalOperands = old_disable_UpdateLocalOperands;
				disable_UpdateBranchOperands = old_disable_UpdateBranchOperands;

				UpdateLocalOperands();
				UpdateBranchOperands();
			}
			finally {
				disable_UpdateLocalOperands = old_disable_UpdateLocalOperands;
				disable_UpdateBranchOperands = old_disable_UpdateBranchOperands;
				RestoreHasError(old);
			}
		}

		void InstructionsUpdateIndexes(int i) {
			var old_disable_UpdateLocalOperands = disable_UpdateLocalOperands;
			var old_disable_UpdateBranchOperands = disable_UpdateBranchOperands;
			var old = DisableHasError();
			try {
				disable_UpdateLocalOperands = true;
				disable_UpdateBranchOperands = true;
				InstructionsListVM.UpdateIndexesOffsets(i);
				disable_UpdateLocalOperands = old_disable_UpdateLocalOperands;
				disable_UpdateBranchOperands = old_disable_UpdateBranchOperands;

				UpdateLocalOperands();
				UpdateBranchOperands();
				UpdateParameterOperands();
				UpdateExceptionHandlerInstructionReferences();
			}
			finally {
				disable_UpdateLocalOperands = old_disable_UpdateLocalOperands;
				disable_UpdateBranchOperands = old_disable_UpdateBranchOperands;
				RestoreHasError(old);
			}
		}

		void ExceptionHandlersUpdateIndexes(int i) {
			var old = DisableHasError();
			try {
				ExceptionHandlersListVM.DefaultUpdateIndexes(i);
			}
			finally {
				RestoreHasError(old);
			}
		}

		InstructionVM CreateInstructionVM(Code code = Code.Nop) => new InstructionVM() { Code = code };

		void UpdateExceptionHandlerInstructionReferences() {
			foreach (var eh in ExceptionHandlersListVM)
				eh.InstructionChanged(InstructionsListVM);
		}

		void UpdateBranchOperands() {
			if (disable_UpdateBranchOperands)
				return;
			foreach (var instr in InstructionsListVM) {
				if (instr.InstructionOperandVM.InstructionOperandType == InstructionOperandType.BranchTarget)
					instr.InstructionOperandVM.BranchOperandChanged(InstructionsListVM);
				else if (instr.InstructionOperandVM.InstructionOperandType == InstructionOperandType.SwitchTargets)
					instr.InstructionOperandVM.SwitchOperandChanged();
			}
		}
		bool disable_UpdateBranchOperands = false;

		void UpdateLocalOperands() {
			if (disable_UpdateLocalOperands)
				return;
			foreach (var instr in InstructionsListVM) {
				if (instr.InstructionOperandVM.InstructionOperandType == InstructionOperandType.Local)
					instr.InstructionOperandVM.LocalOperandChanged(LocalsListVM);
			}
		}
		bool disable_UpdateLocalOperands = false;

		void UpdateParameterOperands() {
			foreach (var instr in InstructionsListVM) {
				if (instr.InstructionOperandVM.InstructionOperandType == InstructionOperandType.Parameter)
					instr.InstructionOperandVM.ParameterOperandChanged(ownerMethod.Parameters);
			}
		}

		void SimplifyAllInstructions() {
			var old1 = InstructionsListVM.DisableAutoUpdateProps;
			var old2 = DisableHasError();
			try {
				InstructionsListVM.DisableAutoUpdateProps = true;

				// Absolutely required for speed. The UI list is virtualized so most of the
				// instructions won't have a handler to notify, only the visible ones and a few
				// nearby instructions will.
				UninstallInstructionHandlers(InstructionsListVM);

				InstructionsListVM.SimplifyMacros(LocalsListVM, ownerMethod.Parameters);
			}
			finally {
				InstallInstructionHandlers(InstructionsListVM);
				RestoreHasError(old2);
				InstructionsListVM.DisableAutoUpdateProps = old1;
			}
			InstructionsUpdateIndexes(0);
		}

		bool SimplifyAllInstructionsCanExecute() => InstructionsListVM.Count > 0;

		void OptimizeAllInstructions() {
			var old1 = InstructionsListVM.DisableAutoUpdateProps;
			var old2 = DisableHasError();
			try {
				InstructionsListVM.DisableAutoUpdateProps = true;

				// Speed optimization, see comment in SimplifyAllInstructions()
				UninstallInstructionHandlers(InstructionsListVM);

				InstructionsListVM.OptimizeMacros();
			}
			finally {
				InstallInstructionHandlers(InstructionsListVM);
				RestoreHasError(old2);
				InstructionsListVM.DisableAutoUpdateProps = old1;
			}
			InstructionsUpdateIndexes(0);
		}

		bool OptimizeAllInstructionsCanExecute() => InstructionsListVM.Count > 0;

		void ReplaceInstructionWithNop(InstructionVM[] instrs) {
			var old1 = InstructionsListVM.DisableAutoUpdateProps;
			var old2 = DisableHasError();
			try {
				InstructionsListVM.DisableAutoUpdateProps = true;

				// Speed optimization, see comment in SimplifyAllInstructions()
				UninstallInstructionHandlers(instrs);

				foreach (var instr in instrs)
					instr.Code = Code.Nop;
			}
			finally {
				InstallInstructionHandlers(instrs);
				RestoreHasError(old2);
				InstructionsListVM.DisableAutoUpdateProps = old1;
			}
			InstructionsUpdateIndexes(0);
		}

		bool ReplaceInstructionWithNopCanExecute(InstructionVM[] instrs) => instrs.Any(a => a.Code != Code.Nop);

		void ReplaceInstructionWithMultipleNops(InstructionVM[] instrs) {
			var old1 = InstructionsListVM.DisableAutoUpdateProps;
			var old2 = DisableHasError();
			try {
				InstructionsListVM.DisableAutoUpdateProps = true;

				// Speed optimization, see comment in SimplifyAllInstructions()
				UninstallInstructionHandlers(instrs);

				Array.Sort(instrs, (a, b) => b.Index - a.Index);
				foreach (var instr in instrs) {
					var size = instr.GetSize();
					for (int i = 1; i < size; i++)
						InstructionsListVM.Insert(instr.Index + i, CreateInstructionVM(Code.Nop));
					instr.Code = Code.Nop;
				}
			}
			finally {
				InstallInstructionHandlers(instrs);
				RestoreHasError(old2);
				InstructionsListVM.DisableAutoUpdateProps = old1;
			}
			InstructionsUpdateIndexes(0);
		}

		bool ReplaceInstructionWithMultipleNopsCanExecute(InstructionVM[] instrs) => instrs.Any(a => a.Code != Code.Nop);

		void InvertBranch(InstructionVM[] instrs) {
			foreach (var instr in instrs) {
				var code = InvertBcc(instr.Code);
				if (code is not null)
					instr.Code = code.Value;
			}
		}

		bool InvertBranchCanExecute(InstructionVM[] instrs) => instrs.Any(a => InvertBcc(a.Code) is not null);

		static Code? InvertBcc(Code code) {
			switch (code) {
			case Code.Beq:		return Code.Bne_Un;
			case Code.Beq_S:	return Code.Bne_Un_S;
			case Code.Bge:		return Code.Blt;
			case Code.Bge_S:	return Code.Blt_S;
			case Code.Bge_Un:	return Code.Blt_Un;
			case Code.Bge_Un_S:	return Code.Blt_Un_S;
			case Code.Bgt:		return Code.Ble;
			case Code.Bgt_S:	return Code.Ble_S;
			case Code.Bgt_Un:	return Code.Ble_Un;
			case Code.Bgt_Un_S:	return Code.Ble_Un_S;
			case Code.Ble:		return Code.Bgt;
			case Code.Ble_S:	return Code.Bgt_S;
			case Code.Ble_Un:	return Code.Bgt_Un;
			case Code.Ble_Un_S:	return Code.Bgt_Un_S;
			case Code.Blt:		return Code.Bge;
			case Code.Blt_S:	return Code.Bge_S;
			case Code.Blt_Un:	return Code.Bge_Un;
			case Code.Blt_Un_S:	return Code.Bge_Un_S;
			case Code.Bne_Un:	return Code.Beq;
			case Code.Bne_Un_S:	return Code.Beq_S;
			case Code.Brfalse:	return Code.Brtrue;
			case Code.Brfalse_S:return Code.Brtrue_S;
			case Code.Brtrue:	return Code.Brfalse;
			case Code.Brtrue_S:	return Code.Brfalse_S;
			default:			return null;
			}
		}

		void ConvertBranchToUnconditionalBranch(InstructionVM[] instrs) {
			foreach (var instr in instrs) {
				var popCount = GetBccPopCount(instr.Code);
				if (popCount <= 0)
					continue;

				var targetOperand = instr.InstructionOperandVM.Value;
				instr.Code = Code.Pop;
				int index = instr.Index + 1;
				while (--popCount > 0)
					InstructionsListVM.Insert(index++, CreateInstructionVM(Code.Pop));
				var brInstr = CreateInstructionVM(Code.Br);
				brInstr.InstructionOperandVM.OperandListItem = targetOperand ?? InstructionVM.Null;
				InstructionsListVM.Insert(index++, brInstr);
			}
		}

		static int GetBccPopCount(Code code) {
			switch (code) {
			case Code.Beq:
			case Code.Beq_S:
			case Code.Bge:
			case Code.Bge_S:
			case Code.Bge_Un:
			case Code.Bge_Un_S:
			case Code.Bgt:
			case Code.Bgt_S:
			case Code.Bgt_Un:
			case Code.Bgt_Un_S:
			case Code.Ble:
			case Code.Ble_S:
			case Code.Ble_Un:
			case Code.Ble_Un_S:
			case Code.Blt:
			case Code.Blt_S:
			case Code.Blt_Un:
			case Code.Blt_Un_S:
			case Code.Bne_Un:
			case Code.Bne_Un_S:
				return 2;
			case Code.Brfalse:
			case Code.Brfalse_S:
			case Code.Brtrue:
			case Code.Brtrue_S:
				return 1;
			default:
				return 0;
			}
		}

		bool ConvertBranchToUnconditionalBranchCanExecute(InstructionVM[] instrs) => instrs.Any(a => GetBccPopCount(a.Code) > 0);

		void RemoveInstructionAndAddPops(InstructionVM[] instrs) {
			foreach (var instr in instrs) {
				var info = GetInstructionPops(instr);
				if (info is null)
					continue;

				var popCount = info.Value.PopCount;
				var origCode = instr.Code;
				if (origCode == Code.Callvirt && instr.Index >= 1 && InstructionsListVM[instr.Index - 1].Code == Code.Constrained)
					InstructionsListVM[instr.Index - 1].Code = Code.Nop;

				int index = instr.Index + 1;
				if (popCount == 0)
					instr.Code = Code.Nop;
				else {
					instr.Code = Code.Pop;
					while (--popCount > 0)
						InstructionsListVM.Insert(index++, CreateInstructionVM(Code.Pop));
				}

				if (info.Value.Pushes)
					AddPushDefaultValue(0, ref index, info.Value.PushType);
			}
		}

		bool RemoveInstructionAndAddPopsCanExecute(InstructionVM[] instrs) => instrs.Any(a => GetInstructionPops(a) is not null);

		readonly struct InstructionPushPopInfo {
			public readonly int PopCount;
			public readonly bool Pushes;// Needed in case there's an invalid method sig with a null type
			public readonly TypeSig? PushType;

			public InstructionPushPopInfo(int pops) {
				PopCount = pops;
				Pushes = false;
				PushType = null;
			}

			public InstructionPushPopInfo(int pops, TypeSig? pushType) {
				PopCount = pops;
				Pushes = true;
				PushType = pushType;
			}
		}

		static InstructionPushPopInfo? GetInstructionPops(InstructionVM instr) {
			var code = instr.Code;
			if (code == Code.Pop || code == Code.Nop)
				return null;
			instr.CalculateStackUsage(out int pushes, out int pops);
			if (pops < 0)
				return null;
			if (pushes == 1 && (code == Code.Call || code == Code.Callvirt || code == Code.Calli))
				return new InstructionPushPopInfo(pops, GetMethodSig(instr.InstructionOperandVM.Other).GetRetType());
			if (pushes == 1 && code == Code.Newobj) {
				var ctor = instr.InstructionOperandVM.Other as IMethod;
				return new InstructionPushPopInfo(pops, ctor is null ? null : ctor.DeclaringType.ToTypeSig());
			}
			if (pushes != 0)
				return null;
			return new InstructionPushPopInfo(pops);
		}

		void AddPushDefaultValue(int count, ref int index, TypeSig? pushType) {
			pushType = pushType.RemovePinned();
			switch (count > 10 ? ElementType.End : pushType.RemovePinnedAndModifiers().GetElementType()) {
			case ElementType.Void:
				break;

			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
				InstructionsListVM.Insert(index++, CreateInstructionVM(Code.Ldc_I4_0));
				break;

			case ElementType.I8:
			case ElementType.U8:
				InstructionsListVM.Insert(index++, CreateInstructionVM(Code.Ldc_I4_0));
				InstructionsListVM.Insert(index++, CreateInstructionVM(Code.Conv_I8));
				break;

			case ElementType.R4:
				InstructionsListVM.Insert(index++, CreateInstructionVM(Code.Ldc_R4));
				break;

			case ElementType.R8:
				InstructionsListVM.Insert(index++, CreateInstructionVM(Code.Ldc_R8));
				break;

			case ElementType.I:
				InstructionsListVM.Insert(index++, CreateInstructionVM(Code.Ldc_I4_0));
				InstructionsListVM.Insert(index++, CreateInstructionVM(Code.Conv_I));
				break;

			case ElementType.U:
			case ElementType.Ptr:
			case ElementType.FnPtr:
				InstructionsListVM.Insert(index++, CreateInstructionVM(Code.Ldc_I4_0));
				InstructionsListVM.Insert(index++, CreateInstructionVM(Code.Conv_U));
				break;

			case ElementType.ValueType:
				var td = ((ValueTypeSig)pushType!).TypeDefOrRef.ResolveTypeDef();
				if (td is not null && td.IsEnum) {
					var undType = td.GetEnumUnderlyingType().RemovePinnedAndModifiers();
					var et = undType.GetElementType();
					if ((ElementType.Boolean <= et && et <= ElementType.R8) || et == ElementType.I || et == ElementType.U) {
						AddPushDefaultValue(count + 1, ref index, undType);
						break;
					}
				}
				goto case ElementType.TypedByRef;

			case ElementType.TypedByRef:
			case ElementType.Var:
			case ElementType.MVar:
				var local = new LocalVM(TypeSigCreatorOptions, new LocalOptions(new Local(pushType!)));
				LocalsListVM.Add(local);

				var newInstr = CreateInstructionVM(Code.Ldloca);
				newInstr.InstructionOperandVM.OperandListItem = local;
				InstructionsListVM.Insert(index++, newInstr);

				newInstr = CreateInstructionVM(Code.Initobj);
				newInstr.InstructionOperandVM.Other = local.Type.ToTypeDefOrRef();
				InstructionsListVM.Insert(index++, newInstr);

				newInstr = CreateInstructionVM(Code.Ldloc);
				newInstr.InstructionOperandVM.OperandListItem = local;
				InstructionsListVM.Insert(index++, newInstr);
				break;

			case ElementType.GenericInst:
				if (((GenericInstSig)pushType!).GenericType is ValueTypeSig)
					goto case ElementType.TypedByRef;
				goto case ElementType.Class;

			case ElementType.End:
			case ElementType.String:
			case ElementType.ByRef:
			case ElementType.Class:
			case ElementType.Array:
			case ElementType.ValueArray:
			case ElementType.R:
			case ElementType.Object:
			case ElementType.SZArray:
			case ElementType.CModReqd:
			case ElementType.CModOpt:
			case ElementType.Internal:
			case ElementType.Module:
			case ElementType.Sentinel:
			case ElementType.Pinned:
			default:
				InstructionsListVM.Insert(index++, CreateInstructionVM(Code.Ldnull));
				break;
			}
		}

		static MethodBaseSig? GetMethodSig(object? operand) {
			if (operand is MethodSig msig)
				return msig;

			if (operand is MethodDef md)
				return md.MethodSig;

			if (operand is MemberRef mr) {
				var type = mr.DeclaringType;
				return GetMethodSig(type, mr.MethodSig, null);
			}

			if (operand is MethodSpec ms) {
				var type = ms.DeclaringType;
				var genMeth = ms.GenericInstMethodSig;
				var meth = ms.Method;
				return GetMethodSig(type, meth is null ? null : meth.MethodSig, genMeth is null ? null : genMeth.GenericArguments);
			}

			return null;
		}

		static MethodBaseSig? GetMethodSig(ITypeDefOrRef type, MethodSig? msig, IList<TypeSig>? methodGenArgs) {
			IList<TypeSig>? typeGenArgs = null;
			if (type is TypeSpec ts) {
				var genSig = ts.TypeSig.ToGenericInstSig();
				if (genSig is not null)
					typeGenArgs = genSig.GenericArguments;
			}
			if (typeGenArgs is null && methodGenArgs is null)
				return msig;
			return GenericArgumentResolver.Resolve(msig, typeGenArgs, methodGenArgs);
		}

		void InstructionsListVM_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
			if (e.NewItems is not null)
				InstallInstructionHandlers(e.NewItems);

			if (!InstructionsListVM.DisableAutoUpdateProps)
				CallHasErrorUpdated();
		}

		void InstallInstructionHandlers(System.Collections.IList list) {
			foreach (InstructionVM? instr in list) {
				instr!.PropertyChanged -= instr_PropertyChanged;
				instr.PropertyChanged += instr_PropertyChanged;
				instr.InstructionOperandVM.PropertyChanged -= InstructionOperandVM_PropertyChanged;
				instr.InstructionOperandVM.PropertyChanged += InstructionOperandVM_PropertyChanged;
			}
		}

		void UninstallInstructionHandlers(IList<InstructionVM> list) {
			foreach (var instr in list) {
				instr.PropertyChanged -= instr_PropertyChanged;
				instr.InstructionOperandVM.PropertyChanged -= InstructionOperandVM_PropertyChanged;
			}
		}

		void instr_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(InstructionVM.HasError))
				CallHasErrorUpdated();
			else if (!InstructionsListVM.DisableAutoUpdateProps && e.PropertyName == nameof(InstructionVM.Code))
				InstructionsUpdateIndexes(0);
		}

		void InstructionOperandVM_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (!disableInstrOpUpdate && e.PropertyName == "Modified") {
				try {
					disableInstrOpUpdate = true;
					InstructionsUpdateIndexes(0);
				}
				finally {
					disableInstrOpUpdate = false;
				}
			}
			CallHasErrorUpdated();
		}
		bool disableInstrOpUpdate = false;

		void LocalsListVM_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
			if (e.NewItems is not null) {
				foreach (LocalVM? local in e.NewItems) {
					local!.PropertyChanged -= local_PropertyChanged;
					local.PropertyChanged += local_PropertyChanged;
				}
			}

			if (!LocalsListVM.DisableAutoUpdateProps)
				CallHasErrorUpdated();
		}

		void local_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(LocalVM.HasError))
				CallHasErrorUpdated();
			if (!LocalsListVM.DisableAutoUpdateProps && (e.PropertyName == nameof(LocalVM.Index) || e.PropertyName == nameof(LocalVM.Name))) {
				UpdateLocalOperands();
				UpdateBranchOperands(); // branches whose target instruction is a ldloc/stloc instruction
			}
		}

		void ExceptionHandlersListVM_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
			if (e.NewItems is not null) {
				foreach (ExceptionHandlerVM? eh in e.NewItems) {
					eh!.PropertyChanged -= eh_PropertyChanged;
					eh.PropertyChanged += eh_PropertyChanged;
				}
			}

			if (!ExceptionHandlersListVM.DisableAutoUpdateProps) {
				if (e.Action == NotifyCollectionChangedAction.Add) {
					if (e.NewItems is not null) {
						foreach (ExceptionHandlerVM? eh in e.NewItems)
							eh!.InstructionChanged(InstructionsListVM);
					}
				}

				CallHasErrorUpdated();
			}
		}

		void eh_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(ExceptionHandlerVM.HasError))
				CallHasErrorUpdated();
		}

		void Reinitialize() => InitializeFrom(origOptions);
		public CilBodyOptions CreateCilBodyOptions() => CopyTo(new CilBodyOptions());

		public void InitializeFrom(CilBodyOptions options) {
			try {
				LocalsListVM.DisableAutoUpdateProps = true;
				InstructionsListVM.DisableAutoUpdateProps = true;
				ExceptionHandlersListVM.DisableAutoUpdateProps = true;
				HasError_disabled = true;

				var ops = new Dictionary<object, object>();
				foreach (var local in options.Locals)
					ops.Add(local, new LocalVM(TypeSigCreatorOptions, new LocalOptions(local)));
				foreach (var instr in options.Instructions)
					ops.Add(instr, new InstructionVM());
				foreach (var instr in options.Instructions)
					((InstructionVM)ops[instr]).Initialize(new InstructionOptions(ops, instr));

				KeepOldMaxStack = options.KeepOldMaxStack;
				InitLocals = options.InitLocals;
				MaxStack.Value = options.MaxStack;
				LocalVarSigTok.Value = options.LocalVarSigTok;
				HeaderSize.Value = options.HeaderSize;
				HeaderRVA.Value = (uint)options.HeaderRVA;
				HeaderFileOffset.Value = unchecked((ulong)(long)options.HeaderFileOffset);
				RVA.Value = (uint)options.RVA;
				FileOffset.Value = unchecked((ulong)(long)options.FileOffset);
				LocalsListVM.Clear();
				LocalsListVM.AddRange(options.Locals.Select(a => (LocalVM)ops[a]));
				InstructionsListVM.Clear();
				InstructionsListVM.AddRange(options.Instructions.Select(a => (InstructionVM)ops[a]));
				ExceptionHandlersListVM.Clear();
				ExceptionHandlersListVM.AddRange(options.ExceptionHandlers.Select(a => new ExceptionHandlerVM(TypeSigCreatorOptions, new ExceptionHandlerOptions(ops, a))));
			}
			finally {
				HasError_disabled = false;
				LocalsListVM.DisableAutoUpdateProps = false;
				InstructionsListVM.DisableAutoUpdateProps = false;
				ExceptionHandlersListVM.DisableAutoUpdateProps = false;
			}
			LocalsUpdateIndexes(0);
			InstructionsUpdateIndexes(0);
			ExceptionHandlersUpdateIndexes(0);
			HasErrorUpdated();
		}

		public CilBodyOptions CopyTo(CilBodyOptions options) {
			var ops = new Dictionary<object, object>();
			foreach (var vm in LocalsListVM)
				ops.Add(vm, vm.CreateLocalOptions().Create());
			foreach (var vm in InstructionsListVM)
				ops.Add(vm, new Instruction());

			options.KeepOldMaxStack = KeepOldMaxStack;
			options.InitLocals = InitLocals;
			options.MaxStack = MaxStack.Value;
			options.LocalVarSigTok = LocalVarSigTok.Value;
			options.HeaderSize = HeaderSize.Value;
			options.Locals.Clear();
			options.Locals.AddRange(LocalsListVM.Select(a => (Local)ops[a]));
			options.Instructions.Clear();
			options.Instructions.AddRange(InstructionsListVM.Select(a => a.CreateInstructionOptions().CopyTo(ops, (Instruction)ops[a])));
			options.ExceptionHandlers.Clear();
			options.ExceptionHandlers.AddRange(ExceptionHandlersListVM.Select(a => a.CreateExceptionHandlerOptions().Create(ops)));
			return options;
		}

		public override bool HasError {
			get {
				if (HasError_disabled)
					return false;
				return
					MaxStack.HasError ||
					LocalVarSigTok.HasError ||
					LocalsListVM.Any(a => a.HasError) ||
					InstructionsListVM.Any(a => a.HasError) ||
					ExceptionHandlersListVM.Any(a => a.HasError);
			}
		}

		bool HasError_disabled = false;
		bool DisableHasError() {
			var old = HasError_disabled;
			HasError_disabled = true;
			return old;
		}
		void RestoreHasError(bool old) {
			HasError_disabled = old;
			if (!HasError_disabled)
				HasErrorUpdated();
		}
		void CallHasErrorUpdated() {
			if (!HasError_disabled)
				HasErrorUpdated();
		}
	}
}
