// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace dnSpy.Roslyn.Internal.QuickInfo {
	internal class QuickInfoItem {
		public TextSpan TextSpan { get; }
		public QuickInfoContent Content { get; }

		public QuickInfoItem(TextSpan textSpan, QuickInfoContent content) {
			this.TextSpan = textSpan;
			this.Content = content;
		}
	}
}
