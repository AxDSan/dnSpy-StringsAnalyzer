// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;

namespace ICSharpCode.Decompiler.ILAst {
	sealed class MicrosoftYieldReturnDecompiler : YieldReturnDecompiler {
		public override string CompilerName => compilerName;
		string compilerName;

		MicrosoftYieldReturnDecompiler(DecompilerContext context, AutoPropertyProvider autoPropertyProvider)
			: base(context, autoPropertyProvider) {
		}

		public static YieldReturnDecompiler TryCreateCore(DecompilerContext context, ILBlock method, AutoPropertyProvider autoPropertyProvider) {
			var yrd = new MicrosoftYieldReturnDecompiler(context, autoPropertyProvider);
			if (!yrd.MatchEnumeratorCreationPattern(method))
				return null;
			yrd.enumeratorType = yrd.enumeratorCtor.DeclaringType;
			return yrd;
		}

		#region Match the enumerator creation pattern
		bool MatchEnumeratorCreationPattern(ILBlock method) {
			var body = method.Body;
			if (body.Count == 0)
				return false;
			ILExpression newObj;
			if (body.Count == 1) {
				// ret(newobj(...))
				if (body[0].Match(ILCode.Ret, out newObj))
					return MatchEnumeratorCreationNewObj(newObj, out enumeratorCtor);
				else
					return false;
			}
			// stloc(var_1, newobj(..)
			ILVariable var1;
			if (!body[0].Match(ILCode.Stloc, out var1, out newObj))
				return false;
			if (!MatchEnumeratorCreationNewObj(newObj, out enumeratorCtor))
				return false;

			int i = 1;
			if (!InitializeFieldToParameterMap(method, var1, ref i))
				return false;

			ILVariable var2;
			ILExpression ldlocForStloc2;
			if (i < body.Count && body[i].Match(ILCode.Stloc, out var2, out ldlocForStloc2)) {
				// stloc(var_2, ldloc(var_1))
				if (ldlocForStloc2.Code != ILCode.Ldloc || ldlocForStloc2.Operand != var1)
					return false;
				i++;
			}
			else {
				// the compiler might skip the above instruction in release builds; in that case, it directly returns stloc.Operand
				var2 = var1;
			}
			ILExpression retArg;
			if (i < body.Count && body[i].Match(ILCode.Ret, out retArg)) {
				// ret(ldloc(var_2))
				if (retArg.Code == ILCode.Ldloc && retArg.Operand == var2)
					return true;
			}
			return false;
		}

		bool MatchEnumeratorCreationNewObj(ILExpression expr, out MethodDef ctor) {
			// newobj(CurrentType/...::.ctor, ldc.i4(-2))
			ctor = null;
			if (expr.Code != ILCode.Newobj || expr.Arguments.Count != 1)
				return false;
			if (expr.Arguments[0].Code != ILCode.Ldc_I4)
				return false;
			int initialState = (int)expr.Arguments[0].Operand;
			if (!(initialState == -2 || initialState == 0))
				return false;
			ctor = GetMethodDefinition(expr.Operand as IMethod);
			if (ctor == null || ctor.DeclaringType.DeclaringType != context.CurrentType)
				return false;
			return IsCompilerGeneratorEnumerator(ctor.DeclaringType);
		}
		#endregion

		#region Figure out what the 'state' field is (analysis of .ctor())
		/// <summary>
		/// Looks at the enumerator's ctor and figures out which of the fields holds the state.
		/// </summary>
		protected override void AnalyzeCtor() {
			ILBlock method = CreateILAst(enumeratorCtor);

			foreach (ILNode node in method.Body) {
				IField field;
				ILExpression instExpr;
				ILExpression stExpr;
				ILVariable arg;
				if (node.Match(ILCode.Stfld, out field, out instExpr, out stExpr) &&
					instExpr.MatchThis() &&
					stExpr.Match(ILCode.Ldloc, out arg) &&
					arg.IsParameter && arg.OriginalParameter.MethodSigIndex == 0) {
					stateField = GetFieldDefinition(field);
					break;
				}
			}
			if (stateField == null)
				throw new SymbolicAnalysisFailedException();
		}
		#endregion

