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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	sealed class LogEditor : ILogEditor {
		public object? UIObject => wpfTextViewHost.HostControl;
		public IInputElement? FocusedElement => wpfTextView.VisualElement;
		public FrameworkElement? ZoomElement => wpfTextView.VisualElement;
		public object? Tag { get; set; }
		public IDsWpfTextView TextView => wpfTextViewHost.TextView;
		public IDsWpfTextViewHost TextViewHost => wpfTextViewHost;

		public WordWrapStyles WordWrapStyle {
			get => wpfTextView.Options.WordWrapStyle();
			set => wpfTextView.Options.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, value);
		}

		public bool ShowLineNumbers {
			get => wpfTextView.Options.IsLineNumberMarginEnabled();
			set => wpfTextView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, value);
		}

		readonly IDsWpfTextViewHost wpfTextViewHost;
		readonly IDsWpfTextView wpfTextView;
		readonly CachedColorsList cachedColorsList;
		readonly Dispatcher dispatcher;
		CachedTextColorsCollection? cachedTextColorsCollection;

		sealed class GuidObjectsProvider : IGuidObjectsProvider {
			readonly LogEditor logEditorUI;

			public GuidObjectsProvider(LogEditor logEditorUI) => this.logEditorUI = logEditorUI;

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsProviderArgs args) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_LOG_EDITOR_GUID, logEditorUI);
			}
		}

		static readonly string[] defaultRoles = new string[] {
			PredefinedTextViewRoles.Interactive,
			PredefinedTextViewRoles.Zoomable,
			PredefinedDsTextViewRoles.CanHaveCurrentLineHighlighter,
			PredefinedDsTextViewRoles.CanHaveBackgroundImage,
			PredefinedDsTextViewRoles.CanHaveLineNumberMargin,
			PredefinedDsTextViewRoles.LogEditor,
		};

		public LogEditor(LogEditorOptions? options, IDsTextEditorFactoryService dsTextEditorFactoryService, IContentTypeRegistryService contentTypeRegistryService, ITextBufferFactoryService textBufferFactoryService, IEditorOptionsFactoryService editorOptionsFactoryService) {
			dispatcher = Dispatcher.CurrentDispatcher;
			cachedColorsList = new CachedColorsList();
			options = options?.Clone() ?? new LogEditorOptions();
			options.CreateGuidObjects = CommonGuidObjectsProvider.Create(options.CreateGuidObjects, new GuidObjectsProvider(this));

			var contentType = contentTypeRegistryService.GetContentType(options.ContentType, options.ContentTypeString) ?? textBufferFactoryService.TextContentType;
			var textBuffer = textBufferFactoryService.CreateTextBuffer(contentType);
			CachedColorsListTaggerProvider.AddColorizer(textBuffer, cachedColorsList);
			var rolesList = new List<string>(defaultRoles);
			rolesList.AddRange(options.ExtraRoles);
			var roles = dsTextEditorFactoryService.CreateTextViewRoleSet(rolesList);
			var textView = dsTextEditorFactoryService.CreateTextView(textBuffer, roles, editorOptionsFactoryService.GlobalOptions, options);
			var wpfTextViewHost = dsTextEditorFactoryService.CreateTextViewHost(textView, false);
			this.wpfTextViewHost = wpfTextViewHost;
			wpfTextView = wpfTextViewHost.TextView;
			wpfTextView.Options.SetOptionValue(DefaultTextViewOptions.DragDropEditingId, false);
			wpfTextView.Options.SetOptionValue(DefaultTextViewOptions.ViewProhibitUserInputId, true);
			wpfTextView.Options.SetOptionValue(DefaultTextViewOptions.AutoScrollId, true);
			SetNewDocument();
		}

		void SetNewDocument() {
			cachedTextColorsCollection = new CachedTextColorsCollection();
			wpfTextView.TextBuffer.Replace(new Span(0, wpfTextView.TextBuffer.CurrentSnapshot.Length), string.Empty);
			ClearUndoRedoHistory();
			cachedColorsList.Clear();
			cachedColorsList.Add(0, cachedTextColorsCollection);
		}

		void ClearUndoRedoHistory() {
			//TODO:
		}

		public void Clear() {
			ClearPendingOutput();
			SetNewDocument();
			wpfTextView.Selection.Clear();
			wpfTextView.Caret.MoveTo(new SnapshotPoint(wpfTextView.TextSnapshot, 0));
		}

		public string GetText() => wpfTextView.TextSnapshot.GetText();
		public void Write(string text, object color) => OutputPrint(text, color);
		public void Write(string text, TextColor color) => OutputPrint(text, color.Box());
		public void WriteLine(string text, TextColor color) => WriteLine(text, color.Box());

		public void WriteLine(string text, object color) {
			OutputPrint(text, color);
			OutputPrint(Environment.NewLine, color);
		}

		public void Write(IEnumerable<ColorAndText> text) {
			var list = text as IList<ColorAndText>;
			if (list is null)
				list = text.ToArray();
			if (list.Count == 0)
				return;

			lock (pendingOutputLock)
				pendingOutput.AddRange(list);

			FlushOutput();
		}

		void OutputPrint(string text, object color, bool startOnNewLine = false) {
			if (string.IsNullOrEmpty(text))
				return;

			lock (pendingOutputLock) {
				if (startOnNewLine) {
					if (pendingOutput.Count > 0) {
						var last = pendingOutput[pendingOutput.Count - 1];
						if (last.Text.Length > 0 && last.Text[last.Text.Length - 1] != '\n')
							pendingOutput.Add(new ColorAndText(BoxedTextColor.Text, Environment.NewLine));
					}
					else if (LastLine.Length != 0)
						pendingOutput.Add(new ColorAndText(BoxedTextColor.Text, Environment.NewLine));
				}
				pendingOutput.Add(new ColorAndText(color, text));
			}

			FlushOutput();
		}

		ITextSnapshotLine LastLine {
			get {
				var line = wpfTextView.TextSnapshot.GetLineFromLineNumber(wpfTextView.TextSnapshot.LineCount - 1);
				Debug.Assert(line.Length == line.LengthIncludingLineBreak);
				return line;
			}
		}

		void RawAppend(string text) => wpfTextView.TextBuffer.Insert(wpfTextView.TextSnapshot.Length, text);

		void FlushOutputUIThread() {
			dispatcher.VerifyAccess();
			if (wpfTextViewHost.IsClosed)
				return;

			ColorAndText[] newPendingOutput;
			var sb = new StringBuilder();
			lock (pendingOutputLock) {
				pendingOutput_dispatching = false;
				newPendingOutput = pendingOutput.ToArray();
				pendingOutput.Clear();
			}

			foreach (var info in newPendingOutput) {
				sb.Append(info.Text);
				cachedTextColorsCollection!.Append(info.Color, info.Text);
			}
			if (sb.Length == 0)
				return;

			RawAppend(sb.ToString());
		}

		void FlushOutput() {
			if (!dispatcher.CheckAccess()) {
				lock (pendingOutputLock) {
					if (pendingOutput_dispatching)
						return;
					pendingOutput_dispatching = true;
					try {
						dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(FlushOutputUIThread));
					}
					catch {
						pendingOutput_dispatching = false;
						throw;
					}
				}
			}
			else
				FlushOutputUIThread();
		}
		readonly object pendingOutputLock = new object();
		List<ColorAndText> pendingOutput = new List<ColorAndText>();
		bool pendingOutput_dispatching;

		void ClearPendingOutput() {
			lock (pendingOutputLock) {
				pendingOutput = new List<ColorAndText>();
				pendingOutput_dispatching = false;
			}
		}

		public void Dispose() {
			if (!wpfTextViewHost.IsClosed)
				wpfTextViewHost.Close();
		}
	}
}
