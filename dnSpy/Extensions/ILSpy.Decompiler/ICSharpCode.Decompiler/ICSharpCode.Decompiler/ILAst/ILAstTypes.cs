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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.NRefactory;

namespace ICSharpCode.Decompiler.ILAst {
	public abstract class ILNode : IEnumerable<ILNode>
	{
		public readonly List<ILSpan> ILSpans = new List<ILSpan>(1);

		public virtual List<ILSpan> EndILSpans {
			get { return ILSpans; }
		}
		public virtual ILSpan GetAllILSpans(ref long index, ref bool done) {
			if (index < ILSpans.Count)
				return ILSpans[(int)index++];
			done = true;
			return default(ILSpan);
		}

		public bool HasEndILSpans {
			get { return ILSpans != EndILSpans; }
		}

		public bool WritesNewLine {
			get { return !(this is ILLabel || this is ILExpression || this is ILSwitch.CaseBlock); }
		}

		public virtual bool SafeToAddToEndILSpans {
			get { return false; }
		}

		public IEnumerable<ILSpan> GetSelfAndChildrenRecursiveILSpans()
		{
			foreach (var node in GetSelfAndChildrenRecursive<ILNode>()) {
				long index = 0;
				bool done = false;
				for (;;) {
					var b = node.GetAllILSpans(ref index, ref done);
					if (done)
						break;
					yield return b;
				}
			}
		}

		public void AddSelfAndChildrenRecursiveILSpans(List<ILSpan> coll)
		{
			foreach (var a in GetSelfAndChildrenRecursive<ILNode>()) {
				long index = 0;
				bool done = false;
				for (;;) {
					var b = a.GetAllILSpans(ref index, ref done);
					if (done)
						break;
					coll.Add(b);
				}
			}
		}

		public List<ILSpan> GetSelfAndChildrenRecursiveILSpans_OrderAndJoin() {
			// The current callers save the list as an annotation so always create a new list here
			// instead of having them pass in a cached list.
			var list = new List<ILSpan>();
			AddSelfAndChildrenRecursiveILSpans(list);
			return ILSpan.OrderAndCompactList(list);
		}

		public List<T> GetSelfAndChildrenRecursive<T>(Func<T, bool> predicate = null) where T: ILNode
		{
			List<T> result = new List<T>(16);
			AccumulateSelfAndChildrenRecursive(result, predicate);
			return result;
		}

		public List<T> GetSelfAndChildrenRecursive<T>(List<T> result, Func<T, bool> predicate = null) where T: ILNode
		{
			result.Clear();
			AccumulateSelfAndChildrenRecursive(result, predicate);
			return result;
		}

		void AccumulateSelfAndChildrenRecursive<T>(List<T> list, Func<T, bool> predicate) where T:ILNode
		{
			// Note: RemoveEndFinally depends on self coming before children
			T thisAsT = this as T;
			if (thisAsT != null && (predicate == null || predicate(thisAsT)))
				list.Add(thisAsT);
			int index = 0;
			for (;;) {
				var node = GetNext(ref index);
				if (node == null)
					break;
				node.AccumulateSelfAndChildrenRecursive(list, predicate);
			}
		}

		internal virtual ILNode GetNext(ref int index)
		{
			return null;
		}

		public bool HasChildren {
			get {
				foreach (var c in GetChildren())
					return true;
				return false;
			}
		}

		public ILNode GetChildren()
		{
			return this;
		}

		public ILNode_Enumerator GetEnumerator()
		{
			return new ILNode_Enumerator(this);
		}

		IEnumerator<ILNode> IEnumerable<ILNode>.GetEnumerator()
		{
			return new ILNode_Enumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new ILNode_Enumerator(this);
		}

		public struct ILNode_Enumerator : IEnumerator<ILNode>
		{
			readonly ILNode node;
			int index;
			ILNode current;

			internal ILNode_Enumerator(ILNode node)
			{
				this.node = node;
				this.index = 0;
				this.current = null;
			}

			public ILNode Current
			{
				get { return current; }
			}

			object IEnumerator.Current
			{
				get { return current; }
			}

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				return (this.current = this.node.GetNext(ref index)) != null;
			}

			public void Reset()
			{
				this.index = 0;
			}
		}

		public override string ToString()
		{
			var output = new StringBuilderDecompilerOutput();
			WriteTo(output, null);
			return output.ToString().Replace("\r\n", "; ");
		}