		#region Construction of the exception table (analysis of Dispose())
		// We construct the exception table by analyzing the enumerator's Dispose() method.

		// Assumption: there are no loops/backward jumps
		// We 'run' the code, with "state" being a symbolic variable
		// so it can form expressions like "state + x" (when there's a sub instruction)
		// For each instruction, we maintain a list of value ranges for state for which the instruction is reachable.
		// This is (int.MinValue, int.MaxValue) for the first instruction.
		// These ranges are propagated depending on the conditional jumps performed by the code.

		protected override void AnalyzeDispose() {
			disposeMethod = MethodUtils.GetMethod_Dispose(enumeratorType).FirstOrDefault();
			var ilMethod = CreateILAst(disposeMethod);

			methodMoveNext = MethodUtils.GetMethod_MoveNext(enumeratorType).FirstOrDefault();
			if (IsVisualBasicDispose(ilMethod, methodMoveNext)) {
				finallyMethodToStateRange = new Dictionary<MethodDef, StateRange>();
				vbFinalizerStates = new HashSet<int>();
				// This is a very simple format, no need to use MicrosoftStateRangeAnalysis.
				//
				// The Dispose() states are only set by Dispose(), which calls MoveNext().
				// MoveNext() will branch to the correct try block and then 'leave' it so the
				// finally blocks get executed.
				foreach (var node in ilMethod.Body) {
					var expr = node as ILExpression;
					// stfld($State, ldloc(this), ldc.i4(11))
					IField f;
					ILExpression ldloc, ldci4;
					int disposeState;
					if (!expr.Match(ILCode.Stfld, out f, out ldloc, out ldci4) || !ldloc.MatchThis() || !ldci4.Match(ILCode.Ldc_I4, out disposeState))
						continue;
					if (GetFieldDefinition(f) != stateField)
						continue;
					vbFinalizerStates.Add(disposeState);
				}
			}
			else {
				var rangeAnalysis = new MicrosoftStateRangeAnalysis(ilMethod.Body[0], StateRangeAnalysisMode.IteratorDispose, stateField);
				rangeAnalysis.AssignStateRanges(ilMethod.Body, ilMethod.Body.Count);
				finallyMethodToStateRange = rangeAnalysis.finallyMethodToStateRange;

				// Now look at the finally blocks:
				foreach (var tryFinally in ilMethod.GetSelfAndChildrenRecursive<ILTryCatchBlock>()) {
					var range = rangeAnalysis.ranges[tryFinally.TryBlock.Body[0]];
					var finallyBody = tryFinally.FinallyBlock.Body;
					if (finallyBody.Count != 2)
						throw new SymbolicAnalysisFailedException();
					ILExpression call = finallyBody[0] as ILExpression;
					if (call == null || call.Code != ILCode.Call || call.Arguments.Count != 1)
						throw new SymbolicAnalysisFailedException();
					if (!call.Arguments[0].MatchThis())
						throw new SymbolicAnalysisFailedException();
					if (!finallyBody[1].Match(ILCode.Endfinally))
						throw new SymbolicAnalysisFailedException();

					MethodDef mdef = GetMethodDefinition(call.Operand as IMethod);
					if (mdef == null || finallyMethodToStateRange.ContainsKey(mdef))
						throw new SymbolicAnalysisFailedException();
					finallyMethodToStateRange.Add(mdef, range);
				}
			}
		}
		MethodDef methodMoveNext;
		HashSet<int> vbFinalizerStates;

		// VB's Dispose() method doesn't call the finally method, it just calls MoveNext()
		static bool IsVisualBasicDispose(ILBlock method, MethodDef moveNextMethod) {
			foreach (var node in method.Body) {
				// There should be no exception handlers if it's a VB Dispose() method so no need
				// to check for anything else than ILExpression
				IMethod m;
				ILExpression ldloc;
				if (!node.Match(ILCode.Call, out m, out ldloc) || !ldloc.MatchThis())
					continue;
				if (GetMethodDefinition(m) == moveNextMethod)
					return true;
			}
			return false;
		}
		#endregion
		Dictionary<MethodDef, StateRange> finallyMethodToStateRange;
		ILVariable returnVariable;
		ILLabel returnLabel;
		ILLabel returnFalseLabel;

