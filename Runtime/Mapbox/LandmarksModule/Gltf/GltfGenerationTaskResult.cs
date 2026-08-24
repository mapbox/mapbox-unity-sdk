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

        /// <summary>
        /// Footprints extracted from the tile. Never null - a tile with no footprints carries an
        /// empty list. "None were extracted" and "none exist" are the same fact, and an empty list
        /// states it without a sentinel, so consumers can iterate unconditionally. They do: both
        /// TileFinished subscribers foreach this straight away with no null check, and the
        /// empty-tile path used to reach them with null.
        /// </summary>
        public List<Vector3[]> FootprintResults;

        /// <summary>
        /// One entry per building in the tile, giving the transform to lift and the ground point to
        /// sample elevation at. Never null; empty when the tile produced no buildings, or when
        /// generation failed before instantiation.
        /// </summary>
        public List<LandmarkAnchor> Anchors;

        public GltfGenerationTaskResult(TaskResultType result, IEnumerable<GameObject> visuals = null, List<Vector3[]> footprintResults = null, List<LandmarkAnchor> anchors = null)
        {
            ResultType = result;
            GeneratedObjects = visuals;
            FootprintResults = footprintResults ?? new List<Vector3[]>();
            Anchors = anchors ?? new List<LandmarkAnchor>();
        }
    }
}