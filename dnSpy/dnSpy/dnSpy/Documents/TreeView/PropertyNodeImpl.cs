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
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Documents.TreeView {
	sealed class PropertyNodeImpl : PropertyNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.PROPERTY_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid, PropertyDef.FullName);
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => dnImgMgr.GetImageReference(PropertyDef);
		public override ITreeNodeGroup? TreeNodeGroup { get; }

		public PropertyNodeImpl(ITreeNodeGroup treeNodeGroup, PropertyDef property)
			: base(property) => TreeNodeGroup = treeNodeGroup;

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				WriteMemberRef(output, decompiler, PropertyDef);
				output.WriteLine();
				WriteFilename(output);
			}
			else
				new NodeFormatter().Write(output, decompiler, PropertyDef, GetShowToken(options), null);
		}

		public override IEnumerable<TreeNodeData> CreateChildren() {
			foreach (var m in PropertyDef.GetMethods)
				yield return new MethodNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.MethodTreeNodeGroupProperty), m);
			foreach (var m in PropertyDef.SetMethods)
				yield return new MethodNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.MethodTreeNodeGroupProperty), m);
			foreach (var m in PropertyDef.OtherMethods)
				yield return new MethodNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.MethodTreeNodeGroupProperty), m);
		}

		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) {
			var res = filter.GetResult(PropertyDef);
			if (res.FilterType != FilterType.Default)
				return res.FilterType;
			if (Context.Decompiler.ShowMember(PropertyDef))
				return FilterType.Visible;
			return FilterType.Hide;
		}
	}
}
