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
	/// <summary>
	/// vbc (VS2012-VS2013)
	/// </summary>
	sealed class VisualBasic11YieldReturnDecompiler : YieldReturnDecompiler {
		FieldDef disposingField;
		ILVariable doFinallyBodiesVar;

		public override string CompilerName => PredefinedCompilerNames.MicrosoftVisualBasic;

		VisualBasic11YieldReturnDecompiler(DecompilerContext context, AutoPropertyProvider autoPropertyProvider)
			: base(context, autoPropertyProvider) {
		}

		public static YieldReturnDecompiler TryCreateCore(DecompilerContext context, ILBlock method, AutoPropertyProvider autoPropertyProvider) {
			var yrd = new VisualBasic11YieldReturnDecompiler(context, autoPropertyProvider);
			if (!yrd.MatchEnumeratorCreationPattern(method))
				return null;
			yrd.enumeratorType = yrd.enumeratorCtor.DeclaringType;
			return yrd;
		}

		bool MatchEnumeratorCreationPattern(ILBlock method) {
			var body = method.Body;
			if (body.Count < 3)
				return false;

			// stloc($sm_05, newobj(.ctor))
			ILVariable iterVar;
			ILExpression newobj;
			if (!body[0].Match(ILCode.Stloc, out iterVar, out newobj))
				return false;
			IMethod m;
			if (!newobj.Match(ILCode.Newobj, out m) || m.Name != nameCtor)
				return false;
			enumeratorCtor = GetMethodDefinition(m);
			if (enumeratorCtor == null || enumeratorCtor.DeclaringType.DeclaringType != context.CurrentType)
				return false;
			if (!IsCompilerGeneratorEnumerator(enumeratorCtor.DeclaringType))
				return false;

			// ret(ldloc($sm_05))
			ILExpression ldloc;
			if (!body[body.Count - 1].Match(ILCode.Ret, out ldloc) || !ldloc.MatchLdloc(iterVar))
				return false;

			// stfld($State, ldloc($sm_05), ldc.i4(N)) // N = -1 (enumerator) or -2 (enumerable)
			IField f;
			ILExpression ldci4;
			if (!body[body.Count - 2].Match(ILCode.Stfld, out f, out ldloc, out ldci4) || !ldloc.MatchLdloc(iterVar))
				return false;
			int state;
			if (!ldci4.Match(ILCode.Ldc_I4, out state) || (state != -1 && state != -2))
				return false;
			stateField = GetFieldDefinition(f);
			if (stateField.DeclaringType != enumeratorCtor.DeclaringType)
				return false;

			int i = 1;
			if (!InitializeFieldToParameterMap(method, iterVar, ref i, body.Count - 2))
				return false;

			return true;
		}
		static readonly UTF8String nameCtor = new UTF8String(".ctor");

		protected override void AnalyzeDispose() {
			disposeMethod = MethodUtils.GetMethod_Dispose(enumeratorType).FirstOrDefault();
			var ilMethod = CreateILAst(disposeMethod);

			disposingField = FindDisposingField(ilMethod);
			if (disposingField?.DeclaringType != enumeratorType)
				throw new SymbolicAnalysisFailedException();
		}

		static FieldDef FindDisposingField(ILBlock method) {
			var body = method.Body;
			if (body.Count == 0)
				return null;

			// It's the first one
			IField f;
			ILExpression ldthis, ldci4;
			if (!body[0].Match(ILCode.Stfld, out f, out ldthis, out ldci4) || !ldthis.MatchThis() || !ldci4.MatchLdcI4(1))
				return null;
			return GetFieldDefinition(f);
		}

		protected override void AnalyzeMoveNext() {
			var methodMoveNext = MethodUtils.GetMethod_MoveNext(enumeratorType).FirstOrDefault();
			var ilMethod = CreateILAst(methodMoveNext);
			iteratorMoveNextMethod = methodMoveNext;

			var body = ilMethod.Body;
			if (body.Count < 3)
				throw new SymbolicAnalysisFailedException();
			int pos = 0;
			ILExpression ldci4;
			// stloc(VB$doFinallyBodies, ldc.i4(1))
			if (body[pos].Match(ILCode.Stloc, out doFinallyBodiesVar, out ldci4)) {
				if (!ldci4.MatchLdcI4(1))
					throw new SymbolicAnalysisFailedException();
				pos++;
			}

			var tryCatchBlock = body[pos++] as ILTryCatchBlock;
			if (tryCatchBlock == null)
				throw new SymbolicAnalysisFailedException();
			if (tryCatchBlock.FinallyBlock != null || tryCatchBlock.FaultBlock != null)
				throw new SymbolicAnalysisFailedException();
			if (tryCatchBlock.CatchBlocks.Count != 1)
				throw new SymbolicAnalysisFailedException();
			var cb = tryCatchBlock.CatchBlocks[0];
			if (cb.FilterBlock != null)
				throw new SymbolicAnalysisFailedException();
			if (cb.ExceptionType?.FullName != "System.Exception")
				throw new SymbolicAnalysisFailedException();
			// Verify catch body:
			//	stfld($State, ldloc(this), ldc.i4(4))
			//	rethrow()
			ILExpression ldloc;
			IField f;
			if (cb.Body.Count != 2)
				throw new SymbolicAnalysisFailedException();
			if (!cb.Body[0].Match(ILCode.Stfld, out f, out ldloc, out ldci4) || ldci4.Code != ILCode.Ldc_I4 || !ldloc.MatchThis())
				throw new SymbolicAnalysisFailedException();
			if (GetFieldDefinition(f) != stateField)
				throw new SymbolicAnalysisFailedException();
			if (!cb.Body[1].Match(ILCode.Rethrow))
				throw new SymbolicAnalysisFailedException();

			ILVariable returnVariableTmp = null;
			if (pos + 5 <= body.Count) {
				returnFalseLabel = body[pos++] as ILLabel;
				if (returnFalseLabel == null)
					throw new SymbolicAnalysisFailedException();
				if (!body[pos++].Match(ILCode.Stfld, out f, out ldloc, out ldci4) || ldci4.Code != ILCode.Ldc_I4 || !ldloc.MatchThis())
					throw new SymbolicAnalysisFailedException();
				if (body[pos].Match(ILCode.Ret, out ldci4) && ldci4.MatchLdcI4(0))
					pos++;
				else if (body[pos].Match(ILCode.Stloc, out returnVariableTmp, out ldci4) && ldci4.MatchLdcI4(0))
					pos++;
				else
					throw new SymbolicAnalysisFailedException();
			}
			returnLabel = body[pos++] as ILLabel;
			if (returnLabel == null)
				throw new SymbolicAnalysisFailedException();
			if (pos >= body.Count)
				throw new SymbolicAnalysisFailedException();
			if (!body[pos++].Match(ILCode.Ret, out ldloc))
				throw new SymbolicAnalysisFailedException();
			if (!ldloc.Match(ILCode.Ldloc, out returnVariable))
				throw new SymbolicAnalysisFailedException();
			if (returnVariableTmp != null && returnVariableTmp != returnVariable)
				throw new SymbolicAnalysisFailedException();

			body = tryCatchBlock.TryBlock.Body;
			var bodyLength = body.Count;
			var rangeAnalysis = new MicrosoftStateRangeAnalysis(body[0], StateRangeAnalysisMode.IteratorMoveNext, stateField);
			pos = rangeAnalysis.AssignStateRanges(body, bodyLength);
			rangeAnalysis.EnsureLabelAtPos(body, ref pos, ref bodyLength);

			labels = rangeAnalysis.CreateLabelRangeMapping(body, pos, bodyLength);
			stateVariables = rangeAnalysis.StateVariables;
			ConvertBody(body, pos, bodyLength);
		}
		List<KeyValuePair<ILLabel, StateRange>> labels;
		List<ILVariable> stateVariables;
		ILVariable returnVariable;
		ILLabel returnLabel;
		ILLabel returnFalseLabel;

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
				newBody.Add(MakeGoTo(labels, -1));
			ConvertBodyCore(body, body, newBody, startPos, bodyLength);
			newBody.Add(CreateYieldBreak());
		}

		List<ILNode> ConvertBodyCore(List<ILNode> origTopLevelBody, List<ILNode> body, List<ILNode> newBody, int startPos, int bodyLength) {
			int labelsCount = labels.Count;
			int currentState = -1;
			int val;
			ILVariable v;
			ILExpression ldthis, ldci4, ldfld;
			ILLabel lbl;
			IField f;
			for (int pos = startPos; pos < bodyLength; pos++) {
				var expr = body[pos] as ILExpression;
				switch (expr?.Code ?? (ILCode)(-1)) {
				case ILCode.Stfld:
					if (expr.Arguments[0].MatchThis()) {
						FieldDef field;
						if ((field = GetFieldDefinition(expr.Operand as IField)) == stateField) {
							if (expr.Arguments[1].Code != ILCode.Ldc_I4)
								throw new SymbolicAnalysisFailedException();
							currentState = (int)expr.Arguments[1].Operand;
						}
						else if (field == currentField)
							newBody.Add(CreateYieldReturn(expr));
						else
							newBody.Add(body[pos]);
						break;
					}
					goto default;

				case ILCode.Stloc:
					v = (ILVariable)expr.Operand;
					if (v == returnVariable) {
						pos++;
						var br = body[pos] as ILExpression;
						if (br == null || br.Code != ILCode.Leave || expr.Arguments[0].Code != ILCode.Ldc_I4)
							throw new SymbolicAnalysisFailedException();
						if (br.Operand != returnLabel)
							throw new SymbolicAnalysisFailedException();
						val = (int)expr.Arguments[0].Operand;
						if (val == 0)
							newBody.Add(CreateYieldBreak(expr));
						else if (val == 1)
							newBody.Add(MakeGoTo(labels, currentState));
						else
							throw new SymbolicAnalysisFailedException();
						break;
					}
					else if (v == doFinallyBodiesVar)
						break;
					else if (pos + 1 < bodyLength && expr.Arguments[0].MatchLdloc(doFinallyBodiesVar)) {
						ILExpression logicnot, ldloc;
						if (body[pos + 1].Match(ILCode.Brtrue, out lbl, out logicnot) && logicnot.Match(ILCode.LogicNot, out ldloc) && ldloc.MatchLdloc(v)) {
							pos++;
							break;
						}
					}
					else if (pos + 1 < bodyLength && expr.Arguments[0].Match(ILCode.Ldfld, out f, out ldthis) && ldthis.MatchThis() && GetFieldDefinition(f) == disposingField) {
						ILExpression logicnot, ldloc;
						if (body[pos + 1].Match(ILCode.Brtrue, out lbl, out logicnot) && logicnot.Match(ILCode.LogicNot, out ldloc) && ldloc.MatchLdloc(v)) {
							pos++;
							newBody.Add(new ILExpression(ILCode.Br, lbl));
							break;
						}
					}
					goto default;

				case ILCode.Switch:
					var arg = expr.Arguments[0];
					if (arg.Match(ILCode.Ldfld, out f, out ldthis) && ldthis.MatchThis() && GetFieldDefinition(f) == stateField)
						val = 0;
					else if ((arg.Code == ILCode.Sub_Ovf || arg.Code == ILCode.Sub) &&
						arg.Arguments[0].Match(ILCode.Ldfld, out f, out ldthis) && ldthis.MatchThis() &&
						arg.Arguments[1].Match(ILCode.Ldc_I4, out val) &&
						GetFieldDefinition(f) == stateField) {
					}
					else
						break;
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
					var sr = new StateRange(int.MinValue, val - 1);
					sr.UnionWith(new StateRange(val + targetLabels.Length, int.MaxValue));
					lbl = body[pos + 1] as ILLabel;
					if (lbl == null) {
						lbl = CreateTempLabel();
						body.Insert(pos + 1, lbl);
						bodyLength++;
					}
					labels.Add(new KeyValuePair<ILLabel, StateRange>(lbl, sr));
					break;

				case ILCode.Leave:
					if (expr.Operand == returnFalseLabel) {
						newBody.Add(CreateYieldBreak(expr));
						break;
					}
					goto default;

				case ILCode.Brtrue:
					if (expr.Arguments[0].Match(ILCode.LogicNot, out ldfld)) {
						if (ldfld.Match(ILCode.Ldfld, out f, out ldthis) && ldthis.MatchThis() && GetFieldDefinition(f) == disposingField) {
							var targetLbl = (ILLabel)expr.Operand;
							if (pos + 2 < bodyLength && body[pos + 1].Match(ILCode.Stloc, out v, out ldci4) && v == returnVariable &&
								ldci4.MatchLdcI4(0) && body[pos + 2].Match(ILCode.Leave, out lbl) && lbl == returnLabel) {
								pos += 2;
							}
							newBody.Add(new ILExpression(ILCode.Br, targetLbl));
							break;
						}
						else if (doFinallyBodiesVar != null && ldfld.MatchLdloc(doFinallyBodiesVar))
							break;
					}
					goto default;

				default:
					if (expr == null) {
						var tryCatch = body[pos] as ILTryCatchBlock;
						if (tryCatch != null) {
							if (tryCatch.TryBlock != null)
								ConvertBodyCore(origTopLevelBody, ref tryCatch.TryBlock.Body);
							foreach (var cb in tryCatch.CatchBlocks) {
								ConvertBodyCore(origTopLevelBody, ref cb.Body);
								if (cb.FilterBlock != null)
									ConvertBodyCore(origTopLevelBody, ref cb.FilterBlock.Body);
							}
							if (tryCatch.FinallyBlock != null)
								ConvertBodyCore(origTopLevelBody, ref tryCatch.FinallyBlock.Body);
							if (tryCatch.FaultBlock != null)
								ConvertBodyCore(origTopLevelBody, ref tryCatch.FaultBlock.Body);
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

		ILLabel CreateTempLabel() => new ILLabel { Name = "__tmp_lbl_iter" + (tempLabelCounter++).ToString() };
		int tempLabelCounter;

		void ConvertBodyCore(List<ILNode> origTopLevelBody, ref List<ILNode> body) {
			List<ILNode> newBody;
			if (freeBodies.Count > 0) {
				newBody = freeBodies[freeBodies.Count - 1];
				freeBodies.RemoveAt(freeBodies.Count - 1);
			}
			else
				newBody = new List<ILNode>();
			ConvertBodyCore(origTopLevelBody, body, newBody, 0, body.Count);
			body.Clear();
			freeBodies.Add(body);
			body = newBody;
		}
		readonly List<List<ILNode>> freeBodies = new List<List<ILNode>>();

		ILExpression MakeGoTo(ILLabel targetLabel) {
			Debug.Assert(targetLabel != null);
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
	}
}
