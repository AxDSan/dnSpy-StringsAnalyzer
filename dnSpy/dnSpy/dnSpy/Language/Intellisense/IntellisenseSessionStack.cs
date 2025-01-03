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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Language.Intellisense {
	sealed partial class IntellisenseSessionStack : IIntellisenseSessionStack {
		public ReadOnlyObservableCollection<IIntellisenseSession> Sessions { get; }
		public IIntellisenseSession? TopSession => sessions.Count == 0 ? null : sessions[0];

		readonly IWpfTextView wpfTextView;
		readonly ObservableCollection<IIntellisenseSession> sessions;
		readonly CommandTargetFilter commandTargetFilter;
		readonly List<SessionState> sessionStates;
		readonly DispatcherTimer clearOpacityTimer;

		const double clearOpacityIntervalMilliSecs = 250;

		sealed class SessionState {
			public IIntellisenseSession Session { get; }
			public ISpaceReservationManager? SpaceReservationManager { get; private set; }
			public ISpaceReservationAgent? SpaceReservationAgent;
			public IPopupIntellisensePresenter? PopupIntellisensePresenter { get; set; }
			public SessionState(IIntellisenseSession session) => Session = session;
			public void SetSpaceReservationManager(ISpaceReservationManager manager) {
				if (SpaceReservationManager is not null)
					throw new InvalidOperationException();
				SpaceReservationManager = manager ?? throw new ArgumentNullException(nameof(manager));
			}
		}

		public IntellisenseSessionStack(IWpfTextView wpfTextView) {
			this.wpfTextView = wpfTextView ?? throw new ArgumentNullException(nameof(wpfTextView));
			sessions = new ObservableCollection<IIntellisenseSession>();
			commandTargetFilter = new CommandTargetFilter(this);
			sessionStates = new List<SessionState>();
			clearOpacityTimer = new DispatcherTimer(DispatcherPriority.Background, wpfTextView.VisualElement.Dispatcher);
			clearOpacityTimer.Interval = TimeSpan.FromMilliseconds(clearOpacityIntervalMilliSecs);
			clearOpacityTimer.Tick += ClearOpacityTimer_Tick;
			Sessions = new ReadOnlyObservableCollection<IIntellisenseSession>(sessions);
			wpfTextView.Closed += WpfTextView_Closed;
			wpfTextView.VisualElement.KeyDown += VisualElement_KeyDown;
			wpfTextView.VisualElement.KeyUp += VisualElement_KeyUp;
		}

		void ClearOpacityTimer_Tick(object? sender, EventArgs e) {
			clearOpacityTimer.Stop();
			if (wpfTextView.IsClosed)
				return;
			SetOpacity(0.3);
		}

		void VisualElement_KeyUp(object? sender, KeyEventArgs e) {
			if (wpfTextView.IsClosed)
				return;
			if (clearOpacityTimer.IsEnabled)
				StopClearOpacityTimer();
			else
				SetOpacity(1);
		}

		void VisualElement_KeyDown(object? sender, KeyEventArgs e) {
			if (wpfTextView.IsClosed)
				return;
			var key = e.Key == Key.System ? e.SystemKey : e.Key;
			bool isCtrl = key == Key.LeftCtrl || key == Key.RightCtrl;
			if (isCtrl && e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
				if (!clearOpacityTimer.IsEnabled)
					clearOpacityTimer.Start();
			}
			else
				StopClearOpacityTimer();
		}

		void StopClearOpacityTimer() {
			if (!clearOpacityTimer.IsEnabled)
				return;
			clearOpacityTimer.Stop();
			SetOpacity(1);
		}

		void SetOpacity(double opacity) {
			bool newIsInClearOpacityMode = opacity != 1;
			if (isInClearOpacityMode == newIsInClearOpacityMode)
				return;
			isInClearOpacityMode = newIsInClearOpacityMode;
			foreach (var session in sessions.ToArray()) {
				if (session.Presenter is IPopupIntellisensePresenter popupPresenter)
					popupPresenter.Opacity = opacity;
			}
		}
		bool isInClearOpacityMode;

		bool ExecuteKeyboardCommand(IntellisenseKeyboardCommand command) {
			foreach (var session in sessions) {
				if ((session.Presenter as IIntellisenseCommandTarget)?.ExecuteKeyboardCommand(command) == true)
					return true;
			}
			return false;
		}

		public void PushSession(IIntellisenseSession session) {
			if (wpfTextView.IsClosed)
				throw new InvalidOperationException();
			if (session is null)
				throw new ArgumentNullException(nameof(session));
			if (sessions.Contains(session))
				throw new InvalidOperationException();
			if (sessions.Count == 0)
				commandTargetFilter.HookKeyboard();
			sessions.Insert(0, session);
			session.Dismissed += Session_Dismissed;
			session.PresenterChanged += Session_PresenterChanged;
			PresenterUpdated(session);
		}

		public IIntellisenseSession? PopSession() {
			if (wpfTextView.IsClosed)
				throw new InvalidOperationException();
			if (sessions.Count == 0)
				return null;
			var session = sessions[0];
			RemoveSessionAt(0);
			return session;
		}

		public void MoveSessionToTop(IIntellisenseSession session) {
			if (wpfTextView.IsClosed)
				throw new InvalidOperationException();
			if (session is null)
				throw new ArgumentNullException(nameof(session));
			int index = sessions.IndexOf(session);
			if (index < 0)
				throw new InvalidOperationException();
			if (index == 0)
				return;
			sessions.Move(index, 0);
		}

		public void CollapseAllSessions() {
			if (wpfTextView.IsClosed)
				throw new InvalidOperationException();
			CollapseAllSessionsCore();
		}

		void CollapseAllSessionsCore() {
			var allSessions = sessions.ToArray();
			for (int i = allSessions.Length - 1; i >= 0; i--)
				allSessions[i].Collapse();
		}

		void Session_PresenterChanged(object? sender, EventArgs e) {
			if (wpfTextView.IsClosed)
				return;
			PresenterUpdated((IIntellisenseSession)sender!);
		}

		int GetSessionStateIndex(IIntellisenseSession session) {
			for (int i = 0; i < sessionStates.Count; i++) {
				if (sessionStates[i].Session == session)
					return i;
			}
			return -1;
		}

		SessionState GetSessionState(IIntellisenseSession session) {
			int index = GetSessionStateIndex(session);
			if (index >= 0)
				return sessionStates[index];

			var sessionState = new SessionState(session);
			sessionStates.Add(sessionState);
			return sessionState;
		}

		SessionState? TryGetSessionState(ISpaceReservationAgent agent) {
			foreach (var sessionState in sessionStates) {
				if (sessionState.SpaceReservationAgent == agent)
					return sessionState;
			}
			return null;
		}

		SessionState? TryGetSessionState(IPopupIntellisensePresenter popupPresenter) {
			foreach (var sessionState in sessionStates) {
				if (sessionState.PopupIntellisensePresenter == popupPresenter)
					return sessionState;
			}
			return null;
		}

		void PresenterUpdated(IIntellisenseSession session) {
			var sessionState = GetSessionState(session);
			if (sessionState.SpaceReservationAgent is not null)
				sessionState.SpaceReservationManager!.RemoveAgent(sessionState.SpaceReservationAgent);
			Debug2.Assert(sessionState.SpaceReservationAgent is null);

			var presenter = session.Presenter;
			if (presenter is IPopupIntellisensePresenter popupPresenter) {
				if (sessionState.SpaceReservationManager is null) {
					sessionState.SetSpaceReservationManager(wpfTextView.GetSpaceReservationManager(popupPresenter.SpaceReservationManagerName));
					Debug2.Assert(sessionState.SpaceReservationManager is not null);
					sessionState.SpaceReservationManager.AgentChanged += SpaceReservationManager_AgentChanged;
				}
				UnregisterPopupIntellisensePresenterEvents(sessionState.PopupIntellisensePresenter);
				sessionState.PopupIntellisensePresenter = popupPresenter;
				RegisterPopupIntellisensePresenterEvents(sessionState.PopupIntellisensePresenter);

				var presentationSpan = popupPresenter.PresentationSpan;
				var surfaceElement = popupPresenter.SurfaceElement;
				if (presentationSpan is not null && surfaceElement is not null) {
					sessionState.SpaceReservationAgent = sessionState.SpaceReservationManager.CreatePopupAgent(presentationSpan, popupPresenter.PopupStyles, surfaceElement);
					sessionState.SpaceReservationManager.AddAgent(sessionState.SpaceReservationAgent);
				}
			}
			else {
				if (presenter is ICustomIntellisensePresenter customPresenter)
					customPresenter.Render();
				else
					Debug2.Assert(presenter is null, $"Unsupported presenter: {presenter?.GetType()}");
			}
		}

		void RegisterPopupIntellisensePresenterEvents(IPopupIntellisensePresenter? popupPresenter) {
			if (popupPresenter is not null) {
				popupPresenter.PopupStylesChanged += PopupIntellisensePresenter_PopupStylesChanged;
				popupPresenter.PresentationSpanChanged += PopupIntellisensePresenter_PresentationSpanChanged;
				popupPresenter.SurfaceElementChanged += PopupIntellisensePresenter_SurfaceElementChanged;
			}
		}

		void UnregisterPopupIntellisensePresenterEvents(IPopupIntellisensePresenter? popupPresenter) {
			if (popupPresenter is not null) {
				popupPresenter.PopupStylesChanged -= PopupIntellisensePresenter_PopupStylesChanged;
				popupPresenter.PresentationSpanChanged -= PopupIntellisensePresenter_PresentationSpanChanged;
				popupPresenter.SurfaceElementChanged -= PopupIntellisensePresenter_SurfaceElementChanged;
			}
		}

		void PopupIntellisensePresenter_SurfaceElementChanged(object? sender, EventArgs e) =>
			PopupIntellisensePresenter_PropertyChanged((IPopupIntellisensePresenter)sender!, nameof(IPopupIntellisensePresenter.SurfaceElement));

		void PopupIntellisensePresenter_PresentationSpanChanged(object? sender, EventArgs e) =>
			PopupIntellisensePresenter_PropertyChanged((IPopupIntellisensePresenter)sender!, nameof(IPopupIntellisensePresenter.PresentationSpan));

		void PopupIntellisensePresenter_PopupStylesChanged(object? sender, ValueChangedEventArgs<PopupStyles> e) =>
			PopupIntellisensePresenter_PropertyChanged((IPopupIntellisensePresenter)sender!, nameof(IPopupIntellisensePresenter.PopupStyles));

		void PopupIntellisensePresenter_PropertyChanged(IPopupIntellisensePresenter popupPresenter, string propertyName) {
			if (wpfTextView.IsClosed) {
				UnregisterPopupIntellisensePresenterEvents(popupPresenter);
				return;
			}
			var sessionState = TryGetSessionState(popupPresenter);
			Debug2.Assert(sessionState is not null);
			if (sessionState is null)
				return;
			if (propertyName == nameof(popupPresenter.PresentationSpan) || propertyName == nameof(popupPresenter.PopupStyles)) {
				var presentationSpan = popupPresenter.PresentationSpan;
				if (presentationSpan is null || sessionState.SpaceReservationAgent is null)
					PresenterUpdated(popupPresenter.Session);
				else
					sessionState.SpaceReservationManager!.UpdatePopupAgent(sessionState.SpaceReservationAgent, presentationSpan, popupPresenter.PopupStyles);
			}
			else if (propertyName == nameof(popupPresenter.SurfaceElement))
				PresenterUpdated(popupPresenter.Session);
		}

		void SpaceReservationManager_AgentChanged(object? sender, SpaceReservationAgentChangedEventArgs e) {
			if (wpfTextView.IsClosed)
				return;
			var sessionState = TryGetSessionState(e.OldAgent);
			if (sessionState is not null) {
				sessionState.SpaceReservationAgent = null;
				// Its popup was hidden, so dismiss the session
				sessionState.Session.Dismiss();
			}
		}

		void Session_Dismissed(object? sender, EventArgs e) {
			var session = sender as IIntellisenseSession;
			Debug2.Assert(session is not null);
			if (session is null)
				return;
			int index = sessions.IndexOf(session);
			Debug.Assert(index >= 0);
			if (index < 0)
				return;
			RemoveSessionAt(index);
		}

		void RemoveSessionAt(int index) {
			Debug.Assert(sessionStates.Count <= sessions.Count);
			var session = sessions[index];
			sessions.RemoveAt(index);
			session.Dismissed -= Session_Dismissed;
			session.PresenterChanged -= Session_PresenterChanged;
			var sessionState = GetSessionState(session);
			if (sessionState.SpaceReservationAgent is not null)
				sessionState.SpaceReservationManager!.RemoveAgent(sessionState.SpaceReservationAgent);
			if (sessionState.SpaceReservationManager is not null)
				sessionState.SpaceReservationManager.AgentChanged -= SpaceReservationManager_AgentChanged;
			UnregisterPopupIntellisensePresenterEvents(sessionState.PopupIntellisensePresenter);
			sessionStates.Remove(sessionState);
			if (sessions.Count == 0) {
				Debug.Assert(sessionStates.Count == 0);
				commandTargetFilter.UnhookKeyboard();
			}
		}

		void WpfTextView_Closed(object? sender, EventArgs e) {
			clearOpacityTimer.Stop();
			CollapseAllSessionsCore();
			while (sessions.Count > 0)
				RemoveSessionAt(sessions.Count - 1);
			commandTargetFilter.Destroy();
			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.VisualElement.KeyDown -= VisualElement_KeyDown;
			wpfTextView.VisualElement.KeyUp -= VisualElement_KeyUp;
		}
	}
}
