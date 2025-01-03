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
using System.Linq;
using System.Runtime.CompilerServices;
using dnSpy.Contracts.Decompiler;
using ICSharpCode.Decompiler.FlowAnalysis;

namespace ICSharpCode.Decompiler.ILAst {
	/// <summary>
	/// Description of LoopsAndConditions.
	/// </summary>
	public class LoopsAndConditions
	{
		readonly Dictionary<ILLabel, ControlFlowNode> labelToCfNode = new Dictionary<ILLabel, ControlFlowNode>();

		DecompilerContext context;

		uint nextLabelIndex;

		public LoopsAndConditions(DecompilerContext context)
		{
			Initialize(context);
		}

		public void Initialize(DecompilerContext context)
		{
			this.context = context;
			this.labelToCfNode.Clear();
			this.nextLabelIndex = 0;
		}

		public void FindLoops(ILBlock block)
		{
			if (block.Body.Count > 0) {
				ControlFlowGraph graph;
				graph = BuildGraph(block.Body, (ILLabel)block.EntryGoto.Operand);
				graph.ComputeDominance(context.CancellationToken);
				graph.ComputeDominanceFrontier();
				//TODO: Keep ILSpans when writing to Body
				block.Body = FindLoops(new HashSet<ControlFlowNode>(graph.Nodes.Skip(3)), graph.EntryPoint, false);
			}
		}

		public void FindConditions(ILBlock block)
		{
			if (block.Body.Count > 0) {
				ControlFlowGraph graph;
				graph = BuildGraph(block.Body, (ILLabel)block.EntryGoto.Operand);
				graph.ComputeDominance(context.CancellationToken);
				graph.ComputeDominanceFrontier();
				//TODO: Keep ILSpans when writing to Body
				block.Body = FindConditions(new HashSet<ControlFlowNode>(graph.Nodes.Skip(3)), graph.EntryPoint);
			}
		}

		readonly ControlFlowGraph cached_ControlFlowGraph = new ControlFlowGraph();
		ControlFlowGraph BuildGraph(List<ILNode> nodes, ILLabel entryLabel)
		{
			cached_ControlFlowGraph.Nodes.Clear();
			int index = 0;
			var cfNodes = cached_ControlFlowGraph.Nodes;
			ControlFlowNode entryPoint = new ControlFlowNode(index++, 0, ControlFlowNodeType.EntryPoint);
			cfNodes.Add(entryPoint);
			ControlFlowNode regularExit = new ControlFlowNode(index++, null, ControlFlowNodeType.RegularExit);
			cfNodes.Add(regularExit);
			ControlFlowNode exceptionalExit = new ControlFlowNode(index++, null, ControlFlowNodeType.ExceptionalExit);
			cfNodes.Add(exceptionalExit);

			// Create graph nodes
			labelToCfNode.Clear();
			Dictionary<ILNode, ControlFlowNode> astNodeToCfNode = new Dictionary<ILNode, ControlFlowNode>();
			List<ILLabel> listLabels = null;
			for (int i = 0; i < nodes.Count; i++) {
				var node = (ILBasicBlock)nodes[i];
				ControlFlowNode cfNode = new ControlFlowNode(index++, null, ControlFlowNodeType.Normal);
				cfNodes.Add(cfNode);
				astNodeToCfNode[node] = cfNode;
				cfNode.UserData = node;

				// Find all contained labels
				var labelList = node.GetSelfAndChildrenRecursive<ILLabel>(listLabels ?? (listLabels = new List<ILLabel>()));
				for (int j = 0; j < labelList.Count; j++)
					labelToCfNode[labelList[j]] = cfNode;
			}

			// Entry endge
			ControlFlowNode entryNode = labelToCfNode[entryLabel];
			ControlFlowEdge entryEdge = new ControlFlowEdge(entryPoint, entryNode, JumpType.Normal);
			entryPoint.Outgoing.Add(entryEdge);
			entryNode.Incoming.Add(entryEdge);

			// Create edges
			List<ILExpression> listExpressions = null;
			for (int i = 0; i < nodes.Count; i++) {
				var node = (ILBasicBlock)nodes[i];
				ControlFlowNode source = astNodeToCfNode[node];

				// Find all branches
				foreach(ILLabel target in node.GetSelfAndChildrenRecursive<ILExpression>(listExpressions ?? (listExpressions = new List<ILExpression>()), e => e.IsBranch()).SelectMany(e => e.GetBranchTargets())) {
					ControlFlowNode destination;
					// Labels which are out of out scope will not be in the collection
					// Insert self edge only if we are sure we are a loop
					if (labelToCfNode.TryGetValue(target, out destination) && (destination != source || target == node.Body.FirstOrDefault())) {
						ControlFlowEdge edge = new ControlFlowEdge(source, destination, JumpType.Normal);
						source.Outgoing.Add(edge);
						destination.Incoming.Add(edge);
					}
				}
			}

			return cached_ControlFlowGraph;
		}

