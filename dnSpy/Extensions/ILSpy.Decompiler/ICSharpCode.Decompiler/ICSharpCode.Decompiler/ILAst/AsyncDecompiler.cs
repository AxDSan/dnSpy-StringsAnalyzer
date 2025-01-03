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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;

namespace ICSharpCode.Decompiler.ILAst {
	/// <summary>
	/// Decompiler step for C# 5 async/await.
	/// </summary>
	abstract class AsyncDecompiler {
		public static bool IsCompilerGeneratedStateMachine(TypeDef type) {
			if (!(type.DeclaringType != null && (type.IsCompilerGenerated() || type.HasGeneratedName())))
				return false;
			foreach (var ii in type.Interfaces) {
				var iface = ii.Interface;
				if (iface == null)
					continue;
				if (iface.Name == nameIAsyncStateMachine && iface.Namespace == "System.Runtime.CompilerServices")
					return true;
			}
			return false;
		}
		static readonly UTF8String nameIAsyncStateMachine = new UTF8String("IAsyncStateMachine");

		protected enum AsyncMethodType {
			Void,
			Task,
			TaskOfT
		}

		protected readonly DecompilerContext context;
		readonly AutoPropertyProvider autoPropertyProvider;

		// These fields are set by MatchTaskCreationPattern()
		protected AsyncMethodType methodType;
		protected TypeDef stateMachineType;
		protected bool stateMachineTypeIsValueType;
		protected MethodDef moveNextMethod;
		protected FieldDef builderField;
		protected TypeDef builderType;
		protected FieldDef stateField;
		protected FieldToVariableMap variableMap;

		protected ILLabel exitLabel;
		// See Microsoft.CodeAnalysis.CSharp.MethodToStateMachineRewriter.cachedThis for info on why and when it's cached
		protected ILVariable cachedThisVar;

		protected readonly List<ILExpression> list_ILExpression = new List<ILExpression>();

		public abstract string CompilerName { get; }

		protected AsyncDecompiler(DecompilerContext context, AutoPropertyProvider autoPropertyProvider) {
			this.context = context;
			this.autoPropertyProvider = autoPropertyProvider;
			variableMap = context.VariableMap;
		}

		static AsyncDecompiler TryCreate(DecompilerContext context, ILBlock method, AutoPropertyProvider autoPropertyProvider) =>
			MicrosoftAsyncDecompiler.TryCreateCore(context, method, autoPropertyProvider) ??
			MonoAsyncDecompiler.TryCreateCore(context, method, autoPropertyProvider);

		protected static readonly UTF8String nameCreate = new UTF8String("Create");
		static readonly UTF8String nameStart = new UTF8String("Start");
		static readonly UTF8String nameSystemRuntimeCompilerServices = new UTF8String("System.Runtime.CompilerServices");
		static readonly UTF8String nameAsyncTaskMethodBuilder1 = new UTF8String("AsyncTaskMethodBuilder`1");
		static readonly UTF8String nameAsyncTaskMethodBuilder = new UTF8String("AsyncTaskMethodBuilder");
		static readonly UTF8String nameAsyncVoidMethodBuilder = new UTF8String("AsyncVoidMethodBuilder");
		static readonly UTF8String nameMoveNext = new UTF8String("MoveNext");
		protected static readonly UTF8String nameGetResult = new UTF8String("GetResult");

		#region RunStep1() method
		public static AsyncDecompiler RunStep1(DecompilerContext context, ILBlock method, AutoPropertyProvider autoPropertyProvider, ref StateMachineKind stateMachineKind, ref MethodDef inlinedMethod, ref string compilerName, List<ILExpression> listExpr, List<ILBlock> listBlock, Dictionary<ILLabel, int> labelRefCount) {
			if (!context.Settings.AsyncAwait)
				return null;
			var yrd = TryCreate(context, method, autoPropertyProvider);
			if (yrd == null)
				return null;
			List<ILNode> newTopLevelBody;
			try {
				newTopLevelBody = yrd.Run();
				Debug.Assert(yrd.moveNextMethod != null);
			}
			catch (SymbolicAnalysisFailedException) {
				return null;
			}
			context.CurrentMethodIsAsync = true;

			method.Body.Clear();
			method.EntryGoto = null;
			method.Body.AddRange(newTopLevelBody);
			stateMachineKind = StateMachineKind.AsyncMethod;
			inlinedMethod = yrd.moveNextMethod;
			compilerName = yrd.CompilerName;
			ILAstOptimizer.RemoveRedundantCode(context, method, listExpr, listBlock, labelRefCount);
			return yrd;
		}

