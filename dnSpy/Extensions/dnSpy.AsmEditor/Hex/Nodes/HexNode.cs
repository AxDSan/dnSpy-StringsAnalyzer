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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnSpy.AsmEditor.Hex.PE;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;

namespace dnSpy.AsmEditor.Hex.Nodes {
	abstract class HexNode : DocumentTreeNodeData, IDecompileSelf {
		protected abstract IEnumerable<HexVM> HexVMs { get; }
		public abstract object VMObject { get; }
		public virtual bool IsVirtualizingCollectionVM => false;
		public HexSpan Span { get; }
		protected sealed override ImageReference GetIcon(IDotNetImageService dnImgMgr) => IconReference;
		protected abstract ImageReference IconReference { get; }

		protected virtual IEnumerable<HexSpan> Spans {
			get { yield return Span; }
		}

		protected HexNode(HexSpan span) => Span = span;

		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) => filter.GetResultOther(this).FilterType;

		public bool Decompile(IDecompileNodeContext context) {
			context.ContentTypeString = context.Decompiler.ContentTypeString;
			context.Decompiler.WriteCommentLine(context.Output, $"{Span.Start.ToUInt64():X8} - {Span.End.ToUInt64() - 1:X8} {ToString()}");
			DecompileFields(context.Decompiler, context.Output);
			(context.Output as IDocumentViewerOutput)?.DisableCaching();
			return true;
		}

		protected virtual void DecompileFields(IDecompiler decompiler, IDecompilerOutput output) {
			foreach (var vm in HexVMs) {
				decompiler.WriteCommentLine(output, string.Empty);
				decompiler.WriteCommentLine(output, $"{vm.Name}:");
				foreach (var field in vm.HexFields)
					decompiler.WriteCommentLine(output, $"{field.Span.Start.ToUInt64():X8} - {field.Span.End.ToUInt64() - 1:X8} {field.FormattedValue} = {field.Name}");
			}
		}

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			WriteCore(output, options);
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				output.WriteLine();
				WriteFilename(output);
			}
		}

		protected abstract void WriteCore(ITextColorWriter output, DocumentNodeWriteOptions options);

		public virtual void OnBufferChanged(NormalizedHexChangeCollection changes) {
			if (!changes.OverlapsWith(Span))
				return;

			foreach (var vm in HexVMs)
				vm.OnBufferChanged(changes);
		}

		public HexNode? FindNode(HexVM structure, HexField field) {
			Debug.Assert(!(structure is MetadataTableRecordVM), "Use " + nameof(PENode) + "'s method instead");
			bool found = false;
			foreach (var span in Spans) {
				if (span.Contains(field.Span)) {
					found = true;
					break;
				}
			}
			if (!found)
				return null;

			foreach (var vm in HexVMs) {
				foreach (var f in vm.HexFields) {
					if (f == field)
						return this;
				}
			}

			TreeNode.EnsureChildrenLoaded();
			foreach (var child in TreeNode.DataChildren.OfType<HexNode>()) {
				var node = child.FindNode(structure, field);
				if (node is not null)
					return node;
			}

			return null;
		}
	}
}
