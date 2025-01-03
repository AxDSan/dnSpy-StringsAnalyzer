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
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;

namespace ICSharpCode.Decompiler.ILAst {
	sealed class MonoAsyncDecompiler : AsyncDecompiler {
		readonly List<ILExpression> expressionList = new List<ILExpression>();
		readonly List<ILBlock> list_ILBlock = new List<ILBlock>();
		readonly Dictionary<ILExpression, FieldDef> awaitExprInfos = new Dictionary<ILExpression, FieldDef>();
		ILVariable cachedStateVar;
		ILVariable disposeInFinallyVar;
		ILLabel setResultAndExitLabel;
		ILExpression resultExpr;
		const int initialState = 0;

		public override string CompilerName => PredefinedCompilerNames.MonoCSharp;

		MonoAsyncDecompiler(DecompilerContext context, AutoPropertyProvider autoPropertyProvider)
			: base(context, autoPropertyProvider) {
		}

		public static AsyncDecompiler TryCreateCore(DecompilerContext context, ILBlock method, AutoPropertyProvider autoPropertyProvider) {
			var yrd = new MonoAsyncDecompiler(context, autoPropertyProvider);
			if (!yrd.MatchTaskCreationPattern(method))
				return null;
			return yrd;
		}

		bool MatchTaskCreationPattern(ILBlock method) {
			var body = method.Body;
			// Need at least Create(), Start(), get_Task()
			if (body.Count < 3)
				return false;

			ILVariable stateMachineVar;
			if (!MatchStartCall(body[body.Count - 2], out stateMachineVar))
				return false;
			if (!MatchCallCreate(body[body.Count - 3], stateMachineVar))
				return false;
			if (!MatchReturnTask(body[body.Count - 1], stateMachineVar))
				return false;
			if (!InitializeFieldToParameterMap(body, body.Count - 3, stateMachineVar))
				return false;

			return true;
		}

		protected override void AnalyzeMoveNext(out ILMethodBody bodyInfo, out ILTryCatchBlock tryCatchBlock, out int finalState, out ILLabel exitLabel) {
			if (!AnalyzeMoveNextCore(out bodyInfo, out tryCatchBlock, out finalState, out exitLabel))
				throw new SymbolicAnalysisFailedException();
		}

		bool AnalyzeMoveNextCore(out ILMethodBody bodyInfo, out ILTryCatchBlock tryCatchBlock, out int finalState, out ILLabel exitLabel) {
			bodyInfo = default(ILMethodBody);
			tryCatchBlock = null;
			finalState = -1;
			exitLabel = null;

			var ilMethod = CreateILAst(moveNextMethod);
			var pos = 0;
			var body = ilMethod.Body;
			if (body.Count < 5)
				return false;

			// stloc(var_0_06, ldfld('<Async020>c__async16'::$PC, ldloc(this)))
			IField field;
			ILExpression ldfld;
			if (!body[pos++].Match(ILCode.Stloc, out cachedStateVar, out ldfld))
				return false;
			ILExpression ldloc;
			if (!ldfld.Match(ILCode.Ldfld, out field, out ldloc) || !ldloc.MatchThis())
				return false;
			stateField = field.ResolveFieldWithinSameModule();
			if (stateField?.DeclaringType != stateMachineType || stateField.FieldSig.GetFieldType().RemovePinnedAndModifiers().GetElementType() != ElementType.I4)
				return false;

			// stfld('<Async020>c__async16'::$PC, ldloc(this), ldc.i4(-1))
			ILExpression ldci4;
			if (!body[pos++].Match(ILCode.Stfld, out field, out ldloc, out ldci4) || !ldloc.MatchThis() || !ldci4.MatchLdcI4(-1))
				return false;
			if (field.ResolveFieldWithinSameModule() != stateField)
				return false;

			ILVariable v;
			if (body[pos].Match(ILCode.Stloc, out v, out ldci4) && v.OriginalVariable != null && ldci4.MatchLdcI4(0)) {
				pos++;
				disposeInFinallyVar = v;
			}

			// Check if it's a small async method with just one valid user-state (0)
			ILLabel lbl;
			if (body[pos].Match(ILCode.Brtrue, out lbl, out ldloc)) {
				if (!ldloc.MatchLdloc(cachedStateVar))
					return false;
				pos++;
			}

			// Check instructions from the end since older mcs versions didn't include
			// a try-catch block (there was no SetException() call).
			// This was fixed in some version after 3.2.3 and at the latest in 3.12.0.
			int startPos = pos;

			var endPos = body.Count - 1;
			if (endPos < pos || !body[endPos--].Match(ILCode.Ret))
				return false;

			if (endPos < pos || (exitLabel = body[endPos--] as ILLabel) == null)
				return false;
			// Verify brtrue if it's a small async method
			if (lbl != null && lbl != exitLabel)
				return false;

			ILVariable resultVariable;
			if (endPos < pos || !MatchCallSetResult(body[endPos--], out resultExpr, out resultVariable))
				return false;

			// stfld($PC, ldloc(this), ldc.i4(-1))
			if (endPos >= pos && body[endPos].Match(ILCode.Stfld, out field, out ldloc, out ldci4) &&
				ldloc.MatchThis() && ldci4.MatchLdcI4(-1) &&
				field.ResolveFieldWithinSameModule() == stateField) {
				endPos--;
			}

			if (endPos >= pos && body[endPos] is ILLabel)
				setResultAndExitLabel = body[endPos--] as ILLabel;

			if (endPos >= pos) {
				tryCatchBlock = GetMainTryCatchBlock(body[endPos]);
				if (tryCatchBlock != null)
					endPos--;
			}
			if (tryCatchBlock != null) {
				if (endPos + 1 != pos)
					return false;
				if (setResultAndExitLabel == null)
					return false;
				bodyInfo = new ILMethodBody(tryCatchBlock.TryBlock.Body);
			}
			else {
				if (endPos + 1 < startPos)
					return false;
				bodyInfo = new ILMethodBody(body, startPos, endPos + 1);
			}

			return true;
		}