		protected abstract void AnalyzeMoveNext(out ILMethodBody bodyInfo, out ILTryCatchBlock tryCatchBlock, out int finalState, out ILLabel exitLabel);
		protected abstract List<ILNode> AnalyzeStateMachine(ILMethodBody bodyInfo);

		protected struct ILMethodBody {
			public List<ILNode> Body { get; }
			public int StartPosition { get; }
			// Not inclusive
			public int EndPosition { get; }
			public ILMethodBody(List<ILNode> body) {
				Body = body;
				StartPosition = 0;
				EndPosition = body.Count;
			}
			public ILMethodBody(List<ILNode> body, int startPosition, int endPosition) {
				Body = body;
				StartPosition = startPosition;
				EndPosition = endPosition;
			}
		}

		List<ILNode> Run() {
			ILMethodBody body;
			ILTryCatchBlock tryCatchBlock;
			int finalState;
			AnalyzeMoveNext(out body, out tryCatchBlock, out finalState, out exitLabel);
			if (tryCatchBlock != null) {
				ValidateCatchBlock(tryCatchBlock.CatchBlocks[0], finalState, exitLabel);
				if (context.CalculateILSpans) {
					var cb = tryCatchBlock.CatchBlocks[0];
					catchHandlerOffset = GetOffset(cb.Body, 0, cb.Body.Count);
					foreach (var span in cb.StlocILSpans)
						catchHandlerOffset = Math.Min(span.Start, catchHandlerOffset);
				}
			}
			var newTopLevelBody = AnalyzeStateMachine(body);
			MarkGeneratedVariables(newTopLevelBody);
			YieldReturnDecompiler.TranslateFieldsToLocalAccess(newTopLevelBody, variableMap, cachedThisVar, context.CalculateILSpans, true);
			return newTopLevelBody;
		}
		#endregion

		protected ILTryCatchBlock GetMainTryCatchBlock(ILNode node) {
			var tryCatchBlock = node as ILTryCatchBlock;
			if (tryCatchBlock == null || tryCatchBlock.CatchBlocks.Count != 1)
				return null;
			if (tryCatchBlock.FaultBlock != null || tryCatchBlock.FinallyBlock != null)
				return null;
			return tryCatchBlock;
		}

		protected bool MatchStartCall(ILNode expr, out ILVariable stateMachineVar) {
			ILVariable builderVar;
			return MatchStartCallCore(expr, out stateMachineVar, out builderVar, useLdflda: true);
		}

		protected bool MatchStartCall(ILNode expr, out ILVariable stateMachineVar, out ILVariable builderVar) =>
			MatchStartCallCore(expr, out stateMachineVar, out builderVar, useLdflda: false);

		static readonly UTF8String stringSystem_Threading_Tasks = new UTF8String("System.Threading.Tasks");
		static readonly UTF8String stringTask = new UTF8String("Task");
		static readonly UTF8String stringTask_1 = new UTF8String("Task`1");
		static readonly UTF8String stringSystem = new UTF8String("System");
		static readonly UTF8String stringVoid = new UTF8String("Void");
		static bool TryGetAsyncMethodType(ITypeDefOrRef tdr, ITypeDefOrRef builderType, out AsyncMethodType asyncMethodType) {
			var td = tdr.ResolveTypeDef();
			if (td != null) {
				int gpCount = td.GenericParameters.Count;
				if (gpCount > 1) {
					asyncMethodType = 0;
					return false;
				}

				if (td.Namespace == stringSystem_Threading_Tasks) {
					if (gpCount == 1 && td.Name == stringTask_1) {
						asyncMethodType = AsyncMethodType.TaskOfT;
						return true;
					}
					else if (gpCount == 0 && td.Name == stringTask) {
						asyncMethodType = AsyncMethodType.Task;
						return true;
					}
				}

				if (IsCustomTaskType(td)) {
					if (gpCount == 0) {
						asyncMethodType = AsyncMethodType.Task;
						return true;
					}
					if (gpCount == 1) {
						asyncMethodType = AsyncMethodType.TaskOfT;
						return true;
					}
					Debug.Fail("Shouldn't be here");
				}

				if (td.Name == stringVoid && td.Namespace == stringSystem) {
					var btd = builderType.ResolveTypeDef();
					if (btd != null) {
						if (btd.Namespace == nameSystemRuntimeCompilerServices && btd.Name == nameAsyncVoidMethodBuilder) {
							asyncMethodType = AsyncMethodType.Void;
							return true;
						}
					}
				}
			}

			asyncMethodType = 0;
			return false;
		}

