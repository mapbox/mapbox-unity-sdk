using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Utilities;
using UnityEngine;

namespace Mapbox.Unity
{
	public class TaskLevel
	{


	}

	public class TaskManager
	{
		public Action<TaskWrapper> TaskStarted = (t) => { };
		public Action<CanonicalTileId> TaskCancelled = (t) => { };
		public int ActiveTaskLimit = 3;
		protected HashSet<TaskWrapper> _runningTasks;

		protected Dictionary<int, TaskWrapper> _allTasks;
		public Queue<int> _taskQueue;

		// protected Dictionary<int, TaskWrapper> _tasksInQueue;
		// protected Queue<int> _taskQueue;
		protected Dictionary<CanonicalTileId, HashSet<int>> _tasksByTile = new Dictionary<CanonicalTileId, HashSet<int>>();
		//protected PriorityQueue<TaskWrapper, int> _taskPriorityQueue;

		private static object _lock = new object();

		public TaskManager()
		{
			_runningTasks = new HashSet<TaskWrapper>();
			_taskQueue = new Queue<int>();
			_allTasks = new Dictionary<int, TaskWrapper>();

			_tasksByTile = new Dictionary<CanonicalTileId, HashSet<int>>();
			//_taskPriorityQueue = new PriorityQueue<TaskWrapper, int>();
			Runnable.Run(UpdateTaskManager());
		}

		public IEnumerator UpdateTaskManager()
		{
			while (true)
			{
				while (_taskQueue.Count > 0 && _runningTasks.Count <= ActiveTaskLimit)
				{
					var firstPeek = _taskQueue.Peek();
					if (_allTasks.ContainsKey(firstPeek) &&
						_allTasks[firstPeek].EnqueueFrame > Time.frameCount - 15)
					{
						yield return null;
					}
					else
					{
						var wrapperId = _taskQueue.Dequeue();
						TaskWrapper wrapper;
						if (!_allTasks.ContainsKey(wrapperId))
						{
							continue;
						}
						else
						{
							wrapper = _allTasks[wrapperId];
							_allTasks.Remove(wrapperId);
							_tasksByTile[wrapper.OwnerTileId].Remove(wrapperId);
							if (_tasksByTile[wrapper.OwnerTileId].Count == 0)
							{
								_tasksByTile.Remove(wrapper.OwnerTileId);
							}
						}

						var task = Task.Run(wrapper.Action);
						_runningTasks.Add(wrapper);
						task.ContinueWith((t) => { ContinueWrapper(t, wrapper); }, TaskScheduler.FromCurrentSynchronizationContext());
						TaskStarted(wrapper);
					}
				}

				yield return null;
			}
		}

		private void ContinueWrapper(Task task, TaskWrapper taskWrapper)
		{
			_runningTasks.Remove(taskWrapper);
			//taskWrapper.Finished(taskWrapper);
			if (taskWrapper.ContinueWith != null)
			{
				taskWrapper.ContinueWith(task);
			}
		}

		public virtual void AddTask(TaskWrapper taskWrapper, int priorityLevel = 3)
		{
			lock (_lock)
			{
				if (taskWrapper != null)
				{
					if (!_allTasks.ContainsKey(taskWrapper.Id))
					{
						taskWrapper.EnqueueFrame = Time.frameCount;
						_allTasks.Add(taskWrapper.Id, taskWrapper);

						if (!_tasksByTile.ContainsKey(taskWrapper.OwnerTileId))
						{
							_tasksByTile.Add(taskWrapper.OwnerTileId, new HashSet<int>());
						}
						_tasksByTile[taskWrapper.OwnerTileId].Add(taskWrapper.Id);
						_taskQueue.Enqueue(taskWrapper.Id);
					}
					else
					{
						_allTasks.Remove(taskWrapper.Id);
						if (_tasksByTile.ContainsKey(taskWrapper.OwnerTileId))
						{
							_tasksByTile[taskWrapper.OwnerTileId].Remove(taskWrapper.Id);
							if (_tasksByTile[taskWrapper.OwnerTileId].Count == 0)
							{
								_tasksByTile.Remove(taskWrapper.OwnerTileId);
							}
						}
						else
						{
							Debug.Log(taskWrapper.TileId);
						}

						taskWrapper.EnqueueFrame = Time.frameCount;
						_allTasks.Add(taskWrapper.Id, taskWrapper);

						if (!_tasksByTile.ContainsKey(taskWrapper.OwnerTileId))
						{
							_tasksByTile.Add(taskWrapper.OwnerTileId, new HashSet<int>());
						}
						_tasksByTile[taskWrapper.OwnerTileId].Add(taskWrapper.Id);
						_taskQueue.Enqueue(taskWrapper.Id);
					}
				}
			}

			//_taskPriorityQueue.Enqueue(taskWrapper, priority);
		}

