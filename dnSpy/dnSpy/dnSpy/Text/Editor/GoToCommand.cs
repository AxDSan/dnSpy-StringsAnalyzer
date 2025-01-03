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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Properties;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	[ExportCommandTargetFilterProvider(CommandTargetFilterOrder.TextEditor - 1)]
	sealed class GoToCommandTargetFilterProvider : ICommandTargetFilterProvider {
		readonly IMessageBoxService messageBoxService;

		[ImportingConstructor]
		GoToCommandTargetFilterProvider(IMessageBoxService messageBoxService) => this.messageBoxService = messageBoxService;

		public ICommandTargetFilter? Create(object target) {
			var textView = target as ITextView;
			if (textView is null)
				return null;

			return new GoToCommandTargetFilter(textView, messageBoxService);
		}
	}

	sealed class GoToCommandTargetFilter : ICommandTargetFilter {
		readonly ITextView textView;
		readonly IMessageBoxService messageBoxService;

		public GoToCommandTargetFilter(ITextView textView, IMessageBoxService messageBoxService) {
			this.textView = textView;
			this.messageBoxService = messageBoxService;
		}

		public CommandTargetStatus CanExecute(Guid group, int cmdId) =>
			group == CommandConstants.TextEditorGroup && (TextEditorIds)cmdId == TextEditorIds.GOTOLINE ? CommandTargetStatus.Handled : CommandTargetStatus.NotHandled;

		public CommandTargetStatus Execute(Guid group, int cmdId, object? args = null) {
			object? result = null;
			return Execute(group, cmdId, args, ref result);
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object? args, ref object? result) {
			if (group == CommandConstants.TextEditorGroup && (TextEditorIds)cmdId == TextEditorIds.GOTOLINE) {
				int lineNumber;
				int? columnNumber;
				if (args is int) {
					lineNumber = (int)args;
					columnNumber = null;
				}
				else {
					if (!GetLineColumn(out lineNumber, out columnNumber))
						return CommandTargetStatus.Handled;
				}
				if ((uint)lineNumber >= (uint)textView.TextSnapshot.LineCount)
					lineNumber = textView.TextSnapshot.LineCount - 1;
				var line = textView.TextSnapshot.GetLineFromLineNumber(lineNumber);
				int col;
				if (columnNumber is null) {
					col = 0;
					var snapshot = line.Snapshot;
					for (; col < line.Length; col++) {
						if (!char.IsWhiteSpace(snapshot[line.Start.Position + col]))
							break;
					}
				}
				else
					col = columnNumber.Value;
				if ((uint)col > (uint)line.Length)
					col = line.Length;
				textView.Selection.Clear();
				textView.Caret.MoveTo(line.Start + col);
				textView.EnsureCaretVisible();
				return CommandTargetStatus.Handled;
			}
			return CommandTargetStatus.NotHandled;
		}

		bool GetLineColumn(out int chosenLine, out int? chosenColumn) {
			var viewLine = textView.Caret.ContainingTextViewLine;
			var snapshotLine = viewLine.Start.GetContainingLine();
			var wpfTextView = textView as IWpfTextView;
			Debug2.Assert(wpfTextView is not null);
			var ownerWindow = wpfTextView is null ? null : Window.GetWindow(wpfTextView.VisualElement);
			int maxLines = snapshotLine.Snapshot.LineCount;

			var res = messageBoxService.Ask(dnSpy_Resources.GoToLine_Label, null, dnSpy_Resources.GoToLine_Title, s => {
				TryGetRowCol(s, snapshotLine.LineNumber, maxLines, out var line, out var column);
				return Tuple.Create<int, int?>(line!.Value, column);
			}, s => {
				return TryGetRowCol(s, snapshotLine.LineNumber, maxLines, out var line, out var column);
			}, ownerWindow);
			if (res is null) {
				chosenLine = 0;
				chosenColumn = null;
				return false;
			}

			chosenLine = res.Item1;
			chosenColumn = res.Item2;
			return true;
		}

		string TryGetRowCol(string s, int currentLine, int maxLines, out int? line, out int? column) {
			line = null;
			column = null;
			bool columnError = false;
			Match match;
			if ((match = goToLineRegex1.Match(s)) is not null && match.Groups.Count == 4) {
				TryParseOneBasedToZeroBased(match.Groups[1].Value, out line);
				if (line is not null && line.Value >= maxLines)
					line = null;
				if (match.Groups[3].Value != string.Empty)
					columnError = !TryParseOneBasedToZeroBased(match.Groups[3].Value, out column);
			}
			else if ((match = goToLineRegex2.Match(s)) is not null && match.Groups.Count == 2) {
				line = currentLine;
				columnError = !TryParseOneBasedToZeroBased(match.Groups[1].Value, out column);
			}
			if (line is null || columnError) {
				if (string.IsNullOrWhiteSpace(s))
					return dnSpy_Resources.GoToLine_EnterLineNum;
				return string.Format(dnSpy_Resources.GoToLine_InvalidLine, s);
			}
			return string.Empty;
		}
		static readonly Regex goToLineRegex1 = new Regex(@"^\s*(\d+)\s*(,\s*(\d+))?\s*$");
		static readonly Regex goToLineRegex2 = new Regex(@"^\s*,\s*(\d+)\s*$");

		static bool TryParseOneBasedToZeroBased(string valText, out int? res) {
			if (int.TryParse(valText, out int val) && val > 0) {
				res = val - 1;
				return true;
			}
			res = null;
			return false;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}
}