		protected override List<ILNode> AnalyzeStateMachine(ILMethodBody bodyInfo) {
			var body = bodyInfo.Body;
			var startPos = bodyInfo.StartPosition;
			var endPos = bodyInfo.EndPosition;
			if (startPos >= endPos) {
				if (startPos == endPos)
					return new List<ILNode>();
				throw new SymbolicAnalysisFailedException();
			}

			var rangeAnalysis = new MonoStateRangeAnalysis(body[startPos], StateRangeAnalysisMode.AsyncMoveNext, stateField, null, disposeInFinallyVar, cachedStateVar);
			int bodyLength = endPos;
			int pos = rangeAnalysis.AssignStateRanges(body, startPos, bodyLength);
			rangeAnalysis.EnsureLabelAtPos(body, ref pos, ref bodyLength);

			var labelStateRangeMapping = rangeAnalysis.CreateLabelRangeMapping(body, pos, bodyLength);
			stateVariables = rangeAnalysis.StateVariables;
			var newBody = ConvertBody(body, pos, bodyLength, labelStateRangeMapping);

			newBody.Insert(0, MakeGoTo(labelStateRangeMapping, initialState));
			if (setResultAndExitLabel != null)
				newBody.Add(setResultAndExitLabel);
			if (methodType == AsyncMethodType.TaskOfT)
				newBody.Add(new ILExpression(ILCode.Ret, null, resultExpr));
			else
				newBody.Add(new ILExpression(ILCode.Ret, null));

			SaveAwaiterFields(newBody);
			RemoveAsyncStepInfoState(initialState);

			return newBody;
		}
		List<ILVariable> stateVariables;

		// Hack to prevent the awaiter fields from being converted to locals by the base class
		void SaveAwaiterFields(List<ILNode> newBody) {
			var list = expressionList;
			foreach (var block in new ILBlock { Body = newBody }.GetSelfAndChildrenRecursive<ILBlock>(list_ILBlock)) {
				var body = block.Body;
				for (int i = 0; i < body.Count; i++) {
					var expr = body[i] as ILExpression;
					if (expr != null)
						list.Add(expr);
				}
				while (list.Count > 0) {
					var expr = list[list.Count - 1];
					list.RemoveAt(list.Count - 1);
					list.AddRange(expr.Arguments);
					var ldflda = MatchCallGetResult(expr, false, null);
					if (ldflda != null) {
						ldflda.Code = ILCode.Ldsflda;
						ldflda.Arguments.Clear();
					}
				}
			}
		}

