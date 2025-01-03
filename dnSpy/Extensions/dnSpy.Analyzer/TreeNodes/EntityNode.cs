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

using dnlib.DotNet;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Analyzer.TreeNodes {
	abstract class EntityNode : AnalyzerTreeNodeData, IMDTokenNode {
		public abstract IMemberRef? Member { get; }
		public abstract IMDTokenProvider? Reference { get; }
		public SourceRef? SourceRef { get; set; }

		public override bool Activate() {
			Context.AnalyzerService.OnActivated(this);
			return true;
		}

		public override bool HandleAssemblyListChanged(IDsDocument[] removedAssemblies, IDsDocument[] addedAssemblies) {
			foreach (var asm in removedAssemblies) {
				if (Member?.Module == asm.ModuleDef)
					return false; // remove this node
			}
			HandleAssemblyListChanged(TreeNode, removedAssemblies, addedAssemblies);
			return true;
		}

		public override bool HandleModelUpdated(IDsDocument[] documents) {
			if (Member?.Module is null)
				return false; // remove this node
			if ((Member is IField || Member is IMethod || Member is PropertyDef || Member is EventDef) &&
				Member.DeclaringType is null)
				return false;
			HandleModelUpdated(TreeNode, documents);
			return true;
		}
	}
}
