using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.ComponentSystem.Data;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem
{
    public abstract class MapboxComponentVisualizer : VectorLayerVisualizer
    {
        public int VisualizerId => _id;
        private static int _nextId = 0;
        private readonly int _id;

        protected ObjectPool<VectorEntity> _buildingObjectPool;
        
        public MapboxComponentVisualizer(string name, IMapInformation mapInformation, UnityContext unityContext = null) : base(name, mapInformation, unityContext, null)
        {
            _id = System.Threading.Interlocked.Increment(ref _nextId);
            _buildingObjectPool = new ObjectPool<VectorEntity>(VectorEntityGenerator, 20);
        }
        
        public virtual MeshData CreateMesh(CanonicalTileId tileId, VectorTileLayer layer)
        {
            return null;
        }
        
        public virtual List<GameObject> CreateGo(CanonicalTileId tileId, MeshData meshData)
        {
            return null;
        }

        public override void UpdateForView(CanonicalTileId canonicalTileId, IMapInformation information)
        {
            if (_results.TryGetValue(canonicalTileId, out var visuals))
            {
                _mapInformation.PositionObjectFor(canonicalTileId, out var position, out var scale);
                foreach (var entity in visuals)
                {
                    entity.GameObject.transform.localPosition = position;
                    entity.GameObject.transform.localScale = scale;
                }
            }
        }
        
        public override void UnregisterTile(CanonicalTileId tileId)
        {
            if (_results.ContainsKey(tileId))
            {
                foreach (var entity in _results[tileId])
                {
                    entity.GameObject.SetActive(false);
                    _buildingObjectPool.Put(entity);
                    //TODO call finalize for gameobject modifiers here
                    
                    OnBuildingMeshDestroyed(tileId, entity.GameObject);
                }

                _results.Remove(tileId);
            }

            //TODO call unregister for gameobject modifiers here
        }

        public override void SetActive(CanonicalTileId canonicalTileId, bool isActive, IMapInformation mapInformation)
        {
            if (_results.TryGetValue(canonicalTileId, out var visuals))
            {
                foreach (var entity in visuals)
                {
                    entity.GameObject.SetActive(isActive);
                }
            }
        }
        
        public override void ClearCaches()
        {
            base.ClearCaches();
        }
        
        private VectorEntity VectorEntityGenerator()
        {
            var go = new GameObject();
            go.transform.SetParent(_layerRootObject, worldPositionStays: false);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = new Mesh();
            var mr = go.AddComponent<MeshRenderer>();
            var tempVectorEntity = new VectorEntity()
            {
                GameObject = go,
                Transform = go.transform,
                MeshFilter = mf,
                MeshRenderer = mr,
                Mesh = mf.sharedMesh
            };
            return tempVectorEntity;
        }
        
        public Action<CanonicalTileId, GameObject, MeshData> OnBuildingMeshCreated = (id, list, info) => { };
        public Action<CanonicalTileId, GameObject> OnBuildingMeshDestroyed = (id, list) => { };

        /// <summary>
        /// Reports a vertex buffer containing NaN or infinity, naming the tile and the feature it
        /// falls in, then leaves it alone.
        ///
        /// Unity's own complaint ("abnormal mesh bounds ... invalid vertices") is raised by whichever
        /// call happens to trigger a bounds recalculation, which can be far from the code that
        /// produced the bad value - and the mesh it names is anonymous, so the report on its own does
        /// not say which visualizer or feature is at fault. Every CreateGo calls this just before
        /// uploading, so whichever one built the bad geometry is the one that says so.
        ///
        /// Deliberately reports and continues rather than skipping or repairing: substituting a
        /// value would hide a generation bug, and the mesh is already wrong by this point.
        /// </summary>
        protected void WarnOnNonFiniteVertices(CanonicalTileId tileId, MeshData meshData)
        {
            var vertices = meshData?.Vertices;
            if (vertices == null) return;

            for (var i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                if (!float.IsNaN(v.x) && !float.IsInfinity(v.x) &&
                    !float.IsNaN(v.y) && !float.IsInfinity(v.y) &&
                    !float.IsNaN(v.z) && !float.IsInfinity(v.z))
                {
                    continue;
                }

                // Map the vertex back to the feature that wrote it, so the report is actionable.
                var featureIndex = -1;
                var ranges = meshData.MeshInfo?.vertexRanges;
                var sizes = meshData.MeshInfo?.vertexSize;
                if (ranges != null && sizes != null)
                {
                    for (var f = 0; f < ranges.Length; f++)
                    {
                        if (i >= ranges[f] && i < ranges[f] + sizes[f])
                        {
                            featureIndex = f;
                            break;
                        }
                    }
                }

                Debug.LogWarning(
                    $"{GetType().Name} ('{VectorLayerName}'): tile {tileId} produced a non-finite vertex " +
                    $"at index {i} of {vertices.Length} ({v.x}, {v.y}, {v.z}), in feature {featureIndex} " +
                    $"of {ranges?.Length ?? 0}. The mesh will render with abnormal bounds. " +
                    "Reported once per mesh.");
                return;
            }
        }

    }
}