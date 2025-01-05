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

using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Text.Operations {
	readonly struct WordParser {
		public enum WordKind : byte {
			Word,
			Whitespace,
			Other,
		}

		enum SpanKind : byte {
			Empty,
			MultipleCharacters,
			Word,
			MultipleWords,
			Sentence,
			MultipleSentences,
			Paragraph,
			MultipleParagraphs,
			Document
		}

		public static SnapshotSpan GetWordSpan(SnapshotPoint currentPosition, out WordKind kind) {
			kind = GetWordKind(currentPosition);
			return GetWordSpan(currentPosition, kind);
		}

		public static SnapshotSpan GetSpanOfEnclosing(SnapshotSpan activeSpan) => GetSpanOfEnclosing(activeSpan, GetSpanKind(activeSpan));

		public static SnapshotSpan GetSpanOfFirstChild(SnapshotSpan activeSpan) => GetSpanOfFirstChild(activeSpan, GetSpanKind(activeSpan));

		public static SnapshotSpan GetSpanOfNextSibling(SnapshotSpan activeSpan) => GetSpanOfNextSibling(activeSpan, GetSpanKind(activeSpan));

		public static SnapshotSpan GetSpanOfPreviousSibling(SnapshotSpan activeSpan) => GetSpanOfPreviousSibling(activeSpan, GetSpanKind(activeSpan));

		static SnapshotPoint GetStartSpanBefore(ITextSnapshotLine line, int column, WordKind kind) {
			int position = line.Start.Position + column;
			var snapshot = line.Snapshot;
			for (;;) {
				if (position == line.Start.Position)
					return line.Start;
				position--;
				if (GetWordKind(snapshot[position]) != kind)
					return new SnapshotPoint(snapshot, position + 1);
			}
		}

		static SnapshotPoint GetEndSpanAfter(ITextSnapshotLine line, int column, WordKind kind) {
			int position = line.Start.Position + column;
			var snapshot = line.Snapshot;
			for (;;) {
				if (position + 1 >= line.End.Position)
					return new SnapshotPoint(snapshot, line.End.Position);
				position++;
				if (GetWordKind(snapshot[position]) != kind)
					return new SnapshotPoint(snapshot, position);
			}
		}

		static SnapshotSpan GetWordSpan(SnapshotPoint currentPosition, WordKind kind) {
			Debug.Assert(GetWordKind(currentPosition) == kind);
			var line = currentPosition.GetContainingLine();
			int column = currentPosition.Position - line.Start.Position;
			var start = GetStartSpanBefore(line, column, kind);
			var end = GetEndSpanAfter(line, column, kind);
			return new SnapshotSpan(start, end);
		}

		static WordKind GetWordKind(SnapshotPoint currentPosition) {
			if (currentPosition.Position >= currentPosition.Snapshot.Length)
				return WordKind.Whitespace;
			return GetWordKind(currentPosition.GetChar());
		}

		static WordKind GetWordKind(char c) {
			if (char.IsLetterOrDigit(c) || c == '_' || c == '$')
				return WordKind.Word;
			if (char.IsWhiteSpace(c))
				return WordKind.Whitespace;
			if (char.IsSurrogate(c) || char.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
				return WordKind.Word;
			return WordKind.Other;
		}

		static SpanKind GetSpanKind(SnapshotSpan activeSpan) {
			if (activeSpan.IsEmpty)
				return SpanKind.Empty;
			var start = activeSpan.Start;
			if (activeSpan == GetSpanOfDocument(start.Snapshot))
				return SpanKind.Document;
			var span = GetWordSpan(start, out _);
			if (span == activeSpan)
				return SpanKind.Word;
			if (span.Contains(activeSpan))
				return SpanKind.MultipleCharacters;
			span = GetSpanOfSentence(start);
			if (span == activeSpan)
				return SpanKind.Sentence;
			if (span.Contains(activeSpan))
				return SpanKind.MultipleWords;
			span = GetSpanOfParagraph(start);
			if (span == activeSpan)
				return SpanKind.Paragraph;
			if (span.Contains(activeSpan))
				return SpanKind.MultipleSentences;
			return SpanKind.MultipleParagraphs;
		}

		static SnapshotSpan GetSpanOfEnclosing(SnapshotSpan activeSpan, SpanKind spanKind) {
			var start = activeSpan.Start;
			switch (spanKind) {
			case SpanKind.Empty:
			case SpanKind.MultipleCharacters: {
				var spanOfWord = GetWordSpan(start, out _);
				if (activeSpan.Contains(spanOfWord))
					return GetSpanOfEnclosing(activeSpan, SpanKind.Word);
				return spanOfWord;
			}
			case SpanKind.Word:
			case SpanKind.MultipleWords: {
				var spanOfSentence = GetSpanOfSentence(start);
				if (activeSpan.Contains(spanOfSentence))
					return GetSpanOfEnclosing(activeSpan, SpanKind.Sentence);
				return spanOfSentence;
			}
			case SpanKind.Sentence:
			case SpanKind.MultipleSentences: {
				var spanOfParagraph = GetSpanOfParagraph(start);
				if (activeSpan.Contains(spanOfParagraph))
					return GetSpanOfEnclosing(activeSpan, SpanKind.Paragraph);
				return spanOfParagraph;
			}
			case SpanKind.Paragraph:
			case SpanKind.MultipleParagraphs:
				return GetSpanOfDocument(start.Snapshot);
			default:
				return GetSpanOfDocument(start.Snapshot);
			}
		}

		static SnapshotSpan GetSpanOfFirstChild(SnapshotSpan activeSpan, SpanKind spanKind) {
			var start = activeSpan.Start;
			switch (spanKind) {
			case SpanKind.Empty:
			case SpanKind.MultipleCharacters:
			case SpanKind.MultipleWords:
			case SpanKind.MultipleSentences:
			case SpanKind.MultipleParagraphs:
				return GetSpanOfEnclosing(activeSpan, spanKind);
			case SpanKind.Word:
				return activeSpan;
			case SpanKind.Sentence: {
				var span = GetWordSpan(start, out var kind);
				if (kind == WordKind.Whitespace)
					return GetSpanOfNextSibling(span);
				return span;
			}
			case SpanKind.Paragraph:
				return GetSpanOfSentence(start);
			case SpanKind.Document:
				return GetSpanOfParagraph(start);
			default:
				return activeSpan;
			}
		}

		static SnapshotSpan GetSpanOfNextSibling(SnapshotSpan activeSpan, SpanKind spanKind) {
			var snapshotPoint = new SnapshotPoint(activeSpan.Snapshot, activeSpan.Start);
			switch (spanKind) {
			case SpanKind.Empty:
			case SpanKind.MultipleCharacters:
				return GetSpanOfEnclosing(activeSpan, spanKind);
			case SpanKind.Word:
			case SpanKind.MultipleWords: {
				Span spanOfSentence = GetSpanOfSentence(snapshotPoint);
				if (activeSpan.End == spanOfSentence.End)
					return GetSpanOfEnclosing(activeSpan, spanKind);
				var wordSpan = GetWordSpan(activeSpan.End, out var kind);
				if (kind == WordKind.Whitespace)
					return GetSpanOfNextSibling(wordSpan);
				return wordSpan;
			}
			case SpanKind.Sentence:
			case SpanKind.MultipleSentences: {
				Span spanOfParagraph = GetSpanOfParagraph(snapshotPoint);
				if (activeSpan == spanOfParagraph)
					return GetSpanOfNextSibling(activeSpan, SpanKind.Paragraph);
				if (activeSpan.End == spanOfParagraph.End)
					return GetSpanOfEnclosing(activeSpan, spanKind);
				return GetSpanOfSentence(activeSpan.End + 1);
			}
			case SpanKind.Paragraph:
			case SpanKind.MultipleParagraphs: {
				Span spanOfDocument = GetSpanOfDocument(snapshotPoint.Snapshot);
				if (activeSpan.End == spanOfDocument.End)
					return GetSpanOfEnclosing(activeSpan, spanKind);
				return GetSpanOfParagraph(activeSpan.End + 2);
			}
			case SpanKind.Document:
			default:
				return activeSpan;
			}
		}

		static SnapshotSpan GetSpanOfPreviousSibling(SnapshotSpan activeSpan, SpanKind spanKind) {
			var snapshotPoint = new SnapshotPoint(activeSpan.Snapshot, activeSpan.Start);
			switch (spanKind) {
			case SpanKind.Empty:
			case SpanKind.MultipleCharacters:
				return GetSpanOfEnclosing(activeSpan, spanKind);
			case SpanKind.Word:
			case SpanKind.MultipleWords: {
				var spanOfSentence = GetSpanOfSentence(snapshotPoint);
				if (activeSpan == spanOfSentence)
					return GetSpanOfPreviousSibling(activeSpan, SpanKind.Sentence);
				if (activeSpan.Start <= spanOfSentence.Start)
					return GetSpanOfEnclosing(activeSpan, spanKind);
				var wordSpan = GetWordSpan(activeSpan.Start - 1, out var kind);
				if (kind == WordKind.Whitespace)
					return GetSpanOfPreviousSibling(wordSpan);
				return wordSpan;
			}
			case SpanKind.Sentence:
			case SpanKind.MultipleSentences: {
				var spanOfParagraph = GetSpanOfParagraph(snapshotPoint);
				if (activeSpan == spanOfParagraph)
					return GetSpanOfPreviousSibling(activeSpan, SpanKind.Paragraph);
				if (activeSpan.Start == spanOfParagraph.Start)
					return GetSpanOfEnclosing(activeSpan, spanKind);
				return GetSpanOfSentence(activeSpan.Start - 1);
			}
			case SpanKind.Paragraph:
			case SpanKind.MultipleParagraphs: {
				var spanOfDocument = GetSpanOfDocument(snapshotPoint.Snapshot);
				if (activeSpan.Start == spanOfDocument.Start)
					return GetSpanOfEnclosing(activeSpan, spanKind);
				return GetSpanOfParagraph(activeSpan.Start - 3);
			}
			case SpanKind.Document:
			default:
				return activeSpan;
			}
		}

		static SnapshotSpan GetSpanOfDocument(ITextSnapshot snapshot) => new SnapshotSpan(snapshot, 0, snapshot.Length);

		static SnapshotSpan GetSpanOfSentence(SnapshotPoint currentPosition) => new SnapshotSpan(FindStartOfSentence(currentPosition), FindEndOfSentence(currentPosition));

		static SnapshotPoint FindStartOfSentence(SnapshotPoint currentPosition) {
			var snapshot = currentPosition.Snapshot;
			var currentLine = currentPosition.GetContainingLine();

			int line = currentLine.LineNumber;
			while (line >= 0) {
				currentLine = snapshot.GetLineFromLineNumber(line--);
				if (currentLine.Length == 0)
					return currentLine.EndIncludingLineBreak;

				int j = currentLine.EndIncludingLineBreak - 1 < currentPosition.Position ? currentLine.EndIncludingLineBreak - 1 : currentPosition.Position - 1;
				for (; j >= currentLine.Start; j--) {
					if (IsEndOfSentenceChar(snapshot[j]))
						return new SnapshotPoint(snapshot, j + 1);
				}

				if (currentLine.Start == 0)
					break;
			}

			return new SnapshotPoint(snapshot, 0);
		}

		static SnapshotPoint FindEndOfSentence(SnapshotPoint currentPosition) {
			var snapshot = currentPosition.Snapshot;
			var currentLine = currentPosition.GetContainingLine();

			int line = currentLine.LineNumber;
			while (line != snapshot.LineCount) {
				currentLine = snapshot.GetLineFromLineNumber(line++);
				if (currentLine.Length == 0 && snapshot.GetLineNumberFromPosition(currentPosition.Position) != line - 1)
					return currentLine.Start;

				int i = currentLine.Start <= currentPosition.Position ? currentPosition.Position : currentLine.Start;
				for (; i < currentLine.EndIncludingLineBreak; i++) {
					if (IsEndOfSentenceChar(snapshot[i]))
						return new SnapshotPoint(snapshot, i + 1);
				}

				if (currentLine.EndIncludingLineBreak == snapshot.Length)
					break;
			}

			return new SnapshotPoint(snapshot, snapshot.Length);
		}

		static bool IsEndOfSentenceChar(char value) => value == '.' || value == '!' || value == '?';

		static SnapshotSpan GetSpanOfParagraph(SnapshotPoint currentPosition) => new SnapshotSpan(FindStartOfParagraph(currentPosition), FindEndOfParagraph(currentPosition));

		static SnapshotPoint FindStartOfParagraph(SnapshotPoint currentPosition) {
			var snapshot = currentPosition.Snapshot;
			var currentLine = currentPosition.GetContainingLine();
			int line = currentLine.LineNumber;

			while (line >= 0) {
				currentLine = snapshot.GetLineFromLineNumber(line--);
				if (currentLine.Length == 0 && snapshot.GetLineNumberFromPosition(currentPosition.Position) != line + 1)
					return currentLine.EndIncludingLineBreak;
				if (currentLine.Start == 0)
					break;
			}

			return new SnapshotPoint(snapshot, 0);
		}

		static SnapshotPoint FindEndOfParagraph(SnapshotPoint currentPosition) {
			var snapshot = currentPosition.Snapshot;
			var currentLine = currentPosition.GetContainingLine();
			int line = currentLine.LineNumber;

			while (line != snapshot.LineCount) {
				currentLine = snapshot.GetLineFromLineNumber(line++);
				if (currentLine.Length == 0 && snapshot.GetLineNumberFromPosition(currentPosition.Position) != line - 1)
					return currentLine.Start;
				if (currentLine.EndIncludingLineBreak == snapshot.Length)
					break;
			}

			return new SnapshotPoint(snapshot, snapshot.Length);
		}
	}
}
