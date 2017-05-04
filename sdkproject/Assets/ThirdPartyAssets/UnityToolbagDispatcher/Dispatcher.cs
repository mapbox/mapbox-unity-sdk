using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;

namespace UnityToolbag {
	/// <summary>
	/// A system for dispatching code to execute on the main thread.
	/// </summary>
	[AddComponentMenu("UnityToolbag/Dispatcher")]
	[ExecuteInEditMode]
	//[UnityEditor.InitializeOnLoad]
	public class Dispatcher : MonoBehaviour {

		private static Dispatcher _instance;

		// We can't use the behaviour reference from other threads, so we use a separate bool
		// to track the instance so we can use that on the other threads.
		private static bool _instanceExists;

		private static bool _enabled;

		private static Thread _mainThread;
		private static object _lockObject = new object();
		private static readonly Queue<Action> _actions = new Queue<Action>();

		//public Dispatcher() {
		//	UnityEngine.Debug.Log("Dispatcher()");
		//}

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

			UnityEngine.Debug.Log(string.Format(
				"InvokeAsync, enabled:{0} instanceExists:{1} mainthread:{2}"
				, _enabled
				, _instanceExists
				, null == _mainThread ? "NULL" : _mainThread.ManagedThreadId.ToString()
			));
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

			UnityEngine.Debug.Log(string.Format("Invoke, enabled:{0} instanceExists:{1}", _enabled, _instanceExists));

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
#if !NETFX_CORE
				Thread.Sleep(5);
#else
				System.Threading.Tasks.Task.Delay(5).Wait();
#endif
			}
		}

		void Awake() {
			System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			for (int i = 0; i < st.FrameCount; i++) {
				System.Diagnostics.StackFrame sf = st.GetFrame(i);
				sb.AppendLine(sf.GetMethod().ToString() + " " + sf.GetFileName() + " " + sf.GetFileLineNumber() + " " + sf.GetFileColumnNumber());
			}
			UnityEngine.Debug.Log("Awake()" + Environment.NewLine + sb.ToString());

#if UNITY_EDITOR
			if (Application.isEditor && !Application.isPlaying) {
				UnityEngine.Debug.Log("Awake(), hooking into EditorUpdate");
				UnityEditor.EditorApplication.update += EditorUpdate;
			}
#endif
			if (_instance) {
				UnityEngine.Debug.Log("Awake() destroying instance");
				DestroyImmediate(this);
			} else {
				UnityEngine.Debug.Log("Awake() creating instance");
				_instance = this;
				//_instance = Mapbox.Unity.Utilities.Singleton<Dispatcher>.Instance;
				_instanceExists = true;
				_mainThread = Thread.CurrentThread;
			}

			UnityEngine.Debug.Log(string.Format(
				"Awake(), enabled:{0} instanceExists:{1} mainthread:{2}"
				, _enabled
				, _instanceExists
				, null == _mainThread ? "NULL" : _mainThread.ManagedThreadId.ToString()
			));
		}


		void OnDestroy() {
			UnityEngine.Debug.Log("OnDestroy()");
#if UNITY_EDITOR
			if (Application.isEditor && !Application.isPlaying) {
				UnityEngine.Debug.Log("Awake(), disconnecting EditorUpdate");
				UnityEditor.EditorApplication.update -= EditorUpdate;
			}
#endif
			if (_instance == this) {
				_instance = null;
				_instanceExists = false;
			}
		}


		private void Update() {
			UnityEngine.Debug.Log(string.Format("Update(), enabled:{0} instanceExists:{1}", _enabled, _instanceExists));
			MyUpdate();
		}


		private void EditorUpdate() {
			UnityEngine.Debug.Log(string.Format("EditorUpdate(), enabled:{0} instanceExists:{1}", _enabled, _instanceExists));
			MyUpdate();
		}

		void MyUpdate() {
			lock (_lockObject) {
				while (_actions.Count > 0) {
					_actions.Dequeue()();
				}
			}
		}


		//private void OnEnable() {
		//	UnityEngine.Debug.Log("======== OnEnable() ==========");
		//	_enabled = true;
		//}


	}
}
