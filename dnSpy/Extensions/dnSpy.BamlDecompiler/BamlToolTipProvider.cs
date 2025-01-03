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

using dnSpy.Contracts.Documents.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Text;

namespace dnSpy.BamlDecompiler {
	[ExportDocumentViewerToolTipProvider]
	sealed class BamlDocumentViewerToolTipProvider : IDocumentViewerToolTipProvider {
		public object Create(IDocumentViewerToolTipProviderContext context, object @ref) {
			if (@ref is BamlToolTipReference bref) {
				var provider = context.Create();
				provider.Output.Write(BoxedTextColor.Text, bref.String);
				return provider.Create();
			}

			return null;
		}
	}

	// Don't use a string since it should only show tooltips if it's from the baml disassembler
	sealed class BamlToolTipReference {
		public static object Create(string s) => string.IsNullOrEmpty(s) ? null : new BamlToolTipReference(s);
		public string String { get; }

		BamlToolTipReference(string s) => String = s;
	}
}
