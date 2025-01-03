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
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Documents.TreeView.Resources {
	[ExportResourceNodeProvider(Order = DocumentTreeViewConstants.ORDER_RSRCPROVIDER_SERIALIZED_IMAGE_LIST_STREAMER_RESOURCE_ELEMENT_NODE)]
	sealed class SerializedImageListStreamerResourceElementNodeProvider : IResourceNodeProvider {
		public DocumentTreeNodeData? Create(ModuleDef module, Resource resource, ITreeNodeGroup treeNodeGroup) => null;

		public DocumentTreeNodeData? Create(ModuleDef module, ResourceElement resourceElement, ITreeNodeGroup treeNodeGroup) {
			var serializedData = resourceElement.ResourceData as BinaryResourceData;
			if (serializedData is null)
				return null;

			if (SerializedImageListStreamerUtilities.GetImageData(module, serializedData.TypeName, serializedData.Data, out var imageData))
				return new SerializedImageListStreamerResourceElementNodeImpl(treeNodeGroup, resourceElement, imageData);

			return null;
		}
	}

	sealed class SerializedImageListStreamerResourceElementNodeImpl : SerializedImageListStreamerResourceElementNode {
		ImageListOptions? imageListOptions;
		byte[]? imageData;

		public override Guid Guid => new Guid(DocumentTreeViewConstants.SERIALIZED_IMAGE_LIST_STREAMER_RESOURCE_ELEMENT_NODE_GUID);
		public override ImageListOptions ImageListOptions => new ImageListOptions(imageListOptions!) { Name = Name };
		protected override ImageReference GetIcon() => DsImages.Image;

		public SerializedImageListStreamerResourceElementNodeImpl(ITreeNodeGroup treeNodeGroup, ResourceElement resourceElement, byte[] imageData)
			: base(treeNodeGroup, resourceElement) => InitializeImageData(imageData);

		void InitializeImageData(byte[] imageData) {
			imageListOptions = SerializedImageListStreamerUtilities.ReadImageData(imageData);
			this.imageData = imageData;
		}

		public override void WriteShort(IDecompilerOutput output, IDecompiler decompiler, bool showOffset) {
			Debug2.Assert(imageListOptions is not null);
			if (output is IDocumentViewerOutput documentViewerOutput) {
				for (int i = 0; i < imageListOptions.ImageSources.Count; i++) {
					if (i > 0)
						output.Write(" ", BoxedTextColor.Text);
					var imageSource = imageListOptions.ImageSources[i];
					documentViewerOutput.AddUIElement(() => {
						return new System.Windows.Controls.Image {
							Source = imageSource,
						};
					});
				}
			}

			base.WriteShort(output, decompiler, showOffset);
		}

		protected override IEnumerable<ResourceData> GetDeserializedData() {
			var id = imageData;
			Debug2.Assert(id is not null);
			yield return new ResourceData(ResourceElement.Name, token => new MemoryStream(id));
		}

		public override string? CheckCanUpdateData(ResourceElement newResElem) {
			var res = base.CheckCanUpdateData(newResElem);
			if (!string.IsNullOrEmpty(res))
				return res;
			return SerializedImageListStreamerUtilities.CheckCanUpdateData(this.GetModule(), newResElem);
		}

		public override void UpdateData(ResourceElement newResElem) {
			base.UpdateData(newResElem);

			var binData = (BinaryResourceData)newResElem.ResourceData;
			SerializedImageListStreamerUtilities.GetImageData(this.GetModule(), binData.TypeName, binData.Data, out var imageData);
			Debug2.Assert(imageData is not null);
			InitializeImageData(imageData);
		}
	}
}
