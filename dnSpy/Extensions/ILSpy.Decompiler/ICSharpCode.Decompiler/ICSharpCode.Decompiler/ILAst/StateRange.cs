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
using dnlib.DotNet;

namespace ICSharpCode.Decompiler.ILAst {
	struct Interval {
		public readonly int Start, End;

		public Interval(int start, int end) {
			Debug.Assert(start <= end || (start == 0 && end == -1));
			this.Start = start;
			this.End = end;
		}

		public override string ToString() {
			return string.Format("({0} to {1})", Start, End);
		}
	}

	class StateRange {
		readonly List<Interval> data = new List<Interval>();

		public StateRange() {
		}

		public StateRange(int start, int end) {
			this.data.Add(new Interval(start, end));
		}

		public bool IsEmpty {
			get { return data.Count == 0; }
		}

		public int? TryGetSingleState() {
			if (data.Count != 1)
				return null;
			var i = data[0];
			if (i.Start == i.End)
				return i.Start;
			return null;
		}

		public bool Contains(int val) {
			for (int i = 0; i < data.Count; i++) {
				var v = data[i];
				if (v.Start <= val && val <= v.End)
					return true;
			}
			return false;
		}

		public void UnionWith(StateRange other) {
			data.AddRange(other.data);
		}

		/// <summary>
		/// Unions this state range with (other intersect (minVal to maxVal))
		/// </summary>
		public void UnionWith(StateRange other, int minVal, int maxVal) {
			for (int i = 0; i < other.data.Count; i++) {
				var v = other.data[i];
				int start = Math.Max(v.Start, minVal);
				int end = Math.Min(v.End, maxVal);
				if (start <= end)
					data.Add(new Interval(start, end));
			}
		}

		/// <summary>
		/// Merges overlapping interval ranges.
		/// </summary>
		public void Simplify() {
			if (data.Count < 2)
				return;
			data.Sort((a, b) => a.Start.CompareTo(b.Start));
			Interval prev = data[0];
			int prevIndex = 0;
			for (int i = 1; i < data.Count; i++) {
				Interval next = data[i];
				Debug.Assert(prev.Start <= next.Start);
				if (next.Start <= prev.End + 1) { // intervals overlapping or touching
					prev = new Interval(prev.Start, Math.Max(prev.End, next.End));
					data[prevIndex] = prev;
				}
				else {
					prev = next;
					prevIndex = i;
				}
			}
			int c = data.Count - prevIndex - 1;
			if (c > 0)
				data.RemoveRange(prevIndex + 1, c);
		}

		public override string ToString() {
			return string.Join(",", data);
		}

		public Interval ToEnclosingInterval() {
			if (data.Count == 0)
				throw new SymbolicAnalysisFailedException();
			return new Interval(data[0].Start, data[data.Count - 1].End);
		}
	}

	enum StateRangeAnalysisMode {
		IteratorMoveNext,
		IteratorDispose,
		AsyncMoveNext
	}

	abstract class StateRangeAnalysis {
		protected readonly StateRangeAnalysisMode mode;
		protected readonly FieldDef stateField;
		internal readonly DefaultDictionary<ILNode, StateRange> ranges;
		protected readonly SymbolicEvaluationContext evalContext;

		public List<ILVariable> StateVariables => evalContext.StateVariables;

		/// <summary>
		/// Initializes the state range logic:
		/// Clears 'ranges' and sets 'ranges[entryPoint]' to the full range (int.MinValue to int.MaxValue)
		/// </summary>
		protected StateRangeAnalysis(ILNode entryPoint, StateRangeAnalysisMode mode, FieldDef stateField, ILVariable cachedStateVar) {
			this.mode = mode;
			this.stateField = stateField;

			ranges = new DefaultDictionary<ILNode, StateRange>(n => new StateRange());
			ranges[entryPoint] = new StateRange(int.MinValue, int.MaxValue);
			evalContext = new SymbolicEvaluationContext(stateField);
			if (cachedStateVar != null)
				evalContext.AddStateVariable(cachedStateVar);
		}