		static bool IsCustomTaskType(TypeDef td) {
			if (td == null)
				return false;

			// See (Roslyn): IsCustomTaskType

			if (td.GenericParameters.Count > 1)
				return false;

			foreach (var ca in td.CustomAttributes) {
				if (ca.TypeFullName != "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute")
					continue;
				if (ca.ConstructorArguments.Count != 1)
					continue;
				if ((ca.ConstructorArguments[0].Type as ClassSig)?.TypeDefOrRef.FullName != "System.Type")
					continue;

				return true;
			}

			return false;
		}

		bool MatchStartCallCore(ILNode expr, out ILVariable stateMachineVar, out ILVariable builderVar, bool useLdflda) {
			stateMachineVar = null;
			builderVar = null;

			IMethod startMethod;
			ILExpression loadStartTarget, loadStartArgument;
			// call(AsyncTaskMethodBuilder::Start, ldloca(builder), ldloca(stateMachine))
			if (!expr.Match(ILCode.Call, out startMethod, out loadStartTarget, out loadStartArgument))
				return false;
			if (startMethod.Name != nameStart)
				return false;
			if (!TryGetAsyncMethodType(context.CurrentMethod.ReturnType.RemovePinnedAndModifiers().ToTypeDefOrRef(), startMethod.DeclaringType, out methodType))
				return false;
			if (!loadStartArgument.Match(ILCode.Ldloca, out stateMachineVar))
				return false;
			if (useLdflda) {
				IField f;
				ILExpression ldloca;
				if (!loadStartTarget.Match(ILCode.Ldflda, out f, out ldloca))
					return false;
				ILVariable v;
				if ((!ldloca.Match(ILCode.Ldloca, out v) && !ldloca.Match(ILCode.Ldloc, out v)) || v != stateMachineVar)
					return false;
			}
			else {
				if (!loadStartTarget.Match(ILCode.Ldloca, out builderVar))
					return false;
			}

			stateMachineType = stateMachineVar.Type.GetTypeDefOrRef().ResolveWithinSameModule();
			builderType = startMethod.DeclaringType.ResolveTypeDef();
			if (stateMachineType == null)
				return false;
			// It's a class if EnC is enabled (see Microsoft.CodeAnalysis.CSharp.AsyncRewriter.Rewrite())
			// because the CLR doesn't support adding fields to structs.
			stateMachineTypeIsValueType = DnlibExtensions.IsValueType(stateMachineType);
			moveNextMethod = stateMachineType.Methods.FirstOrDefault(f => f.Name == nameMoveNext);
			if (moveNextMethod == null)
				return false;

			return true;
		}