		public virtual void CancelTile(CanonicalTileId cancelledTileId)
		{
			if (_tasksByTile.ContainsKey(cancelledTileId))
			{
				foreach (var taskId in _tasksByTile[cancelledTileId])
				{
					if (_allTasks.ContainsKey(taskId))
					{
						var task = _allTasks[taskId];
						TaskCancelled(cancelledTileId);
						_allTasks.Remove(taskId);
						task.OnCancelled?.Invoke();
					}
				}

				_tasksByTile.Remove(cancelledTileId);
			}
		}
	}

	public class TaskWrapper
	{
		public int Id;
		public string TilesetId;

		public int EnqueueFrame;
		// public Action<TaskWrapper> Cancelled = (t) => { };
		// public Action<TaskWrapper> Finished = (t) => { };
		public CanonicalTileId TileId;
		public CanonicalTileId OwnerTileId;
		public Action Action;
		public CancellationTokenSource Token;
		public Action<Task> ContinueWith;
		public Action OnCancelled;

		public TaskWrapper(int id)
		{
			Id = id;
		}

		public string Info;

	}

	public class EditorTaskManager : TaskManager
	{
		public bool EnableLogging = false;
		public int TotalTaskEnqueuedCount;
		public int TotalCancelledCount;
		public List<string> Logs = new List<string>();

		public int ActiveTaskCount => _runningTasks.Count;
		public int TaskQueueSize => _taskQueue.Count; //_taskQueue.Count;
		public int TasksInQueue => _taskQueue.Count;

		public Dictionary<string, int> TaskType = new Dictionary<string, int>();

		public EditorTaskManager()
		{
			base.TaskStarted += (t) =>
			{
				if (EnableLogging)
				{
					Logs.Add(Time.frameCount + " - " + t.Info);

					if (!TaskType.ContainsKey(t.Info))
					{
						TaskType.Add(t.Info, 0);
					}

					TaskType[t.Info]++;
				}
			};
		}

		public override void AddTask(TaskWrapper taskWrapper, int priorityLevel = 3)
		{
			if (EnableLogging)
			{
				TotalTaskEnqueuedCount++;
				Logs.Add(string.Format("{0,-10} {2,-10} {1, -30}", Time.frameCount, "added", taskWrapper.Info));
			}
			base.AddTask(taskWrapper, priorityLevel);
		}

		public override void CancelTile(CanonicalTileId cancelledTileId)
		{
			if (EnableLogging)
			{
				var taskCount = 0;
				var tileTypes = "";
				if (_tasksByTile.ContainsKey(cancelledTileId))
				{
					taskCount = _tasksByTile[cancelledTileId].Count;
					tileTypes = string.Join(" | ", _tasksByTile[cancelledTileId].Select(x => _allTasks[x].Info));
				}

				Logs.Add(string.Format("{0,-10} {1,-15} {2,-30}; ({3}) {4}", Time.frameCount, cancelledTileId, "cancel", taskCount, tileTypes));
				TotalCancelledCount += taskCount;
			}

			base.CancelTile(cancelledTileId);
		}

		public void ClearLogsAndStats()
		{
			TotalTaskEnqueuedCount = 0;
			TotalCancelledCount = 0;
			TaskType.Clear();
			Logs.Clear();
		}

		public void ToggleLogging()
		{
			EnableLogging = !EnableLogging;
		}
	}
}