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
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;

namespace ICSharpCode.Decompiler.ILAst {
	sealed class MonoYieldReturnDecompiler : YieldReturnDecompiler {
		FieldDef disposingField;

		public override string CompilerName => PredefinedCompilerNames.MonoCSharp;

		MonoYieldReturnDecompiler(DecompilerContext context, AutoPropertyProvider autoPropertyProvider)
			: base(context, autoPropertyProvider) {
		}

		public static YieldReturnDecompiler TryCreateCore(DecompilerContext context, ILBlock method, AutoPropertyProvider autoPropertyProvider) {
			var yrd = new MonoYieldReturnDecompiler(context, autoPropertyProvider);
			if (!yrd.MatchEnumeratorCreationPattern(method))
				return null;
			yrd.enumeratorType = yrd.enumeratorCtor.DeclaringType;
			return yrd;
		}

		bool MatchEnumeratorCreationPattern(ILBlock method) {
			var body = method.Body;
			if (body.Count == 0)
				return false;

			ILVariable enumVar;
			ILExpression newobj;
			IMethod ctor;

			int i = 0;

			// Check if it could be an IEnumerator method with just a yield break in it
			if (body.Count == 1) {
				if (!body[i].Match(ILCode.Ret, out newobj))
					return false;
				enumVar = null;
			}
			else {
				if (!body[i].Match(ILCode.Stloc, out enumVar, out newobj))
					return false;
			}

			if (!newobj.Match(ILCode.Newobj, out ctor))
				return false;
			enumeratorCtor = GetMethodDefinition(ctor);
			if (enumeratorCtor == null || enumeratorCtor.DeclaringType.DeclaringType != context.CurrentType)
				return false;
			if (!IsCompilerGeneratorEnumerator(enumeratorCtor.DeclaringType))
				return false;

			if (method.Body.Count == 1)
				return true;

			i++;
			if (!InitializeFieldToParameterMap(method, enumVar, ref i))
				return false;

			ILExpression ldloc;
			ILVariable retVar;
			if (body[i].Match(ILCode.Stloc, out retVar, out ldloc)) {
				// IEnumerable or IEnumerable<T> method
				if (!ldloc.MatchLdloc(enumVar))
					return false;
				i++;
				ILExpression ldci4;
				IField field;
				if (!body[i++].Match(ILCode.Stfld, out field, out ldloc, out ldci4))
					return false;
				if (!ldloc.MatchLdloc(retVar))
					return false;
				var fd = GetFieldDefinition(field);
				if (fd == null || fd.DeclaringType != enumeratorCtor.DeclaringType)
					return false;
				if (!ldci4.MatchLdcI4(-2))
					return false;
				stateField = fd;
			}
			else {
				// IEnumerator or IEnumerator<T> method
				retVar = enumVar;
			}

			if (!body[i].Match(ILCode.Ret, out ldloc))
				return false;
			return ldloc.MatchLdloc(retVar);
		}

		protected override void AnalyzeCtor() {
			ILBlock method = CreateILAst(enumeratorCtor);
			var body = method.Body;
			if (body.Count != 2)
				throw new SymbolicAnalysisFailedException();
			IMethod m;
			ILExpression ldthis;
			if (!body[0].Match(ILCode.Call, out m, out ldthis))
				throw new SymbolicAnalysisFailedException();
			if (!ldthis.MatchThis())
				throw new SymbolicAnalysisFailedException();
			if (m.Name != ".ctor")
				throw new SymbolicAnalysisFailedException();
			if (!body[1].Match(ILCode.Ret))
				throw new SymbolicAnalysisFailedException();
		}

