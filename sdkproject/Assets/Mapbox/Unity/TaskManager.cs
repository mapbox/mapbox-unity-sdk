using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mapbox.Map;
using Mapbox.Unity.Utilities;
using Unity.UNetWeaver;

namespace Mapbox.Unity
{
	public class TaskManager
    {
        public Action<TaskWrapper> TaskStarted = (t) => { };
		public int ActiveTaskLimit = 3;
		protected HashSet<TaskWrapper> _runningTasks;
		protected Queue<TaskWrapper> _taskQueue;
        //protected PriorityQueue<TaskWrapper, int> _taskPriorityQueue;

		public TaskManager()
		{
			_runningTasks = new HashSet<TaskWrapper>();
			_taskQueue = new Queue<TaskWrapper>();
            //_taskPriorityQueue = new PriorityQueue<TaskWrapper, int>();
			Runnable.Run(UpdateTaskManager());
		}

		public IEnumerator UpdateTaskManager()
		{
			while (true)
			{
				while (_taskQueue.Count > 0 && _runningTasks.Count < ActiveTaskLimit)
				{
					var wrapper = _taskQueue.Dequeue();
					var task = Task.Run(wrapper.Action);
					_runningTasks.Add(wrapper);
					wrapper.Cancelled += (w) =>
					{
						_runningTasks.Remove(w);
					};
					task.ContinueWith((t) => { ContinueWrapper(t, wrapper); }, TaskScheduler.FromCurrentSynchronizationContext());
                    TaskStarted(wrapper);
					yield return null;

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
			taskWrapper.Finished(taskWrapper);
			if (taskWrapper.ContinueWith != null)
			{
				taskWrapper.ContinueWith(task);
			}
			_runningTasks.Remove(taskWrapper);
		}

		public void AddTask(TaskWrapper taskWrapper, int priority = 5)
		{
			_taskQueue.Enqueue(taskWrapper);
            //_taskPriorityQueue.Enqueue(taskWrapper, priority);
        }
	}

	public class TaskWrapper
	{
		public Action<TaskWrapper> Cancelled = (t) => { };
		public Action<TaskWrapper> Finished = (t) => { };
		public CanonicalTileId TileId;
		public Action Action;
		public CancellationTokenSource Token;
		public Action<Task> ContinueWith;

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