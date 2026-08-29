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
		public int ActiveTaskLimit = 10;
		protected const float _requestDelay = 0.2f;
		
		protected HashSet<TaskWrapper> _runningTasks;
		protected List<Queue<TaskWrapper>> _taskQueue;
		private bool _isDestroying = false;
		private CancellationTokenSource _globalCancellationTokenSource;

		private static object _lock = new object();

		public TaskManager()
		{
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

		private bool TaskQueueAny()
		{
			foreach (var queue in _taskQueue)
			{
				if (queue.Count != 0)
					return true;
			}

			return false;
		}

		private TaskWrapper TaskQueuePeek()
		{
			foreach (var queue in _taskQueue)
			{
				if (queue.Count != 0)
				{
					return queue.Peek();
				}
			}

			return null;
		}

		private TaskWrapper TaskQueueDequeue()
		{
			foreach (var queue in _taskQueue)
			{
				if (queue.Count != 0)
				{
					return queue.Dequeue();
				}
			}

			return null;
		}

		protected IEnumerator UpdateTaskManager()
		{
			while (!_isDestroying)
			{
				while (TaskQueueAny() && _runningTasks.Count < ActiveTaskLimit)
				{
					var task = TaskQueuePeek();
					if (task.IsCancelled)
					{
						TaskQueueDequeue();
						TaskCancelled(task);
						task.Completed(null);
						continue;
					}
					else if (QueueTimeHasMatured(task.EnqueueFrame, _requestDelay) || !Application.isPlaying)
					{
						TaskQueueDequeue();
						HandleTask(task);
					}
					else
					{
						yield return null;
					}
				}
				yield return null;
			}
		}
		
		private bool QueueTimeHasMatured(float queueTime, float maturationAge)
		{
			return Time.time - queueTime >= maturationAge;
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