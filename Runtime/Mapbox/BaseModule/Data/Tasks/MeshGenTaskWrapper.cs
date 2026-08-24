using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mapbox.BaseModule.Data.Tasks
{
    public class MeshGenTaskWrapper<T> : TaskWrapper where T : MeshGenTaskWrapperResult, new()
    {
        public T DataResult;
        public Func<T> DataAction;
        public Action<Task, T> DataCompleted;

        public override void Action()
        {
            DataResult = DataAction();
        }

        public override void Completed(Task task)
        {
            if (task != null && !task.IsFaulted)
            {
                DataCompleted(Task.CompletedTask, DataResult);
                IsCompleted = true;
                return;
            }

            if (task == null)
            {
                DataResult ??= new T();
                DataResult.ResultType = TaskResultType.Cancelled;
                DataCompleted(null, DataResult);
            }
            else
            {
                DataResult ??= new T();
                DataResult.ResultType = TaskResultType.MeshGenerationFailure;
                DataCompleted(task, DataResult);
            }
            IsCompleted = true;
        }
    }
    
    public class MeshGenTaskWrapperResult : TaskResult
    {
        public TaskResultType ResultType;
        public Dictionary<string, Dictionary<int, HashSet<MeshData>>> Data;
		
		
        public MeshGenTaskWrapperResult()
        {
            Data = new Dictionary<string, Dictionary<int, HashSet<MeshData>>>();
        }
    }
	
    public enum TaskResultType
    {
        DataProcessingFailure,
        MeshGenerationFailure,
        GameObjectFailure,

        /// <summary>
        /// This call generated the visual, and it is carrying the result. The only member
        /// that implies GeneratedObjects is populated.
        /// </summary>
        Success,
        Cancelled,

        /// <summary>
        /// The visual already existed, so this call did no work and carries no objects - the
        /// ones that exist belong to whichever call generated them. The tile IS usable, so a
        /// caller that only needs it present can proceed. Distinct from Success because
        /// GeneratedObjects is null here, and consumers key off that to mean "no visual for
        /// this tile" (see LandmarksLayerModule's generation callback) - reporting Success
        /// would have them cache a null visual over a tile that has a perfectly good one.
        /// </summary>
        AlreadyGenerated,

        /// <summary>
        /// Generation for this tile is already in flight, owned by an earlier caller. The
        /// visual is NOT ready and this result says nothing about whether it will ever
        /// succeed - the owning call reports that outcome to its own callback, not to this
        /// one. A caller must not treat this as completion or start work that depends on the
        /// tile being finished. It only means "you don't need to start this yourself."
        /// </summary>
        AlreadyInProgress
    }
}