		void InitializeDisposeMethod(ILBlock method) {
			FieldDef localStateField = null;
			foreach (var n in method.Body) {
				var expr = n as ILExpression;
				if (expr == null)
					break;
				if (expr.Code == ILCode.Switch)
					break;
				IField field;
				ILExpression ldloc, ldci4;
				if (!expr.Match(ILCode.Stfld, out field, out ldloc, out ldci4))
					continue;
				if (!ldloc.MatchThis())
					continue;
				int val;
				if (!ldci4.Match(ILCode.Ldc_I4, out val))
					continue;
				var fd = GetFieldDefinition(field);
				if (fd?.DeclaringType != enumeratorType)
					continue;
				if (variableMap.TryGetParameter(fd, out var parameter))
					break;
				if (val == -1) {
					if (fd.FieldSig.Type.ElementType != ElementType.I4)
						continue;
					if (localStateField != null)
						throw new SymbolicAnalysisFailedException();
					localStateField = fd;
				}
				else if (val == 1) {
					if (fd.FieldSig.Type.ElementType != ElementType.Boolean)
						continue;
					if (disposingField != null)
						throw new SymbolicAnalysisFailedException();
					disposingField = fd;
				}
				else
					continue;
				if (localStateField != null && disposingField != null)
					break;
			}
			if (stateField != null && localStateField != null && stateField != localStateField)
				throw new SymbolicAnalysisFailedException();
			if (localStateField != null)
				stateField = localStateField;
		}

		protected override void AnalyzeDispose() {
			disposeMethod = MethodUtils.GetMethod_Dispose(enumeratorType).FirstOrDefault();
			var ilMethod = CreateILAst(disposeMethod);
			InitializeDisposeMethod(ilMethod);
		}

		protected override void AnalyzeMoveNext() {
			var moveNextMethod = MethodUtils.GetMethod_MoveNext(enumeratorType).FirstOrDefault();
			var ilMethod = CreateILAst(moveNextMethod);
			iteratorMoveNextMethod = moveNextMethod;
			var body = ilMethod.Body;
			if (body.Count == 0)
				throw new SymbolicAnalysisFailedException();

			// If it's an IEnumerator method with just a yield break in it, we haven't found the state field yet
			if (stateField == null) {
				const int index = 1;
				IField f;
				List<ILExpression> args;
				if (index + 1 >= body.Count || !body[index].Match(ILCode.Stfld, out f, out args) || args.Count != 2 || !args[0].MatchThis() || !args[1].MatchLdcI4(-1))
					throw new SymbolicAnalysisFailedException();
				var field = GetFieldDefinition(f);
				if (field?.DeclaringType != enumeratorType)
					throw new SymbolicAnalysisFailedException();
				stateField = field;
			}

			disposeInFinallyVar = MonoStateMachineUtils.FindDisposeLocal(ilMethod);

			int bodyLength;
			if (!FindReturnLabels(body, out bodyLength, out returnFalseLabel, out returnTrueLabel))
				throw new SymbolicAnalysisFailedException();

			var rangeAnalysis = new MonoStateRangeAnalysis(body[0], StateRangeAnalysisMode.IteratorMoveNext, stateField, disposingField, disposeInFinallyVar);
			int pos = rangeAnalysis.AssignStateRanges(body, bodyLength);
			rangeAnalysis.EnsureLabelAtPos(body, ref pos, ref bodyLength);

			labels = rangeAnalysis.CreateLabelRangeMapping(body, pos, bodyLength);
			stateVariables = rangeAnalysis.StateVariables;
			ConvertBody(body, pos, bodyLength);
		}
		List<ILVariable> stateVariables;
		ILLabel returnFalseLabel, returnTrueLabel;
		List<KeyValuePair<ILLabel, StateRange>> labels;
		ILVariable disposeInFinallyVar;

		bool FindReturnLabels(List<ILNode> body, out int bodyLength, out ILLabel retZeroLabel, out ILLabel retOneLabel) {
			bodyLength = 0;
			retZeroLabel = null;
			retOneLabel = null;
			// MoveNext ends with 'lbl1: return 0; lbl2: return 1;' or 'lbl1: return 0;'

			ILLabel lbl;
			int val;
			int pos = body.Count - 2;
			if (!GetReturnValueLabel(body, pos, out lbl, out val))
				return false;
			if (val == 0) {
				retZeroLabel = lbl;
				bodyLength = pos;
				return true;
			}
			else if (val == 1) {
				retOneLabel = lbl;
				pos -= 2;
				if (!GetReturnValueLabel(body, pos, out lbl, out val))
					return false;
				if (val != 0)
					return false;
				retZeroLabel = lbl;
				bodyLength = pos;
				return true;
			}
			else
				return false;
		}