		protected bool MatchReturnTask(ILNode expr, ILVariable stateMachineVar) {
			if (methodType == AsyncMethodType.Void) {
				if (!expr.Match(ILCode.Ret))
					return false;
			}
			else {
				// ret(call(AsyncTaskMethodBuilder::get_Task, ldflda(StateMachine::<>t__builder, ldloca(stateMachine))))
				ILExpression returnValue;
				if (!expr.Match(ILCode.Ret, out returnValue))
					return false;
				IMethod getTaskMethod;
				ILExpression builderExpr;
				if (!returnValue.Match(ILCode.Call, out getTaskMethod, out builderExpr))
					return false;
				ILExpression loadStateMachineForBuilderExpr2;
				IField builderField2;

				if (builderType.IsValueType) {
					if (!builderExpr.Match(ILCode.Ldflda, out builderField2, out loadStateMachineForBuilderExpr2))
						return false;
				}
				else {
					if (!builderExpr.Match(ILCode.Ldfld, out builderField2, out loadStateMachineForBuilderExpr2))
						return false;
				}

				if (builderField2.ResolveFieldWithinSameModule() != builderField)
					return false;
				if (!(loadStateMachineForBuilderExpr2.MatchLdloc(stateMachineVar) || loadStateMachineForBuilderExpr2.MatchLdloca(stateMachineVar)))
					return false;
			}
			return true;
		}

		protected bool MatchCallCreate(ILNode expr, ILVariable stateMachineVar) {
			FieldDef builderField3;
			ILExpression builderInitialization;
			if (!MatchStFld(expr, stateMachineVar, stateMachineTypeIsValueType, out builderField3, out builderInitialization))
				return false;
			IMethod createMethodRef;
			if (builderField == null)
				builderField = builderField3;
			else if (builderField3 != builderField)
				return false;
			if (!builderInitialization.Match(ILCode.Call, out createMethodRef))
				return false;
			if (createMethodRef.Name != nameCreate)
				return false;
			return true;
		}

		protected bool InitializeFieldToParameterMap(List<ILNode> body, int bodyLength, ILVariable stateMachineVar) =>
			InitializeFieldToParameterMap(body, stateMachineTypeIsValueType ? 0 : 1, bodyLength, stateMachineVar);
		protected bool InitializeFieldToParameterMap(List<ILNode> body, int startPos, int bodyLength, ILVariable stateMachineVar) {
			for (int i = startPos; i < bodyLength; i++) {
				FieldDef field;
				ILExpression fieldInit;
				if (!MatchStFld(body[i], stateMachineVar, stateMachineTypeIsValueType, out field, out fieldInit))
					return false;
				ILVariable v;
				if (!fieldInit.Match(ILCode.Ldloc, out v)) {
					ILExpression ldloc;
					ITypeDefOrRef type;
					IMethod m;
					if (fieldInit.Match(ILCode.Ldobj, out type, out ldloc) && ldloc.MatchThis() && type.ResolveWithinSameModule() == context.CurrentMethod.DeclaringType)
						v = (ILVariable)ldloc.Operand;
					else if (fieldInit.Match(ILCode.Call, out m, out ldloc) && ldloc.Match(ILCode.Ldloc, out v)) {
						// VB 11 & 12 calls RuntimeHelpers.GetObjectValue(o)
						if (m.Name != nameGetObjectValue)
							return false;
						if (m.DeclaringType.FullName != "System.Runtime.CompilerServices.RuntimeHelpers")
							return false;
					}
					else
						return false;
				}
				if (!v.IsParameter)
					return false;
				variableMap.SetParameter(field, v);
			}
			return true;
		}
		static readonly UTF8String nameGetObjectValue = new UTF8String("GetObjectValue");

		protected static bool MatchStFld(ILNode stfld, ILVariable stateMachineVar, bool stateMachineStructIsValueType, out FieldDef field, out ILExpression expr) {
			field = null;
			IField fieldRef;
			ILExpression ldloca;
			if (!stfld.Match(ILCode.Stfld, out fieldRef, out ldloca, out expr))
				return false;
			field = fieldRef.ResolveFieldWithinSameModule();
			if (field == null)
				return false;
			return stateMachineStructIsValueType ? ldloca.MatchLdloca(stateMachineVar) : ldloca.MatchLdloc(stateMachineVar);
		}

		/// <summary>
		/// Creates ILAst for the specified method, optimized up to before the 'YieldReturn' step.
		/// </summary>
		protected ILBlock CreateILAst(MethodDef method) {
			if (method == null || !method.HasBody)
				throw new SymbolicAnalysisFailedException();

			ILBlock ilMethod = new ILBlock(CodeBracesRangeFlags.MethodBraces);

			var astBuilder = context.Cache.GetILAstBuilder();
			try {
				ilMethod.Body = astBuilder.Build(method, true, context);
			}
			finally {
				context.Cache.Return(astBuilder);
			}

			var optimizer = this.context.Cache.GetILAstOptimizer();
			try {
				optimizer.Optimize(context, ilMethod, autoPropertyProvider, out _, out _, out _, ILAstOptimizationStep.YieldReturn);
			}
			finally {
				context.Cache.Return(optimizer);
			}

			return ilMethod;
		}