		ILExpression MakeGoTo(LabelRangeMapping mapping, int state) {
			// Reverse order since the latest labels have been added to the end (it's a stack)
			for (int i = mapping.Count - 1; i >= 0; i--) {
				var pair = mapping[i];
				if (pair.Value.Contains(state))
					return new ILExpression(ILCode.Br, pair.Key);
			}
			throw new SymbolicAnalysisFailedException();
		}

		static readonly UTF8String nameGetAwaiter = new UTF8String("GetAwaiter");
		static readonly UTF8String nameget_IsCompleted = new UTF8String("get_IsCompleted");
		List<ILNode> ConvertBody(List<ILNode> body, int startPos, int bodyLength, LabelRangeMapping mapping, bool keepMappings = false) {
			int mappingCount = mapping.Count;
			List<ILNode> newBody = new List<ILNode>();
			for (int pos = startPos; pos < bodyLength; pos++) {
				var node = body[pos];
				var expr = node as ILExpression;
				ILVariable v;
				ILExpression arg;
				int val;
				switch (expr?.Code ?? (ILCode)(-1)) {
				case ILCode.Stfld:
					var field = DnlibExtensions.ResolveFieldWithinSameModule(expr.Operand as IField);
					if (field == null)
						goto default;
					if (field == stateField && expr.Arguments[0].MatchThis() && expr.Arguments[1].Match(ILCode.Ldc_I4, out val)) {
						// Store to $PC, it should be an await
						int newBodyCount = newBody.Count;
						if (newBodyCount < 2)
							throw new SymbolicAnalysisFailedException();

						AddYieldOffset(body, pos, 1, val);

						// stfld($awaiter0, ldloc(this), callvirt(GetAwaiter, ...))
						IField awaiterField;
						ILExpression ldloc, callGetAwaiter;
						if (!newBody[newBodyCount - 2].Match(ILCode.Stfld, out awaiterField, out ldloc, out callGetAwaiter) || !ldloc.MatchThis())
							throw new SymbolicAnalysisFailedException();
						if (callGetAwaiter.Code != ILCode.Callvirt && callGetAwaiter.Code != ILCode.Call)
							throw new SymbolicAnalysisFailedException();
						if (callGetAwaiter.Arguments.Count != 1)
							throw new SymbolicAnalysisFailedException();
						var origExpr = callGetAwaiter.Arguments[0];
						if (context.CalculateILSpans) {
							origExpr.ILSpans.AddRange(ldloc.ILSpans);
							origExpr.ILSpans.AddRange(newBody[newBodyCount - 2].ILSpans);
							origExpr.ILSpans.AddRange(callGetAwaiter.ILSpans);
						}
						var methodGetAwaiter = (IMethod)callGetAwaiter.Operand;
						if (methodGetAwaiter.Name != nameGetAwaiter)
							throw new SymbolicAnalysisFailedException();
						var awaiterFieldDef = awaiterField.ResolveFieldWithinSameModule();
						if (awaiterFieldDef?.DeclaringType != stateMachineType)
							throw new SymbolicAnalysisFailedException();

						// brtrue(IL_74, call(TaskAwaiter::get_IsCompleted, ldflda($awaiter0, ldloc(this))))
						ILLabel lbl;
						ILExpression call;
						if (!newBody[newBodyCount - 1].Match(ILCode.Brtrue, out lbl, out call))
							throw new SymbolicAnalysisFailedException();
						if (call.Code != ILCode.Callvirt && call.Code != ILCode.Call)
							throw new SymbolicAnalysisFailedException();
						var methodIsCompleted = (IMethod)call.Operand;
						if (methodIsCompleted.Name != nameget_IsCompleted)
							throw new SymbolicAnalysisFailedException();
						IField f;
						if (!call.Arguments[0].Match(ILCode.Ldflda, out f, out ldloc) || !ldloc.MatchThis() || f.ResolveFieldWithinSameModule() != awaiterFieldDef)
							throw new SymbolicAnalysisFailedException();

						pos++;

						// stloc(var_1, ldc.i4(1))
						ILExpression ldci4;
						if (body[pos].Match(ILCode.Stloc, out v, out ldci4)) {
							if (v != disposeInFinallyVar || !ldci4.MatchLdcI4(1))
								throw new SymbolicAnalysisFailedException();
							pos++;
						}

						// call(AwaitUnsafeOnCompleted, ldflda($builder, ldloc(this)), ldflda($awaiter0, ldloc(this)), ldloc(this))
						if (pos >= bodyLength)
							throw new SymbolicAnalysisFailedException();
						var ldflda = MatchCallAwaitOnCompletedMethod(body[pos++]);
						if (!ldflda.Match(ILCode.Ldflda, out f, out ldloc) || !ldloc.MatchThis() || f.ResolveFieldWithinSameModule() != awaiterFieldDef)
							throw new SymbolicAnalysisFailedException();

						if (pos >= bodyLength || !body[pos].Match(ILCode.Leave, out lbl) || lbl != exitLabel)
							throw new SymbolicAnalysisFailedException();

						AddResumeLabel(lbl, val);

						var awaitExpr = new ILExpression(ILCode.Await, null, origExpr);
						awaitExprInfos.Add(awaitExpr, awaiterFieldDef);
						newBody[newBody.Count - 2] = awaitExpr;
						newBody[newBody.Count - 1] = MakeGoTo(mapping, val);
						break;
					}
					goto default;

				case ILCode.Brtrue:
					arg = expr.Arguments[0];
					if (disposeInFinallyVar != null && arg.Code == ILCode.LogicNot && arg.Arguments[0].MatchLdloc(disposeInFinallyVar)) {
						if (!body[pos + 1].Match(ILCode.Endfinally))
							throw new SymbolicAnalysisFailedException();
						pos++;
						break;
					}
					goto default;

				case ILCode.Stloc:
					v = (ILVariable)expr.Operand;
					if (v == disposeInFinallyVar) {
						if (!expr.Arguments[0].MatchLdcI4(1))
							throw new SymbolicAnalysisFailedException();
						break;
					}
					else if (stateVariables.Contains(v)) {
						// Check if it inits the local state to -3, which it does just before the try block
						//     stloc(var_0_610, ldc.i4(-3))
						//     .try {
						//         IL_91:	// switch at beginning of method branches here
						//         switch(IL_E0, ..., sub(ldloc(var_0_610), ldc.i4(2)))
						if (expr.Arguments[0].MatchLdcI4(-3))
							break;
					}
					goto default;

				case ILCode.Switch:
					// Check if it's this pattern inside the try block:
					//     stloc(var_0_610, ldc.i4(-3))
					//     .try {
					//         IL_91:	// switch at beginning of method branches here
					//         switch(IL_E0, ..., sub(ldloc(var_0_610), ldc.i4(2)))
					arg = expr.Arguments[0];
					if (arg.Code == ILCode.Sub && arg.Arguments[0].Match(ILCode.Ldloc, out v) && arg.Arguments[1].Match(ILCode.Ldc_I4, out val) && stateVariables.Contains(v)) {
						var targetLabels = (ILLabel[])expr.Operand;
						stateRanges.Clear();
						for (int i = 0; i < targetLabels.Length; i++) {
							var state = val + i;
							var targetLabel = targetLabels[i];
							StateRange stateRange;
							if (stateRanges.TryGetValue(targetLabel, out stateRange))
								stateRange.UnionWith(new StateRange(state, state));
							else {
								stateRange = new StateRange(state, state);
								stateRanges.Add(targetLabel, stateRange);
								mapping.Add(new KeyValuePair<ILLabel, StateRange>(targetLabel, stateRange));
							}
						}
						break;
					}
					goto default;

				default:
					if (expr == null) {
						var tryCatch = node as ILTryCatchBlock;
						if (tryCatch != null) {
							// There's a bug in older versions of the mcs compiler. There could be branches
							// from the try block into a catch block if the catch block contains an await
							// statement. A quick solution is to keep the mapping so the state labels can
							// be used by the catch block.
							int mappingCount2 = mapping.Count;
							if (tryCatch.TryBlock != null)
								ConvertBody(ref tryCatch.TryBlock.Body, mapping);
							foreach (var cb in tryCatch.CatchBlocks) {
								ConvertBody(ref cb.Body, mapping);
								if (cb.FilterBlock != null)
									ConvertBody(ref cb.FilterBlock.Body, mapping);
							}
							if (tryCatch.FinallyBlock != null)
								ConvertBody(ref tryCatch.FinallyBlock.Body, mapping);
							if (tryCatch.FaultBlock != null)
								ConvertBody(ref tryCatch.FaultBlock.Body, mapping);
							mapping.RemoveRange(mappingCount2, mapping.Count - mappingCount2);
						}
					}

					newBody.Add(node);
					break;
				}
			}
			if (!keepMappings)
				mapping.RemoveRange(mappingCount, mapping.Count - mappingCount);
			return newBody;
		}
		readonly Dictionary<ILLabel, StateRange> stateRanges = new Dictionary<ILLabel, StateRange>();