		bool GetReturnValueLabel(List<ILNode> body, int pos, out ILLabel lbl, out int val) {
			lbl = null;
			val = 0;
			if (pos < 0)
				return false;
			lbl = body[pos] as ILLabel;
			if (lbl == null)
				return false;
			ILExpression ldci4;
			return body[pos + 1].Match(ILCode.Ret, out ldci4) &&
				ldci4.Match(ILCode.Ldc_I4, out val);
		}

		ILExpression CreateYieldReturn(ILExpression stExpr) {
			var arg = stExpr.Arguments[1];
			if (context.CalculateILSpans) {
				arg.ILSpans.AddRange(stExpr.ILSpans);
				arg.ILSpans.AddRange(stExpr.Arguments[0].GetSelfAndChildrenRecursiveILSpans());
			}
			return new ILExpression(ILCode.YieldReturn, null, arg);
		}

		ILExpression CreateYieldBreak(ILNode original = null) {
			var yieldBreak = new ILExpression(ILCode.YieldBreak, null);
			original?.AddSelfAndChildrenRecursiveILSpans(yieldBreak.ILSpans);
			return yieldBreak;
		}

		void ConvertBody(List<ILNode> body, int startPos, int bodyLength) {
			newBody = new List<ILNode>();
			if (startPos != bodyLength)
				newBody.Add(MakeGoTo(labels, 0));
			ConvertBodyCore(body, newBody, startPos, bodyLength);
			newBody.Add(CreateYieldBreak());
		}

		List<ILNode> ConvertBodyCore(List<ILNode> body, List<ILNode> newBody, int startPos, int bodyLength) {
			int labelsCount = labels.Count;
			int currentState = -1;
			ILExpression arg;
			for (int pos = startPos; pos < bodyLength; pos++) {
				var expr = body[pos] as ILExpression;
				int val;
				ILLabel lbl;
				switch (expr?.Code ?? (ILCode)(-1)) {
				case ILCode.Stfld:
					var field = DnlibExtensions.ResolveFieldWithinSameModule(expr.Operand as IField);
					if (field == null)
						goto default;
					if (field == currentField && expr.Arguments[0].MatchThis()) {
						newBody.Add(CreateYieldReturn(expr));
						break;
					}
					else if (field == stateField && expr.Arguments[0].MatchThis() && expr.Arguments[1].Match(ILCode.Ldc_I4, out val)) {
						currentState = val;
						break;
					}
					goto default;

				case ILCode.Brtrue:
					arg = expr.Arguments[0];
					if (arg.Code == ILCode.Ldfld && DnlibExtensions.ResolveFieldWithinSameModule(arg.Operand as IField) == disposingField && arg.Arguments[0].MatchThis())
						break;
					if (disposeInFinallyVar != null && arg.Code == ILCode.LogicNot && arg.Arguments[0].MatchLdloc(disposeInFinallyVar)) {
						if (!body[pos + 1].Match(ILCode.Endfinally))
							throw new SymbolicAnalysisFailedException();
						pos++;
						break;
					}
					lbl = (ILLabel)expr.Operand;
					if (lbl == returnFalseLabel || lbl == returnTrueLabel) {
						var skipLbl = CreateLabel();
						var yieldOrGoToLabel = CreateLabel();
						expr.Operand = yieldOrGoToLabel;
						newBody.Add(expr);
						newBody.Add(new ILExpression(ILCode.Br, skipLbl));
						newBody.Add(yieldOrGoToLabel);
						if (lbl == returnFalseLabel)
							newBody.Add(CreateYieldBreak());
						else
							newBody.Add(MakeGoTo(labels, currentState));
						newBody.Add(skipLbl);
						break;
					}
					goto default;

				case ILCode.Br:
				case ILCode.Leave:
					lbl = (ILLabel)expr.Operand;
					if (lbl == returnFalseLabel)
						newBody.Add(CreateYieldBreak(expr));
					else if (lbl == returnTrueLabel) {
						newBody.Add(MakeGoTo(labels, currentState));
						currentState = -1;
					}
					else
						goto default;
					break;

				case ILCode.Stloc:
					var v = (ILVariable)expr.Operand;
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
								labels.Add(new KeyValuePair<ILLabel, StateRange>(targetLabel, stateRange));
							}
						}
						break;
					}
					goto default;

