using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mapbox.Map;
using Mapbox.Unity.Utilities;

namespace Mapbox.Unity
{
	public class TaskManager
	{
		public Action<TaskWrapper> TaskStarted = (t) => { };
		public int ActiveTaskLimit = 3;
		protected HashSet<TaskWrapper> _runningTasks;

		protected Dictionary<int, TaskWrapper> _tasksInQueue;
		protected Queue<int> _taskQueue;
		//protected PriorityQueue<TaskWrapper, int> _taskPriorityQueue;

		public TaskManager()
		{
			_runningTasks = new HashSet<TaskWrapper>();
			_taskQueue = new Queue<int>();
			_tasksInQueue = new Dictionary<int, TaskWrapper>();
			//_taskPriorityQueue = new PriorityQueue<TaskWrapper, int>();
			Runnable.Run(UpdateTaskManager());
		}

		public IEnumerator UpdateTaskManager()
		{
			while (true)
			{
				while (_taskQueue.Count > 0 && _runningTasks.Count < ActiveTaskLimit)
				{
					var wrapperId = _taskQueue.Dequeue();
					TaskWrapper wrapper;
					if (wrapperId == null || !_tasksInQueue.ContainsKey(wrapperId))
					{
						continue;
					}
					else
					{
						wrapper = _tasksInQueue[wrapperId];
						_tasksInQueue.Remove(wrapperId);
					}

					var task = Task.Run(wrapper.Action);
					_runningTasks.Add(wrapper);
					// wrapper.Cancelled += (w) =>
					// {
					// 	_runningTasks.Remove(w);
					// };
					task.ContinueWith((t) =>
					{
						ContinueWrapper(t, wrapper);
					}, TaskScheduler.FromCurrentSynchronizationContext());
					TaskStarted(wrapper);
				}

				// while (_taskPriorityQueue.Count > 0 && _runningTasks.Count < ActiveTaskLimit)
				// {
				//     var wrapper = _taskPriorityQueue.Dequeue();
				//     var task = Task.Run(wrapper.Action);
				//     _runningTasks.Add(wrapper);
				//     wrapper.Cancelled += (w) =>
				//     {
				//         _runningTasks.Remove(w);
				//     };
				//     task.ContinueWith((t) => { ContinueWrapper(t, wrapper); }, TaskScheduler.FromCurrentSynchronizationContext());
				//     TaskStarted(wrapper);
				//     yield return null;
				//
				// }

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

		public void AddTask(TaskWrapper taskWrapper)
		{
			if (taskWrapper != null)
			{
				if (!_tasksInQueue.ContainsKey(taskWrapper.Id))
				{
					_tasksInQueue.Add(taskWrapper.Id, taskWrapper);
					_taskQueue.Enqueue(taskWrapper.Id);
				}
				else
				{
					_tasksInQueue.Remove(taskWrapper.Id);
					_tasksInQueue.Add(taskWrapper.Id, taskWrapper);
					_taskQueue.Enqueue(taskWrapper.Id);
				}
			}
			//_taskPriorityQueue.Enqueue(taskWrapper, priority);
		}

		public void CancelTask(int taskId)
		{
			if (_tasksInQueue.ContainsKey(taskId))
			{
				_tasksInQueue.Remove(taskId);
			}
		}
	}

	public class TaskWrapper
	{
		public int Id;
		// public Action<TaskWrapper> Cancelled = (t) => { };
		// public Action<TaskWrapper> Finished = (t) => { };
		public CanonicalTileId TileId;
		public Action Action;
		public CancellationTokenSource Token;
		public Action<Task> ContinueWith;
		public Action OnCancelled;

		public TaskWrapper(int id)
		{
			Id = id;
		}

#if UNITY_EDITOR
		public string Info;
#endif
	}

	public class EditorTaskManager : TaskManager
	{
		public int ActiveTaskCount => _runningTasks.Count;
		public int TaskQueueSize => _taskQueue.Count; //_taskQueue.Count;
	}
}