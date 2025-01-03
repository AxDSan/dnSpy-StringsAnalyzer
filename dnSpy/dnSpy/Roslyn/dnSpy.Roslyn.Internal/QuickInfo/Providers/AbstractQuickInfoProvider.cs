// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace dnSpy.Roslyn.Internal.QuickInfo {
	internal abstract partial class AbstractQuickInfoProvider : IQuickInfoProvider {
		protected AbstractQuickInfoProvider() { }

		public async Task<QuickInfoItem> GetQuickInfoAsync(QuickInfoContext context) {
			var cancellationToken = context.CancellationToken;
			var tree = await context.Document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
			var tokens = await GetTokensAsync(tree, context.Position, context.CancellationToken).ConfigureAwait(false);

			foreach (var token in tokens) {
				var info = await GetQuickInfoItemAsync(context, token).ConfigureAwait(false);
				if (info != null)
					return info;
			}

			return null;
		}

		protected async Task<ImmutableArray<SyntaxToken>> GetTokensAsync(SyntaxTree tree, int position,
			CancellationToken cancellationToken) {
			var result = new List<SyntaxToken>();
			var token = await tree.GetTouchingTokenAsync(position, cancellationToken, findInsideTrivia: true)
								  .ConfigureAwait(false);
			if (token != default) {
				result.Add(token);

				if (ShouldCheckPreviousToken(token)) {
					token = token.GetPreviousToken();
					if (token != default && token.Span.IntersectsWith(position))
						result.Add(token);
				}
			}

			return result.ToImmutableArray();
		}


		protected virtual bool ShouldCheckPreviousToken(SyntaxToken token) => true;

		private async Task<QuickInfoItem> GetQuickInfoItemAsync(QuickInfoContext context, SyntaxToken token) {
			if (token != default &&
				token.Span.IntersectsWith(context.Position)) {
				var deferredContent = await BuildContentAsync(context, token).ConfigureAwait(false);
				if (deferredContent != null) {
					return new QuickInfoItem(token.Span, deferredContent);
				}
			}

			return null;
		}

		protected abstract Task<QuickInfoContent> BuildContentAsync(QuickInfoContext context, SyntaxToken token);

		protected QuickInfoContent CreateQuickInfoDisplayDeferredContent(ISymbol symbol,
			bool showWarningGlyph,
			bool showSymbolGlyph,
			IList<TaggedText> mainDescription,
			ImmutableArray<TaggedText> documentation,
			IList<TaggedText> typeParameterMap,
			IList<TaggedText> anonymousTypes,
			IList<TaggedText> usageText,
			IList<TaggedText> exceptionText) {
			return new InformationQuickInfoContent(
				symbolGlyph: showSymbolGlyph ? CreateGlyphDeferredContent(symbol) : (Glyph?)null,
				warningGlyph: showWarningGlyph ? CreateWarningGlyph() : (Glyph?)null,
				mainDescription: CreateClassifiableDeferredContent(mainDescription),
				documentation: documentation,
				typeParameterMap: CreateClassifiableDeferredContent(typeParameterMap),
				anonymousTypes: CreateClassifiableDeferredContent(anonymousTypes),
				usageText: CreateClassifiableDeferredContent(usageText),
				exceptionText: CreateClassifiableDeferredContent(exceptionText));
		}

		private Glyph CreateWarningGlyph() {
			return Glyph.CompletionWarning;
		}

		protected QuickInfoContent CreateQuickInfoDisplayDeferredContent(Glyph glyph,
			IList<TaggedText> mainDescription,
			ImmutableArray<TaggedText> documentation,
			IList<TaggedText> typeParameterMap,
			IList<TaggedText> anonymousTypes,
			IList<TaggedText> usageText,
			IList<TaggedText> exceptionText) {
			return new InformationQuickInfoContent(
				symbolGlyph: glyph,
				warningGlyph: null,
				mainDescription: CreateClassifiableDeferredContent(mainDescription),
				documentation: documentation,
				typeParameterMap: CreateClassifiableDeferredContent(typeParameterMap),
				anonymousTypes: CreateClassifiableDeferredContent(anonymousTypes),
				usageText: CreateClassifiableDeferredContent(usageText),
				exceptionText: CreateClassifiableDeferredContent(exceptionText));
		}

		protected Glyph CreateGlyphDeferredContent(ISymbol symbol) {
			return symbol.GetGlyph().ToOurGlyph();
		}

		protected ImmutableArray<TaggedText> CreateClassifiableDeferredContent(IEnumerable<TaggedText> content) {
			return content.AsImmutable();
		}

		protected ImmutableArray<TaggedText> CreateDocumentationCommentDeferredContent(string documentationComment) {
			return string.IsNullOrEmpty(documentationComment)
				? ImmutableArray<TaggedText>.Empty
				: ImmutableArray.Create(new TaggedText(TextTags.Text, documentationComment));
		}

		protected QuickInfoContent CreateProjectionBufferDeferredContent(TextSpan span) {
			return new CodeSpanQuickInfoContent(span);
		}
	}
}
