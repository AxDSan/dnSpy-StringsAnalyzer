﻿// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using ICSharpCode.Decompiler.FlowAnalysis;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Decompiler;

namespace ICSharpCode.Decompiler.Disassembler {
	/// <summary>
	/// Specifies the type of an IL structure.
	/// </summary>
	public enum ILStructureType
	{
		/// <summary>
		/// The root block of the method
		/// </summary>
		Root,
		/// <summary>
		/// A nested control structure representing a loop.
		/// </summary>
		Loop,
		/// <summary>
		/// A nested control structure representing a try block.
		/// </summary>
		Try,
		/// <summary>
		/// A nested control structure representing a catch, finally, or fault block.
		/// </summary>
		Handler,
		/// <summary>
		/// A nested control structure representing an exception filter block.
		/// </summary>
		Filter
	}

	/// <summary>
	/// An IL structure.
	/// </summary>
	public class ILStructure
	{
		public readonly ILStructureType Type;

		/// <summary>
		/// Start position of the structure.
		/// </summary>
		public readonly int StartOffset;

		/// <summary>
		/// End position of the structure. (exclusive)
		/// </summary>
		public readonly int EndOffset;

		/// <summary>
		/// The exception handler associated with the Try, Filter or Handler block.
		/// </summary>
		public readonly ExceptionHandler ExceptionHandler;

		/// <summary>
		/// The loop's entry point.
		/// </summary>
		public readonly Instruction LoopEntryPoint;

		/// <summary>
		/// The list of child structures.
		/// </summary>
		public readonly List<ILStructure> Children = new List<ILStructure>();

		public ILStructure(CilBody body)
			: this(ILStructureType.Root, 0, body.GetCodeSize())
		{
			uint codeSize = (uint)EndOffset;
			// Build the tree of exception structures:
			for (int i = 0; i < body.ExceptionHandlers.Count; i++) {
				ExceptionHandler eh = body.ExceptionHandlers[i];
				if (!body.ExceptionHandlers.Take(i).Any(oldEh => oldEh.TryStart == eh.TryStart && oldEh.TryEnd == eh.TryEnd))
					AddNestedStructure(new ILStructure(ILStructureType.Try, (int)eh.TryStart.GetOffset(), (int)(eh.TryEnd?.Offset ?? codeSize), eh));
				int handlerStartOffset = (int)eh.HandlerStart.GetOffset();
				if (eh.HandlerType == ExceptionHandlerType.Filter)
					AddNestedStructure(new ILStructure(ILStructureType.Filter, (int)eh.FilterStart.GetOffset(), handlerStartOffset, eh));
				AddNestedStructure(new ILStructure(ILStructureType.Handler, handlerStartOffset, (int)(eh.HandlerEnd?.Offset ?? codeSize), eh));
			}
			// Very simple loop detection: look for backward branches
			List<KeyValuePair<Instruction, Instruction>> allBranches = FindAllBranches(body);
			// We go through the branches in reverse so that we find the biggest possible loop boundary first (think loops with "continue;")
			for (int i = allBranches.Count - 1; i >= 0; i--) {
				int loopEnd = allBranches[i].Key.GetEndOffset();
				int loopStart = (int)allBranches[i].Value.Offset;
				if (loopStart < loopEnd) {
					// We found a backward branch. This is a potential loop.
					// Check that is has only one entry point:
					Instruction entryPoint = null;

					// entry point is first instruction in loop if prev inst isn't an unconditional branch
					Instruction prev = body.GetPrevious(allBranches[i].Value);
					if (prev != null && !OpCodeInfo.IsUnconditionalBranch(prev.OpCode))
						entryPoint = allBranches[i].Value;

					bool multipleEntryPoints = false;
					for (int j = 0; j < allBranches.Count; j++) {
						var pair = allBranches[j];
						if (pair.Key.Offset < loopStart || pair.Key.Offset >= loopEnd) {
							if (loopStart <= pair.Value.Offset && pair.Value.Offset < loopEnd) {
								// jump from outside the loop into the loop
								if (entryPoint == null)
									entryPoint = pair.Value;
								else if (pair.Value != entryPoint)
									multipleEntryPoints = true;
							}
						}
					}
					if (!multipleEntryPoints) {
						AddNestedStructure(new ILStructure(ILStructureType.Loop, loopStart, loopEnd, entryPoint));
					}
				}
			}
			SortChildren();
		}

