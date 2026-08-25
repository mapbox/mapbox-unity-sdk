using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tasks;
using UnityEngine;

namespace Mapbox.LandmarkModule
{
    public class GltfGenerationTaskResult : TaskResult
    {
        public TaskResultType ResultType;
        public bool InvalidateAndRetry = false;
        public IEnumerable<GameObject> GeneratedObjects;
        public List<Vector3[]> FootprintResults;

        public GltfGenerationTaskResult(TaskResultType result, IEnumerable<GameObject> visuals = null, List<Vector3[]> footprintResults = null)
        {
            ResultType = result;
            GeneratedObjects = visuals;
            FootprintResults = footprintResults;
        }
    }
}