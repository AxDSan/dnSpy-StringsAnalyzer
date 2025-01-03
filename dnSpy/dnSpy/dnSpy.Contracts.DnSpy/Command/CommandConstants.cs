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

namespace dnSpy.Contracts.Command {
	/// <summary>
	/// Command constants
	/// </summary>
	public static class CommandConstants {
		/// <summary>Standard command IDs (<see cref="StandardIds"/>)</summary>
		public static readonly Guid StandardGroup = new Guid("14608CB3-3965-49B2-A8A9-46CDBB4E2E30");

		/// <summary>Text editor command IDs (<see cref="TextEditorIds"/>)</summary>
		public static readonly Guid TextEditorGroup = new Guid("2313BC9A-8895-4390-87BF-FA563F35B33B");

		/// <summary>REPL command IDs (<see cref="ReplIds"/>)</summary>
		public static readonly Guid ReplGroup = new Guid("8DBB0C94-6B10-4AC3-A715-CC4D478F7B67");

		/// <summary>Output logger text pane command IDs (<see cref="OutputTextPaneIds"/>)</summary>
		public static readonly Guid OutputTextPaneGroup = new Guid("091D1F2F-175A-4BD9-A0F3-C5F052D22D75");

		/// <summary>Text reference command IDs</summary>
		public static readonly Guid TextReferenceGroup = new Guid("8D5BC6C7-C013-4401-9ADC-62B411573F3C");

		/// <summary>Bookmark command IDs (<see cref="BookmarkIds"/>)</summary>
		public static readonly Guid BookmarkGroup = new Guid("BF33ED4E-B503-4D95-995D-F5C5A4541923");

		/// <summary>Hex editor command IDs (<see cref="HexEditorIds"/>)</summary>
		public static readonly Guid HexEditorGroup = new Guid("3C6A823B-CF80-4D19-914E-498F773DEC7E");
	}
}
