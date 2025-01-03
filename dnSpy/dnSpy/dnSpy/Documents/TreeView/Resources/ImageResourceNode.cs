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
using System.Windows.Media;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnlib.IO;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Properties;

namespace dnSpy.Documents.TreeView.Resources {
	[ExportResourceNodeProvider(Order = DocumentTreeViewConstants.ORDER_RSRCPROVIDER_IMAGE_RESOURCE_NODE)]
	sealed class ImageResourceNodeProvider : IResourceNodeProvider {
		public DocumentTreeNodeData? Create(ModuleDef module, Resource resource, ITreeNodeGroup treeNodeGroup) {
			var er = resource as EmbeddedResource;
			if (er is null)
				return null;

			var reader = er.CreateReader();
			if (!CouldBeImage(er.Name, ref reader))
				return null;

			return new ImageResourceNodeImpl(treeNodeGroup, er);
		}

		public DocumentTreeNodeData? Create(ModuleDef module, ResourceElement resourceElement, ITreeNodeGroup treeNodeGroup) {
			if (resourceElement.ResourceData.Code != ResourceTypeCode.ByteArray && resourceElement.ResourceData.Code != ResourceTypeCode.Stream)
				return null;

			var data = (byte[])((BuiltInResourceData)resourceElement.ResourceData).Data;
			var reader = ByteArrayDataReaderFactory.CreateReader(data);
			if (!CouldBeImage(resourceElement.Name, ref reader))
				return null;

			return new ImageResourceElementNodeImpl(treeNodeGroup, resourceElement);
		}

		static bool CouldBeImage(string name, ref DataReader reader) => CouldBeImage(name) || CouldBeImage(ref reader);

		static readonly string[] fileExtensions = {
			".png",
			".gif",
			".bmp", ".dib",
			".jpg", ".jpeg", ".jpe", ".jif", ".jfif", ".jfi",
			".ico", ".cur",
		};
		static bool CouldBeImage(string name) {
			foreach (var ext in fileExtensions) {
				if (name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}

		static bool CouldBeImage(ref DataReader reader) {
			reader.Position = 0;
			if (reader.Length < 0x16)
				return false;

			uint d = reader.ReadUInt32();
			if (unchecked((ushort)d) == 0x4D42) {
				// Possible BMP image
				reader.Position -= 2;
				uint size = reader.ReadUInt32();
				if (size > reader.Length)
					return false;
				reader.Position += 4;
				uint offs = reader.ReadUInt32();
				return offs < size;
			}

			// Check if GIF87a or GIF89a
			if (d == 0x38464947)
				return (d = reader.ReadUInt16()) == 0x6139 || d == 0x6137;

			// Check if PNG
			if (d == 0x474E5089)
				return reader.ReadUInt32() == 0x0A1A0A0D;

			// Check if ICO or CUR
			if (d == 0x00010000 || d == 0x00020000) {
				int num = reader.ReadUInt16();
				if (num <= 0)
					return false;

				reader.Position += 8;
				uint size = reader.ReadUInt32();
				uint offs = reader.ReadUInt32();
				uint end = unchecked(offs + size);
				return offs <= end && end <= reader.Length;
			}

			return false;
		}
	}

	sealed class ImageResourceNodeImpl : ImageResourceNode {
		readonly ImageSource imageSource;
		readonly byte[] imageData;

		public override Guid Guid => new Guid(DocumentTreeViewConstants.IMAGE_RESOURCE_NODE_GUID);
		protected override ImageReference GetIcon() => DsImages.Image;

		public ImageResourceNodeImpl(ITreeNodeGroup treeNodeGroup, EmbeddedResource resource)
			: base(treeNodeGroup, resource) {
			imageData = resource.CreateReader().ToArray();
			imageSource = ImageResourceUtilities.CreateImageSource(imageData);
		}

		public override void WriteShort(IDecompilerOutput output, IDecompiler decompiler, bool showOffset) {
			var documentViewerOutput = output as IDocumentViewerOutput;
			if (documentViewerOutput is not null) {
				documentViewerOutput.AddUIElement(() => {
					return new System.Windows.Controls.Image {
						Source = imageSource,
					};
				});
			}

			base.WriteShort(output, decompiler, showOffset);
			if (documentViewerOutput is not null) {
				documentViewerOutput.AddButton(dnSpy_Resources.SaveResourceButton, () => Save());
				documentViewerOutput.WriteLine();
				documentViewerOutput.WriteLine();
			}
		}

		protected override IEnumerable<ResourceData> GetDeserializedData() {
			var id = imageData;
			yield return new ResourceData(Resource.Name, token => new MemoryStream(id));
		}
	}

	sealed class ImageResourceElementNodeImpl : ImageResourceElementNode {
		ImageSource? imageSource;
		byte[]? imageData;

		public override Guid Guid => new Guid(DocumentTreeViewConstants.IMAGE_RESOURCE_ELEMENT_NODE_GUID);
		protected override ImageReference GetIcon() => DsImages.Image;

		public ImageResourceElementNodeImpl(ITreeNodeGroup treeNodeGroup, ResourceElement resourceElement)
			: base(treeNodeGroup, resourceElement) => InitializeImageData();

		void InitializeImageData() {
			imageData = (byte[])((BuiltInResourceData)ResourceElement.ResourceData).Data;
			imageSource = ImageResourceUtilities.CreateImageSource(imageData);
		}

		public override void WriteShort(IDecompilerOutput output, IDecompiler decompiler, bool showOffset) {
			if (output is IDocumentViewerOutput documentViewerOutput) {
				decompiler.WriteCommentBegin(output, true);
				output.WriteOffsetComment(this, showOffset);
				documentViewerOutput.AddUIElement(() => {
					return new System.Windows.Controls.Image {
						Source = imageSource,
					};
				});
				output.Write(" = ", BoxedTextColor.Comment);
				const string LTR = "\u200E";
				output.Write(NameUtilities.CleanName(Name) + LTR, this, DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Definition, BoxedTextColor.Comment);
				decompiler.WriteCommentEnd(output, true);
				output.WriteLine();
				return;
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

			try {
				ImageResourceUtilities.CreateImageSource((byte[])((BuiltInResourceData)newResElem.ResourceData).Data);
			}
			catch {
				return dnSpy_Resources.NewDataIsNotAnImage;
			}

			return null;
		}

		public override void UpdateData(ResourceElement newResElem) {
			base.UpdateData(newResElem);
			InitializeImageData();
		}
	}
}
