using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Decompiler;

namespace ICSharpCode.Decompiler.ILAst {
	static class Utils
	{
		public static void NopMergeILSpans(ILBlockBase block, List<ILNode> newBody, ref int i) {
			var body = block.Body;

			var j = i;
			do {
				j++;
			}
			while (j < body.Count && body[j].Match(ILCode.Nop));

			var spans = new List<ILSpan>(j - i);
			while (i < j) {
				body[i].AddSelfAndChildrenRecursiveILSpans(spans);
				i++;
			}
			i--;

			ILNode prevNode = null, nextNode = null;
			ILExpression prev = null, next = null;
			if (newBody.Count > 0)
				prev = (prevNode = newBody[newBody.Count - 1]) as ILExpression;
			if (i + 1 < body.Count)
				next = (nextNode = body[i + 1]) as ILExpression;

			ILNode node = null;

			if (prev != null && prev.Prefixes == null) {
				switch (prev.Code) {
				case ILCode.Call:
				case ILCode.CallGetter:
				case ILCode.Calli:
				case ILCode.CallSetter:
				case ILCode.Callvirt:
				case ILCode.CallvirtGetter:
				case ILCode.CallvirtSetter:
				case ILCode.CallReadOnlySetter:
					node = prev;
					break;
				}
			}

			if (next != null && next.Prefixes == null) {
				if (next.Match(ILCode.Leave))
					node = next;
			}

			if (node != null && node == prevNode) {
				if (prevNode != null && prevNode.SafeToAddToEndILSpans)
					prevNode.EndILSpans.AddRange(spans);
				else if (nextNode != null)
					nextNode.ILSpans.AddRange(spans);
				else if (prevNode != null)
					block.EndILSpans.AddRange(spans);
				else
					block.ILSpans.AddRange(spans);
			}
			else {
				if (nextNode != null)
					nextNode.ILSpans.AddRange(spans);
				else if (prevNode != null) {
					if (prevNode.SafeToAddToEndILSpans)
						prevNode.EndILSpans.AddRange(spans);
					else
						block.EndILSpans.AddRange(spans);
				}
				else
					block.ILSpans.AddRange(spans);
			}
		}

		public static void LabelMergeILSpans(ILBlockBase block, List<ILNode> newBody, int instrIndexToRemove)
		{
			var body = block.Body;
			ILNode prevNode = null, nextNode = null;
			if (newBody.Count > 0)
				prevNode = newBody[newBody.Count - 1];
			if (instrIndexToRemove + 1 < body.Count)
				nextNode = body[instrIndexToRemove + 1];

			AddILSpansTryNextFirst(body[instrIndexToRemove], prevNode, nextNode, block);
		}

		public static void AddILSpansTryPreviousFirst(ILNode removed, ILNode prev, ILNode next, ILBlockBase block)
		{
			if (removed == null)
				return;
			AddILSpansTryPreviousFirst(prev, next, block, removed);
		}

		public static void AddILSpansTryNextFirst(ILNode removed, ILNode prev, ILNode next, ILBlockBase block)
		{
			if (removed == null)
				return;
			AddILSpansTryNextFirst(prev, next, block, removed);
		}

		public static void AddILSpansTryPreviousFirst(ILNode prev, ILNode next, ILBlockBase block, ILNode removed)
		{
			if (prev != null && prev.SafeToAddToEndILSpans)
				removed.AddSelfAndChildrenRecursiveILSpans(prev.EndILSpans);
			else if (next != null)
				removed.AddSelfAndChildrenRecursiveILSpans(next.ILSpans);
			else if (prev != null)
				removed.AddSelfAndChildrenRecursiveILSpans(block.EndILSpans);
			else
				removed.AddSelfAndChildrenRecursiveILSpans(block.ILSpans);
		}

		public static void AddILSpansTryNextFirst(ILNode prev, ILNode next, ILBlockBase block, ILNode removed)
		{
			if (next != null)
				removed.AddSelfAndChildrenRecursiveILSpans(next.ILSpans);
			else if (prev != null) {
				if (prev.SafeToAddToEndILSpans)
					removed.AddSelfAndChildrenRecursiveILSpans(prev.EndILSpans);
				else
					removed.AddSelfAndChildrenRecursiveILSpans(block.EndILSpans);
			}
			else
				removed.AddSelfAndChildrenRecursiveILSpans(block.ILSpans);
		}

		public static void AddILSpansTryNextFirst(ILNode prev, ILNode next, ILBlockBase block, IEnumerable<ILSpan> ilSpans)
		{
			if (next != null)
				next.ILSpans.AddRange(ilSpans);
			else if (prev != null) {
				if (prev.SafeToAddToEndILSpans)
					prev.EndILSpans.AddRange(ilSpans);
				else
					block.EndILSpans.AddRange(ilSpans);
			}
			else
				block.ILSpans.AddRange(ilSpans);
		}

