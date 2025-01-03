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

using dnSpy.Contracts.Decompiler;

namespace dnSpy.Contracts.Documents.Tabs.DocViewer {
	/// <summary>
	/// Reference and id. Created from a <see cref="SpanReference"/>
	/// </summary>
	public readonly struct ReferenceAndId {
		/// <summary>
		/// Gets the reference
		/// </summary>
		public object? Reference { get; }

		/// <summary>
		/// Id or null (eg. <see cref="PredefinedSpanReferenceIds.HighlightRelatedKeywords"/>). This is used to enable
		/// or disable the reference. If null, it's always enabled.
		/// </summary>
		public string? Id { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reference">Reference</param>
		/// <param name="id">Reference id or null, eg. <see cref="PredefinedSpanReferenceIds.HighlightRelatedKeywords"/></param>
		public ReferenceAndId(object? reference, string? id) {
			Reference = reference;
			Id = id;
		}
	}
}
