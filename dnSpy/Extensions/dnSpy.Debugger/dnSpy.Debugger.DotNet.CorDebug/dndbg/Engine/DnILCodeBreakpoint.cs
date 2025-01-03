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

namespace dndbg.Engine {
	sealed class ILCodeBreakpointConditionContext : BreakpointConditionContext {
		public override DnBreakpoint Breakpoint => ILCodeBreakpoint;
		public DnILCodeBreakpoint ILCodeBreakpoint { get; }
		public BreakpointDebugCallbackEventArgs E { get; }

		public ILCodeBreakpointConditionContext(DnDebugger debugger, DnILCodeBreakpoint bp, BreakpointDebugCallbackEventArgs e)
			: base(debugger) {
			ILCodeBreakpoint = bp;
			E = e;
		}
	}

	sealed class DnILCodeBreakpoint : DnCodeBreakpoint {
		internal Func<ILCodeBreakpointConditionContext, bool> Condition { get; }

		internal DnILCodeBreakpoint(DnModuleId module, uint token, uint offset, Func<ILCodeBreakpointConditionContext, bool>? cond)
			: base(module, token, offset) => Condition = cond ?? defaultCond;
		static readonly Func<ILCodeBreakpointConditionContext, bool> defaultCond = a => true;

		internal override CorCode? GetCode(CorFunction func) => func.ILCode;
	}
}
