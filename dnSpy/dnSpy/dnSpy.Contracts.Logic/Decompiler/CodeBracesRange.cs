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

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Brace pair
	/// </summary>
	public readonly struct CodeBracesRange {
		/// <summary>
		/// Span of start brace
		/// </summary>
		public TextSpan Left { get; }

		/// <summary>
		/// Span of end brace
		/// </summary>
		public TextSpan Right { get; }

		/// <summary>
		/// Gets the kind
		/// </summary>
		public CodeBracesRangeFlags Flags { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="left">Span of left brace</param>
		/// <param name="right">Span of right brace</param>
		/// <param name="flags">Flags</param>
		public CodeBracesRange(TextSpan left, TextSpan right, CodeBracesRangeFlags flags) {
			Left = left;
			Right = right;
			Flags = flags;
		}
	}
}