		protected bool MatchCallSetResult(ILNode expr, out ILExpression resultExpr, out ILVariable resultVariable) {
			resultExpr = null;
			resultVariable = null;
			if (context.CalculateILSpans) {
				var ilSpans = ILSpan.OrderAndCompact(expr.GetSelfAndChildrenRecursiveILSpans());
				if (ilSpans.Count > 0)
					setResultOffset = ilSpans[0].Start;
			}
			IMethod setResultMethod;
			ILExpression builderExpr;
			// call(AsyncTaskMethodBuilder`1::SetResult, ldflda(StateMachine::<>t__builder, ldloc(this)), ldloc(<>t__result))
			if (methodType == AsyncMethodType.TaskOfT) {
				if (!expr.Match(ILCode.Call, out setResultMethod, out builderExpr, out resultExpr))
					return false;
				if (!resultExpr.Match(ILCode.Ldloc, out resultVariable))
					return false;
			}
			else {
				if (!expr.Match(ILCode.Call, out setResultMethod, out builderExpr))
					return false;
			}
			if (!(setResultMethod.Name == nameSetResult && IsBuilderFieldOnThis(builderExpr)))
				return false;
			return true;
		}
		static readonly UTF8String nameSetResult = new UTF8String("SetResult");

		protected ILExpression MatchCallAwaitOnCompletedMethod(ILNode expr) {
			var call = expr as ILExpression;
			if (call == null || (call.Code != ILCode.Call && call.Code != ILCode.Callvirt))
				return null;
			var methodName = ((IMethod)call.Operand).Name;
			if (methodName != nameAwaitUnsafeOnCompleted && methodName != nameAwaitOnCompleted)
				return null;
			if (call.Arguments.Count != 3)
				return null;
			return call.Arguments[1];
		}
		static readonly UTF8String nameAwaitUnsafeOnCompleted = new UTF8String("AwaitUnsafeOnCompleted");
		static readonly UTF8String nameAwaitOnCompleted = new UTF8String("AwaitOnCompleted");

		void ValidateCatchBlock(ILTryCatchBlock.CatchBlock catchBlock, int finalState, ILLabel exitLabel) {
			if (!CheckCatchBlock(catchBlock, stateField, builderField, builderType, finalState, exitLabel))
				throw new SymbolicAnalysisFailedException();
		}

		static bool CheckCatchBlock(ILTryCatchBlock.CatchBlock catchBlock, FieldDef stateField, FieldDef builderField, TypeDef builderType, int finalState, ILLabel exitLabel) {
			if (catchBlock.ExceptionType == null || catchBlock.ExceptionType.TypeName != "Exception")
				return false;
			var body = catchBlock.Body;
			int pos = 0;
			ILVariable exLoc;

			if (body[pos].Match(ILCode.Stloc, out ILVariable tempExloc, out ILExpression ldloc) && ldloc.MatchLdloc(catchBlock.ExceptionVariable)) {
				exLoc = tempExloc;
				pos++;
			}
			else {
				exLoc = catchBlock.ExceptionVariable;
			}

			// C# (csc, mcs)
			int stateID;
			if (!(MatchStateAssignment(body[pos++], stateField, out stateID) && stateID == finalState))
				return false;
			MatchHoistedLocalCleanup(body, ref pos);

			IMethod setExceptionMethod;
			ILExpression builderExpr, exceptionExpr;
			if (!body[pos++].Match(ILCode.Call, out setExceptionMethod, out builderExpr, out exceptionExpr))
				return false;
			if (!(setExceptionMethod.Name == nameSetException && IsBuilderFieldOnThis(builderExpr, builderField, builderType) && exceptionExpr.MatchLdloc(exLoc)))
				return false;

			ILLabel label;
			if (!(body[pos++].Match(ILCode.Leave, out label) && label == exitLabel))
				return false;

			return true;
		}
		static readonly UTF8String nameSetException = new UTF8String("SetException");