		public abstract void WriteTo(IDecompilerOutput output, MethodDebugInfoBuilder builder);

		protected void UpdateDebugInfo(MethodDebugInfoBuilder builder, int startLoc, int endLoc, IEnumerable<ILSpan> ranges)
		{
			if (builder == null)
				return;
			foreach (var ilSpan in ILSpan.OrderAndCompact(ranges))
				builder.Add(new SourceStatement(ilSpan, new TextSpan(startLoc, endLoc - startLoc)));
		}

		protected readonly struct BraceInfo {
			public int Start { get; }
			public BraceInfo(int start) {
				Start = start;
			}
		}

		protected BraceInfo WriteHiddenStart(IDecompilerOutput output, MethodDebugInfoBuilder builder, IEnumerable<ILSpan> extraILSpans = null)
		{
			var location = output.NextPosition;
			var start = output.NextPosition;
			output.Write("{", BoxedTextColor.Punctuation);
			var ilr = new List<ILSpan>(ILSpans);
			if (extraILSpans != null)
				ilr.AddRange(extraILSpans);
			UpdateDebugInfo(builder, location, output.NextPosition, ilr);
			output.WriteLine();
			output.IncreaseIndent();
			return new BraceInfo(start);
		}

		protected void WriteHiddenEnd(IDecompilerOutput output, MethodDebugInfoBuilder builder, BraceInfo info, CodeBracesRangeFlags flags)
		{
			output.DecreaseIndent();
			var location = output.NextPosition;
			var end = output.NextPosition;
			output.Write("}", BoxedTextColor.Punctuation);
			output.AddBracePair(new TextSpan(info.Start, 1), new TextSpan(end, 1), flags);
			UpdateDebugInfo(builder, location, output.NextPosition, EndILSpans);
			output.WriteLine();
		}
	}

	public abstract class ILBlockBase: ILNode
	{
		public List<ILNode> Body;
		public List<ILSpan> endILSpans = new List<ILSpan>(1);
		protected abstract CodeBracesRangeFlags CodeBracesRangeFlags { get; }

		public override List<ILSpan> EndILSpans {
			get { return endILSpans; }
		}
		public override ILSpan GetAllILSpans(ref long index, ref bool done) {
			if (index < ILSpans.Count)
				return ILSpans[(int)index++];
			int i = (int)index - ILSpans.Count;
			if (i < endILSpans.Count) {
				index++;
				return endILSpans[i];
			}
			done = true;
			return default(ILSpan);
		}

		public override bool SafeToAddToEndILSpans {
			get { return true; }
		}

		public ILBlockBase()
		{
			this.Body = new List<ILNode>();
		}

		public ILBlockBase(params ILNode[] body)
		{
			this.Body = new List<ILNode>(body);
		}

		public ILBlockBase(List<ILNode> body)
		{
			this.Body = body;
		}

		internal override ILNode GetNext(ref int index)
		{
			if (index < this.Body.Count)
				return this.Body[index++];
			return null;
		}

		public override void WriteTo(IDecompilerOutput output, MethodDebugInfoBuilder builder)
		{
			WriteTo(output, builder, null);
		}

		internal void WriteTo(IDecompilerOutput output, MethodDebugInfoBuilder builder, IEnumerable<ILSpan> ilSpans)
		{
			var info = WriteHiddenStart(output, builder, ilSpans);
			foreach(ILNode child in this.GetChildren()) {
				child.WriteTo(output, builder);
				if (!child.WritesNewLine)
					output.WriteLine();
			}
			WriteHiddenEnd(output, builder, info, CodeBracesRangeFlags);
		}
	}

	public class ILBlock: ILBlockBase
	{
		protected override CodeBracesRangeFlags CodeBracesRangeFlags => codeBracesRangeFlags;
		readonly CodeBracesRangeFlags codeBracesRangeFlags;
		public ILExpression EntryGoto;

		public ILBlock()
			: this(CodeBracesRangeFlags.OtherBlockBraces) {
		}

		public ILBlock(CodeBracesRangeFlags codeBracesRangeFlags) {
			this.codeBracesRangeFlags = codeBracesRangeFlags;
		}

		public ILBlock(List<ILNode> body)
			: this (body, CodeBracesRangeFlags.OtherBlockBraces) {
		}

		public ILBlock(List<ILNode> body, CodeBracesRangeFlags codeBracesRangeFlags) : base(body) {
			this.codeBracesRangeFlags = codeBracesRangeFlags;
		}

		internal override ILNode GetNext(ref int index)
		{
			if (index == 0) {
				index = 1;
				if (this.EntryGoto != null)
					return this.EntryGoto;
			}
			if (index <= this.Body.Count)
				return this.Body[index++ - 1];

			return null;
		}
	}

	// Body has to start with a label and end with unconditional control flow
	public class ILBasicBlock: ILBlockBase
	{
		protected override CodeBracesRangeFlags CodeBracesRangeFlags => CodeBracesRangeFlags.OtherBlockBraces;
	}

	public class ILLabel: ILNode
	{
		public string Name;
		public uint Offset = uint.MaxValue;
		public object Reference => o ?? (o = new object());
		object o;

		public override bool SafeToAddToEndILSpans {
			get { return true; }
		}

		public override void WriteTo(IDecompilerOutput output, MethodDebugInfoBuilder builder)
		{
			var location = output.NextPosition;
			output.Write(Name, Reference, DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Definition, BoxedTextColor.Label);
			output.Write(":", BoxedTextColor.Punctuation);
			UpdateDebugInfo(builder, location, output.NextPosition, ILSpans);
		}
	}

	public class ILTryCatchBlock: ILNode
	{
		public abstract class CatchBlockBase: ILBlock {
			public TypeSig ExceptionType;
			public ILVariable ExceptionVariable;
			public List<ILSpan> StlocILSpans = new List<ILSpan>(1);

			protected CatchBlockBase(bool calculateILSpans, List<ILNode> body) {
				this.Body = body;
				if (calculateILSpans && body.Count > 0 && body[0].Match(ILCode.Pop))
					body[0].AddSelfAndChildrenRecursiveILSpans(StlocILSpans);
			}

			public override ILSpan GetAllILSpans(ref long index, ref bool done) {
				if (index < ILSpans.Count)
					return ILSpans[(int)index++];
				int i = (int)index - ILSpans.Count;
				if (i < StlocILSpans.Count) {
					index++;
					return StlocILSpans[i];
				}
				done = true;
				return default(ILSpan);
			}
		}
		public class CatchBlock: CatchBlockBase
		{
			public FilterILBlock FilterBlock;
			protected override CodeBracesRangeFlags CodeBracesRangeFlags => CodeBracesRangeFlags.CatchBraces;

			public CatchBlock(bool calculateILSpans, List<ILNode> body) : base(calculateILSpans, body)
			{
			}

			public override void WriteTo(IDecompilerOutput output, MethodDebugInfoBuilder builder)
			{
				FilterBlock?.WriteTo(output, builder);
				var startLoc = output.NextPosition;
				if (ExceptionType != null) {
					output.Write("catch", BoxedTextColor.Keyword);
					output.Write(" ", BoxedTextColor.Text);
					ExceptionType.WriteTo(output, new StringBuilder(), ILNameSyntax.TypeName);
					if (ExceptionVariable != null) {
						output.Write(" ", BoxedTextColor.Text);
						output.Write(ExceptionVariable.Name, ExceptionVariable.GetTextReferenceObject(), DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Definition, BoxedTextColor.Local);
					}
				}
				else {
					output.Write("handler", BoxedTextColor.Keyword);
					if (ExceptionVariable != null) {
						output.Write(" ", BoxedTextColor.Text);
						output.Write(ExceptionVariable.Name, ExceptionVariable.GetTextReferenceObject(), DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Definition, BoxedTextColor.Local);
					}
				}
				UpdateDebugInfo(builder, startLoc, output.NextPosition, StlocILSpans);
				output.Write(" ", BoxedTextColor.Text);
				base.WriteTo(output, builder);
			}
		}
		public class FilterILBlock: CatchBlockBase
		{
			protected override CodeBracesRangeFlags CodeBracesRangeFlags => CodeBracesRangeFlags.FilterBraces;
			public FilterILBlock(bool calculateILSpans, List<ILNode> body) : base(calculateILSpans, body)
			{
			}

			public override void WriteTo(IDecompilerOutput output, MethodDebugInfoBuilder builder)
			{
				output.Write("filter", BoxedTextColor.Keyword);
				if (ExceptionVariable != null) {
					output.Write(" ", BoxedTextColor.Text);
					output.Write(ExceptionVariable.Name, ExceptionVariable.GetTextReferenceObject(), DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Definition, BoxedTextColor.Local);
					output.Write(" ", BoxedTextColor.Text);
				}
				base.WriteTo(output, builder);
			}
		}

		public ILBlock          TryBlock;
		public List<CatchBlock> CatchBlocks;
		public ILBlock          FinallyBlock;
		public ILBlock          FaultBlock;

		// Used for inlined finally blocks in yield return state machines
		public MethodDef InlinedFinallyMethod;

		internal override ILNode GetNext(ref int index)
		{
			if (index == 0) {
				index = 1;
				if (this.TryBlock != null)
					return this.TryBlock;
			}
			int b = 1 + this.CatchBlocks.Count * 2;
			if (index < b) {
				var cb = this.CatchBlocks[(index - 1) / 2];
				index++;
				if ((index & 1) == 0) {
					if (cb.FilterBlock != null)
						return cb.FilterBlock;
					index++;
				}
				return cb;
			}
			if (index == b) {
				index++;
				if (this.FaultBlock != null)
					return this.FaultBlock;
			}
			if (index == b + 1) {
				index++;
				if (this.FinallyBlock != null)
					return this.FinallyBlock;
			}
			return null;
		}

		public override void WriteTo(IDecompilerOutput output, MethodDebugInfoBuilder builder)
		{
			output.Write(".try", BoxedTextColor.Keyword);
			output.Write(" ", BoxedTextColor.Text);
			TryBlock.WriteTo(output, builder, ILSpans);
			foreach (CatchBlock block in CatchBlocks) {
				block.WriteTo(output, builder);
			}
			if (FaultBlock != null) {
				output.Write("fault", BoxedTextColor.Keyword);
				output.Write(" ", BoxedTextColor.Text);
				FaultBlock.WriteTo(output, builder);
			}
			if (FinallyBlock != null) {
				output.Write("finally", BoxedTextColor.Keyword);
				output.Write(" ", BoxedTextColor.Text);
				FinallyBlock.WriteTo(output, builder);
			}
		}
	}

	public class ILVariable
	{
		[Flags]
		enum Flags : byte {
			GeneratedByDecompiler = 1,
			Renamed = 2,
			Declared = 4,
		}
		public ILVariable(string name) => Name = name;
		public string Name;
		Flags flags;
		public bool GeneratedByDecompiler {
			get => (flags & Flags.GeneratedByDecompiler) != 0;
			set {
				if (value)
					flags |= Flags.GeneratedByDecompiler;
				else
					flags &= ~Flags.GeneratedByDecompiler;
			}
		}
		public bool Renamed {
			get => (flags & Flags.Renamed) != 0;
			set {
				if (value)
					flags |= Flags.Renamed;
				else
					flags &= ~Flags.Renamed;
			}
		}
		public bool Declared {
			get => (flags & Flags.Declared) != 0;
			set {
				if (value)
					flags |= Flags.Declared;
				else
					flags &= ~Flags.Declared;
			}
		}
		public TypeSig Type;
		public TypeSig GetVariableType() => Type ?? OriginalVariable?.Type ?? OriginalParameter?.Type ?? new SentinelSig();
		public Local OriginalVariable;
		public Parameter OriginalParameter;
		public FieldDef HoistedField;
		public SourceLocal GetSourceLocal() {
			Debug.Assert(OriginalParameter == null);
			Debug.Assert(Name != null);
			if (sourceParamOrLocal == null)
				Interlocked.CompareExchange(ref sourceParamOrLocal, HoistedField != null ? new SourceLocal(OriginalVariable, Name, HoistedField, GetSourceVariableFlags()) : new SourceLocal(OriginalVariable, Name, GetVariableType(), GetSourceVariableFlags()), null);
			return (SourceLocal)sourceParamOrLocal;
		}
		SourceVariableFlags GetSourceVariableFlags() => SourceVariableFlags.None;
		public SourceParameter GetSourceParameter() {
			Debug.Assert(OriginalParameter != null);
			Debug.Assert(Name != null);
			if (sourceParamOrLocal == null)
				Interlocked.CompareExchange(ref sourceParamOrLocal, HoistedField != null ? new SourceParameter(OriginalParameter, Name, HoistedField, GetSourceVariableFlags()) : new SourceParameter(OriginalParameter, Name, GetVariableType(), GetSourceVariableFlags()), null);
			return (SourceParameter)sourceParamOrLocal;
		}
		object sourceParamOrLocal;
		public object GetTextReferenceObject() {
			if (OriginalParameter != null)
				return OriginalParameter;
			return GetSourceLocal();
		}

		public bool IsPinned {
			get { return OriginalVariable != null && OriginalVariable.Type is PinnedSig; }
		}

		public bool IsParameter {
			get { return OriginalParameter != null; }
		}

		public override string ToString()
		{
			return Name;
		}
	}

	public class ILExpressionPrefix
	{
		public readonly ILCode Code;
		public readonly object Operand;

		public ILExpressionPrefix(ILCode code, object operand = null)
		{
			this.Code = code;
			this.Operand = operand;
		}
	}

	public class ILExpression : ILNode
	{
		public ILCode Code { get; set; }
		public object Operand { get; set; }
		public List<ILExpression> Arguments { get; }
		public ILExpressionPrefix[] Prefixes { get; set; }

		public TypeSig ExpectedType { get; set; }
		public TypeSig InferredType { get; set; }

		public override bool SafeToAddToEndILSpans {
			get { return true; }
		}

		public ILExpression(ILCode code, object operand, List<ILExpression> args)
		{
			if (operand is ILExpression)
				throw new ArgumentException("operand");

			this.Code = code;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>(args);
		}

		public ILExpression(ILCode code, object operand)
		{
			if (operand is ILExpression)
				throw new ArgumentException("operand");

			this.Code = code;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>();
		}

		public ILExpression(ILCode code, object operand, ILExpression arg1)
		{
			if (operand is ILExpression)
				throw new ArgumentException("operand");

			this.Code = code;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>(1) { arg1 };
		}

		public ILExpression(ILCode code, object operand, ILExpression arg1, ILExpression arg2)
		{
			if (operand is ILExpression)
				throw new ArgumentException("operand");

			this.Code = code;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>(2) { arg1, arg2 };
		}

		public ILExpression(ILCode code, object operand, ILExpression arg1, ILExpression arg2, ILExpression arg3)
		{
			if (operand is ILExpression)
				throw new ArgumentException("operand");

			this.Code = code;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>(3) { arg1, arg2, arg3 };
		}

		public ILExpression(ILCode code, object operand, ILExpression[] args)
		{
			if (operand is ILExpression)
				throw new ArgumentException("operand");

			this.Code = code;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>(args);
		}

		public ILExpressionPrefix GetPrefix(ILCode code)
		{
			var prefixes = this.Prefixes;
			if (prefixes != null) {
				foreach (ILExpressionPrefix p in prefixes) {
					if (p.Code == code)
						return p;
				}
			}
			return null;
		}

		internal override ILNode GetNext(ref int index)
		{
			if (index < Arguments.Count)
				return Arguments[index++];
			return null;
		}

		public bool IsBranch()
		{
			return this.Operand is ILLabel || this.Operand is ILLabel[];
		}

		public ILLabel[] GetBranchTargets()
		{
			if (this.Operand is ILLabel) {
				return new ILLabel[] { (ILLabel)this.Operand };
			} else if (this.Operand is ILLabel[]) {
				return (ILLabel[])this.Operand;
			} else {
				return Array.Empty<ILLabel>();
			}
		}

		void WriteExpectedType(IDecompilerOutput output, StringBuilder sb) {
			var parenStart = output.NextPosition;
			output.Write("[", BoxedTextColor.Punctuation);
			output.Write("exp", BoxedTextColor.Keyword);
			output.Write(":", BoxedTextColor.Punctuation);
			ExpectedType.WriteTo(output, sb, ILNameSyntax.ShortTypeName);
			output.Write("]", BoxedTextColor.Punctuation);
			output.AddBracePair(new TextSpan(parenStart, 1), new TextSpan(output.Length - 1, 1), CodeBracesRangeFlags.SquareBrackets);
		}

		public override void WriteTo(IDecompilerOutput output, MethodDebugInfoBuilder builder)
		{
			var sb = new StringBuilder();
			var startLoc = output.NextPosition;
			if (Operand is ILVariable && ((ILVariable)Operand).GeneratedByDecompiler) {
				var v = (ILVariable)Operand;
				var op = v.GetTextReferenceObject();
				if (Code == ILCode.Stloc && this.InferredType == null) {
					output.Write(((ILVariable)Operand).Name, op, DecompilerReferenceFlags.Local, ((ILVariable)Operand).IsParameter ? BoxedTextColor.Parameter : BoxedTextColor.Local);
					output.Write(" ", BoxedTextColor.Text);
					output.Write("=", BoxedTextColor.Operator);
					output.Write(" ", BoxedTextColor.Text);
					Arguments.First().WriteTo(output, null);
					UpdateDebugInfo(builder, startLoc, output.NextPosition, this.GetSelfAndChildrenRecursiveILSpans());
					return;
				} else if (Code == ILCode.Ldloc) {
					output.Write(((ILVariable)Operand).Name, op, DecompilerReferenceFlags.Local, ((ILVariable)Operand).IsParameter ? BoxedTextColor.Parameter : BoxedTextColor.Local);
					if (this.InferredType != null) {
						output.Write(":", BoxedTextColor.Punctuation);
						this.InferredType.WriteTo(output, sb, ILNameSyntax.ShortTypeName);
						if (this.ExpectedType != null && this.ExpectedType.FullName != this.InferredType.FullName)
							WriteExpectedType(output, sb);
					}
					UpdateDebugInfo(builder, startLoc, output.NextPosition, this.GetSelfAndChildrenRecursiveILSpans());
					return;
				}
			}

			if (this.Prefixes != null) {
				foreach (var prefix in this.Prefixes) {
					var prefixName = prefix.Code.GetName() + ".";
					output.Write(prefixName, prefixName, DecompilerReferenceFlags.Local, BoxedTextColor.OpCode);
					output.Write(" ", BoxedTextColor.Text);
				}
			}

			var codeName = Code.GetName();
			output.Write(codeName, codeName, DecompilerReferenceFlags.Local, BoxedTextColor.OpCode);
			if (this.InferredType != null) {
				output.Write(":", BoxedTextColor.Punctuation);
				this.InferredType.WriteTo(output, sb, ILNameSyntax.ShortTypeName);
				if (this.ExpectedType != null && this.ExpectedType.FullName != this.InferredType.FullName)
					WriteExpectedType(output, sb);
			} else if (this.ExpectedType != null)
				WriteExpectedType(output, sb);
			var parenStart = output.NextPosition;
			output.Write("(", BoxedTextColor.Punctuation);
			bool first = true;
			if (Operand != null) {
				if (Operand is ILLabel) {
					var lbl = (ILLabel)Operand;
					output.Write(lbl.Name, lbl.Reference, DecompilerReferenceFlags.Local, BoxedTextColor.Label);
				} else if (Operand is ILLabel[]) {
					ILLabel[] labels = (ILLabel[])Operand;
					for (int i = 0; i < labels.Length; i++) {
						if (i > 0) {
							output.Write(",", BoxedTextColor.Punctuation);
							output.Write(" ", BoxedTextColor.Text);
						}
						output.Write(labels[i].Name, labels[i].Reference, DecompilerReferenceFlags.Local, BoxedTextColor.Label);
					}
				} else if ((Operand as IMethod)?.MethodSig != null) {
					IMethod method = (IMethod)Operand;
					if (method.DeclaringType != null) {
						method.DeclaringType.WriteTo(output, sb, ILNameSyntax.ShortTypeName);
						output.Write("::", BoxedTextColor.Operator);
					}
					output.Write(method.Name, method, DecompilerReferenceFlags.None, CSharpMetadataTextColorProvider.Instance.GetColor(method));
				} else if (Operand is IField) {
					IField field = (IField)Operand;
					field.DeclaringType.WriteTo(output, sb, ILNameSyntax.ShortTypeName);
					output.Write("::", BoxedTextColor.Operator);
					output.Write(field.Name, field, DecompilerReferenceFlags.None, CSharpMetadataTextColorProvider.Instance.GetColor(field));
				} else if (Operand is ILVariable) {
					var v = (ILVariable)Operand;
					var op = v.GetTextReferenceObject();
					output.Write(v.Name, op, DecompilerReferenceFlags.Local, v.IsParameter ? BoxedTextColor.Parameter : BoxedTextColor.Local);
				} else {
					DisassemblerHelpers.WriteOperand(output, Operand, DecompilerSettings.ConstMaxStringLength, NumberFormatter.GetCSharpInstance(hex: false, upper: true), sb);
				}
				first = false;
			}
			foreach (ILExpression arg in this.Arguments) {
				if (!first) {
					output.Write(",", BoxedTextColor.Punctuation);
					output.Write(" ", BoxedTextColor.Text);
				}
				arg.WriteTo(output, null);
				first = false;
			}
			output.Write(")", BoxedTextColor.Punctuation);
			output.AddBracePair(new TextSpan(parenStart, 1), new TextSpan(output.Length - 1, 1), CodeBracesRangeFlags.Parentheses);
			UpdateDebugInfo(builder, startLoc, output.NextPosition, this.GetSelfAndChildrenRecursiveILSpans());
		}
	}

	public class ILWhileLoop : ILNode
	{
		public ILExpression Condition;
		public ILBlock      BodyBlock;

		internal override ILNode GetNext(ref int index)
		{
			if (index == 0) {
				index = 1;
				if (this.Condition != null)
					return this.Condition;
			}
			if (index == 1) {
				index = 2;
				if (this.BodyBlock != null)
					return this.BodyBlock;
			}
			return null;
		}

		public override void WriteTo(IDecompilerOutput output, MethodDebugInfoBuilder builder)
		{
			var startLoc = output.NextPosition;
			output.Write("loop", BoxedTextColor.Keyword);
			output.Write(" ", BoxedTextColor.Text);
			var parenStart = output.NextPosition;
			output.Write("(", BoxedTextColor.Punctuation);
			if (this.Condition != null)
				this.Condition.WriteTo(output, null);
			output.Write(")", BoxedTextColor.Punctuation);
			output.AddBracePair(new TextSpan(parenStart, 1), new TextSpan(output.Length - 1, 1), CodeBracesRangeFlags.Parentheses);
			var ilSpans = new List<ILSpan>(ILSpans);
			if (this.Condition != null)
				this.Condition.AddSelfAndChildrenRecursiveILSpans(ilSpans);
			UpdateDebugInfo(builder, startLoc, output.NextPosition, ilSpans);
			output.Write(" ", BoxedTextColor.Text);
			this.BodyBlock.WriteTo(output, builder);
		}
	}

	public class ILCondition : ILNode
	{
		public ILExpression Condition;
		public ILBlock TrueBlock;   // Branch was taken
		public ILBlock FalseBlock;  // Fall-though

		internal override ILNode GetNext(ref int index)
		{
			if (index == 0) {
				index = 1;
				if (this.Condition != null)
					return this.Condition;
			}
			if (index == 1) {
				index = 2;
				if (this.TrueBlock != null)
					return this.TrueBlock;
			}
			if (index == 2) {
				index = 3;
				if (this.FalseBlock != null)
					return this.FalseBlock;
			}
			return null;
		}

		public override void WriteTo(IDecompilerOutput output, MethodDebugInfoBuilder builder)
		{
			var startLoc = output.NextPosition;
			output.Write("if", BoxedTextColor.Keyword);
			output.Write(" ", BoxedTextColor.Text);
			var parenStart = output.NextPosition;
			output.Write("(", BoxedTextColor.Punctuation);
			Condition.WriteTo(output, null);
			output.Write(")", BoxedTextColor.Punctuation);
			output.AddBracePair(new TextSpan(parenStart, 1), new TextSpan(output.Length - 1, 1), CodeBracesRangeFlags.Parentheses);
			var ilSpans = new List<ILSpan>(ILSpans);
			Condition.AddSelfAndChildrenRecursiveILSpans(ilSpans);
			UpdateDebugInfo(builder, startLoc, output.NextPosition, ilSpans);
			output.Write(" ", BoxedTextColor.Text);
			TrueBlock.WriteTo(output, builder);
			if (FalseBlock != null) {
				output.Write("else", BoxedTextColor.Keyword);
				output.Write(" ", BoxedTextColor.Text);
				FalseBlock.WriteTo(output, builder);
			}
		}
	}

	public class ILSwitch: ILNode
	{
		public class CaseBlock: ILBlock
		{
			protected override CodeBracesRangeFlags CodeBracesRangeFlags => CodeBracesRangeFlags.CaseBraces;
			public List<int> Values;  // null for the default case

			public override void WriteTo(IDecompilerOutput output, MethodDebugInfoBuilder builder)
			{
				if (this.Values != null) {
					foreach (int i in this.Values) {
						output.Write("case", BoxedTextColor.Keyword);
						output.Write(" ", BoxedTextColor.Text);
						output.Write(string.Format("{0}", i), BoxedTextColor.Number);
						output.WriteLine(":", BoxedTextColor.Punctuation);
					}
				} else {
					output.Write("default", BoxedTextColor.Keyword);
					output.WriteLine(":", BoxedTextColor.Punctuation);
				}
				output.IncreaseIndent();
				base.WriteTo(output, builder);
				output.DecreaseIndent();
			}
		}

		public ILExpression Condition;
		public List<CaseBlock> CaseBlocks = new List<CaseBlock>();
		public List<ILSpan> endILSpans = new List<ILSpan>(1);

		public override List<ILSpan> EndILSpans {
			get { return endILSpans; }
		}
		public override ILSpan GetAllILSpans(ref long index, ref bool done) {
			if (index < ILSpans.Count)
				return ILSpans[(int)index++];
			int i = (int)index - ILSpans.Count;
			if (i < endILSpans.Count) {
				index++;
				return endILSpans[i];
			}
			done = true;
			return default(ILSpan);
		}

		public override bool SafeToAddToEndILSpans {
			get { return true; }
		}

		internal override ILNode GetNext(ref int index)
		{
			if (index == 0) {
				index = 1;
				return this.Condition;
			}
			if (index <= this.CaseBlocks.Count)
				return this.CaseBlocks[index++ - 1];
			return null;
		}

		public override void WriteTo(IDecompilerOutput output, MethodDebugInfoBuilder builder)
		{
			var startLoc = output.NextPosition;
			output.Write("switch", BoxedTextColor.Keyword);
			output.Write(" ", BoxedTextColor.Text);
			var parenStart = output.NextPosition;
			output.Write("(", BoxedTextColor.Punctuation);
			Condition.WriteTo(output, null);
			output.Write(")", BoxedTextColor.Punctuation);
			output.AddBracePair(new TextSpan(parenStart, 1), new TextSpan(output.Length - 1, 1), CodeBracesRangeFlags.Parentheses);
			var ilSpans = new List<ILSpan>(ILSpans);
			Condition.AddSelfAndChildrenRecursiveILSpans(ilSpans);
			UpdateDebugInfo(builder, startLoc, output.NextPosition, ilSpans);
			output.Write(" ", BoxedTextColor.Text);
			var info = WriteHiddenStart(output, builder);
			foreach (CaseBlock caseBlock in this.CaseBlocks) {
				caseBlock.WriteTo(output, builder);
			}
			WriteHiddenEnd(output, builder, info, CodeBracesRangeFlags.SwitchBraces);
		}
	}

	public class ILFixedStatement : ILNode
	{
		public List<ILExpression> Initializers = new List<ILExpression>(1);
		public ILBlock      BodyBlock;

		internal override ILNode GetNext(ref int index)
		{
			if (index < this.Initializers.Count)
				return this.Initializers[index++];
			if (index == this.Initializers.Count) {
				index++;
				if (this.BodyBlock != null)
					return this.BodyBlock;
			}
			return null;
		}

		public override void WriteTo(IDecompilerOutput output, MethodDebugInfoBuilder builder)
		{
			var startLoc = output.NextPosition;
			output.Write("fixed", BoxedTextColor.Keyword);
			output.Write(" ", BoxedTextColor.Text);
			var parenStart = output.NextPosition;
			output.Write("(", BoxedTextColor.Punctuation);
			for (int i = 0; i < this.Initializers.Count; i++) {
				if (i > 0) {
					output.Write(",", BoxedTextColor.Punctuation);
					output.Write(" ", BoxedTextColor.Text);
				}
				this.Initializers[i].WriteTo(output, null);
			}
			output.Write(")", BoxedTextColor.Punctuation);
			output.AddBracePair(new TextSpan(parenStart, 1), new TextSpan(output.Length - 1, 1), CodeBracesRangeFlags.Parentheses);
			var ilSpans = new List<ILSpan>(ILSpans);
			foreach (var i in Initializers)
				i.AddSelfAndChildrenRecursiveILSpans(ilSpans);
			UpdateDebugInfo(builder, startLoc, output.NextPosition, ilSpans);
			output.Write(" ", BoxedTextColor.Text);
			this.BodyBlock.WriteTo(output, builder);
		}
	}
}
