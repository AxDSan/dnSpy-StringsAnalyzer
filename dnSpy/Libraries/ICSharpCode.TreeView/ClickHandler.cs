using System;
using System.Timers;
using System.Windows;

namespace ICSharpCode.TreeView {
	public class ClickHandler<T> {
		readonly int delay;
		Timer timer;
		int click;
		Action<T> action;
		T context;

		public ClickHandler(int delay = 300) => this.delay = delay;
		public void UpdateContext(T context) => this.context = context;

		void RunAction() => Application.Current.Dispatcher.BeginInvoke((Action)(() => {
			timer?.Stop();
			timer = null;
			action?.Invoke(context);
			action = null;
		}));

		public void MouseDown(T context) {
			this.context = context;
			click = timer == null ? 1 : click + 1;
			if (click == 1) {
				timer = new Timer { Interval = delay };
				action = null;
				timer.Elapsed += (sender, e) => { RunAction(); };
				timer?.Start();
			}
		}

		public void MouseUp(Action<T> singleClickAction, Action<T> doubleClickAction) {
			action = click == 1 ? singleClickAction : doubleClickAction;
			if (timer == null)
				action(context);
			if (timer != null && click == 2)
				RunAction();
		}
	}
}