		public static void AddILSpansTryPreviousFirst(List<ILNode> newBody, List<ILNode> body, int removedIndex, ILBlockBase block)
		{
			ILNode prev = newBody.Count > 0 ? newBody[newBody.Count - 1] : null;
			ILNode next = removedIndex + 1 < body.Count ? body[removedIndex + 1] : null;
			AddILSpansTryPreviousFirst(body[removedIndex], prev, next, block);
		}

		public static void AddILSpansTryNextFirst(List<ILNode> newBody, List<ILNode> body, int removedIndex, ILBlockBase block)
		{
			ILNode prev = newBody.Count > 0 ? newBody[newBody.Count - 1] : null;
			ILNode next = removedIndex + 1 < body.Count ? body[removedIndex + 1] : null;
			AddILSpansTryNextFirst(body[removedIndex], prev, next, block);
		}

		/// <summary>
		/// Adds the removed instruction's ILSpans to the next or previous instruction
		/// </summary>
		/// <param name="block">The owner block</param>
		/// <param name="body">Body</param>
		/// <param name="removedIndex">Index of removed instruction</param>
		public static void AddILSpans(ILBlockBase block, List<ILNode> body, int removedIndex)
		{
			AddILSpans(block, body, removedIndex, 1);
		}

		/// <summary>
		/// Adds the removed instruction's ILSpans to the next or previous instruction
		/// </summary>
		/// <param name="block">The owner block</param>
		/// <param name="body">Body</param>
		/// <param name="removedIndex">Index of removed instruction</param>
		/// <param name="numRemoved">Number of removed instructions</param>
		public static void AddILSpans(ILBlockBase block, List<ILNode> body, int removedIndex, int numRemoved)
		{
			var prev = removedIndex - 1 >= 0 ? body[removedIndex - 1] : null;
			var next = removedIndex + numRemoved < body.Count ? body[removedIndex + numRemoved] : null;

			ILNode node = null;
			if (next is ILExpression)
				node = next;
			if (node == null && prev is ILExpression)
				node = prev;
			if (node == null && next is ILLabel)
				node = next;
			if (node == null && prev is ILLabel)
				node = prev;
			if (node == null)
				node = next ?? prev;	// Using next before prev should work better

			for (int i = 0; i < numRemoved; i++)
				AddILSpansToInstruction(node, prev, next, block, body[removedIndex + i]);
		}

		public static void AddILSpans(ILBlockBase block, List<ILNode> body, int removedIndex, IEnumerable<ILSpan> ilSpans)
		{
			var prev = removedIndex - 1 >= 0 ? body[removedIndex - 1] : null;
			var next = removedIndex + 1 < body.Count ? body[removedIndex + 1] : null;

			ILNode node = null;
			if (next is ILExpression)
				node = next;
			if (node == null && prev is ILExpression)
				node = prev;
			if (node == null && next is ILLabel)
				node = next;
			if (node == null && prev is ILLabel)
				node = prev;
			if (node == null)
				node = next ?? prev;	// Using next before prev should work better

			AddILSpansToInstruction(node, prev, next, block, ilSpans);
		}

		public static void AddILSpansToInstruction(ILNode nodeToAddTo, ILNode prev, ILNode next, ILBlockBase block, ILNode removed)
		{
			Debug.Assert(nodeToAddTo == prev || nodeToAddTo == next || nodeToAddTo == block);
			if (nodeToAddTo != null) {
				if (nodeToAddTo == prev && prev.SafeToAddToEndILSpans) {
					removed.AddSelfAndChildrenRecursiveILSpans(prev.EndILSpans);
					return;
				}
				else if (nodeToAddTo == next) {
					removed.AddSelfAndChildrenRecursiveILSpans(next.ILSpans);
					return;
				}
			}
			AddILSpansTryNextFirst(prev, next, block, removed);
		}

		public static void AddILSpansToInstruction(ILNode nodeToAddTo, ILNode prev, ILNode next, ILBlockBase block, IEnumerable<ILSpan> ilSpans)
		{
			Debug.Assert(nodeToAddTo == prev || nodeToAddTo == next || nodeToAddTo == block);
			if (nodeToAddTo != null) {
				if (nodeToAddTo == prev && prev.SafeToAddToEndILSpans) {
					prev.EndILSpans.AddRange(ilSpans);
					return;
				}
				else if (nodeToAddTo == next) {
					next.ILSpans.AddRange(ilSpans);
					return;
				}
			}
			AddILSpansTryNextFirst(prev, next, block, ilSpans);
		}
	}
}
