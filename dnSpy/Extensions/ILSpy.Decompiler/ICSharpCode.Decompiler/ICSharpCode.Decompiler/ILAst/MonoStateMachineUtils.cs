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

using dnlib.DotNet;

namespace ICSharpCode.Decompiler.ILAst {
	static class MonoStateMachineUtils {
		// Finds the compiler generated boolean local used by finally blocks that determines whether
		// the code in the finally block should execute or not.
		public static ILVariable FindDisposeLocal(ILBlock ilMethod) {
			ILVariable local = null;
			foreach (var block in ilMethod.GetSelfAndChildrenRecursive<ILTryCatchBlock>()) {
				var body = block.FinallyBlock?.Body;
				if (body == null || body.Count < 2)
					continue;
				ILLabel lbl;
				ILExpression logicnot;
				if (!body[0].Match(ILCode.Brtrue, out lbl, out logicnot))
					continue;
				if (!body[1].Match(ILCode.Endfinally))
					continue;
				ILExpression ldloc;
				if (!logicnot.Match(ILCode.LogicNot, out ldloc))
					continue;
				ILVariable v;
				if (!ldloc.Match(ILCode.Ldloc, out v) || v.IsParameter || v.Type.GetElementType() != ElementType.Boolean)
					continue;
				if (!CheckDisposeLocalInTryBlock(block.TryBlock, v))
					continue;
				if (local == null)
					local = v;
				else if (local != v)
					throw new SymbolicAnalysisFailedException();
			}
			return local;
		}

		// Verify that it's written to at least once and that the value is 1 (true)
		static bool CheckDisposeLocalInTryBlock(ILBlock tryBlock, ILVariable local) {
			int count = 0;
			var body = tryBlock.Body;
			for (int i = 0; i < body.Count; i++) {
				ILVariable v;
				ILExpression ldci4;
				if (!body[i].Match(ILCode.Stloc, out v, out ldci4))
					continue;
				if (v != local)
					continue;
				if (!ldci4.MatchLdcI4(1))
					return false;
				count++;
			}
			return count >= 1;
		}
	}
}