		bool IsBuilderFieldOnThis(ILExpression builderExpr) =>
			IsBuilderFieldOnThis(builderExpr, builderField, builderType);

		static bool IsBuilderFieldOnThis(ILExpression builderExpr, FieldDef builderField, TypeDef builderType) {
			// ldflda(StateMachine::<>t__builder, ldloc(this))
			IField fieldRef;
			ILExpression target;

			if (builderType.IsValueType) {
				if (!builderExpr.Match(ILCode.Ldflda, out fieldRef, out target))
					return false;
			}
			else {
				if (!builderExpr.Match(ILCode.Ldfld, out fieldRef, out target))
					return false;
			}

			return fieldRef.ResolveFieldWithinSameModule() == builderField
				   && target.MatchThis();
		}

		protected bool MatchStateAssignment(ILNode stfld, out int stateID) =>
			MatchStateAssignment(stfld, stateField, out stateID);

		static bool MatchStateAssignment(ILNode stfld, FieldDef stateField, out int stateID) {
			// stfld(StateMachine::<>1__state, ldloc(this), ldc.i4(stateId))
			stateID = 0;
			IField fieldRef;
			ILExpression target, val;
			if (stfld.Match(ILCode.Stfld, out fieldRef, out target, out val)) {
				return fieldRef.ResolveFieldWithinSameModule() == stateField
					&& target.MatchThis()
					&& val.Match(ILCode.Ldc_I4, out stateID);
			}
			return false;
		}

		protected static void MatchHoistedLocalCleanup(List<ILNode> block, ref int pos) {
			while (block[pos].Match(ILCode.Stfld, out IField _, out var target, out var value)) {
				// https://github.com/dotnet/roslyn/pull/39735 hoisted local cleanup
				if (!target.MatchThis())
					throw new SymbolicAnalysisFailedException();
				if (!(value.Match(ILCode.Ldnull) || value.Match(ILCode.DefaultValue)))
					throw new SymbolicAnalysisFailedException();
				pos++;
			}
		}

		#region MarkGeneratedVariables
		int smallestGeneratedVariableIndex = int.MaxValue;

		protected void MarkAsGeneratedVariable(ILVariable v) {
			if (v.OriginalVariable != null && v.OriginalVariable.Index >= 0)
				smallestGeneratedVariableIndex = Math.Min(smallestGeneratedVariableIndex, v.OriginalVariable.Index);
		}

		void MarkGeneratedVariables(List<ILNode> newTopLevelBody) {
			var expressions = new ILBlock(newTopLevelBody).GetSelfAndChildrenRecursive<ILExpression>(list_ILExpression);
			foreach (var v in expressions.Select(e => e.Operand).OfType<ILVariable>()) {
				if (v.OriginalVariable != null && v.OriginalVariable.Index >= smallestGeneratedVariableIndex)
					v.GeneratedByDecompiler = true;
			}
		}
		#endregion