		#region Analysis of MoveNext()
		protected override void AnalyzeMoveNext() {
			ILBlock ilMethod = CreateILAst(methodMoveNext);
			iteratorMoveNextMethod = methodMoveNext;
			if (methodMoveNext.DeclaringType.Name.StartsWith("VB$StateMachine_"))
				compilerName = PredefinedCompilerNames.MicrosoftVisualBasic;
			else
				compilerName = PredefinedCompilerNames.MicrosoftCSharp;

			if (ilMethod.Body.Count == 0)
				throw new SymbolicAnalysisFailedException();
			ILExpression lastReturnArg;
			if (!ilMethod.Body.Last().Match(ILCode.Ret, out lastReturnArg)) {
				// The last instruction isn't guaranteed to be a ret, could also be a br
				var body2 = ilMethod.Body;
				ILExpression loadExpr = null, ldloc = null, ldci4 = null;
				for (int i = 0; i < body2.Count; i++) {
					if (!body2[i].Match(ILCode.Ret, out loadExpr))
						continue;
					if (ldci4 == null && loadExpr.Code == ILCode.Ldc_I4 && (int)loadExpr.Operand == 0)
						ldci4 = loadExpr;
					else if (loadExpr.Match(ILCode.Ldloc)) {
						ldloc = loadExpr;
						break;
					}
				}
				lastReturnArg = ldloc ?? ldci4;
				if (lastReturnArg == null)
					throw new SymbolicAnalysisFailedException();
			}

			// There are two possibilities:
			if (lastReturnArg.Code == ILCode.Ldloc) {
				// a) the compiler uses a variable for returns (in debug builds, or when there are try-finally blocks)
				returnVariable = (ILVariable)lastReturnArg.Operand;
				returnLabel = ilMethod.Body.ElementAtOrDefault(ilMethod.Body.Count - 2) as ILLabel;
				if (returnLabel == null)
					throw new SymbolicAnalysisFailedException();
			}
			else {
				// b) the compiler directly returns constants
				returnVariable = null;
				returnLabel = null;
				// In this case, the last return must return false.
				if (lastReturnArg.Code != ILCode.Ldc_I4 || (int)lastReturnArg.Operand != 0)
					throw new SymbolicAnalysisFailedException();
			}

			ILTryCatchBlock tryFaultBlock = ilMethod.Body[0] as ILTryCatchBlock;
			List<ILNode> body;
			int bodyLength;
			if (tryFaultBlock != null) {
				// there are try-finally blocks
				if (returnVariable == null) // in this case, we must use a return variable
					throw new SymbolicAnalysisFailedException();
				// must be a try-fault block:
				if (tryFaultBlock.CatchBlocks.Count != 0 || tryFaultBlock.FinallyBlock != null || tryFaultBlock.FaultBlock == null)
					throw new SymbolicAnalysisFailedException();

				ILBlock faultBlock = tryFaultBlock.FaultBlock;
				// Ensure the fault block contains the call to Dispose().
				if (faultBlock.Body.Count != 2)
					throw new SymbolicAnalysisFailedException();
				IMethod disposeMethodRef;
				ILExpression disposeArg;
				if (!faultBlock.Body[0].Match(ILCode.Call, out disposeMethodRef, out disposeArg))
					throw new SymbolicAnalysisFailedException();
				if (GetMethodDefinition(disposeMethodRef) != disposeMethod || !disposeArg.MatchThis())
					throw new SymbolicAnalysisFailedException();
				if (!faultBlock.Body[1].Match(ILCode.Endfinally))
					throw new SymbolicAnalysisFailedException();

				body = tryFaultBlock.TryBlock.Body;
				bodyLength = body.Count;
			}
			else {
				// no try-finally blocks
				body = ilMethod.Body;
				var lastInstr = body[body.Count - 1] as ILExpression;
				if (lastInstr != null && lastInstr.Code == ILCode.Ret) {
					if (returnVariable == null)
						bodyLength = body.Count - 1; // all except for the return statement
					else
						bodyLength = body.Count - 2; // all except for the return label and statement
				}
				else
					bodyLength = body.Count;
			}

			// Now check if the last instruction in the body is 'ret(false)'
			if (returnVariable != null) {
				// If we don't have a return variable, we already verified that above.
				// If we do have one, check for 'stloc(returnVariable, ldc.i4(0))'

				var origBodyLength = bodyLength;
				// Maybe might be a jump to the return label after the stloc:
				ILExpression leave = body.ElementAtOrDefault(bodyLength - 1) as ILExpression;
				if (leave != null && (leave.Code == ILCode.Br || leave.Code == ILCode.Leave) && leave.Operand == returnLabel)
					bodyLength--;
				ILExpression store0 = body.ElementAtOrDefault(bodyLength - 1) as ILExpression;
				ILExpression expr;
				if ((store0 != null && store0.Code == ILCode.Stloc && store0.Operand == returnVariable) || store0.Match(ILCode.Ret, out expr)) {
					if (store0.Arguments[0].Code != ILCode.Ldc_I4 || (int)store0.Arguments[0].Operand != 0)
						throw new SymbolicAnalysisFailedException();
					bodyLength--; // don't consider the stloc instruction to be part of the body
				}
				else if (store0 != null && store0.Code == ILCode.Throw) {
					// Nothing
				}
				else
					bodyLength = origBodyLength;
			}
			// The last element in the body usually is a label pointing to the 'ret(false)'
			returnFalseLabel = body.ElementAtOrDefault(bodyLength - 1) as ILLabel;
			// Note: in Roslyn-compiled code, returnFalseLabel may be null.

			// C#7 caches original 'this' parameter in a local
			// stloc(cachedThis, ldfld(<>4__this, ldloc(this)))
			ILExpression ldfld;
			ILVariable v;
			const int CACHED_THIS_INDEX = 1;
			if (body.Count > CACHED_THIS_INDEX && body[CACHED_THIS_INDEX].Match(ILCode.Stloc, out v, out ldfld)) {
				ILExpression ldthis;
				IField f;
				if (ldfld.Match(ILCode.Ldfld, out f, out ldthis) && ldthis.MatchThis()) {
					var fd = f.ResolveFieldWithinSameModule();
					if (fd?.DeclaringType == this.enumeratorType && fd.FieldType.RemovePinnedAndModifiers().Resolve() == context.CurrentType) {
						ILVariable realVar;
						if (variableMap.TryGetParameter(fd, out realVar) && realVar.IsParameter && realVar.OriginalParameter.IsHiddenThisParameter) {
							cachedThisVar = v;
							body.RemoveAt(CACHED_THIS_INDEX);
							bodyLength--;
						}
					}
				}
			}

			var rangeAnalysis = new MicrosoftStateRangeAnalysis(body[0], StateRangeAnalysisMode.IteratorMoveNext, stateField);
			int pos = rangeAnalysis.AssignStateRanges(body, bodyLength);
			rangeAnalysis.EnsureLabelAtPos(body, ref pos, ref bodyLength);

			labels = rangeAnalysis.CreateLabelRangeMapping(body, pos, bodyLength);
			stateVariables = rangeAnalysis.StateVariables;
			ConvertBody(body, pos, bodyLength);
		}
		List<KeyValuePair<ILLabel, StateRange>> labels;
		List<ILVariable> stateVariables;
		#endregion

