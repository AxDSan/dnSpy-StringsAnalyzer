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
using System.IO;

namespace dnSpy.Contracts.Documents {
	/// <summary>
	/// Compares filenames
	/// </summary>
	public sealed class FilenameKey : IDsDocumentNameKey, IEquatable<FilenameKey?> {
		readonly string filename;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filename">Filename</param>
		public FilenameKey(string filename) => this.filename = GetFullPath(filename);

		FilenameKey(string filename, bool dummy) => this.filename = filename;

		// PERF: Path.GetFullPath() is slow so if we know that FilenameKey.GetFullPath() == filename, use this method instead
		internal static FilenameKey CreateFullPath(string filename) => new FilenameKey(filename, false);

		static string GetFullPath(string filename) {
			try {
				// Prevent slow exceptions
				if (string.IsNullOrEmpty(filename))
					return filename;
				return Path.GetFullPath(filename);
			}
			catch {
			}
			return filename;
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(FilenameKey? other) => other is not null && StringComparer.OrdinalIgnoreCase.Equals(filename, other.filename);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj">Other instance</param>
		/// <returns></returns>
		public override bool Equals(object? obj) => Equals(obj as FilenameKey);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(filename);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => filename;
	}
}
