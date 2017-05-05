namespace Mapbox.Unity {


	using System;
	using System.Collections.Generic;
	using System.Threading;
	using UnityEditor;


	[InitializeOnLoad]
	public static class DispatcherEditor {


		static DispatcherEditor() {
			_mainThread = Thread.CurrentThread;
			EditorApplication.update += Update;
		}


		private static object _lock = new object();
		private static readonly Queue<Action> _actions = new Queue<Action>();
		private static Thread _mainThread;


		private static bool isMainThread {
			get { return Thread.CurrentThread == _mainThread; }
		}


		public static void Update() {
			lock (_lock) {
				while (_actions.Count > 0) {
					_actions.Dequeue()();
				}
			}
		}


		public static void InvokeAsync(Action action) {
			if (isMainThread) {
				action();
			}else {
				lock (_lock) {
					_actions.Enqueue(action);
				}
			}
		}


	}
}