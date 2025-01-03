﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace ICSharpCode.TreeView {
	/// <summary>
	/// Custom TextSearch-implementation.
	/// Fixes #67 - Moving to class member in tree view by typing in first character of member name selects parent assembly
	/// </summary>
	public class SharpTreeViewTextSearch : DependencyObject {
		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		static extern int GetDoubleClickTime();

		static readonly DependencyPropertyKey TextSearchInstancePropertyKey = DependencyProperty.RegisterAttachedReadOnly(
			"TextSearchInstance",
			typeof(SharpTreeViewTextSearch), typeof(SharpTreeViewTextSearch), new FrameworkPropertyMetadata(null));

		static readonly DependencyProperty TextSearchInstanceProperty = TextSearchInstancePropertyKey.DependencyProperty;

		DispatcherTimer timer;

		bool isActive;
		int lastMatchIndex;
		string matchPrefix;

		readonly Stack<string> inputStack;
		readonly SharpTreeView treeView;

		SharpTreeViewTextSearch(SharpTreeView treeView) {
			this.treeView = treeView ?? throw new ArgumentNullException(nameof(treeView));
			inputStack = new Stack<string>(8);
			ClearState();
		}

		public static SharpTreeViewTextSearch GetInstance(SharpTreeView sharpTreeView) {
			var textSearch = (SharpTreeViewTextSearch)sharpTreeView.GetValue(TextSearchInstanceProperty);
			if (textSearch == null) {
				textSearch = new SharpTreeViewTextSearch(sharpTreeView);
				sharpTreeView.SetValue(TextSearchInstancePropertyKey, textSearch);
			}

			return textSearch;
		}

		public bool RevertLastCharacter() {
			if (!isActive || inputStack.Count == 0)
				return false;
			matchPrefix = matchPrefix.Substring(0, matchPrefix.Length - inputStack.Pop().Length);
			ResetTimeout();
			return true;
		}

		public bool Search(string nextChar) {
			int startIndex = isActive ? lastMatchIndex : Math.Max(0, treeView.SelectedIndex);
			bool lookBackwards = inputStack.Count > 0 &&
								 string.Compare(inputStack.Peek(), nextChar, StringComparison.OrdinalIgnoreCase) == 0;
			int nextMatchIndex = IndexOfMatch(matchPrefix + nextChar, startIndex, lookBackwards, out bool wasNewCharUsed);
			if (nextMatchIndex != -1) {
				if (!isActive || nextMatchIndex != startIndex) {
					treeView.SelectedItem = treeView.Items[nextMatchIndex];
					treeView.FocusNode((SharpTreeNode)treeView.SelectedItem);
					lastMatchIndex = nextMatchIndex;
				}

				if (wasNewCharUsed) {
					matchPrefix += nextChar;
					inputStack.Push(nextChar);
				}

				isActive = true;
			}

			if (isActive) {
				ResetTimeout();
			}

			return nextMatchIndex != -1;
		}

		int IndexOfMatch(string needle, int startIndex, bool tryBackward, out bool charWasUsed) {
			charWasUsed = false;
			if (treeView.Items.Count == 0 || string.IsNullOrEmpty(needle))
				return -1;
			int index = -1;
			int fallbackIndex = -1;
			bool fallbackMatch = false;
			int i = startIndex;
			var comparisonType = treeView.IsTextSearchCaseSensitive
				? StringComparison.Ordinal
				: StringComparison.OrdinalIgnoreCase;
			do {
				var item = (SharpTreeNode)treeView.Items[i];
				if (item != null && item.Text != null) {
					string text = item.Text.ToString();
					if (text.StartsWith(needle, comparisonType)) {
						charWasUsed = true;
						index = i;
						break;
					}

					if (tryBackward) {
						if (fallbackMatch && matchPrefix != string.Empty) {
							if (fallbackIndex == -1 && text.StartsWith(matchPrefix, comparisonType)) {
								fallbackIndex = i;
							}
						}
						else {
							fallbackMatch = true;
						}
					}
				}

				i++;
				if (i >= treeView.Items.Count)
					i = 0;
			} while (i != startIndex);

			return index == -1 ? fallbackIndex : index;
		}

		void ClearState() {
			isActive = false;
			matchPrefix = string.Empty;
			lastMatchIndex = -1;
			inputStack.Clear();
			timer?.Stop();
			timer = null;
		}

		void ResetTimeout() {
			if (timer == null) {
				timer = new DispatcherTimer(DispatcherPriority.Normal);
				timer.Tick += (_, _) => ClearState();
			}
			else {
				timer.Stop();
			}

			timer.Interval = TimeSpan.FromMilliseconds(GetDoubleClickTime() * 2);
			timer.Start();
		}
	}
}
