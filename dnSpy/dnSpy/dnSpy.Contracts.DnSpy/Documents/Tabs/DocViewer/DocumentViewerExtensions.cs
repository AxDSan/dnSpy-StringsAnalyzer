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
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Documents.Tabs.DocViewer {
	/// <summary>
	/// Extensions
	/// </summary>
	public static class DocumentViewerExtensions {
		internal static object DocumentViewerTextBufferKey = new object();

		/// <summary>
		/// Returns a <see cref="IDocumentViewer"/> or null
		/// </summary>
		/// <param name="textBuffer">Text buffer</param>
		/// <returns></returns>
		public static IDocumentViewer? TryGetDocumentViewer(this ITextBuffer textBuffer) {
			if (textBuffer is null)
				throw new ArgumentNullException(nameof(textBuffer));
			textBuffer.Properties.TryGetProperty(DocumentViewerTextBufferKey, out IDocumentViewer documentViewer);
			return documentViewer;
		}
	}
}
