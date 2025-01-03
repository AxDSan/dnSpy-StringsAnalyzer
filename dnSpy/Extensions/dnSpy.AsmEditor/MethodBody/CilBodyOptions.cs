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

using System.Collections.Generic;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using dnlib.IO;
using dnlib.PE;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class CilBodyOptions {
		public bool KeepOldMaxStack;
		public bool InitLocals;
		public byte HeaderSize;
		public ushort MaxStack;
		public uint LocalVarSigTok;
		public RVA HeaderRVA;
		public FileOffset HeaderFileOffset;
		public RVA RVA;
		public FileOffset FileOffset;
		public List<Instruction> Instructions = new List<Instruction>();
		public List<ExceptionHandler> ExceptionHandlers = new List<ExceptionHandler>();
		public List<Local> Locals = new List<Local>();
		public PdbMethod? PdbMethod;//TODO: Use this

		public CilBodyOptions() {
		}

		public CilBodyOptions(CilBody body, RVA headerRva, FileOffset headerFileOffset, RVA rva, FileOffset fileOffset) {
			KeepOldMaxStack = body.KeepOldMaxStack;
			InitLocals = body.InitLocals;
			HeaderSize = body.HeaderSize;
			MaxStack = body.MaxStack;
			LocalVarSigTok = body.LocalVarSigTok;
			HeaderRVA = headerRva;
			HeaderFileOffset = headerFileOffset;
			RVA = rva;
			FileOffset = fileOffset;
			Instructions.AddRange(body.Instructions);
			ExceptionHandlers.AddRange(body.ExceptionHandlers);
			Locals.AddRange(body.Variables);
			PdbMethod = body.PdbMethod;
		}

		public CilBody CopyTo(CilBody body) {
			body.KeepOldMaxStack = KeepOldMaxStack;
			body.InitLocals = InitLocals;
			body.HeaderSize = HeaderSize;
			body.MaxStack = MaxStack;
			body.LocalVarSigTok = LocalVarSigTok;
			body.Instructions.Clear();
			body.Instructions.AddRange(Instructions);
			body.ExceptionHandlers.Clear();
			body.ExceptionHandlers.AddRange(ExceptionHandlers);
			body.Variables.Clear();
			body.Variables.AddRange(Locals);
			body.PdbMethod = PdbMethod;
			body.UpdateInstructionOffsets();
			return body;
		}

		public CilBody Create() => CopyTo(new CilBody());
	}
}