		void ConvertBody(ref List<ILNode> body, LabelRangeMapping mapping) =>
			body = ConvertBody(body, 0, body.Count, mapping, keepMappings: true);

		protected override void Step2(ILBlock method) {
			foreach (var block in method.GetSelfAndChildrenRecursive<ILBlock>(list_ILBlock)) {
				var body = block.Body;
				for (int i = 0; i < body.Count; i++) {
					var expr = body[i] as ILExpression;
					if (expr == null || expr.Code != ILCode.Await)
						continue;

					int replaceExprIndex = GetNextNonAwaitIndex(body, i);
					Debug.Assert(replaceExprIndex >= 0);
					if (replaceExprIndex < 0)
						continue;
					for (; i < replaceExprIndex; i++) {
						expr = (ILExpression)body[i];
						Debug.Assert(expr.Code == ILCode.Await);
						bool b = awaitExprInfos.TryGetValue(expr, out var awaiterField);
						Debug.Assert(b);
						if (!b)
							continue;

						b = UpdateExpression(expressionList, expr.Arguments[0], body[replaceExprIndex] as ILExpression, awaiterField);
						Debug.Assert(b);
						if (b) {
							body.RemoveAt(i);
							i--;
							replaceExprIndex--;
						}
					}
				}
			}
		}

