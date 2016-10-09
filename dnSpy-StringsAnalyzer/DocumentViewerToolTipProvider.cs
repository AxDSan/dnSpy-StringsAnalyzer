﻿using dnSpy.Contracts.Documents.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Text;

namespace Plugin.StringAnalyzer {
	// This reference is added to the "decompiled" code by ModuleChildNode.Decompile()
	sealed class StringInfoReference {
		public string Message { get; }

		public StringInfoReference(string msg) {
			this.Message = msg;
		}
	}

	// Called by dnSpy to create a tooltip when hovering over a reference in the text editor
	[ExportDocumentViewerToolTipProvider]
	sealed class DocumentViewerToolTipProvider : IDocumentViewerToolTipProvider {
		public object Create(IDocumentViewerToolTipProviderContext context, object @ref) {
			// This reference is added to the "decompiled" code by ModuleChildNode.Decompile()
			var sref = @ref as StringInfoReference;
			if (sref != null) {
				var provider = context.Create();
				provider.Output.Write(BoxedTextColor.String, sref.Message);
				return provider.Create();
			}

			return null;
		}
	}
}
