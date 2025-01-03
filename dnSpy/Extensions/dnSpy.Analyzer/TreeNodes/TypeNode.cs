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
using System.Collections.Generic;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class TypeNode : EntityNode {
		readonly TypeDef analyzedType;

		public TypeNode(TypeDef analyzedType) => this.analyzedType = analyzedType ?? throw new ArgumentNullException(nameof(analyzedType));

		public override void Initialize() => TreeNode.LazyLoading = true;
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => dnImgMgr.GetImageReference(analyzedType);
		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			new NodeFormatter().Write(output, decompiler, analyzedType, Context.ShowToken);

		public override IEnumerable<TreeNodeData> CreateChildren() {
			if (AttributeAppliedToNode.CanShow(analyzedType))
				yield return new AttributeAppliedToNode(analyzedType);

			if (TypeInstantiationsNode.CanShow(analyzedType))
				yield return new TypeInstantiationsNode(analyzedType);

			if (TypeUsedByNode.CanShow(analyzedType))
				yield return new TypeUsedByNode(analyzedType);

			if (TypeExposedByNode.CanShow(analyzedType))
				yield return new TypeExposedByNode(analyzedType);

			if (TypeExtensionMethodsNode.CanShow(analyzedType))
				yield return new TypeExtensionMethodsNode(analyzedType);
		}

		public override IMemberRef? Member => analyzedType;
		public override IMDTokenProvider? Reference => analyzedType;
	}
}
