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

using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.NRefactory.CSharp {
	public struct CommentReference {
		public readonly int Length;
		public readonly object Reference;
		public readonly bool IsLocal;

		public CommentReference(int len, object @ref, bool isLocal = false) {
			this.Length = len;
			this.Reference = @ref;
			this.IsLocal = isLocal;
		}
	}

	public sealed class CommentReferencesCreator {
		readonly List<CommentReference> refs;
		readonly StringBuilder sb;

		public CommentReference[] CommentReferences {
			get { return refs.ToArray(); }
		}

		public string Text {
			get { return sb.ToString(); }
		}

		public CommentReferencesCreator(StringBuilder sb) {
			this.refs = new List<CommentReference>();
			this.sb = sb;
			this.sb.Clear();
		}

		public void AddText(string text) {
			Add(text, null, false);
		}

		public void AddReference(string text, object @ref, bool isLocal = false) {
			Add(text, @ref, isLocal);
		}

		void Add(string s, object @ref, bool isLocal) {
			refs.Add(new CommentReference(s.Length, @ref, isLocal));
			sb.Append(s);
		}
	}
}
