using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tasks;
using UnityEngine;

namespace Mapbox.LandmarksModule
{
    internal class GltfGenerationTaskResult : TaskResult
    {
        public TaskResultType ResultType;
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

        /// <summary>
        /// The import that built <see cref="GeneratedObjects"/>, handed over so the caller can
        /// dispose it when it destroys them. Null on every result that produced no visuals.
        ///
        /// Ownership transfers with this field, and it is not optional. Unity does not garbage
        /// collect Mesh, Material or Texture2D, and disposing the import is the only thing that
        /// destroys the ones glTFast created for this tile - so dropping the reference leaks every
        /// mesh and PBR texture in the tile for the rest of the session. Destroying the GameObjects
        /// alone does not touch them.
        ///
        /// It cannot be disposed inside the visualizer either: Dispose destroys those same meshes,
        /// which the instantiated renderers are still using. It has to happen at the moment the
        /// visuals are destroyed, and nothing here knows when that is.
        /// </summary>
        public MapboxGltfImport Import;

        public GltfGenerationTaskResult(TaskResultType result, IEnumerable<GameObject> visuals = null, List<Vector3[]> footprintResults = null, List<LandmarkAnchor> anchors = null, MapboxGltfImport import = null)
        {
            ResultType = result;
            GeneratedObjects = visuals;
            FootprintResults = footprintResults ?? new List<Vector3[]>();
            Anchors = anchors ?? new List<LandmarkAnchor>();
            Import = import;
        }
    }
}
