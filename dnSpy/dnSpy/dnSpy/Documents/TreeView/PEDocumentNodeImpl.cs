/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Decompiler;

namespace dnSpy.Documents.TreeView {
	sealed class PEDocumentNodeImpl : PEDocumentNode {
		public PEDocumentNodeImpl(IDsDocument document)
			: base(document) => Debug2.Assert(document.PEImage is not null && document.ModuleDef is null);

		public override Guid Guid => new Guid(DocumentTreeViewConstants.PEDOCUMENT_NODE_GUID);
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => dnImgMgr.GetImageReference(Document.PEImage!);
		public override void Initialize() => TreeNode.LazyLoading = true;

		public override IEnumerable<TreeNodeData> CreateChildren() {
			foreach (var document in Document.Children)
				yield return Context.DocumentTreeView.CreateNode(this, document);
		}

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			Debug2.Assert(Document.PEImage is not null);
			if ((options & DocumentNodeWriteOptions.ToolTip) == 0)
				new NodeFormatter().Write(output, decompiler, Document);
			else {
				output.Write(BoxedTextColor.Text, TargetFrameworkUtils.GetArchString(Document.PEImage.ImageNTHeaders.FileHeader.Machine));

				output.WriteLine();
				output.WriteFilename(Document.Filename);
			}
		}

		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) =>
			filter.GetResult(Document).FilterType;
	}
}
