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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Documents.Tabs.Dialogs {
	[Export(typeof(ITextClassifierProvider))]
	[ContentType(ContentTypes.GacDialog)]
	sealed class OpenFromGACTextClassifierProvider : ITextClassifierProvider {
		readonly IClassificationType gacMatchHighlightClassificationType;

		[ImportingConstructor]
		OpenFromGACTextClassifierProvider(IThemeClassificationTypeService themeClassificationTypeService) => gacMatchHighlightClassificationType = themeClassificationTypeService.GetClassificationType(TextColor.GacMatchHighlight);

		public ITextClassifier? Create(IContentType contentType) => new OpenFromGACTextClassifier(gacMatchHighlightClassificationType);
	}

	sealed class OpenFromGACTextClassifier : ITextClassifier {
		readonly IClassificationType gacMatchHighlightClassificationType;

		public OpenFromGACTextClassifier(IClassificationType gacMatchHighlightClassificationType) => this.gacMatchHighlightClassificationType = gacMatchHighlightClassificationType ?? throw new ArgumentNullException(nameof(gacMatchHighlightClassificationType));

		public IEnumerable<TextClassificationTag> GetTags(TextClassifierContext context) {
			var gacContext = context as OpenFromGACTextClassifierContext;
			if (gacContext is null)
				yield break;
			if (gacContext.Tag != PredefinedTextClassifierTags.GacDialogName && gacContext.Tag != PredefinedTextClassifierTags.GacDialogVersion)
				yield break;
			foreach (var part in gacContext.SearchText.Split(seps, StringSplitOptions.RemoveEmptyEntries)) {
				int index = gacContext.Text.IndexOf(part, StringComparison.CurrentCultureIgnoreCase);
				if (index >= 0)
					yield return new TextClassificationTag(new Span(index, part.Length), gacMatchHighlightClassificationType);
			}
		}
		static readonly char[] seps = new char[] { ' ' };
	}
}