		protected virtual int? AssignStateRanges(List<ILNode> body, int i, ILExpression expr, StateRange nodeRange) {
			SymbolicValue val;
			StateRange nextRange;
			switch (expr.Code) {
			case ILCode.Switch:
				val = evalContext.Eval(expr.Arguments[0]);
				if (val.Type != SymbolicValueType.State)
					goto default;
				ILLabel[] targetLabels = (ILLabel[])expr.Operand;
				for (int j = 0; j < targetLabels.Length; j++) {
					int state = j - val.Constant;
					ranges[targetLabels[j]].UnionWith(nodeRange, state, state);
				}
				nextRange = ranges[body[i + 1]];
				nextRange.UnionWith(nodeRange, int.MinValue, -1 - val.Constant);
				nextRange.UnionWith(nodeRange, targetLabels.Length - val.Constant, int.MaxValue);
				break;
			case ILCode.Br:
			case ILCode.Leave:
				ranges[(ILLabel)expr.Operand].UnionWith(nodeRange);
				break;
			case ILCode.Brtrue:
				val = evalContext.Eval(expr.Arguments[0]).AsBool();
				if (val.Type == SymbolicValueType.StateEquals) {
					ranges[(ILLabel)expr.Operand].UnionWith(nodeRange, val.Constant, val.Constant);
					nextRange = ranges[body[i + 1]];
					nextRange.UnionWith(nodeRange, int.MinValue, val.Constant - 1);
					nextRange.UnionWith(nodeRange, val.Constant + 1, int.MaxValue);
					break;
				}
				else if (val.Type == SymbolicValueType.StateInEquals) {
					ranges[body[i + 1]].UnionWith(nodeRange, val.Constant, val.Constant);
					StateRange targetRange = ranges[(ILLabel)expr.Operand];
					targetRange.UnionWith(nodeRange, int.MinValue, val.Constant - 1);
					targetRange.UnionWith(nodeRange, val.Constant + 1, int.MaxValue);
					break;
				}
				else if (val.Type == SymbolicValueType.StateIsInRange) {
					ranges[(ILLabel)expr.Operand].UnionWith(nodeRange, val.Constant, val.Constant2);
					nextRange = ranges[body[i + 1]];
					if (val.Constant != int.MinValue)
						nextRange.UnionWith(nodeRange, int.MinValue, val.Constant - 1);
					if (val.Constant2 != int.MaxValue)
						nextRange.UnionWith(nodeRange, val.Constant2 + 1, int.MaxValue);
					break;
				}
				else if (val.Type == SymbolicValueType.StateIsNotInRange) {
					if (val.Constant != int.MinValue)
						ranges[(ILLabel)expr.Operand].UnionWith(nodeRange, int.MinValue, val.Constant - 1);
					if (val.Constant2 != int.MaxValue)
						ranges[(ILLabel)expr.Operand].UnionWith(nodeRange, val.Constant2 + 1, int.MaxValue);
					nextRange = ranges[body[i + 1]];
					nextRange.UnionWith(nodeRange, val.Constant, val.Constant2);
					break;
				}
				else
					goto default;
			case ILCode.Nop:
				ranges[body[i + 1]].UnionWith(nodeRange);
				break;
			case ILCode.Ret:
				break;
			case ILCode.Stloc:
				val = evalContext.Eval(expr.Arguments[0]);
				if (val.Type == SymbolicValueType.State && val.Constant == 0) {
					evalContext.AddStateVariable((ILVariable)expr.Operand);
					goto case ILCode.Nop;
				}
				else
					goto default;
			default:
				if (mode == StateRangeAnalysisMode.IteratorDispose)
					throw new SymbolicAnalysisFailedException();
				return i;
			}
			return null;
		}

		protected virtual int? AssignStateRanges(List<ILNode> body, int i, StateRange nodeRange, ILLabel label) {
			ranges[body[i + 1]].UnionWith(nodeRange);
			return null;
		}

		protected virtual int? AssignStateRanges(List<ILNode> body, int i, StateRange nodeRange, ILTryCatchBlock tryFinally) {
			if (mode == StateRangeAnalysisMode.IteratorDispose) {
				if (tryFinally.CatchBlocks.Count != 0 || tryFinally.FaultBlock != null || tryFinally.FinallyBlock == null)
					throw new SymbolicAnalysisFailedException();
				ranges[tryFinally.TryBlock].UnionWith(nodeRange);
				if (tryFinally.TryBlock.Body.Count != 0) {
					ranges[tryFinally.TryBlock.Body[0]].UnionWith(nodeRange);
					AssignStateRanges(tryFinally.TryBlock.Body, tryFinally.TryBlock.Body.Count);
				}
				return null;
			}
			else if (mode == StateRangeAnalysisMode.AsyncMoveNext)
				return i;
			else
				throw new SymbolicAnalysisFailedException();
		}