		#region RunStep2() method
		public void RunStep2(DecompilerContext context, ILBlock method, out AsyncMethodDebugInfo asyncInfo, List<ILExpression> listExpr, List<ILBlock> listBlock, Dictionary<ILLabel, int> labelRefCount, List<ILNode> list_ILNode, Func<ILBlock, ILInlining> getILInlining) {
			Debug.Assert(context.CurrentMethodIsAsync);
			Step2(method);
			BaseMethodWrapperFixer.FixBaseCalls(context.CurrentMethod.DeclaringType, method, listExpr);
			ILAstOptimizer.RemoveRedundantCode(context, method, listExpr, listBlock, labelRefCount);
			// Repeat the inlining/copy propagation optimization because the conversion of field access
			// to local variables can open up additional inlining possibilities.
			ILInlining inlining = getILInlining(method);
			inlining.InlineAllVariables();
			inlining.CopyPropagation(list_ILNode);

			if (context.CalculateILSpans) {
				var stepInfos = new AsyncStepInfo[asyncStepInfoMap.Count];
				int w = 0;
				foreach (var kv in asyncStepInfoMap) {
					var info = kv.Value;
					// VB state machines have some extra labels that we can ignore
					Debug.Assert(CompilerName == PredefinedCompilerNames.MicrosoftVisualBasic || (info.YieldOffset != 0 && info.ResumeLabel != null));
					if (info.YieldOffset == 0 || info.ResumeLabel == null)
						continue;
					bool b = GetLabelOffset(info.ResumeLabel, out uint resumeOffset);
					Debug.Assert(b);
					if (!b)
						continue;
					stepInfos[w++] = new AsyncStepInfo(info.YieldOffset, moveNextMethod, resumeOffset);
				}
				if (stepInfos.Length != w)
					Array.Resize(ref stepInfos, w);
				if (context.CurrentMethod.MethodSig.RetType.RemovePinnedAndModifiers().GetElementType() != ElementType.Void)
					catchHandlerOffset = uint.MaxValue;
				asyncInfo = new AsyncMethodDebugInfo(stepInfos, builderField, catchHandlerOffset, setResultOffset);
			}
			else
				asyncInfo = null;
		}
		protected abstract void Step2(ILBlock method);
		#endregion

		static bool GetLabelOffset(ILLabel lbl, out uint labelOffset) {
			if (lbl.Offset != uint.MaxValue) {
				labelOffset = lbl.Offset;
				return true;
			}
			labelOffset = 0;
			return false;
		}

		struct TempAsyncStepInfo {
			// Right after 'state = next-state-value;'
			public uint YieldOffset;
			// Where the state machine continues, a unique label in MoveNext
			public ILLabel ResumeLabel;
		}
		readonly Dictionary<int, TempAsyncStepInfo> asyncStepInfoMap = new Dictionary<int, TempAsyncStepInfo>();
		uint catchHandlerOffset = uint.MaxValue;
		uint setResultOffset = uint.MaxValue;

		protected void RemoveAsyncStepInfoState(int stateId) => asyncStepInfoMap.Remove(stateId);

		protected void AddYieldOffset(List<ILNode> body, int index, int count, int stateId) {
			if (!context.CalculateILSpans)
				return;
			asyncStepInfoMap.TryGetValue(stateId, out var info);
			Debug.Assert(info.YieldOffset == 0);
			info.YieldOffset = GetNextOffset(body, index, count);
			asyncStepInfoMap[stateId] = info;
		}

		protected void AddResumeLabel(ILLabel resumeLabel, int stateId) {
			if (!context.CalculateILSpans)
				return;
			asyncStepInfoMap.TryGetValue(stateId, out var info);
			if (info.ResumeLabel == null || info.ResumeLabel.Offset == 0 || resumeLabel.Offset > info.ResumeLabel.Offset) {
				info.ResumeLabel = resumeLabel;
				asyncStepInfoMap[stateId] = info;
			}
		}

		static uint GetNextOffset(List<ILNode> body, int index, int count) {
			uint offs = 0;
			for (int i = 0; i < count; i++) {
				foreach (var span in body[index + i].GetSelfAndChildrenRecursiveILSpans())
					offs = Math.Max(offs, span.End);
			}
			Debug.Assert(offs != 0);
			return offs;
		}

		static uint GetOffset(List<ILNode> body, int index, int count) {
			uint offs = uint.MaxValue;
			for (int i = 0; i < count; i++) {
				foreach (var span in body[index + i].GetSelfAndChildrenRecursiveILSpans())
					offs = Math.Min(offs, span.Start);
			}
			Debug.Assert(offs != uint.MaxValue);
			return offs == uint.MaxValue ? 0 : offs;
		}

		protected LabelRangeMapping CreateLabelRangeMapping(StateRangeAnalysis rangeAnalysis, List<ILNode> body, int pos, int bodyLength) {
			var m = rangeAnalysis.CreateLabelRangeMapping(body, pos, bodyLength);
			if (context.CalculateILSpans) {
				foreach (var kv in m) {
					var state = kv.Value.TryGetSingleState();
					if (state == null)
						continue;
					AddResumeLabel(kv.Key, state.Value);
				}
			}
			return m;
		}
	}
}
