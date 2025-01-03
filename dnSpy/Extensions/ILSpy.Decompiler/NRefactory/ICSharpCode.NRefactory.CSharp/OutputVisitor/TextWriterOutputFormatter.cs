// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace ICSharpCode.NRefactory.CSharp {
	/// <summary>
	/// Writes C# code into a TextWriter.
	/// </summary>
	public class TextWriterTokenWriter : TokenWriter, ILocatable
	{
		readonly TextWriter textWriter;
		readonly int maxStringLength;
		int indentation;
		bool needsIndent = true;
		bool isAtStartOfLine = true;
		int line, column;

		public int Indentation {
			get { return this.indentation; }
			set { this.indentation = value; }
		}

		public TextLocation Location {
			get { return new TextLocation(line, column + (needsIndent ? indentation * IndentationString.Length : 0)); }
		}

		public string IndentationString { get; set; }

		public TextWriterTokenWriter(TextWriter textWriter, int maxStringLength = -1)
		{
			if (textWriter == null)
				throw new ArgumentNullException("textWriter");
			this.textWriter = textWriter;
			this.IndentationString = "\t";
			this.line = 1;
			this.column = 1;
			this.maxStringLength = maxStringLength;
		}

		public override void WriteIdentifier(Identifier identifier, object data)
		{
			WriteIndentation();
			if (!BoxedTextColor.Keyword.Equals(data) && (identifier.IsVerbatim || CSharpOutputVisitor.IsKeyword(identifier.Name, identifier))) {
				textWriter.Write('@');
				column++;
			}
			string name = EscapeIdentifier(identifier.Name);
			textWriter.Write(name);
			column += name.Length;
			isAtStartOfLine = false;
		}

		public override void WriteKeyword(Role role, string keyword)
		{
			WriteIndentation();
			column += keyword.Length;
			textWriter.Write(keyword);
			isAtStartOfLine = false;
		}

		public override void WriteToken(Role role, string token, object data)
		{
			WriteIndentation();
			column += token.Length;
			textWriter.Write(token);
			isAtStartOfLine = false;
		}

		public override void Space()
		{
			WriteIndentation();
			column++;
			textWriter.Write(' ');
		}

		protected void WriteIndentation()
		{
			if (needsIndent) {
				needsIndent = false;
				for (int i = 0; i < indentation; i++) {
					textWriter.Write(this.IndentationString);
				}
				column += indentation * IndentationString.Length;
			}
		}

		public override void NewLine()
		{
			textWriter.WriteLine();
			column = 1;
			line++;
			needsIndent = true;
			isAtStartOfLine = true;
		}

		public override void Indent()
		{
			indentation++;
		}

		public override void Unindent()
		{
			indentation--;
		}

		public override void WriteComment(CommentType commentType, string content, CommentReference[] refs)
		{
			WriteIndentation();
			switch (commentType) {
				case CommentType.SingleLine:
					textWriter.Write("//");
					textWriter.WriteLine(content);
					column = 1;
					line++;
					needsIndent = true;
					isAtStartOfLine = true;
					break;
				case CommentType.MultiLine:
					textWriter.Write("/*");
					textWriter.Write(content);
					textWriter.Write("*/");
					column += 2;
					UpdateEndLocation(content, ref line, ref column);
					column += 2;
					isAtStartOfLine = false;
					break;
				case CommentType.Documentation:
					textWriter.Write("///");
					textWriter.WriteLine(content);
					column = 1;
					line++;
					needsIndent = true;
					isAtStartOfLine = true;
					break;
				case CommentType.MultiLineDocumentation:
					textWriter.Write("/**");
					textWriter.Write(content);
					textWriter.Write("*/");
					column += 3;
					UpdateEndLocation(content, ref line, ref column);
					column += 2;
					isAtStartOfLine = false;
					break;
				default:
					textWriter.Write(content);
					column += content.Length;
					break;
			}
		}

		static void UpdateEndLocation(string content, ref int line, ref int column)
		{
			if (string.IsNullOrEmpty(content))
				return;
			for (int i = 0; i < content.Length; i++) {
				char ch = content[i];
				switch (ch) {
					case '\r':
						if (i + 1 < content.Length && content[i + 1] == '\n')
							i++;
						goto case '\n';
					case '\n':
						line++;
						column = 0;
						break;
				}
				column++;
			}
		}

		public override void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument)
		{
			// pre-processor directive must start on its own line
			if (!isAtStartOfLine)
				NewLine();
			WriteIndentation();
			textWriter.Write('#');
			string directive = type.ToString().ToLowerInvariant();
			textWriter.Write(directive);
			column += 1 + directive.Length;
			if (!string.IsNullOrEmpty(argument)) {
				textWriter.Write(' ');
				textWriter.Write(argument);
				column += 1 + argument.Length;
			}
			NewLine();
		}

		public static string PrintPrimitiveValue(object value)
		{
			TextWriter writer = new StringWriter();
			TextWriterTokenWriter tokenWriter = new TextWriterTokenWriter(writer);
			tokenWriter.WritePrimitiveValue(value, CSharpMetadataTextColorProvider.Instance.GetColor(value));
			return writer.ToString();
		}

		public override void WritePrimitiveValue(object value, object data = null, string literalValue = null)
		{
			var numberFormatter = NumberFormatter.GetCSharpInstance(hex: false, upper: true);
			WritePrimitiveValue(value, data, literalValue, maxStringLength, ref column, numberFormatter, (a, b, c) => textWriter.Write(a), WriteToken);
		}

		public static void WritePrimitiveValue(object value, object data, string literalValue, int maxStringLength, ref int column, NumberFormatter numberFormatter, Action<string, object, object> writer, Action<Role, string, object> writeToken)
		{
			if (literalValue != null) {
				Debug.Assert(data != null);
				writer(literalValue, null, data ?? BoxedTextColor.Text);
				column += literalValue.Length;
				return;
			}

			if (value == null) {
				// usually NullReferenceExpression should be used for this, but we'll handle it anyways
				writer("null", null, BoxedTextColor.Keyword);
				column += 4;
				return;
			}

			if (value is bool) {
				if ((bool)value) {
					writer("true", null, BoxedTextColor.Keyword);
					column += 4;
				} else {
					writer("false", null, BoxedTextColor.Keyword);
					column += 5;
				}
				return;
			}

			var s = value as string;
			if (s != null) {
				string tmp = "\"" + ConvertStringMaxLength(s, maxStringLength) + "\"";
				column += tmp.Length;
				writer(tmp, null, BoxedTextColor.String);
			} else if (value is char) {
				string tmp = "'" + ConvertCharLiteral((char)value) + "'";
				column += tmp.Length;
				writer(tmp, null, BoxedTextColor.Char);
			} else if (value is decimal) {
				string str = ((decimal)value).ToString(NumberFormatInfo.InvariantInfo) + "m";
				column += str.Length;
				writer(str, null, BoxedTextColor.Number);
			} else if (value is float) {
				float f = (float)value;
				if (float.IsInfinity(f) || float.IsNaN(f)) {
					// Strictly speaking, these aren't PrimitiveExpressions;
					// but we still support writing these to make life easier for code generators.
					writer("float", null, BoxedTextColor.Keyword);
					column += 5;
					writeToken(Roles.Dot, ".", BoxedTextColor.Operator);
					if (float.IsPositiveInfinity(f)) {
						writer("PositiveInfinity", null, BoxedTextColor.LiteralField);
						column += "PositiveInfinity".Length;
					} else if (float.IsNegativeInfinity(f)) {
						writer("NegativeInfinity", null, BoxedTextColor.LiteralField);
						column += "NegativeInfinity".Length;
					} else {
						writer("NaN", null, BoxedTextColor.LiteralField);
						column += 3;
					}
					return;
				}
				var number = f.ToString("R", NumberFormatInfo.InvariantInfo) + "f";
				if (f == 0 && 1 / f == float.NegativeInfinity && number[0] != '-') {
					// negative zero is a special case
					// (again, not a primitive expression, but it's better to handle
					// the special case here than to do it in all code generators)
					number = "-" + number;
				}
				column += number.Length;
				writer(number, value, BoxedTextColor.Number);
			} else if (value is double) {
				double f = (double)value;
				if (double.IsInfinity(f) || double.IsNaN(f)) {
					// Strictly speaking, these aren't PrimitiveExpressions;
					// but we still support writing these to make life easier for code generators.
					writer("double", null, BoxedTextColor.Keyword);
					column += 6;
					writeToken(Roles.Dot, ".", BoxedTextColor.Operator);
					if (double.IsPositiveInfinity(f)) {
						writer("PositiveInfinity", null, BoxedTextColor.LiteralField);
						column += "PositiveInfinity".Length;
					} else if (double.IsNegativeInfinity(f)) {
						writer("NegativeInfinity", null, BoxedTextColor.LiteralField);
						column += "NegativeInfinity".Length;
					} else {
						writer("NaN", null, BoxedTextColor.LiteralField);
						column += 3;
					}
					return;
				}
				string number = f.ToString("R", NumberFormatInfo.InvariantInfo);
				if (f == 0 && 1 / f == double.NegativeInfinity && number[0] != '-') {
					// negative zero is a special case
					// (again, not a primitive expression, but it's better to handle
					// the special case here than to do it in all code generators)
					number = "-" + number;
				}
				if (number.IndexOf('.') < 0 && number.IndexOf('E') < 0) {
					number += ".0";
				}
				column += number.Length;
				writer(number, value, BoxedTextColor.Number);
			} else if (value is IFormattable) {
				string valueStr;
				switch (value) {
				case int v:
					valueStr = numberFormatter.Format(v);
					break;
				case uint v:
					valueStr = numberFormatter.Format(v) + "U";
					break;
				case long v:
					valueStr = numberFormatter.Format(v) + "L";
					break;
				case ulong v:
					valueStr = numberFormatter.Format(v) + "UL";
					break;
				case byte v:
					valueStr = numberFormatter.Format(v);
					break;
				case ushort v:
					valueStr = numberFormatter.Format(v);
					break;
				case short v:
					valueStr = numberFormatter.Format(v);
					break;
				case sbyte v:
					valueStr = numberFormatter.Format(v);
					break;
				default:
					valueStr = ((IFormattable)value).ToString(null, NumberFormatInfo.InvariantInfo);
					break;
				}
				writer(valueStr, value, BoxedTextColor.Number);
				column += valueStr.Length;
			} else {
				s = value.ToString();
				writer(s, null, CSharpMetadataTextColorProvider.Instance.GetColor(value));
				column += s.Length;
			}
		}

		/// <summary>
		/// Gets the escape sequence for the specified character within a char literal.
		/// Does not include the single quotes surrounding the char literal.
		/// </summary>
		public static string ConvertCharLiteral(char ch)
		{
			if (ch == '\'') {
				return "\\'";
			}
			return ConvertChar(ch);
		}

		/// <summary>
		/// Gets the escape sequence for the specified character.
		/// </summary>
		/// <remarks>This method does not convert ' or ".</remarks>
		static string ConvertChar(char ch)
		{
			switch (ch) {
				case '\\':
					return "\\\\";
				case '\0':
					return "\\0";
				case '\a':
					return "\\a";
				case '\b':
					return "\\b";
				case '\f':
					return "\\f";
				case '\n':
					return "\\n";
				case '\r':
					return "\\r";
				case '\t':
					return "\\t";
				case '\v':
					return "\\v";
				case ' ':
				case '_':
				case '`':
				case '^':
					// ASCII characters we allow directly in the output even though we don't use
					// other Unicode characters of the same category.
					return ch.ToString();
				case '\ufffd':
					return "\\u" + ((int)ch).ToString("x4");
				default:
					switch (char.GetUnicodeCategory(ch)) {
					case UnicodeCategory.NonSpacingMark:
					case UnicodeCategory.SpacingCombiningMark:
					case UnicodeCategory.EnclosingMark:
					case UnicodeCategory.LineSeparator:
					case UnicodeCategory.ParagraphSeparator:
					case UnicodeCategory.Control:
					case UnicodeCategory.Format:
					case UnicodeCategory.Surrogate:
					case UnicodeCategory.PrivateUse:
					case UnicodeCategory.ConnectorPunctuation:
					case UnicodeCategory.ModifierSymbol:
					case UnicodeCategory.OtherNotAssigned:
					case UnicodeCategory.SpaceSeparator:
						return "\\u" + ((int)ch).ToString("x4");
					default:
						return ch.ToString();
					}
			}
		}

		/// <summary>
		/// Converts special characters to escape sequences within the given string.
		/// </summary>
		public static string ConvertString(string str)
		{
			return ConvertString(str, 0, str.Length, -1);
		}

		public static string ConvertStringMaxLength(string str, int maxChars)
		{
			return ConvertString(str, 0, str.Length, maxChars);
		}

		static string ConvertString(string str, int start, int length, int maxChars)
		{
			int i = start;
			bool truncated = false;
			if (maxChars > 0 && length > maxChars) {
				length = maxChars;
				truncated = true;
			}
			const string TRUNC_MSG = "[...string is too long...]";
			int end = start + length;
			for (; ; i++) {
				if (i >= end) {
					if (start != 0 || end != str.Length)
						str = str.Substring(start, length);
					if (truncated)
						return str + TRUNC_MSG;
					return str;
				}
				char c = str[i];
				switch (c) {
				case '"':
				case '\\':
				case '\0':
				case '\a':
				case '\b':
				case '\f':
				case '\n':
				case '\r':
				case '\t':
				case '\v':
					goto escapeChars;
				case ' ':
				case '_':
				case '`':
				case '^':
					break;
				case '\ufffd':
					goto escapeChars;
				default:
					switch (char.GetUnicodeCategory(c)) {
					case UnicodeCategory.NonSpacingMark:
					case UnicodeCategory.SpacingCombiningMark:
					case UnicodeCategory.EnclosingMark:
					case UnicodeCategory.LineSeparator:
					case UnicodeCategory.ParagraphSeparator:
					case UnicodeCategory.Control:
					case UnicodeCategory.Format:
					case UnicodeCategory.Surrogate:
					case UnicodeCategory.PrivateUse:
					case UnicodeCategory.ConnectorPunctuation:
					case UnicodeCategory.ModifierSymbol:
					case UnicodeCategory.OtherNotAssigned:
					case UnicodeCategory.SpaceSeparator:
						goto escapeChars;
					}
					break;
				}
			}

			escapeChars:
			StringBuilder sb = new StringBuilder();
			if (i > start)
				sb.Append(str, start, i - start);
			for (; i < end; i++) {
				char ch = str[i];
				if (ch == '"') {
					sb.Append("\\\"");
				} else {
					sb.Append(ConvertChar(ch));
				}
			}
			if (truncated)
				sb.Append(TRUNC_MSG);
			return sb.ToString();
		}

		public static string EscapeIdentifier(string identifier)
		{
			if (string.IsNullOrEmpty(identifier))
				return identifier;
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < identifier.Length; i++) {
				if (IsPrintableIdentifierChar(identifier, i)) {
					if (char.IsSurrogatePair(identifier, i)) {
						sb.Append(identifier.Substring(i, 2));
						i++;
					} else {
						sb.Append(identifier[i]);
					}
				} else {
					if (char.IsSurrogatePair(identifier, i)) {
						sb.AppendFormat("\\U{0:x8}", char.ConvertToUtf32(identifier, i));
						i++;
					} else {
						sb.AppendFormat("\\u{0:x4}", (int)identifier[i]);
					}
				}
			}
			return sb.ToString();
		}

		static bool IsPrintableIdentifierChar(string identifier, int index)
		{
			switch (char.GetUnicodeCategory(identifier, index)) {
			case UnicodeCategory.NonSpacingMark:
			case UnicodeCategory.SpacingCombiningMark:
			case UnicodeCategory.EnclosingMark:
			case UnicodeCategory.LineSeparator:
			case UnicodeCategory.ParagraphSeparator:
			case UnicodeCategory.Control:
			case UnicodeCategory.Format:
			case UnicodeCategory.Surrogate:
			case UnicodeCategory.PrivateUse:
			case UnicodeCategory.ConnectorPunctuation:
			case UnicodeCategory.ModifierSymbol:
			case UnicodeCategory.OtherNotAssigned:
			case UnicodeCategory.SpaceSeparator:
				return false;
			default:
				return true;
			}
		}

		public override void WritePrimitiveType(string type)
		{
			textWriter.Write(type);
			column += type.Length;
			if (type == "new") {
				textWriter.Write("()");
				column += 2;
			}
		}

		public override void StartNode(AstNode node)
		{
			// Write out the indentation, so that overrides of this method
			// can rely use the current output length to identify the position of the node
			// in the output.
			WriteIndentation();
		}

		public override void EndNode(AstNode node)
		{
		}
	}
}