		public int AssignStateRanges(List<ILNode> body, int bodyEnd) =>
			AssignStateRanges(body, 0, bodyEnd);

		public int AssignStateRanges(List<ILNode> body, int bodyStart, int bodyEnd) {
			if (bodyEnd == 0)
				return 0;
			for (int i = bodyStart; i < bodyEnd; i++) {
				StateRange nodeRange = ranges[body[i]];
				nodeRange.Simplify();

				int? res;
				var label = body[i] as ILLabel;
				if (label != null) {
					res = AssignStateRanges(body, i, nodeRange, label);
					if (res != null)
						return res.Value;
					continue;
				}

				ILTryCatchBlock tryFinally = body[i] as ILTryCatchBlock;
				if (tryFinally != null) {
					res = AssignStateRanges(body, i, nodeRange, tryFinally);
					if (res != null)
						return res.Value;
				}
				else {
					ILExpression expr = body[i] as ILExpression;
					if (expr == null)
						throw new SymbolicAnalysisFailedException();
					res = AssignStateRanges(body, i, expr, nodeRange);
					if (res != null)
						return res.Value;
				}
			}
			return bodyEnd;
		}

		public void EnsureLabelAtPos(List<ILNode> body, ref int pos, ref int bodyLength) {
			//TODO: HACK FIX so .NET Native System.IO.StringWriter.MakeCompletedTask doesn't crash ILSpy
			if (pos >= body.Count)
				pos = body.Count - 1;

			if (pos > 0 && body[pos - 1] is ILLabel) {
				pos--;
				return; // label found
			}
			if (body[pos] is ILLabel)
				return;

			// ensure that the first element at body[pos] is a label:
			ILLabel newLabel = new ILLabel();
			newLabel.Name = "YieldReturnEntryPoint";

			ILExpression expr = pos == 1 && body.Count == 1 ? body[0] as ILExpression : null;
			if (expr != null && expr.Code == ILCode.Leave && expr.Operand is ILLabel) {
				ranges[newLabel] = ranges[(ILLabel)expr.Operand];
				pos = 0;
			}
			else {
				ranges[newLabel] = ranges[body[pos]]; // give the label the range of the instruction at body[pos]
			}

			body.Insert(pos, newLabel);
			bodyLength++;
		}

		public LabelRangeMapping CreateLabelRangeMapping(List<ILNode> body, int pos, int bodyLength) {
			LabelRangeMapping result = new LabelRangeMapping();
			CreateLabelRangeMapping(body, pos, bodyLength, result, false);
			return result;
		}

		void CreateLabelRangeMapping(List<ILNode> body, int pos, int bodyLength, LabelRangeMapping result, bool onlyInitialLabels) {
			for (int i = pos; i < bodyLength; i++) {
				ILLabel label = body[i] as ILLabel;
				if (label != null) {
					result.Add(new KeyValuePair<ILLabel, StateRange>(label, ranges[label]));
				}
				else {
					ILTryCatchBlock tryCatchBlock = body[i] as ILTryCatchBlock;
					if (tryCatchBlock != null) {
						CreateLabelRangeMapping(tryCatchBlock.TryBlock.Body, 0, tryCatchBlock.TryBlock.Body.Count, result, true);
					}
					else if (onlyInitialLabels) {
						break;
					}
				}
			}
		}
	}

	sealed class MicrosoftStateRangeAnalysis : StateRangeAnalysis {
		internal readonly Dictionary<MethodDef, StateRange> finallyMethodToStateRange; // used only for IteratorDispose

		public MicrosoftStateRangeAnalysis(ILNode entryPoint, StateRangeAnalysisMode mode, FieldDef stateField, ILVariable cachedStateVar = null)
			: base(entryPoint, mode, stateField, cachedStateVar) {
			if (mode == StateRangeAnalysisMode.IteratorDispose)
				finallyMethodToStateRange = new Dictionary<MethodDef, StateRange>();
		}

