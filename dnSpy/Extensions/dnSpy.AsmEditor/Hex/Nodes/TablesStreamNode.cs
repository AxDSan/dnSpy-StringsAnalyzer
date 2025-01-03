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
using System.Linq;
using dnSpy.AsmEditor.Hex.PE;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class TablesStreamNode : HexNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.TBLSSTREAM_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid);
		public override object VMObject => tablesStreamVM;

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return tablesStreamVM; }
		}

		protected override ImageReference IconReference => DsImages.Metadata;

		readonly TablesStreamVM tablesStreamVM;

		public TablesStreamNode(TablesStreamVM tablesStream)
			: base(tablesStream.Span) {
			tablesStreamVM = tablesStream;

			newChildren = new List<TreeNodeData>();
			foreach (var mdTable in tablesStream.MetadataTables) {
				if (mdTable is not null)
					newChildren.Add(new MetadataTableNode(mdTable));
			}
		}
		List<TreeNodeData>? newChildren;

		public override IEnumerable<TreeNodeData> CreateChildren() {
			foreach (var c in newChildren!)
				yield return c;
			newChildren = null;
		}

		public override void OnBufferChanged(NormalizedHexChangeCollection changes) {
			base.OnBufferChanged(changes);

			foreach (HexNode node in TreeNode.DataChildren)
				node.OnBufferChanged(changes);
		}

		protected override void WriteCore(ITextColorWriter output, DocumentNodeWriteOptions options) =>
			output.Write(BoxedTextColor.HexTablesStream, dnSpy_AsmEditor_Resources.HexNode_TablesStream);

		public MetadataTableRecordNode? FindTokenNode(uint token) {
			var mdTblNode = (MetadataTableNode?)TreeNode.DataChildren.FirstOrDefault(a => ((MetadataTableNode)a).TableInfo.Table == (Table)(token >> 24));
			return mdTblNode?.FindTokenNode(token);
		}
	}
}