				case ILCode.Call:
					if (expr.Arguments.Count == 1 && expr.Arguments[0].MatchThis()) {
						var finallyMethod = DnlibExtensions.ResolveMethodWithinSameModule(expr.Operand as IMethod);
						if (finallyMethod != disposeMethod && finallyMethod?.DeclaringType == enumeratorType) {
							if (finallyMethod.IsStatic || finallyMethod.MethodSig.GetRetType().RemovePinnedAndModifiers().GetElementType() != ElementType.Void)
								throw new SymbolicAnalysisFailedException();
							var finallyBlock = ConvertFinallyBlock(finallyMethod);
							newBody.AddRange(finallyBlock.Body);
							break;
						}
					}
					goto default;

				default:
					if (expr == null) {
						var tryCatch = body[pos] as ILTryCatchBlock;
						if (tryCatch != null) {
							if (tryCatch.TryBlock != null)
								ConvertBodyCore(ref tryCatch.TryBlock.Body);
							foreach (var cb in tryCatch.CatchBlocks) {
								ConvertBodyCore(ref cb.Body);
								if (cb.FilterBlock != null)
									ConvertBodyCore(ref cb.FilterBlock.Body);
							}
							if (tryCatch.FinallyBlock != null)
								ConvertBodyCore(ref tryCatch.FinallyBlock.Body);
							if (tryCatch.FaultBlock != null)
								ConvertBodyCore(ref tryCatch.FaultBlock.Body);
						}
					}
					newBody.Add(body[pos]);
					break;
				}
			}
			// Remove temp labels in try blocks
			labels.RemoveRange(labelsCount, labels.Count - labelsCount);
			return newBody;
		}
		readonly Dictionary<ILLabel, StateRange> stateRanges = new Dictionary<ILLabel, StateRange>();

		void ConvertBodyCore(ref List<ILNode> body) {
			List<ILNode> newBody;
			if (freeBodies.Count > 0) {
				newBody = freeBodies[freeBodies.Count - 1];
				freeBodies.RemoveAt(freeBodies.Count - 1);
			}
			else
				newBody = new List<ILNode>();
			ConvertBodyCore(body, newBody, 0, body.Count);
			body.Clear();
			freeBodies.Add(body);
			body = newBody;
		}
		readonly List<List<ILNode>> freeBodies = new List<List<ILNode>>();

		ILExpression MakeGoTo(ILLabel targetLabel) {
			Debug.Assert(targetLabel != null);
			Debug.Assert(targetLabel != returnTrueLabel);
			if (targetLabel == returnFalseLabel)
				return CreateYieldBreak();
			return new ILExpression(ILCode.Br, targetLabel);
		}

		ILExpression MakeGoTo(List<KeyValuePair<ILLabel, StateRange>> labels, int state) {
			// Reverse order since the latest labels have been added to the end (it's a stack)
			for (int i = labels.Count - 1; i >= 0; i--) {
				var pair = labels[i];
				if (pair.Value.Contains(state))
					return MakeGoTo(pair.Key);
			}
			throw new SymbolicAnalysisFailedException();
		}

		ILBlock ConvertFinallyBlock(MethodDef finallyMethod) {
			var block = CreateILAst(finallyMethod);
			var lbl = CreateLabel();
			block.Body.Add(lbl);
			foreach (var expr in block.GetSelfAndChildrenRecursive<ILExpression>(list_ILExpression)) {
				if (expr.Code == ILCode.Ret) {
					expr.Code = ILCode.Br;
					expr.Operand = lbl;
				}
			}
			return block;
		}

		ILLabel CreateLabel() => new ILLabel { Name = "__tmp_lbl_" + (labelCounter++).ToString() };
		int labelCounter;
	}
}
