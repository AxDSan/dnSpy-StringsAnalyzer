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
using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Hex.Tagging {
	/// <summary>
	/// Text span and tag
	/// </summary>
	/// <typeparam name="T">Tag type</typeparam>
	public interface IHexTextTagSpan<out T> where T : HexTag {
		/// <summary>
		/// Gets the span
		/// </summary>
		VST.Span Span { get; }

		/// <summary>
		/// Gets the tag
		/// </summary>
		T Tag { get; }
	}

	/// <summary>
	/// Text span and tag
	/// </summary>
	/// <typeparam name="T">Tag type</typeparam>
	public class HexTextTagSpan<T> : IHexTextTagSpan<T> where T : HexTag {
		/// <summary>
		/// Gets the span
		/// </summary>
		public VST.Span Span { get; }

		/// <summary>
		/// Gets the tag
		/// </summary>
		public T Tag { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span"></param>
		/// <param name="tag"></param>
		public HexTextTagSpan(VST.Span span, T tag) {
			Span = span;
			Tag = tag ?? throw new ArgumentNullException(nameof(tag));
		}
	}
}
