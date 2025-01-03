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
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Properties;

namespace dnSpy.Documents.TreeView {
	sealed class ReferencesFolderNodeImpl : ReferencesFolderNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.REFERENCES_FOLDER_NODE_GUID);
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.Reference;
		public override NodePathName NodePathName => new NodePathName(Guid);
		public override void Initialize() => TreeNode.LazyLoading = true;
		public override ITreeNodeGroup? TreeNodeGroup { get; }

		readonly ModuleDocumentNode moduleNode;

		public ReferencesFolderNodeImpl(ITreeNodeGroup treeNodeGroup, ModuleDocumentNode moduleNode) {
			Debug2.Assert(moduleNode.Document.ModuleDef is not null);
			TreeNodeGroup = treeNodeGroup;
			this.moduleNode = moduleNode;
		}

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			output.Write(BoxedTextColor.Text, dnSpy_Resources.ReferencesFolder);
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				output.WriteLine();
				WriteFilename(output);
			}
		}

		public override IEnumerable<TreeNodeData> CreateChildren() {
			Debug2.Assert(moduleNode.Document.ModuleDef is not null);
			foreach (var asmRef in moduleNode.Document.ModuleDef.GetAssemblyRefs())
				yield return new AssemblyReferenceNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.AssemblyRefTreeNodeGroupReferences), moduleNode.Document.ModuleDef, asmRef);
			foreach (var modRef in moduleNode.Document.ModuleDef.GetModuleRefs())
				yield return new ModuleReferenceNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.ModuleRefTreeNodeGroupReferences), modRef);
		}

		public override AssemblyReferenceNode Create(AssemblyRef asmRef) => Context.DocumentTreeView.Create(asmRef, moduleNode.Document.ModuleDef!);
		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) =>
			filter.GetResult(this).FilterType;
	}
}
