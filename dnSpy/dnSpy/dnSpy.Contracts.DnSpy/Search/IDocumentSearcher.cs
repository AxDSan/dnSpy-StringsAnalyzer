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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.Contracts.Search {
	/// <summary>
	/// Searches for things in <see cref="IDsDocument"/>s and <see cref="IDocumentTreeView"/>
	/// </summary>
	interface IDocumentSearcher {
		/// <summary>
		/// true if too many results were found and the search was canceled
		/// </summary>
		bool TooManyResults { get; }

		/// <summary>
		/// Used by <see cref="ISearchResult"/>. true if the result is syntax highlighted in the UI.
		/// <see cref="ISearchResult.RefreshUI()"/> must be called if this gets updated.
		/// </summary>
		bool SyntaxHighlight { get; set; }

		/// <summary>
		/// Used by <see cref="ISearchResult"/>. Language to use.
		/// <see cref="ISearchResult.RefreshUI()"/> must be called if this gets updated.
		/// </summary>
		IDecompiler Decompiler { get; set; }

		/// <summary>
		/// A search result that was added to indicate that it's searching. Should be removed from
		/// the list after the search has completed if it's not null.
		/// </summary>
		ISearchResult? SearchingResult { get; }

		/// <summary>
		/// Starts the search
		/// </summary>
		/// <param name="files">Files to search</param>
		void Start(IEnumerable<DsDocumentNode> files);

		/// <summary>
		/// Starts the search
		/// </summary>
		/// <param name="typeInfos">Types to search</param>
		void Start(IEnumerable<SearchTypeInfo> typeInfos);

		/// <summary>
		/// Cancels the search
		/// </summary>
		void Cancel();

		/// <summary>
		/// Raised when the search has completed or was canceled
		/// </summary>
		event EventHandler? OnSearchCompleted;

		/// <summary>
		/// Raised when there are more results available
		/// </summary>
		event EventHandler<SearchResultEventArgs>? OnNewSearchResults;
	}
}