		static int GetNextNonAwaitIndex(List<ILNode> body, int i) {
			while (i < body.Count) {
				var expr = body[i] as ILExpression;
				if (expr == null || expr.Code != ILCode.Await)
					return i;
				i++;
			}
			return -1;
		}

		bool UpdateExpression(List<ILExpression> list, ILExpression newExpr, ILExpression target, FieldDef awaiterField) {
			if (target == null)
				return false;
			list.Clear();
			list.Add(target);
			while (list.Count > 0) {
				var expr = list[list.Count - 1];
				list.RemoveAt(list.Count - 1);
				list.AddRange(expr.Arguments);

				if (MatchCallGetResult(expr, true, awaiterField) == null)
					continue;

				expr.Code = ILCode.Await;
				expr.Operand = null;
				expr.Arguments.Clear();
				expr.Arguments.Add(newExpr);
				expr.Prefixes = null;
				expr.ExpectedType = null;
				expr.InferredType = null;
				return true;
			}
			return false;
		}

		ILExpression MatchCallGetResult(ILExpression expr, bool isStatic, FieldDef requiredAwaiterField) {
			if (expr.Code != ILCode.Call)
				return null;
			if (expr.Arguments.Count != 1)
				return null;
			var ldflda = expr.Arguments[0];
			IField field;
			// The ldflda was changed to ldsflda so the field wouldn't be translated to a local
			if (isStatic) {
				if (!ldflda.Match(ILCode.Ldsflda, out field))
					return null;
			}
			else {
				ILExpression ldloc;
				if (!ldflda.Match(ILCode.Ldflda, out field, out ldloc) || !ldloc.MatchThis())
					return null;
			}
			var awaiterFieldDef = field.ResolveFieldWithinSameModule();
			if (requiredAwaiterField != null && requiredAwaiterField != awaiterFieldDef)
				return null;
			if (awaiterFieldDef?.DeclaringType != stateMachineType)
				return null;
			var m = expr.Operand as IMethod;
			if (m?.Name != nameGetResult)
				return null;

			return ldflda;
		}
	}
}
