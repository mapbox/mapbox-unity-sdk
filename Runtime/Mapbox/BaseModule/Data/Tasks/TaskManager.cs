using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.BaseModule.Data.Tasks
{
	public class TaskManager
	{
		public Action<TaskWrapper> TaskStarting = (t) => { };
		public Action<TaskWrapper> TaskFinished = (t) => { };
		public Action<TaskWrapper> TaskCancelled = (t) => { };
		/// <summary>
		/// How many tasks may run at once. Defaulted from the processor count in the constructor
		/// rather than fixed, since the same number oversubscribed a four-core phone and left a
		/// desktop idle. Assign after construction to override.
		/// </summary>
		public int ActiveTaskLimit;

		protected const float _requestDelay = 0.2f;

		/// <summary>
		/// A task waiting longer than this is dispatched ahead of higher-priority work. Mesh
		/// generation enqueues at priority 0 and cache writes at 3-4, so without it a map that keeps
		/// generating tiles can leave cache persistence queued behind them indefinitely.
		/// </summary>
		public float StarvationThresholdSeconds = 2f;

		/// <summary>
		/// Per-priority dispatch delay. The debounce exists so a tile the camera has already left is
		/// cancelled before its network request is spent on it. Queue 0 carries mesh generation,
		/// which works on data already in hand, so making it wait only delays visuals.
		/// </summary>
		private static readonly float[] _queueDispatchDelay =
			{ 0f, _requestDelay, _requestDelay, _requestDelay, _requestDelay };
		
		protected HashSet<TaskWrapper> _runningTasks;
		protected List<Queue<TaskWrapper>> _taskQueue;
		private bool _isDestroying = false;
		private CancellationTokenSource _globalCancellationTokenSource;

		// Per instance, not static: two maps in one process were serializing their enqueues against
		// each other for no reason.
		private readonly object _lock = new object();

		public TaskManager()
		{
			// Two per core, floored so a low-core device still overlaps work, capped so a large
			// desktop does not run more concurrent mesh generation than a map has use for.
			ActiveTaskLimit = Mathf.Clamp(SystemInfo.processorCount * 2, 4, 16);
			_runningTasks = new HashSet<TaskWrapper>();
			_globalCancellationTokenSource = new CancellationTokenSource();
			_taskQueue = new List<Queue<TaskWrapper>>()
			{
				new Queue<TaskWrapper>(),
				new Queue<TaskWrapper>(),
				new Queue<TaskWrapper>(),
				new Queue<TaskWrapper>(),
				new Queue<TaskWrapper>()
			};
		}

		public void Initialize()
		{
			//TODO remove runnable here?
			Runnable.Run(UpdateTaskManager());
		}

		/// <summary>
		/// Picks the next task to run across all priority queues, or returns false if nothing is
		/// ready yet. Three things happen here that a plain highest-priority-first scan did not do:
		/// a queue whose head has not matured is stepped over rather than blocking every lower
		/// queue behind it, a task that has waited past <see cref="StarvationThresholdSeconds"/>
		/// outranks priority, and cancelled work is dropped wherever it sits.
		/// </summary>
		private bool TryTakeNextTask(out TaskWrapper task)
		{
			task = null;
			var now = Time.time;
			var chosen = -1;

			lock (_lock)
			{
				for (int i = 0; i < _taskQueue.Count; i++)
				{
					if (_taskQueue[i].Count == 0)
						continue;

					var head = _taskQueue[i].Peek();

					if (head.IsCancelled)
					{
						chosen = i;
						break;
					}

					if (!HasMatured(head, i, now))
						continue;

					if (chosen < 0)
						chosen = i;

					if (now - head.EnqueueFrame >= StarvationThresholdSeconds)
					{
						chosen = i;
						break;
					}
				}

				if (chosen < 0)
					return false;

				task = _taskQueue[chosen].Dequeue();
			}

			return true;
		}

		private bool HasMatured(TaskWrapper task, int priorityLevel, float now)
		{
			if (!Application.isPlaying)
				return true;

			var delay = priorityLevel < _queueDispatchDelay.Length
				? _queueDispatchDelay[priorityLevel]
				: _requestDelay;

			return now - task.EnqueueFrame >= delay;
		}

		protected IEnumerator UpdateTaskManager()
		{
			while (!_isDestroying)
			{
				while (_runningTasks.Count < ActiveTaskLimit && TryTakeNextTask(out var task))
				{
					if (task.IsCancelled)
					{
						TaskCancelled(task);
						task.Completed(null);
						continue;
					}

					HandleTask(task);
				}

				yield return null;
			}
		}
		
		private void HandleTask(TaskWrapper wrapper)
		{
			OnTaskStarting(wrapper);
			var task = Task.Run(wrapper.Action, _globalCancellationTokenSource.Token);
			_runningTasks.Add(wrapper);
			task.ContinueWith((t) =>
			{
				if (t.IsFaulted)
				{
					// Unwrap before logging: t.Exception is an AggregateException whose own Message
					// is the useless "One or more errors occurred.", and LogException keeps the
					// stack that .Message discards. Faults are reported to the wrapper's own
					// Completed path below, so this only needs to be visible, not fatal - the
					// Debug.Break() that used to live here halted the Editor on any worker throw.
					Exception fault = t.Exception?.GetBaseException() ?? t.Exception;
					if (fault != null) Debug.LogException(fault);
				}
				OnTaskFinished(wrapper);
				_runningTasks.Remove(wrapper);
				wrapper.Completed(task);
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		protected virtual void OnTaskStarting(TaskWrapper task)
		{
			task.StartingTime = Time.time;
			TaskStarting?.Invoke(task);
		}

		protected virtual void OnTaskFinished(TaskWrapper task)
		{
			task.FinishedTime = Time.time;
			TaskFinished?.Invoke(task);
		}

		public virtual void AddTask(TaskWrapper taskWrapper, int priorityLevel = 3)
		{
			if (taskWrapper == null) return;

			lock (_lock)
			{
				if (_isDestroying)
				{
					// Teardown has begun, so nothing will ever run this. Web requests in flight when
					// the app stops still deliver their callbacks, and several of those enqueue
					// bookkeeping work (SqliteCache.UpdateExpiration, for one) - which is why this
					// arrives repeatedly on exit rather than once.
					//
					// Marked completed rather than dropped, so anything polling IsCompleted unwinds
					// instead of spinning. Completion callbacks are deliberately NOT invoked: they
					// would run against objects that are already being destroyed.
					taskWrapper.IsCompleted = true;
					return;
				}

				if (priorityLevel < 0 || priorityLevel >= _taskQueue.Count)
				{
					priorityLevel = Mathf.Clamp(priorityLevel, 0, _taskQueue.Count - 1);
				}

				taskWrapper.EnqueueFrame = Time.time;
				_taskQueue[priorityLevel].Enqueue(taskWrapper);
				//Debug.Log(taskWrapper.Info);
			}
		}
		
		public void OnDestroy()
		{
			_globalCancellationTokenSource.Cancel();
			_isDestroying = true;

			lock (_lock)
			{
				// Drain the queues, keep the list OF queues. Clearing the list itself left
				// _taskQueue.Count at zero, so anything enqueued afterwards indexed an empty list and
				// threw - and the queue slots are structural, created once in the constructor, not
				// per-session state.
				foreach (var queue in _taskQueue)
				{
					foreach (var pending in queue)
					{
						// Release pollers without running their callbacks, as AddTask does.
						if (pending != null) pending.IsCompleted = true;
					}

					queue.Clear();
				}
			}

			TaskStarting = null;
			TaskFinished = null;
			TaskCancelled = null;
		}
	}
}