		List<ILNode> FindLoops(HashSet<ControlFlowNode> scope, ControlFlowNode entryPoint, bool excludeEntryPoint)
		{
			List<ILNode> result = new List<ILNode>();

			// Do not modify entry data
			scope = new HashSet<ControlFlowNode>(scope);

			Queue<ControlFlowNode> agenda  = new Queue<ControlFlowNode>();
			agenda.Enqueue(entryPoint);
			while(agenda.Count > 0) {
				ControlFlowNode node = agenda.Dequeue();

				// If the node is a loop header
				if (scope.Contains(node)
				    && node.DominanceFrontier.Contains(node)
				    && (node != entryPoint || !excludeEntryPoint))
				{
					HashSet<ControlFlowNode> loopContents = FindLoopContent(scope, node);

					// If the first expression is a loop condition
					ILBasicBlock basicBlock = (ILBasicBlock)node.UserData;
					ILExpression condExpr;
					ILLabel trueLabel;
					ILLabel falseLabel;
					// It has to be just brtrue - any preceding code would introduce goto
					if(basicBlock.MatchSingleAndBr(ILCode.Brtrue, out trueLabel, out condExpr, out falseLabel))
					{
						ControlFlowNode trueTarget;
						labelToCfNode.TryGetValue(trueLabel, out trueTarget);
						ControlFlowNode falseTarget;
						labelToCfNode.TryGetValue(falseLabel, out falseTarget);

						// If one point inside the loop and the other outside
						if ((!loopContents.Contains(trueTarget) && loopContents.Contains(falseTarget)) ||
						    (loopContents.Contains(trueTarget) && !loopContents.Contains(falseTarget)) )
						{
							loopContents.RemoveOrThrow(node);
							scope.RemoveOrThrow(node);

							// If false means enter the loop
							if (loopContents.Contains(falseTarget) || falseTarget == node)
							{
								// Negate the condition
								condExpr = new ILExpression(ILCode.LogicNot, null, condExpr);
								ILLabel tmp = trueLabel;
								trueLabel = falseLabel;
								falseLabel = tmp;
							}

							ControlFlowNode postLoopTarget;
							labelToCfNode.TryGetValue(falseLabel, out postLoopTarget);
							if (postLoopTarget != null) {
								// Pull more nodes into the loop
								HashSet<ControlFlowNode> postLoopContents = FindDominatedNodes(scope, postLoopTarget);
								var pullIn = scope.Except(postLoopContents).Where(n => node.Dominates(n));
								loopContents.UnionWith(pullIn);
							}

							// Use loop to implement the brtrue
							var tail = basicBlock.Body.RemoveTail(ILCode.Brtrue, ILCode.Br);
							ILWhileLoop whileLoop;
							basicBlock.Body.Add(whileLoop = new ILWhileLoop() {
								Condition = condExpr,
								BodyBlock = new ILBlock(CodeBracesRangeFlags.LoopBraces) {
									EntryGoto = new ILExpression(ILCode.Br, trueLabel),
									Body = FindLoops(loopContents, node, false)
								}
							});
							if (context.CalculateILSpans) {
								whileLoop.ILSpans.AddRange(tail[0].ILSpans);  // no recursive add
								tail[1].AddSelfAndChildrenRecursiveILSpans(whileLoop.ILSpans);
							}
							basicBlock.Body.Add(new ILExpression(ILCode.Br, falseLabel));
							result.Add(basicBlock);

							scope.ExceptWith(loopContents);
						}
					}

					// Fallback method: while(true)
					if (scope.Contains(node)) {
						result.Add(new ILBasicBlock() {
							Body = new List<ILNode>(2) {
								new ILLabel() { Name = "Loop_" + (nextLabelIndex++).ToString() },
								new ILWhileLoop() {
									BodyBlock = new ILBlock(CodeBracesRangeFlags.LoopBraces) {
										EntryGoto = new ILExpression(ILCode.Br, (ILLabel)basicBlock.Body.First()),
										Body = FindLoops(loopContents, node, true)
									}
								},
							},
						});

						scope.ExceptWith(loopContents);
					}
				}

				// Using the dominator tree should ensure we find the the widest loop first
				for (int i = 0; i < node.DominatorTreeChildren.Count; i++)
					agenda.Enqueue(node.DominatorTreeChildren[i]);
			}

			// Add whatever is left
			foreach(var node in scope) {
				result.Add((ILNode)node.UserData);
			}
			scope.Clear();

			return result;
		}

