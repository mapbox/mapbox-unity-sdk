using System;
using System.Threading;
using System.Threading.Tasks;
using Mapbox.BaseModule.Data.Tiles;

namespace Mapbox.BaseModule.Data.Tasks
{
    public abstract class TaskWrapper
    {
        public float EnqueueFrame;
        public float StartingTime;
        public float FinishedTime;
        public CanonicalTileId TileId;
        
        public bool IsCancelled { get; private set; }
        public bool IsCompleted { get; set; }

        public void Cancel() { IsCancelled = true; }

        public abstract void Action();
        public abstract void Completed(Task task);
    }
    
    /// <summary>
    /// How a <see cref="DataTaskWrapper{T}"/> finished. Needed because DataResult is null in three
    /// unrelated situations and consumers were left to guess which one they had: the task was
    /// cancelled before it ran, the action threw, or the action ran fine and genuinely found
    /// nothing (a cache miss - SqliteCache.Get returns null for a row that isn't there). Only the
    /// last of those is an ordinary answer, and the caller that mattered dereferenced all three.
    /// </summary>
    public enum DataTaskOutcome
    {
        /// <summary>The action ran to completion. DataResult is whatever it produced, which may
        /// legitimately be null - that means "ran, found nothing", not "went wrong".</summary>
        Completed,

        /// <summary>Cancelled before the action ran, so DataResult was never assigned.</summary>
        Cancelled,

        /// <summary>The action threw. TaskManager logs the exception; DataResult is unusable.</summary>
        Faulted
    }

    public class DataTaskWrapper<T> : TaskWrapper
    {
        public T DataResult;
        public Func<T> DataAction;
        public Action<Task, T> DataCompleted;

        /// <summary>
        /// Set before DataCompleted fires, so a subscriber reading it from inside the callback
        /// always sees this task's own outcome.
        /// </summary>
        public DataTaskOutcome Outcome { get; private set; } = DataTaskOutcome.Completed;

        public override void Action()
        {
            DataResult = DataAction();
        }

        public override void Completed(Task task)
        {
            IsCompleted = true;

            // TaskManager signals a queue-time cancellation by passing a null task, and a thrown
            // action through task.IsFaulted. The sibling MeshGenTaskWrapper already distinguished
            // both; this one used to ignore them and report every case identically.
            Outcome = task == null
                ? DataTaskOutcome.Cancelled
                : task.IsFaulted
                    ? DataTaskOutcome.Faulted
                    : DataTaskOutcome.Completed;

            if (DataCompleted != null)
            {
                DataCompleted(task, DataResult);
            }
        }
    }

    public class FileCacheTaskWrapper<T> : DataTaskWrapper<T>
    {
        
    }
    
    public class SqliteCacheAddTaskWrapper<T> : DataTaskWrapper<T>
    {
        
    }
    
    public class SqliteCacheUpdateExpirationTaskWrapper<T> : DataTaskWrapper<T>
    {
        
    }
}