		protected override int? AssignStateRanges(List<ILNode> body, int i, StateRange nodeRange, ILTryCatchBlock tryFinally) {
			if (mode == StateRangeAnalysisMode.IteratorMoveNext)
				return i;
			return base.AssignStateRanges(body, i, nodeRange, tryFinally);
		}

		protected override int? AssignStateRanges(List<ILNode> body, int i, ILExpression expr, StateRange nodeRange) {
			if (expr.Code == ILCode.Call) {
				// in some cases (e.g. foreach over array) the C# compiler produces a finally method outside of try-finally blocks
				if (mode == StateRangeAnalysisMode.IteratorDispose) {
					MethodDef mdef = (expr.Operand as IMethod).ResolveMethodWithinSameModule();
					if (mdef == null || finallyMethodToStateRange.ContainsKey(mdef))
						throw new SymbolicAnalysisFailedException();
					finallyMethodToStateRange.Add(mdef, nodeRange);
					return null;
				}
			}
			return base.AssignStateRanges(body, i, expr, nodeRange);
		}
	}

	sealed class MonoStateRangeAnalysis : StateRangeAnalysis {
		readonly FieldDef disposingField;
		readonly ILVariable disposeInFinallyVar;
		public MonoStateRangeAnalysis(ILNode entryPoint, StateRangeAnalysisMode mode, FieldDef stateField, FieldDef disposingField, ILVariable disposeInFinallyVar, ILVariable cachedStateVar = null)
			: base(entryPoint, mode, stateField, cachedStateVar) {
			this.disposingField = disposingField;
			this.disposeInFinallyVar = disposeInFinallyVar;
		}

		protected override int? AssignStateRanges(List<ILNode> body, int i, ILExpression expr, StateRange nodeRange) {
			switch (expr.Code) {
			case ILCode.Switch:
				seenSwitch = true;
				break;

			case ILCode.Stfld:
				FieldDef field;
				int value;
				if (!MatchStoreField(expr, out field, out value))
					break;
				if (field != disposingField && field != stateField)
					break;
				ranges[body[i + 1]].UnionWith(nodeRange);
				return null;

			case ILCode.Brtrue:
				// Ignore $disposing checks. Next instr is a store to $PC (state field)
				var ldfld = expr.Arguments[0];
				if (ldfld.Code == ILCode.Ldfld && ldfld.Arguments[0].MatchThis() && (ldfld.Operand as IField).ResolveFieldWithinSameModule() == disposingField) {
					ranges[body[i + 1]].UnionWith(nodeRange);
					return null;
				}
				break;

			case ILCode.Stloc:
				if (expr.Operand == disposeInFinallyVar && expr.Arguments[0].MatchLdcI4(0)) {
					ranges[body[i + 1]].UnionWith(nodeRange);
					return null;
				}
				break;
			}

			return base.AssignStateRanges(body, i, expr, nodeRange);
		}
		bool seenSwitch;

		protected override int? AssignStateRanges(List<ILNode> body, int i, StateRange nodeRange, ILTryCatchBlock tryFinally) => i;

		protected override int? AssignStateRanges(List<ILNode> body, int i, StateRange nodeRange, ILLabel label) {
			if (seenSwitch && mode == StateRangeAnalysisMode.IteratorMoveNext) {
				ranges[body[i + 1]].UnionWith(nodeRange);
				return i + 1;
			}

			return base.AssignStateRanges(body, i, nodeRange, label);
		}

		bool MatchStoreField(ILExpression expr, out FieldDef field, out int value) {
			field = null;
			value = 0;
			IField f;
			ILExpression ldthis, ldci4;
			if (!expr.Match(ILCode.Stfld, out f, out ldthis, out ldci4))
				return false;
			if (!ldthis.MatchThis())
				return false;
			if (!ldci4.Match(ILCode.Ldc_I4, out value))
				return false;
			field = DnlibExtensions.ResolveFieldWithinSameModule(f);
			return field != null;
		}
	}

	class LabelRangeMapping : List<KeyValuePair<ILLabel, StateRange>> { }
}
