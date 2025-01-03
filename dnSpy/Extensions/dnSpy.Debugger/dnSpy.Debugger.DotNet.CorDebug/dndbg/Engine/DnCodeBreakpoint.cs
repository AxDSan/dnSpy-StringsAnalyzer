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
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	sealed class ModuleCodeBreakpoint {
		public DnModule Module { get; }
		public CorFunctionBreakpoint FunctionBreakpoint { get; }

		public ModuleCodeBreakpoint(DnModule module, CorFunctionBreakpoint funcBp) {
			Module = module;
			FunctionBreakpoint = funcBp;
		}
	}

	enum DnCodeBreakpointError {
		None,
		OtherError,
		FunctionNotFound,
		CouldNotCreateBreakpoint,
	}

	abstract class DnCodeBreakpoint : DnBreakpoint {
		public DnModuleId Module { get; }
		public uint Token { get; }
		public uint Offset { get; }

		readonly List<ModuleCodeBreakpoint> rawBps = new List<ModuleCodeBreakpoint>();
		readonly CorCode? code;

		internal event EventHandler? ErrorChanged;
		internal DnCodeBreakpointError Error => error;
		DnCodeBreakpointError error;

		internal DnCodeBreakpoint(DnModuleId module, uint token, uint offset) {
			Module = module;
			Token = token;
			Offset = offset;
			code = null;
			error = DnCodeBreakpointError.OtherError;
		}

		internal DnCodeBreakpoint(DnModuleId module, CorCode code, uint offset) {
			Module = module;
			var func = code.Function;
			Token = func?.Token ?? 0;
			Offset = offset;
			this.code = code;
			error = DnCodeBreakpointError.OtherError;
		}

		sealed protected override void OnIsEnabledChanged() {
			foreach (var bp in rawBps)
				bp.FunctionBreakpoint.IsActive = IsEnabled;
		}

		internal void AddBreakpoint(DnModule module) => SetError(AddBreakpointCore(module));

		internal void SetError(DnCodeBreakpointError newError) {
			if (newError != error) {
				error = newError;
				ErrorChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		DnCodeBreakpointError AddBreakpointCore(DnModule module) {
			foreach (var bp in rawBps) {
				if (bp.Module == module)
					return DnCodeBreakpointError.None;
			}

			var c = code;
			if (c is null) {
				var func = module.CorModule.GetFunctionFromToken(Token);
				if (func is null)
					return DnCodeBreakpointError.FunctionNotFound;

				c = GetCode(func);
			}
			else {
				if (!object.Equals(c.Function?.Module, module.CorModule))
					return DnCodeBreakpointError.OtherError;
			}
			if (c is null)
				return DnCodeBreakpointError.FunctionNotFound;

			var funcBp = c.CreateBreakpoint(Offset);
			if (funcBp is null)
				return DnCodeBreakpointError.CouldNotCreateBreakpoint;

			var modIlBp = new ModuleCodeBreakpoint(module, funcBp);
			rawBps.Add(modIlBp);
			funcBp.IsActive = IsEnabled;

			return DnCodeBreakpointError.None;
		}

		internal abstract CorCode? GetCode(CorFunction func);

		sealed internal override void OnRemoved() {
			foreach (var bp in rawBps)
				bp.FunctionBreakpoint.IsActive = false;
			rawBps.Clear();
		}

		public bool IsBreakpoint(ICorDebugBreakpoint? comBp) {
			foreach (var bp in rawBps) {
				if (bp.FunctionBreakpoint.RawObject == comBp)
					return true;
			}
			return false;
		}

		internal void RemoveModule(DnModule module) {
			for (int i = rawBps.Count - 1; i >= 0; i--) {
				if (rawBps[i].Module == module)
					rawBps.RemoveAt(i);
			}
		}
	}
}