		#region ConvertBody
		struct SetState {
			public readonly int NewBodyPos;
			public readonly int NewState;

			public SetState(int newBodyPos, int newState) {
				NewBodyPos = newBodyPos;
				NewState = newState;
			}
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
			ConvertBodyCore(body, body, newBody, startPos, bodyLength);
			newBody.Add(CreateYieldBreak());
		}

		List<ILNode> ConvertBodyCore(List<ILNode> origTopLevelBody, List<ILNode> body, List<ILNode> newBody, int startPos, int bodyLength) {
			int labelsCount = labels.Count;
			List<SetState> stateChanges = new List<SetState>();
			var calledFinallyMethods = new List<MethodDef>();
			int currentState = -1;
			int val;
			ILVariable v;
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
							stateChanges.Add(new SetState(newBody.Count, currentState));
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
					if (expr.Operand == returnVariable) {
						var br = body.ElementAtOrDefault(++pos) as ILExpression;
						if (br == null || !(br.Code == ILCode.Br || br.Code == ILCode.Leave) || expr.Arguments[0].Code != ILCode.Ldc_I4)
							throw new SymbolicAnalysisFailedException();
						if (br.Operand != returnLabel) {
							UpdateFinallyMethodIndex(origTopLevelBody, br, ref finallyMethodIndexes);
							newBody.Add(CreateYieldBreak(expr));
						}
						else {
							val = (int)expr.Arguments[0].Operand;
							if (val == 0)
								newBody.Add(CreateYieldBreak(expr));
							else if (val == 1)
								newBody.Add(MakeGoTo(labels, currentState));
							else
								throw new SymbolicAnalysisFailedException();
						}
						break;
					}
					else if (expr.Arguments[0].Match(ILCode.Ldc_I4, out val) && pos + 2 <= bodyLength) {
						// Could be a Visual Basic iterator method with finally handlers
						//		expr_29 = ldc.i4(N)
						//		stloc(var_1, expr_29)
						//		stfld($State, ldloc(this), expr_29)
						if (!v.GeneratedByDecompiler)
							goto default;
						ILVariable tempState;
						ILExpression ldloc;
						if (!body[pos + 1].Match(ILCode.Stloc, out tempState, out ldloc) || !ldloc.MatchLdloc(v) || !stateVariables.Contains(tempState))
							goto default;
						IField f;
						ILExpression ldloc2;
						if (!body[pos + 2].Match(ILCode.Stfld, out f, out ldloc, out ldloc2) || !ldloc2.MatchLdloc(v) || !ldloc.MatchThis() || GetFieldDefinition(f) != stateField)
							goto default;
						currentState = val;
						stateChanges.Add(new SetState(newBody.Count, currentState));
						pos += 2;
						break;
					}
					goto default;

				case ILCode.Brtrue:
					// Generated by VB compiler only
					if (pos == 0 && vbFinalizerStates != null) {
						var rangeAnalysis = new MicrosoftStateRangeAnalysis(body[pos], StateRangeAnalysisMode.IteratorMoveNext, stateField, stateVariables.FirstOrDefault());
						int newPos = rangeAnalysis.AssignStateRanges(body, pos, bodyLength);
						if (newPos != pos) {
							rangeAnalysis.EnsureLabelAtPos(body, ref newPos, ref bodyLength);
							var newMappings = rangeAnalysis.CreateLabelRangeMapping(body, pos, bodyLength);
							labels.AddRange(newMappings);
							pos = newPos - 1;
							break;
						}
					}
					if (expr.Arguments[0].Code == ILCode.Cge) {
						var cge = expr.Arguments[0];
						if (cge.Arguments[0].Match(ILCode.Ldloc, out v) && stateVariables.Contains(v) && cge.Arguments[1].MatchLdcI4(0))
							break;
					}
					goto default;

				case ILCode.Switch:
					var arg = expr.Arguments[0];
					if ((arg.Code == ILCode.Sub || arg.Code == ILCode.Sub_Ovf) && arg.Arguments[0].Match(ILCode.Ldloc, out v) && arg.Arguments[1].Match(ILCode.Ldc_I4, out val) && stateVariables.Contains(v)) {
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
						var lbl = body[pos + 1] as ILLabel;
						if (lbl == null) {
							lbl = CreateTempLabel();
							body.Insert(pos + 1, lbl);
							bodyLength++;
						}
						labels.Add(new KeyValuePair<ILLabel, StateRange>(lbl, sr));
						break;
					}
					goto default;

				case ILCode.Ret:
					if (expr.Arguments.Count != 1 || expr.Arguments[0].Code != ILCode.Ldc_I4)
						throw new SymbolicAnalysisFailedException();
					val = (int)expr.Arguments[0].Operand;
					if (val == 0)
						newBody.Add(CreateYieldBreak(expr));
					else if (val == 1)
						newBody.Add(MakeGoTo(labels, currentState));
					else
						throw new SymbolicAnalysisFailedException();
					break;

				case ILCode.Call:
					if (expr.Arguments.Count == 1 && expr.Arguments[0].MatchThis()) {
						var method = GetMethodDefinition(expr.Operand as IMethod);
						if (method == null)
							throw new SymbolicAnalysisFailedException();
						StateRange stateRange;
						if (method == disposeMethod) {
							// Explicit call to dispose is used for "yield break;" within the method.
							ILExpression br = body.ElementAtOrDefault(++pos) as ILExpression;
							if (br == null || !(br.Code == ILCode.Br || br.Code == ILCode.Leave) || br.Operand != returnFalseLabel)
								throw new SymbolicAnalysisFailedException();
							newBody.Add(CreateYieldBreak(expr));
						}
						else if (finallyMethodToStateRange.TryGetValue(method, out stateRange)) {
							bool hasBeenCalled = calledFinallyMethods.Contains(method);
							if (!hasBeenCalled)
								calledFinallyMethods.Add(method);
							bool yieldBreakPath = finallyMethodIndexes?.Contains(pos - 1) == true;
							if (yieldBreakPath)
								pos++;
							if (hasBeenCalled)
								continue;

							// Call to Finally-method
							int index = stateChanges.FindIndex(ss => stateRange.Contains(ss.NewState));
							if (index < 0)
								throw new SymbolicAnalysisFailedException();

							var finallyBlock = ConvertFinallyBlock(method);
							if (finallyBlock.Body.Count == 1 && finallyBlock.Body[0].Match(ILCode.Endfinally))
								continue;

							ILLabel label = new ILLabel();
							label.Name = "JumpOutOfTryFinally" + stateChanges[index].NewState.ToString();
							newBody.Add(new ILExpression(ILCode.Leave, label));

							SetState stateChange = stateChanges[index];
							// Move all instructions from stateChange.Pos to newBody.Count into a try-block
							stateChanges.RemoveRange(index, stateChanges.Count - index); // remove all state changes up to the one we found
							ILTryCatchBlock tryFinally = new ILTryCatchBlock();
							tryFinally.TryBlock = new ILBlock(newBody.GetRange(stateChange.NewBodyPos, newBody.Count - stateChange.NewBodyPos), CodeBracesRangeFlags.TryBraces);
							newBody.RemoveRange(stateChange.NewBodyPos, newBody.Count - stateChange.NewBodyPos); // remove all nodes that we just moved into the try block
							tryFinally.CatchBlocks = new List<ILTryCatchBlock.CatchBlock>();
							tryFinally.FinallyBlock = finallyBlock;
							tryFinally.InlinedFinallyMethod = method;
							newBody.Add(tryFinally);
							newBody.Add(label);
						}
						break;
					}
					goto default;

				case ILCode.Br:
				case ILCode.Leave:
					if (expr.Operand == returnFalseLabel && origTopLevelBody != body) {
						newBody.Add(CreateYieldBreak(expr));
						break;
					}
					goto default;

				default:
					if (expr == null) {
						ILLabel lbl;
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
						else if (vbFinalizerStates != null && (lbl = body[pos] as ILLabel) != null) {
							var sr = labels.LastOrDefault(a => a.Key == lbl && a.Value.TryGetSingleState() != null).Value;
							var disposeState = sr?.TryGetSingleState();
							if (disposeState != null && vbFinalizerStates.Contains(disposeState.Value)) {
								// The code just leaves the try block so all finally handlers get executed.
								// It's called from Dispose(). This code can be ignored.
								//
								// Must match:
								//	IL_80:	// current position (pos)
								//	expr_82 = ldc.i4(-1)
								//	stloc($VB$ResumableLocal_c$1, expr_82)
								//	stfld($State, ldloc(this), expr_82)
								//	stloc(var_0, ldc.i4(1))
								//	leave(IL_176)
								//
								// In Debug builds, the target is a br to the real target
								ILLabel realTarget;
								if (body[pos + 1].Match(ILCode.Br, out realTarget)) {
									labels.Add(new KeyValuePair<ILLabel, StateRange>(realTarget, new StateRange(disposeState.Value, disposeState.Value)));
									pos++;
									break;
								}

								if (pos + 5 > bodyLength)
									throw new SymbolicAnalysisFailedException();
								ILVariable tmpV;
								ILLabel leaveLbl;
								IField f;
								ILExpression ldci4, ldloc, ldthis;
								if (!body[pos + 1].Match(ILCode.Stloc, out tmpV, out ldci4) || !ldci4.MatchLdcI4(-1))
									throw new SymbolicAnalysisFailedException();
								if (!body[pos + 2].Match(ILCode.Stloc, out v, out ldloc) || !ldloc.MatchLdloc(tmpV) || !stateVariables.Contains(v))
									throw new SymbolicAnalysisFailedException();
								if (!body[pos + 3].Match(ILCode.Stfld, out f, out ldthis, out ldloc) || !ldthis.MatchThis() || !ldloc.MatchLdloc(tmpV))
									throw new SymbolicAnalysisFailedException();
								if (GetFieldDefinition(f) != stateField)
									throw new SymbolicAnalysisFailedException();
								if (!body[pos + 4].Match(ILCode.Stloc, out v, out ldci4) || !ldci4.MatchLdcI4(1) || v != returnVariable)
									throw new SymbolicAnalysisFailedException();
								if (!body[pos + 5].Match(ILCode.Leave, out leaveLbl) || leaveLbl != returnLabel)
									throw new SymbolicAnalysisFailedException();

								pos += 5;
								break;
							}
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
		List<int> finallyMethodIndexes;
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

		void UpdateFinallyMethodIndex(List<ILNode> body, ILExpression br, ref List<int> finallyMethodIndexes) {
			for (;;) {
				if (br.Operand == returnLabel)
					break;
				int brTargetIndex = body.IndexOf((ILNode)br.Operand);
				IMethod method;
				MethodDef md;
				ILExpression thisExpr;
				if (brTargetIndex < 0 || !(body[brTargetIndex] is ILLabel) || brTargetIndex + 2 >= body.Count ||
					!body[brTargetIndex + 1].Match(ILCode.Call, out method, out thisExpr) || !thisExpr.MatchThis() ||
					(md = GetMethodDefinition(method)) == null || !finallyMethodToStateRange.ContainsKey(md))
					throw new SymbolicAnalysisFailedException();
				if (finallyMethodIndexes == null)
					finallyMethodIndexes = new List<int>(1) { brTargetIndex };
				else if (!finallyMethodIndexes.Contains(brTargetIndex))
					finallyMethodIndexes.Add(brTargetIndex);

				br = body[brTargetIndex + 2] as ILExpression;
				if (br == null || !(br.Code == ILCode.Br || br.Code == ILCode.Leave))
					throw new SymbolicAnalysisFailedException();
			}
		}

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

		ILBlock ConvertFinallyBlock(MethodDef finallyMethod) {
			ILBlock block = CreateILAst(finallyMethod);
			// Get rid of assignment to state
			IField stfld;
			List<ILExpression> args;
			if (block.Body.Count > 0 && block.Body[0].Match(ILCode.Stfld, out stfld, out args)) {
				if (GetFieldDefinition(stfld) == stateField && args[0].MatchThis())
					block.Body.RemoveAt(0);
			}
			// Convert ret to endfinally
			foreach (ILExpression expr in block.GetSelfAndChildrenRecursive<ILExpression>(list_ILExpression)) {
				if (expr.Code == ILCode.Ret)
					expr.Code = ILCode.Endfinally;
			}
			return block;
		}
		#endregion
	}
}
