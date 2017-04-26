using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;

namespace UnityToolbag {
	/// <summary>
	/// A system for dispatching code to execute on the main thread.
	/// </summary>
	[AddComponentMenu("UnityToolbag/Dispatcher")]
	public class Dispatcher : MonoBehaviour {
		private static Dispatcher _instance;

		// We can't use the behaviour reference from other threads, so we use a separate bool
		// to track the instance so we can use that on the other threads.
		private static bool _instanceExists;

		private static Thread _mainThread;
		private static object _lockObject = new object();
		private static readonly Queue<Action> _actions = new Queue<Action>();

		/// <summary>
		/// Gets a value indicating whether or not the current thread is the game's main thread.
		/// </summary>
		public static bool isMainThread {
			get {
				return Thread.CurrentThread == _mainThread;
			}
		}

		/// <summary>
		/// Queues an action to be invoked on the main game thread.
		/// </summary>
		/// <param name="action">The action to be queued.</param>
		public static void InvokeAsync(Action action) {
			if (!_instanceExists) {
				Debug.LogError("No Dispatcher exists in the scene. Actions will not be invoked!");
				return;
			}

			if (isMainThread) {
				// Don't bother queuing work on the main thread; just execute it.
				action();
			} else {
				lock (_lockObject) {
					_actions.Enqueue(action);
				}
			}
		}

		/// <summary>
		/// Queues an action to be invoked on the main game thread and blocks the
		/// current thread until the action has been executed.
		/// </summary>
		/// <param name="action">The action to be queued.</param>
		public static void Invoke(Action action) {
			if (!_instanceExists) {
				Debug.LogError("No Dispatcher exists in the scene. Actions will not be invoked!");
				return;
			}

			bool hasRun = false;

			InvokeAsync(() => {
				action();
				hasRun = true;
			});

			// Lock until the action has run
			while (!hasRun) {
				Thread.Sleep(5);
			}
		}

		void Awake() {
			if (_instance) {
				DestroyImmediate(this);
			} else {
				_instance = this;
				_instanceExists = true;
				_mainThread = Thread.CurrentThread;
			}
		}

		void OnDestroy() {
			if (_instance == this) {
				_instance = null;
				_instanceExists = false;
			}
		}

		int cnt = 0;
		void Update() {
			//cnt++;
			//if (cnt < 100) { return; }
			//cnt = 0;
			////if (_actions.Count > 0) UnityEngine.Debug.Log("-------------------- update -----------------" + DateTime.Now.Ticks);
			lock (_lockObject) {
				while (_actions.Count > 0) {
					//UnityEngine.Debug.Log("QUEUE: " + _actions.Count + " " + DateTime.Now.Ticks);
					_actions.Dequeue()();
				}
			}
		}
	}
}