		List<ILNode> FindConditions(HashSet<ControlFlowNode> scope, ControlFlowNode entryNode)
		{
			List<ILNode> result = new List<ILNode>();

			// Do not modify entry data
			scope = new HashSet<ControlFlowNode>(scope);

			Stack<ControlFlowNode> agenda  = new Stack<ControlFlowNode>();
			agenda.Push(entryNode);
			while(agenda.Count > 0) {
				ControlFlowNode node = agenda.Pop();

				// Find a block that represents a simple condition
				if (scope.Contains(node)) {

					ILBasicBlock block = (ILBasicBlock)node.UserData;

					{
						// Switch
						ILLabel[] caseLabels;
						ILExpression switchArg;
						ILLabel fallLabel;
						if (block.MatchLastAndBr(ILCode.Switch, out caseLabels, out switchArg, out fallLabel)) {

							// Replace the switch code with ILSwitch
							ILSwitch ilSwitch = new ILSwitch() { Condition = switchArg };
							var tail = block.Body.RemoveTail(ILCode.Switch, ILCode.Br);
							if (context.CalculateILSpans) {
								ilSwitch.ILSpans.AddRange(tail[0].ILSpans);   // no recursive add
								tail[1].AddSelfAndChildrenRecursiveILSpans(ilSwitch.ILSpans);
							}
							block.Body.Add(ilSwitch);
							block.Body.Add(new ILExpression(ILCode.Br, fallLabel));
							result.Add(block);

							// Remove the item so that it is not picked up as content
							scope.RemoveOrThrow(node);

							// Find the switch offset
							int addValue = 0;
							List<ILExpression> subArgs;
							if (ilSwitch.Condition.Match(ILCode.Sub, out subArgs) && subArgs[1].Match(ILCode.Ldc_I4, out addValue)) {
								var old = ilSwitch.Condition;
								ilSwitch.Condition = subArgs[0];
								if (context.CalculateILSpans) {
									ilSwitch.Condition.ILSpans.AddRange(old.ILSpans); // no recursive add
									for (int i = 1; i < subArgs.Count; i++)
										subArgs[i].AddSelfAndChildrenRecursiveILSpans(ilSwitch.Condition.ILSpans);
								}
							}

							// Pull in code of cases
							ControlFlowNode fallTarget = null;
							labelToCfNode.TryGetValue(fallLabel, out fallTarget);

							HashSet<ControlFlowNode> frontiers = new HashSet<ControlFlowNode>();
							if (fallTarget != null)
								frontiers.UnionWith(fallTarget.DominanceFrontier.Where(x => x != fallTarget));

							for (int i = 0; i < caseLabels.Length; i++) {
								labelToCfNode.TryGetValue(caseLabels[i], out var condTarget);
								if (condTarget != null)
									frontiers.UnionWith(condTarget.DominanceFrontier.Where(x => x != condTarget));
							}

							bool includedDefault = false;
							for (int i = 0; i < caseLabels.Length; i++) {
								ILLabel condLabel = caseLabels[i];

								// Find or create new case block
								ILSwitch.CaseBlock caseBlock = ilSwitch.CaseBlocks.FirstOrDefault(b => b.EntryGoto.Operand == condLabel);
								if (caseBlock == null) {
									caseBlock = new ILSwitch.CaseBlock() {
										EntryGoto = new ILExpression(ILCode.Br, condLabel)
									};
									ilSwitch.CaseBlocks.Add(caseBlock);
									if (!includedDefault && condLabel == fallLabel) {
										includedDefault = true;
										block.Body.RemoveTail(ILCode.Br);
									}
									else
										caseBlock.Values = new List<int>(caseLabels.Length);

									ControlFlowNode condTarget = null;
									labelToCfNode.TryGetValue(condLabel, out condTarget);
									if (condTarget != null && !frontiers.Contains(condTarget)) {
										HashSet<ControlFlowNode> content = FindDominatedNodes(scope, condTarget);
										scope.ExceptWith(content);
										caseBlock.Body.AddRange(FindConditions(content, condTarget));
										// Add explicit break which should not be used by default, but the goto removal might decide to use it
										caseBlock.Body.Add(new ILBasicBlock() {
											Body = new List<ILNode>(2) {
												new ILLabel() { Name = "SwitchBreak_" + (nextLabelIndex++).ToString() },
												new ILExpression(ILCode.LoopOrSwitchBreak, null)
											}
										});
									}
								}
								caseBlock.Values?.Add(i + addValue);
							}

							// Heuristis to determine if we want to use fallthough as default case
							if (!includedDefault && fallTarget != null && !frontiers.Contains(fallTarget)) {
								HashSet<ControlFlowNode> content = FindDominatedNodes(scope, fallTarget);
								if (content.Any()) {
									var caseBlock = new ILSwitch.CaseBlock() { EntryGoto = new ILExpression(ILCode.Br, fallLabel) };
									ilSwitch.CaseBlocks.Add(caseBlock);
									tail = block.Body.RemoveTail(ILCode.Br);
									if (context.CalculateILSpans)
										tail[0].AddSelfAndChildrenRecursiveILSpans(caseBlock.ILSpans);

									scope.ExceptWith(content);
									caseBlock.Body.AddRange(FindConditions(content, fallTarget));
									// Add explicit break which should not be used by default, but the goto removal might decide to use it
									caseBlock.Body.Add(new ILBasicBlock() {
										Body = new List<ILNode>(2) {
											new ILLabel() { Name = "SwitchBreak_" + (nextLabelIndex++).ToString() },
											new ILExpression(ILCode.LoopOrSwitchBreak, null)
										}
									});
								}
							}
						}

						// Two-way branch
						ILExpression condExpr;
						ILLabel trueLabel;
						ILLabel falseLabel;
						if(block.MatchLastAndBr(ILCode.Brtrue, out trueLabel, out condExpr, out falseLabel)) {

							// Swap bodies since that seems to be the usual C# order
							ILLabel temp = trueLabel;
							trueLabel = falseLabel;
							falseLabel = temp;
							condExpr = new ILExpression(ILCode.LogicNot, null, condExpr);

							// Convert the brtrue to ILCondition
							ILCondition ilCond = new ILCondition() {
								Condition  = condExpr,
								TrueBlock  = new ILBlock(CodeBracesRangeFlags.ConditionalBraces) { EntryGoto = new ILExpression(ILCode.Br, trueLabel) },
								FalseBlock = new ILBlock(CodeBracesRangeFlags.ConditionalBraces) { EntryGoto = new ILExpression(ILCode.Br, falseLabel) }
							};
							var tail = block.Body.RemoveTail(ILCode.Brtrue, ILCode.Br);
							if (context.CalculateILSpans) {
								condExpr.ILSpans.AddRange(tail[0].ILSpans);   // no recursive add
								tail[1].AddSelfAndChildrenRecursiveILSpans(ilCond.FalseBlock.ILSpans);
							}
							block.Body.Add(ilCond);
							result.Add(block);

							// Remove the item immediately so that it is not picked up as content
							scope.RemoveOrThrow(node);

							ControlFlowNode trueTarget = null;
							labelToCfNode.TryGetValue(trueLabel, out trueTarget);
							ControlFlowNode falseTarget = null;
							labelToCfNode.TryGetValue(falseLabel, out falseTarget);

							// Pull in the conditional code
							if (trueTarget != null && HasSingleEdgeEnteringBlock(trueTarget)) {
								HashSet<ControlFlowNode> content = FindDominatedNodes(scope, trueTarget);
								scope.ExceptWith(content);
								ilCond.TrueBlock.Body.AddRange(FindConditions(content, trueTarget));
							}
							if (falseTarget != null && HasSingleEdgeEnteringBlock(falseTarget)) {
								HashSet<ControlFlowNode> content = FindDominatedNodes(scope, falseTarget);
								scope.ExceptWith(content);
								ilCond.FalseBlock.Body.AddRange(FindConditions(content, falseTarget));
							}
						}
					}

					// Add the node now so that we have good ordering
					if (scope.Contains(node)) {
						result.Add((ILNode)node.UserData);
						scope.Remove(node);
					}
				}

				// depth-first traversal of dominator tree
				for (int i = node.DominatorTreeChildren.Count - 1; i >= 0; i--) {
					agenda.Push(node.DominatorTreeChildren[i]);
				}
			}

			// Add whatever is left
			foreach(var node in scope) {
				result.Add((ILNode)node.UserData);
			}

			return result;
		}

