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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using dnSpy.Roslyn.Internal.QuickInfo;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Roslyn.Intellisense.QuickInfo {
	sealed class QuickInfoSession {
		/*readonly*/ SnapshotPoint triggerPoint;
		readonly bool trackMouse;
		readonly IQuickInfoBroker quickInfoBroker;
		readonly ITextView textView;
		CancellationTokenSource? cancellationTokenSource;
		CancellationToken cancellationToken;

		public event EventHandler? Disposed;
		public QuickInfoItem? Item { get; private set; }
		public QuickInfoState State { get; private set; }

		static readonly object thisInstanceKey = new object();

		public QuickInfoSession(QuickInfoState state, SnapshotPoint triggerPoint, bool trackMouse, IQuickInfoBroker quickInfoBroker, ITextView textView) {
			if (state.QuickInfoService is null)
				throw new ArgumentException();
			if (triggerPoint.Snapshot != state.Snapshot)
				throw new ArgumentNullException(nameof(triggerPoint));
			State = state;
			this.triggerPoint = triggerPoint;
			this.trackMouse = trackMouse;
			this.quickInfoBroker = quickInfoBroker ?? throw new ArgumentNullException(nameof(quickInfoBroker));
			this.textView = textView ?? throw new ArgumentNullException(nameof(textView));
			cancellationTokenSource = new CancellationTokenSource();
			cancellationToken = cancellationTokenSource.Token;
		}

		public void Start() => StartAsync()
			.ContinueWith(t => {
				var ex = t.Exception;
				Debug2.Assert(ex is null);
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

		async Task StartAsync() {
			if (isDisposed || cancellationToken.IsCancellationRequested) {
				Dispose();
				return;
			}

			Item = await State.QuickInfoService.GetItemAsync(State.Document, triggerPoint.Position, cancellationToken);
			if (isDisposed || cancellationToken.IsCancellationRequested || Item is null) {
				Dispose();
				return;
			}

			DisposeCancellationToken();

			var trackingPoint = triggerPoint.Snapshot.CreateTrackingPoint(triggerPoint.Position, PointTrackingMode.Negative);
			var session = quickInfoBroker.CreateQuickInfoSession(textView, trackingPoint, trackMouse);
			session.Properties.AddProperty(thisInstanceKey, this);
			session.Dismissed += Session_Dismissed;
			session.Start();
		}

		public static QuickInfoSession? TryGetSession(IQuickInfoSession session) {
			if (session is null)
				return null;
			if (session.Properties.TryGetProperty(thisInstanceKey, out QuickInfoSession instance))
				return instance;
			return null;
		}

		void Session_Dismissed(object? sender, EventArgs e) {
			var session = (IQuickInfoSession)sender!;
			session.Dismissed -= Session_Dismissed;
			session.Properties.RemoveProperty(thisInstanceKey);
			Dispose();
		}

		void DisposeCancellationToken() {
			if (cancellationTokenSource is null)
				return;
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
			cancellationTokenSource = null;
			cancellationToken = CancellationToken.None;
		}

		public void Dispose() {
			if (isDisposed)
				return;
			isDisposed = true;
			Item = null;
			DisposeCancellationToken();
			Disposed?.Invoke(this, EventArgs.Empty);
		}
		bool isDisposed;
	}
}