		public ILStructure(ILStructureType type, int startOffset, int endOffset, ExceptionHandler handler = null)
		{
			Debug.Assert(startOffset < endOffset);
			this.Type = type;
			this.StartOffset = startOffset;
			this.EndOffset = endOffset;
			this.ExceptionHandler = handler;
		}

		public ILStructure(ILStructureType type, int startOffset, int endOffset, Instruction loopEntryPoint)
		{
			Debug.Assert(startOffset < endOffset);
			this.Type = type;
			this.StartOffset = startOffset;
			this.EndOffset = endOffset;
			this.LoopEntryPoint = loopEntryPoint;
		}

		bool AddNestedStructure(ILStructure newStructure)
		{
			// special case: don't consider the loop-like structure of "continue;" statements to be nested loops
			if (this.Type == ILStructureType.Loop && newStructure.Type == ILStructureType.Loop && newStructure.StartOffset == this.StartOffset)
				return false;

			// use <= for end-offset comparisons because both end and EndOffset are exclusive
			Debug.Assert(StartOffset <= newStructure.StartOffset && newStructure.EndOffset <= EndOffset);
			for (int i = 0; i < this.Children.Count; i++) {
				var child = this.Children[i];
				if (child.StartOffset <= newStructure.StartOffset && newStructure.EndOffset <= child.EndOffset) {
					return child.AddNestedStructure(newStructure);
				}
				else if (!(child.EndOffset <= newStructure.StartOffset || newStructure.EndOffset <= child.StartOffset)) {
					// child and newStructure overlap
					if (!(newStructure.StartOffset <= child.StartOffset && child.EndOffset <= newStructure.EndOffset)) {
						// Invalid nesting, can't build a tree. -> Don't add the new structure.
						return false;
					}
				}
			}
			// Move existing structures into the new structure:
			for (int i = 0; i < this.Children.Count; i++) {
				ILStructure child = this.Children[i];
				if (newStructure.StartOffset <= child.StartOffset && child.EndOffset <= newStructure.EndOffset) {
					this.Children.RemoveAt(i--);
					newStructure.Children.Add(child);
				}
			}
			// Add the structure here:
			this.Children.Add(newStructure);
			return true;
		}

		/// <summary>
		/// Finds all branches. Returns list of source offset->target offset mapping.
		/// Multiple entries for the same source offset are possible (switch statements).
		/// The result is sorted by source offset.
		/// </summary>
		List<KeyValuePair<Instruction, Instruction>> FindAllBranches(CilBody body)
		{
			var result = new List<KeyValuePair<Instruction, Instruction>>();
			for (int i = 0; i < body.Instructions.Count; i++) {
				var inst = body.Instructions[i];
				switch (inst.OpCode.OperandType) {
				case OperandType.InlineBrTarget:
				case OperandType.ShortInlineBrTarget:
					if (inst.Operand is Instruction target)
						result.Add(new KeyValuePair<Instruction, Instruction>(inst, target));
					break;
				case OperandType.InlineSwitch:
					if (inst.Operand is IList<Instruction> list) {
						for (int j = 0; j < list.Count; j++) {
							var target2 = list[j];
							if (target2 != null)
								result.Add(new KeyValuePair<Instruction, Instruction>(inst, target2));
						}
					}
					break;
				}
			}
			return result;
		}

		void SortChildren() {
			Children.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
			for (int i = 0; i < Children.Count; i++)
				Children[i].SortChildren();
		}

		/// <summary>
		/// Gets the innermost structure containing the specified offset.
		/// </summary>
		public ILStructure GetInnermost(int offset)
		{
			Debug.Assert(StartOffset <= offset && offset < EndOffset);
			for (int i = 0; i < this.Children.Count; i++) {
				var child = this.Children[i];
				if (child.StartOffset <= offset && offset < child.EndOffset)
					return child.GetInnermost(offset);
			}
			return this;
		}
	}
}