		static bool HasSingleEdgeEnteringBlock(ControlFlowNode node)
		{
			return node.Incoming.Count(edge => !node.Dominates(edge.Source)) == 1;
		}

		static HashSet<ControlFlowNode> FindDominatedNodes(HashSet<ControlFlowNode> scope, ControlFlowNode head)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			HashSet<ControlFlowNode> agenda = new HashSet<ControlFlowNode>();
			HashSet<ControlFlowNode> result = new HashSet<ControlFlowNode>();
			agenda.Add(head);

			while(agenda.Count > 0) {
				ControlFlowNode addNode = agenda.First();
				agenda.Remove(addNode);

				if (scope.Contains(addNode) && head.Dominates(addNode) && result.Add(addNode)) {
					for (int i = 0; i < addNode.Outgoing.Count; i++) {
						agenda.Add(addNode.Outgoing[i].Target);
					}
				}
			}

			return result;
		}

		static HashSet<ControlFlowNode> FindLoopContent(HashSet<ControlFlowNode> scope, ControlFlowNode head)
		{
			HashSet<ControlFlowNode> agenda = new HashSet<ControlFlowNode>();
			for (int i = 0; i < head.Incoming.Count; i++) {
				var p = head.Incoming[i].Source;
				if (head.Dominates(p))
					agenda.Add(p);
			}
			HashSet<ControlFlowNode> result = new HashSet<ControlFlowNode>();

			while(agenda.Count > 0) {
				ControlFlowNode addNode = agenda.First();
				agenda.Remove(addNode);

				if (scope.Contains(addNode) && head.Dominates(addNode) && result.Add(addNode)) {
					for (int i = 0; i < addNode.Incoming.Count; i++)
						agenda.Add(addNode.Incoming[i].Source);
				}
			}
			if (scope.Contains(head))
				result.Add(head);

			return result;
		}
	}
}
