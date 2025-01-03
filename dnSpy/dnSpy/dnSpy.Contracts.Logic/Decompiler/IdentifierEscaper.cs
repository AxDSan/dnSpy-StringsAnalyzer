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
using System.Globalization;
using System.Text;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Escapes identifiers
	/// </summary>
	public static class IdentifierEscaper {
		const int MAX_IDENTIFIER_LENGTH = 512;
		const string EMPTY_NAME = "<<EMPTY_NAME>>";

		/// <summary>
		/// Truncates the length of <paramref name="s"/> if it's too long
		/// </summary>
		/// <param name="s">Identifier string</param>
		/// <returns></returns>
		public static string? Truncate(string? s) => Truncate(s, MAX_IDENTIFIER_LENGTH);

		/// <summary>
		/// Truncates the length of <paramref name="s"/> if it's too long
		/// </summary>
		/// <param name="s">Identifier string</param>
		/// <param name="maxLength">Max length</param>
		/// <returns></returns>
		public static string? Truncate(string? s, int maxLength) {
			if (s is null || s.Length <= maxLength)
				return s;

			return s.Substring(0, maxLength) + "…";
		}

		/// <summary>
		/// Escapes an identifier
		/// </summary>
		/// <param name="id">Identifier</param>
		/// <returns></returns>
		public static string Escape(string? id) => Escape(id, false);

		/// <summary>
		/// Escapes an identifier
		/// </summary>
		/// <param name="id">Identifier</param>
		/// <param name="allowSpaces">true to allow spaces</param>
		/// <returns></returns>
		public static string Escape(string? id, bool allowSpaces) => Escape(id, MAX_IDENTIFIER_LENGTH, allowSpaces);

		/// <summary>
		/// Escapes an identifier
		/// </summary>
		/// <param name="id">Identifier</param>
		/// <param name="maxLength">Max length</param>
		/// <param name="allowSpaces">true to allow spaces</param>
		/// <returns></returns>
		public static string Escape(string? id, int maxLength, bool allowSpaces) {
			if (string2.IsNullOrEmpty(id))
				return EMPTY_NAME;

			// Common case is a valid string
			int i = 0;
			if (id.Length <= maxLength) {
				for (; ; i++) {
					if (i >= id.Length)
						return id;
					if (!IsValidChar(id[i], allowSpaces))
						break;
				}
			}

			// Here if obfuscated or weird string
			var sb = new StringBuilder(id.Length + 10);
			if (i != 0)
				sb.Append(id, 0, i);

			for (; i < id.Length; i++) {
				char c = id[i];
				if (!IsValidChar(c, allowSpaces)) {
					sb.Append(@"\u");
					sb.Append(((ushort)c).ToString("X4"));
				}
				else
					sb.Append(c);
				if (sb.Length >= maxLength)
					break;
			}

			if (sb.Length > maxLength) {
				sb.Length = maxLength;
				sb.Append('…');
			}

			return sb.ToString();
		}

		static bool IsValidChar(char c, bool allowSpaces) {
			if (0x21 <= c && c <= 0x7E)
				return true;
			if (c <= 0x20)
				return c == ' ' && allowSpaces;

			switch (char.GetUnicodeCategory(c)) {
			case UnicodeCategory.UppercaseLetter:
			case UnicodeCategory.LowercaseLetter:
			case UnicodeCategory.OtherLetter:
			case UnicodeCategory.DecimalDigitNumber:
				return true;

			case UnicodeCategory.TitlecaseLetter:
			case UnicodeCategory.ModifierLetter:
			case UnicodeCategory.NonSpacingMark:
			case UnicodeCategory.SpacingCombiningMark:
			case UnicodeCategory.EnclosingMark:
			case UnicodeCategory.LetterNumber:
			case UnicodeCategory.OtherNumber:
			case UnicodeCategory.SpaceSeparator:
			case UnicodeCategory.LineSeparator:
			case UnicodeCategory.ParagraphSeparator:
			case UnicodeCategory.Control:
			case UnicodeCategory.Format:
			case UnicodeCategory.Surrogate:
			case UnicodeCategory.PrivateUse:
			case UnicodeCategory.ConnectorPunctuation:
			case UnicodeCategory.DashPunctuation:
			case UnicodeCategory.OpenPunctuation:
			case UnicodeCategory.ClosePunctuation:
			case UnicodeCategory.InitialQuotePunctuation:
			case UnicodeCategory.FinalQuotePunctuation:
			case UnicodeCategory.OtherPunctuation:
			case UnicodeCategory.MathSymbol:
			case UnicodeCategory.CurrencySymbol:
			case UnicodeCategory.ModifierSymbol:
			case UnicodeCategory.OtherSymbol:
			case UnicodeCategory.OtherNotAssigned:
			default:
				return false;
			}
		}
	}
}
