// Copyright (c) 2012 AlphaSierraPapa for the SharpDevelop Team
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
	/// <summary>
	/// Supports
	///		csc (11.0 - 15.0)
	///		vbc (11.0 - 15.0)
	/// </summary>
	sealed class MicrosoftAsyncDecompiler : AsyncDecompiler {
		// These fields are set by MatchTaskCreationPattern()
		ILVariable cachedStateVar;
		int initialState;

		// These fields are set by AnalyzeMoveNext()
		ILLabel setResultAndExitLabel;
		ILExpression resultExpr;
		ILVariable resultVariable;

		public override string CompilerName => compilerName;
		string compilerName;

		MicrosoftAsyncDecompiler(DecompilerContext context, AutoPropertyProvider autoPropertyProvider)
			: base(context, autoPropertyProvider) {
		}

		public static AsyncDecompiler TryCreateCore(DecompilerContext context, ILBlock method, AutoPropertyProvider autoPropertyProvider) {
			var yrd = new MicrosoftAsyncDecompiler(context, autoPropertyProvider);
			if (!yrd.MatchTaskCreationPattern(method))
				return null;
			return yrd;
		}

		#region MatchTaskCreationPattern
		bool MatchTaskCreationPattern(ILBlock method) {
			var body = method.Body;
			if (body.Count < 4)
				return false;

			// VB initializes the struct (or class in Debug builds) at the top of the kickoff method
			// C# also does that in Debug builds
			ILVariable vbStateMachineVar;
			if (IsPerhapsVisualBasicKickoffMethod(body[0] as ILExpression, out vbStateMachineVar)) {
				if (VisualBasicMatchTaskCreationPattern(body, vbStateMachineVar))
					return true;
			}

			// C#

			int pos = body.Count - 2;
			ILExpression loadBuilderExpr;
			if (MatchStartCall(body[pos], out var stateMachineVar, out var builderVar) && builderVar != null) {
				pos--;
				if (!body[pos].MatchStloc(builderVar, out loadBuilderExpr))
					return false;
				pos--;
			}
			else if (MatchStartCall(body[pos], out stateMachineVar)) {
				pos--;
				if (!body[pos + 1].Match(ILCode.Call, out IMethod _, out loadBuilderExpr, out _))
					return false;
			}
			else
				return false;

			if (!MatchBuilderField(stateMachineVar, loadBuilderExpr))
				return false;

			if (!MatchReturnTask(body[body.Count - 1], stateMachineVar))
				return false;

			// Check the last field assignment - this should be the state field
			ILExpression initialStateExpr;
			if (!MatchStFld(body[pos], stateMachineVar, stateMachineTypeIsValueType, out stateField, out initialStateExpr))
				return false;
			if (!initialStateExpr.Match(ILCode.Ldc_I4, out initialState))
				return false;
			if (initialState != -1)
				return false;

			int stopPos = pos;
			pos = 0;
			if (!stateMachineTypeIsValueType) {
				if (!body[pos].MatchStloc(stateMachineVar, out var init))
					return false;
				if (!init.Match(ILCode.Newobj, out IMethod constructor))
					return false;
				if (init.Arguments.Count != 0)
					return false;
				if (constructor.DeclaringType != stateMachineType)
					return false;
				pos++;
			}

			bool builderFieldIsInitialized = false;
			for (; pos < stopPos; pos++) {
				// stfld StateMachine.field(ldloca stateMachine, ldvar(param))
				if (!MatchStFld(body[pos], stateMachineVar, stateMachineTypeIsValueType, out var field, out var fieldInit))
					return false;
				if (field == builderField && fieldInit.Match(ILCode.Call, out IMethod createMethodRef) && createMethodRef.Name == nameCreate) {
					// stfld StateMachine.builder(ldloca stateMachine, call Create())
					builderFieldIsInitialized = true;
				}
				else if (fieldInit.Match(ILCode.Ldloc, out ILVariable v) && v.IsParameter) {
					// OK, copies parameter into state machine
					variableMap.SetParameter(field, v);
				}
				else if (fieldInit.Match(ILCode.Ldobj, out ITypeDefOrRef _, out ILExpression ldloc) && ldloc.MatchThis()) {
					variableMap.SetParameter(field, (ILVariable)ldloc.Operand);
				}
				else {
					return false;
				}
			}

			return builderFieldIsInitialized;
		}

		bool VisualBasicMatchTaskCreationPattern(List<ILNode> body, ILVariable vbStateMachineVar) {
			ILVariable stateMachineVar;
			if (!MatchStartCall(body[body.Count - 2], out stateMachineVar) || vbStateMachineVar != stateMachineVar)
				return false;
			if (!MatchCallCreate(body[body.Count - 3], stateMachineVar))
				return false;
			if (!MatchReturnTask(body[body.Count - 1], stateMachineVar))
				return false;

			// Check the last field assignment - this should be the state field
			ILExpression initialStateExpr;
			if (!MatchStFld(body[body.Count - 4], stateMachineVar, stateMachineTypeIsValueType, out stateField, out initialStateExpr))
				return false;
			if (!initialStateExpr.Match(ILCode.Ldc_I4, out initialState))
				return false;
			if (initialState != -1)
				return false;

			if (!InitializeFieldToParameterMap(body, 1, body.Count - 4, stateMachineVar))
				return false;

			return true;
		}

		bool IsPerhapsVisualBasicKickoffMethod(ILExpression expr, out ILVariable stateMachineVar) {
			ILExpression ldloca;
			ITypeDefOrRef asyncType;
			// Release builds (struct)
			if (expr.Match(ILCode.Initobj, out asyncType, out ldloca))
				return ldloca.Match(ILCode.Ldloca, out stateMachineVar);

			ILExpression newobj;
			// Debug builds (class)
			if (expr.Match(ILCode.Stloc, out stateMachineVar, out newobj)) {
				IMethod ctor;
				return newobj.Match(ILCode.Newobj, out ctor) && ctor.Name == nameCtor;
			}

			return false;
		}
		static readonly UTF8String nameCtor = new UTF8String(".ctor");

		bool MatchBuilderField(ILVariable stateMachineVar, ILExpression loadBuilderExpr) {
			IField builderFieldRef;
			ILExpression loadStateMachineForBuilderExpr;
			if (loadBuilderExpr.Match(ILCode.Ldfld, out builderFieldRef, out loadStateMachineForBuilderExpr)) {
				// OK, calling Start on copy of stateMachine.<>t__builder
			}
			else if (loadBuilderExpr.Match(ILCode.Ldflda, out builderFieldRef, out loadStateMachineForBuilderExpr)) {
				// OK, Roslyn 3.6 started directly calling Start without making a copy
			}
			else {
				return false;
			}
			if (!(loadStateMachineForBuilderExpr.MatchLdloca(stateMachineVar) || loadStateMachineForBuilderExpr.MatchLdloc(stateMachineVar)))
				return false;
			builderField = builderFieldRef.ResolveFieldWithinSameModule();
			return builderField != null;
		}
		#endregion

		#region Analyze MoveNext
		protected override void AnalyzeMoveNext(out ILMethodBody bodyInfo, out ILTryCatchBlock tryCatchBlock, out int finalState, out ILLabel exitLabel) {
			ILBlock ilMethod = CreateILAst(moveNextMethod);
			if (moveNextMethod.DeclaringType.Name.StartsWith("VB$StateMachine_"))
				compilerName = PredefinedCompilerNames.MicrosoftVisualBasic;
			else
				compilerName = PredefinedCompilerNames.MicrosoftCSharp;

			var body = ilMethod.Body;
			if (body.Count < 5)
				throw new SymbolicAnalysisFailedException();
			int pos = 0;

			ILVariable v;
			ILExpression ldci4;
			// stloc(VB$doFinallyBodies, ldc.i4(1))
			if (body[pos].Match(ILCode.Stloc, out v, out ldci4)) {
				if (ldci4.MatchLdcI4(1)) {
					doFinallyBodies = v;
					pos++;
				}
			}

			// stloc(cachedState, ldfld(valuetype StateMachineStruct::<>1__state, ldloc(this)))
			ILExpression cachedStateInit;
			if (body[pos].Match(ILCode.Stloc, out v, out cachedStateInit)) {
				ILExpression instanceExpr;
				IField loadedField;
				if (cachedStateInit.Match(ILCode.Ldfld, out loadedField, out instanceExpr) && loadedField.ResolveFieldWithinSameModule() == stateField && instanceExpr.MatchThis()) {
					cachedStateVar = v;
					pos++;
				}
			}

			// C#7 caches original 'this' parameter in a local
			// stloc(cachedThis, ldfld(<>4__this, ldloc(this)))
			ILExpression ldfld;
			if (body[pos].Match(ILCode.Stloc, out v, out ldfld)) {
				ILExpression ldthis;
				IField f;
				if (ldfld.Match(ILCode.Ldfld, out f, out ldthis) && ldthis.MatchThis()) {
					var fd = f.ResolveFieldWithinSameModule();
					if (fd?.DeclaringType == stateMachineType && fd.FieldType.RemovePinnedAndModifiers().Resolve() == context.CurrentType) {
						ILVariable realVar;
						if (variableMap.TryGetParameter(fd, out realVar) && realVar.IsParameter && realVar.OriginalParameter.IsHiddenThisParameter) {
							cachedThisVar = v;
							pos++;
						}
					}
				}
			}

			tryCatchBlock = GetMainTryCatchBlock(body[pos++]);
			if (tryCatchBlock == null)
				throw new SymbolicAnalysisFailedException();

			setResultAndExitLabel = body[pos++] as ILLabel;
			if (setResultAndExitLabel == null)
				throw new SymbolicAnalysisFailedException();

			bool methodNeverReturns = false;
			ILVariable tmpV;
			if ((body[pos] as ILExpression)?.Code == ILCode.Ret) {
				methodNeverReturns = true;
				finalState = -2;
			}
			// VB14 sometimes generates this code
			else if (body[pos].Match(ILCode.Stloc, out tmpV, out ldci4)) {
				if (!ldci4.Match(ILCode.Ldc_I4, out finalState) || finalState >= -1)
					throw new SymbolicAnalysisFailedException();
				pos++;
				ILExpression ldloc;
				if (!body[pos++].Match(ILCode.Stloc, out v, out ldloc) || !ldloc.MatchLdloc(tmpV) || v != cachedStateVar)
					throw new SymbolicAnalysisFailedException();
				IField f;
				ILExpression ldthis;
				if (!body[pos++].Match(ILCode.Stfld, out f, out ldthis, out ldloc) || !ldthis.MatchThis() || !ldloc.MatchLdloc(tmpV))
					throw new SymbolicAnalysisFailedException();
				if (f.ResolveFieldWithinSameModule() != stateField)
					throw new SymbolicAnalysisFailedException();
			}
			else if (!MatchStateAssignment(body[pos++], out finalState))
				throw new SymbolicAnalysisFailedException();

			if (!methodNeverReturns) {
				MatchHoistedLocalCleanup(body, ref pos);
				if (!MatchCallSetResult(body[pos++], out resultExpr, out resultVariable))
					throw new SymbolicAnalysisFailedException();

				exitLabel = body[pos++] as ILLabel;
				if (exitLabel == null)
					throw new SymbolicAnalysisFailedException();
			}
			else
				exitLabel = setResultAndExitLabel;

			bodyInfo = new ILMethodBody(tryCatchBlock.TryBlock.Body);
		}

		bool MatchRoslynStateAssignment(List<ILNode> block, int index, out int stateID) {
			// v = ldc.i4(stateId)
			// stloc(cachedState, v)
			// stfld(StateMachine::<>1__state, ldloc(this), v)
			stateID = 0;
			if (index < 0)
				return false;
			ILVariable v;
			ILExpression val;
			if (!block[index].Match(ILCode.Stloc, out v, out val) || !val.Match(ILCode.Ldc_I4, out stateID))
				return false;
			ILExpression loadV;
			if (!block[index + 1].MatchStloc(cachedStateVar, out loadV) || !loadV.MatchLdloc(v))
				return false;
			ILExpression target;
			IField fieldRef;
			if (block[index + 2].Match(ILCode.Stfld, out fieldRef, out target, out loadV)) {
				return fieldRef.ResolveFieldWithinSameModule() == stateField
					&& target.MatchThis()
					&& loadV.MatchLdloc(v);
			}
			return false;
		}
		#endregion

		#region AnalyzeStateMachine
		ILVariable doFinallyBodies;

		protected override List<ILNode> AnalyzeStateMachine(ILMethodBody bodyInfo) {
			var body = bodyInfo.Body;
			var startPos = bodyInfo.StartPosition;
			var endPos = bodyInfo.EndPosition;
			if (startPos >= endPos)
				throw new SymbolicAnalysisFailedException();
			if (DetectDoFinallyBodies(body, startPos)) {
				startPos++;
				if (startPos >= endPos)
					throw new SymbolicAnalysisFailedException();
			}
			StateRangeAnalysis rangeAnalysis = new MicrosoftStateRangeAnalysis(body[startPos], StateRangeAnalysisMode.AsyncMoveNext, stateField, cachedStateVar);
			int bodyLength = endPos;
			int pos = rangeAnalysis.AssignStateRanges(body, startPos, bodyLength);
			rangeAnalysis.EnsureLabelAtPos(body, ref pos, ref bodyLength);

			var labelStateRangeMapping = CreateLabelRangeMapping(rangeAnalysis, body, pos, bodyLength);
			var newBody = ConvertBody(body, pos, bodyLength, labelStateRangeMapping);
			newBody.Insert(0, MakeGoTo(labelStateRangeMapping, initialState));
			newBody.Add(setResultAndExitLabel);
			if (methodType == AsyncMethodType.TaskOfT)
				newBody.Add(new ILExpression(ILCode.Ret, null, resultExpr));
			else
				newBody.Add(new ILExpression(ILCode.Ret, null));
			return newBody;
		}

		bool DetectDoFinallyBodies(List<ILNode> body, int startPos) {
			ILVariable v;
			ILExpression initExpr;
			if (!body[startPos].Match(ILCode.Stloc, out v, out initExpr) || (resultVariable != null && v == resultVariable))
				return false;
			int initialValue;
			if (!(initExpr.Match(ILCode.Ldc_I4, out initialValue) && initialValue == 1))
				return false;
			doFinallyBodies = v;
			return true;
		}
		#endregion

		#region ConvertBody
		ILExpression MakeGoTo(LabelRangeMapping mapping, int state) {
			// Reverse order since the latest labels have been added to the end (it's a stack)
			for (int i = mapping.Count - 1; i >= 0; i--) {
				var pair = mapping[i];
				if (pair.Value.Contains(state))
					return new ILExpression(ILCode.Br, pair.Key);
			}
			throw new SymbolicAnalysisFailedException();
		}

		bool TryAddStateRanges(List<ILNode> body, ref int pos, int bodyLength, LabelRangeMapping mapping) {
			var rangeAnalysis = new MicrosoftStateRangeAnalysis(body[pos], StateRangeAnalysisMode.AsyncMoveNext, stateField, cachedStateVar);
			int bodyLengthTmp = bodyLength;
			int posInBody = rangeAnalysis.AssignStateRanges(body, pos, bodyLengthTmp);
			if (posInBody == pos)
				return false;
			var newMapping = CreateLabelRangeMapping(rangeAnalysis, body, posInBody, bodyLengthTmp);
			mapping.AddRange(newMapping);
			pos = posInBody - 1;
			return true;
		}

		List<ILNode> ConvertBody(List<ILNode> body, int startPos, int bodyLength, LabelRangeMapping mapping) {
			int mappingCount = mapping.Count;
			List<ILNode> newBody = new List<ILNode>();
			for (int pos = startPos; pos < bodyLength; pos++) {
				var node = body[pos];
				var expr = node as ILExpression;
				ILExpression ldloc;
				switch (expr?.Code ?? (ILCode)(-1)) {
				case ILCode.Stloc:
					if (VerifyLoadStateField(expr.Arguments[0]) && TryAddStateRanges(body, ref pos, bodyLength, mapping))
						break;
					if (expr.Operand == doFinallyBodies)
						break;
					// VB14 adds this after the state check at the top of a try block
					//	expr_149 = ldc.i4(-1)
					//	stloc(var_1, expr_149)
					//	stfld($State, ldloc(this), expr_149)
					//	leave(IL_2EA)	// exit label
					if (pos + 3 < bodyLength && expr.Arguments[0].MatchLdcI4(-1)) {
						ILLabel lbl;
						ILVariable v;
						ILExpression ldthis;
						IField f;
						if (body[pos + 1].Match(ILCode.Stloc, out v, out ldloc) &&
							ldloc.MatchLdloc(expr.Operand as ILVariable) &&
							body[pos + 3].Match(ILCode.Leave, out lbl) && lbl == exitLabel &&
							body[pos + 2].Match(ILCode.Stfld, out f, out ldthis, out ldloc) &&
							ldthis.MatchThis() && ldloc.MatchLdloc(expr.Operand as ILVariable)) {
							pos += 3;
							break;
						}
					}
					goto default;

				case ILCode.Switch:
					if (TryAddStateRanges(body, ref pos, bodyLength, mapping))
						break;
					goto default;

				case ILCode.Brtrue:
					// Seen in Debug builds
					ILLabel endFinallyLabel;
					ILExpression ceqExpr;
					if (expr.Match(ILCode.Brtrue, out endFinallyLabel, out ceqExpr)) {
						ILExpression condition;
						if (MatchLogicNot(ceqExpr, out condition)) {
							if (condition.MatchLdloc(doFinallyBodies))
								break;
						}
					}
					goto default;

				case ILCode.Stfld:
					// VB14 adds this after the state check at the top of a try block
					//	stfld($State, ldloc(this), ldc.i4(-1))
					//	leave(IL_17B)	// exit label
					if (expr.Arguments[0].MatchThis() && expr.Arguments[1].MatchLdcI4(-1)) {
						ILLabel lbl;
						if (pos + 1 < bodyLength && body[pos + 1].Match(ILCode.Leave, out lbl) && lbl == exitLabel && (expr.Operand as IField).ResolveFieldWithinSameModule() == stateField) {
							pos++;
							break;
						}
					}
					if (doFinallyBodies != null && expr.Arguments[0].Match(ILCode.LogicNot, out ldloc) && ldloc.MatchLdloc(doFinallyBodies))
						break;
					goto default;

				case ILCode.Leave:
					if (expr.Operand == exitLabel) {
						ILVariable awaiterVar;
						FieldDef awaiterField;
						int targetStateID;
						HandleAwait(newBody, out awaiterVar, out awaiterField, out targetStateID);
						MarkAsGeneratedVariable(awaiterVar);
						newBody.Add(new ILExpression(ILCode.Await, null, new ILExpression(ILCode.Ldloca, awaiterVar)));
						newBody.Add(MakeGoTo(mapping, targetStateID));
						break;
					}
					goto default;

				default:
					var tryCatch = node as ILTryCatchBlock;
					if (tryCatch != null) {
						var tryBody = tryCatch.TryBlock.Body;
						if (tryBody.Count == 0)
							throw new SymbolicAnalysisFailedException();

						var rangeAnalysis = new MicrosoftStateRangeAnalysis(tryBody[0], StateRangeAnalysisMode.AsyncMoveNext, stateField, cachedStateVar);
						int tryBodyLength = tryBody.Count;
						int posInTryBody = rangeAnalysis.AssignStateRanges(tryBody, tryBodyLength);
						rangeAnalysis.EnsureLabelAtPos(tryBody, ref posInTryBody, ref tryBodyLength);

						var mappingInTryBlock = CreateLabelRangeMapping(rangeAnalysis, tryBody, posInTryBody, tryBodyLength);
						var newTryBody = ConvertBody(tryBody, posInTryBody, tryBodyLength, mappingInTryBlock);
						newTryBody.Insert(0, MakeGoTo(mappingInTryBlock, initialState));

						// If there's a label at the beginning of the state dispatcher, copy that
						if (posInTryBody > 0 && tryBody.FirstOrDefault() is ILLabel)
							newTryBody.Insert(0, tryBody.First());

						tryCatch.TryBlock.Body = newTryBody;
						if (tryCatch.FinallyBlock != null)
							tryCatch.FinallyBlock.Body = ConvertFinally(tryCatch.FinallyBlock.Body);

						newBody.Add(tryCatch);
					}
					else
						newBody.Add(node);
					break;
				}
			}
			mapping.RemoveRange(mappingCount, mapping.Count - mappingCount);
			return newBody;
		}

		bool VerifyLoadStateField(ILExpression expr) {
			IField field;
			ILExpression ldloc;
			if (!expr.Match(ILCode.Ldfld, out field, out ldloc))
				return false;
			return ldloc.MatchThis() && field.ResolveFieldWithinSameModule() == stateField;
		}

		List<ILNode> ConvertFinally(List<ILNode> body) {
			if (body.Count == 0)
				return body;
			ILLabel endFinallyLabel;
			ILExpression ceqExpr;

			ILExpression ldloc;
			ILVariable v;
			if (body[0].Match(ILCode.Stloc, out v, out ldloc)) {
				if (v == doFinallyBodies)
					body.RemoveAt(0);
				else if (ldloc.MatchLdloc(doFinallyBodies)) {
					ILExpression expr;
					if (body[1].Match(ILCode.Brtrue, out endFinallyLabel, out expr) && MatchLogicNot(expr, out ldloc) && ldloc.MatchLdloc(v))
						body.RemoveRange(0, 2);
				}
			}

			ILExpression cge;
			if (body[0].Match(ILCode.Brtrue, out endFinallyLabel, out cge)) {
				List<ILExpression> args;
				if (cge.Match(ILCode.Cge, out args) && args.Count == 2) {
					if (args[1].MatchLdcI4(0) && args[0].MatchLdloc(cachedStateVar))
						body.RemoveAt(0);
				}
			}

			if (body[0].Match(ILCode.Brtrue, out endFinallyLabel, out ceqExpr)) {
				ILExpression condition;
				if (MatchLogicNot(ceqExpr, out condition)) {
					if (condition.MatchLdloc(doFinallyBodies))
						body.RemoveAt(0);
					else if (condition.Code == ILCode.Clt && condition.Arguments[0].MatchLdloc(cachedStateVar) && condition.Arguments[1].MatchLdcI4(0))
						body.RemoveAt(0);
				}
			}
			return body;
		}

		bool MatchLogicNot(ILExpression expr, out ILExpression arg) {
			ILExpression loadZero;
			object unused;
			if (expr.Match(ILCode.Ceq, out unused, out arg, out loadZero)) {
				int num;
				return loadZero.Match(ILCode.Ldc_I4, out num) && num == 0;
			}
			return expr.Match(ILCode.LogicNot, out arg);
		}

		void HandleAwait(List<ILNode> newBody, out ILVariable awaiterVar, out FieldDef awaiterField, out int targetStateID) {
			// Handle the instructions prior to the exit out of the method to detect what is being awaited.
			// (analyses the last instructions in newBody and removes the analyzed instructions from newBody)

			if (doFinallyBodies != null) {
				// It's not always present even if the local exists
				// stloc(<>t__doFinallyBodies, ldc.i4(0))
				ILExpression dfbInitExpr;
				if (newBody.LastOrDefault().MatchStloc(doFinallyBodies, out dfbInitExpr)) {
					int val;
					if (!(dfbInitExpr.Match(ILCode.Ldc_I4, out val) && val == 0))
						throw new SymbolicAnalysisFailedException();
					newBody.RemoveAt(newBody.Count - 1); // remove doFinallyBodies assignment
				}
			}

			// call(AsyncTaskMethodBuilder::AwaitUnsafeOnCompleted, ldflda(StateMachine::<>t__builder, ldloc(this)), ldloca(CS$0$0001), ldloc(this))
			var callAwaitUnsafeOnCompleted = newBody.LastOrDefault();
			newBody.RemoveAt(newBody.Count - 1); // remove AwaitUnsafeOnCompleted call
			var ldloca = MatchCallAwaitOnCompletedMethod(callAwaitUnsafeOnCompleted);
			if (!ldloca.Match(ILCode.Ldloca, out awaiterVar))
				throw new SymbolicAnalysisFailedException();

			// stfld(StateMachine::<>u__$awaiter6, ldloc(this), ldloc(CS$0$0001))
			IField awaiterFieldRef;
			ILExpression loadThis, loadAwaiterVar;

			if (!stateMachineTypeIsValueType) {
				if (newBody.Count < 2)
					throw new SymbolicAnalysisFailedException();
				ILVariable v;
				ILExpression ldloc;
				if (!newBody.LastOrDefault().Match(ILCode.Stloc, out v, out ldloc))
					throw new SymbolicAnalysisFailedException();
				if (!ldloc.Match(ILCode.Ldloc, out v))
					throw new SymbolicAnalysisFailedException();
				newBody.RemoveAt(newBody.Count - 1);
			}

			if (!newBody.LastOrDefault().Match(ILCode.Stfld, out awaiterFieldRef, out loadThis, out loadAwaiterVar))
				throw new SymbolicAnalysisFailedException();
			newBody.RemoveAt(newBody.Count - 1); // remove awaiter field assignment
			awaiterField = awaiterFieldRef.ResolveFieldWithinSameModule();
			if (!(awaiterField != null && loadThis.MatchThis() && loadAwaiterVar.MatchLdloc(awaiterVar)))
				throw new SymbolicAnalysisFailedException();

			// stfld(StateMachine::<>1__state, ldloc(this), ldc.i4(0))
			if (MatchStateAssignment(newBody.LastOrDefault(), out targetStateID)) {
				AddYieldOffset(newBody, newBody.Count - 1, 1, targetStateID);
				newBody.RemoveAt(newBody.Count - 1); // remove awaiter field assignment
			}
			else if (MatchRoslynStateAssignment(newBody, newBody.Count - 3, out targetStateID)) {
				AddYieldOffset(newBody, newBody.Count - 3, 3, targetStateID);
				newBody.RemoveRange(newBody.Count - 3, 3); // remove awaiter field assignment
			}
			else
				Debug.Fail("Couldn't find new async state machine state");
		}
		#endregion

		protected override void Step2(ILBlock method) => Step2Core(method.Body);
		void Step2Core(List<ILNode> body) {
			for (int pos = 0; pos < body.Count; pos++) {
				ILTryCatchBlock tc = body[pos] as ILTryCatchBlock;
				if (tc != null)
					Step2Core(tc.TryBlock.Body);
				else
					Step2Core(body, ref pos);
			}
		}

		bool Step2Core(List<ILNode> body, ref int currPos) {
			// stloc(CS$0$0001, callvirt(class System.Threading.Tasks.Task`1<bool>::GetAwaiter, awaiterExpr)
			// brtrue(IL_7C, call(valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<bool>::get_IsCompleted, ldloca(CS$0$0001)))
			// await(ldloca(CS$0$0001))
			// ...
			// IL_7C:
			// arg_8B_0 = call(valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<bool>::GetResult, ldloca(CS$0$0001))
			// initobj(valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<bool>, ldloca(CS$0$0001))

			var pos = currPos;

			ILExpression loadAwaiter;
			ILVariable awaiterVar;
			if (!body[pos].Match(ILCode.Await, out loadAwaiter))
				return false;
			if (!loadAwaiter.Match(ILCode.Ldloca, out awaiterVar))
				return false;

			ILVariable stackVar;
			ILExpression stackExpr;
			while (pos >= 1 && body[pos - 1].Match(ILCode.Stloc, out stackVar, out stackExpr))
				pos--;

			// stloc(CS$0$0001, callvirt(class System.Threading.Tasks.Task`1<bool>::GetAwaiter, awaiterExpr)
			ILExpression getAwaiterCall;
			if (!(pos >= 2 && body[pos - 2].MatchStloc(awaiterVar, out getAwaiterCall)))
				return false;
			IMethod getAwaiterMethod;
			ILExpression awaitedExpr;
			if (!(getAwaiterCall.Match(ILCode.Call, out getAwaiterMethod, out awaitedExpr) || getAwaiterCall.Match(ILCode.Callvirt, out getAwaiterMethod, out awaitedExpr)))
				return false;

			ILExpression addrOffExpr = null;
			if (awaitedExpr.Code == ILCode.AddressOf) {
				// remove 'AddressOf()' when calling GetAwaiter() on a value type
				addrOffExpr = awaitedExpr;
				awaitedExpr = awaitedExpr.Arguments[0];
			}

			// brtrue(IL_7C, call(valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<bool>::get_IsCompleted, ldloca(CS$0$0001)))
			ILLabel label;
			ILExpression getIsCompletedCall;
			if (!(pos >= 1 && body[pos - 1].Match(ILCode.Brtrue, out label, out getIsCompletedCall)))
				return false;

			int labelPos = body.IndexOf(label);
			if (labelPos < pos)
				return false;
			for (int i = pos + 1; i < labelPos; i++) {
				// validate that we aren't deleting any unexpected instructions -
				// between the await and the label, there should only be the stack, awaiter and state logic
				ILExpression expr = body[i] as ILExpression;
				if (expr == null)
					return false;
				switch (expr.Code) {
				case ILCode.Stloc:
				case ILCode.Initobj:
				case ILCode.Stfld:
				case ILCode.Await:
					// e.g.
					// stloc(CS$0$0001, ldfld(StateMachine::<>u__$awaitere, ldloc(this)))
					// initobj(valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<bool>, ldloca(CS$0$0002_66))
					// stfld('<AwaitInLoopCondition>d__d'::<>u__$awaitere, ldloc(this), ldloc(CS$0$0002_66))
					// stfld('<AwaitInLoopCondition>d__d'::<>1__state, ldloc(this), ldc.i4(-1))
					break;
				default:
					return false;
				}
			}
			if (labelPos + 1 >= body.Count)
				return false;
			ILExpression resultAssignment = body[labelPos + 1] as ILExpression;
			ILVariable resultVar;
			ILExpression getResultCall;
			ILExpression patchExpr = null;
			bool isResultAssignment = resultAssignment.Match(ILCode.Stloc, out resultVar, out getResultCall) && IsGetResult(getResultCall.Operand);
			if (!isResultAssignment) {
				if (getResultCall == null)
					getResultCall = resultAssignment;
				for (;;) {
					if (IsGetResult(getResultCall.Operand))
						break;
					switch (getResultCall.Code) {
					case ILCode.Call:
					case ILCode.CallGetter:
					case ILCode.Calli:
					case ILCode.CallReadOnlySetter:
					case ILCode.CallSetter:
					case ILCode.Callvirt:
					case ILCode.CallvirtGetter:
					case ILCode.CallvirtSetter:
					case ILCode.AddressOf:
						break;
					default:
						return false;
					}
					if (getResultCall.Arguments.Count == 0)
						return false;
					patchExpr = getResultCall;
					getResultCall = getResultCall.Arguments[0];
				}
			}
			if (!IsGetResult(getResultCall.Operand))
				return false;

			pos -= 2; // also delete 'stloc', 'brtrue' and 'await'
			if (context.CalculateILSpans) {
				awaitedExpr.ILSpans.AddRange(body[pos].ILSpans);
				awaitedExpr.ILSpans.AddRange(getAwaiterCall.ILSpans);
				if (addrOffExpr != null)
					awaitedExpr.ILSpans.AddRange(addrOffExpr.ILSpans);
			}
			body.RemoveRange(pos, labelPos - pos);
			Debug.Assert(body[pos] == label);

			pos++;
			if (isResultAssignment) {
				Debug.Assert(body[pos] == resultAssignment);
				resultAssignment.Arguments[0] = new ILExpression(ILCode.Await, null, awaitedExpr);
			}
			else if (patchExpr != null) {
				if (context.CalculateILSpans)
					awaitedExpr.ILSpans.AddRange(patchExpr.Arguments[0].GetSelfAndChildrenRecursiveILSpans());
				patchExpr.Arguments[0] = new ILExpression(ILCode.Await, null, awaitedExpr);
			}
			else
				body[pos] = new ILExpression(ILCode.Await, null, awaitedExpr);

			// if the awaiter variable is cleared out in the next instruction, remove that instruction
			if (IsVariableReset(body.ElementAtOrDefault(pos + 1), awaiterVar))
				body.RemoveAt(pos + 1);

			currPos = pos;
			return true;
		}

		static bool IsGetResult(object operand) => operand is IMethod method && !method.IsField && method.Name == nameGetResult;

		static bool IsVariableReset(ILNode expr, ILVariable variable) {
			object unused;
			ILExpression ldloca;
			return expr.Match(ILCode.Initobj, out unused, out ldloca) && ldloca.MatchLdloca(variable);
		}
	}
}
