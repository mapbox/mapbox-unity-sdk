using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;
using System.Collections;
using Mapbox.BaseModule.Data;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Unity;
using Mapbox.VectorModule.MeshGeneration;
using Mapbox.VectorTile;

namespace Mapbox.VectorModule
{
    public enum ModifierStackExecutionMode
    {
        All,
        FirstHit
    }

    [Serializable]
    public class VectorLayerVisualizer : IVectorLayerVisualizer
    {
        public Dictionary<int, ModifierStack> GetModStacks => _stackList;
        public string VectorLayerName => _vectorLayerName;
        public bool Active { get; set; }

        private VectorLayerVisualizerSettings _settings;
        private string _vectorLayerName;
        private UnityContext _unityContext;
        private Dictionary<int, ModifierStack> _stackList;
        private Dictionary<CanonicalTileId, List<VectorEntity>> _results;
        private IMapInformation _mapInformation;
        
        private Transform _layerRootObject;
        
        public VectorLayerVisualizer(string name, IMapInformation mapInformation, UnityContext unityContext, VectorLayerVisualizerSettings settings)
        {
            _vectorLayerName = name;
            _mapInformation = mapInformation;
            _unityContext = unityContext;
            _settings = settings;
            _stackList = new Dictionary<int, ModifierStack>();
            _results = new Dictionary<CanonicalTileId, List<VectorEntity>>();
            _layerRootObject = new GameObject(_vectorLayerName + " layer objects").transform;
            _layerRootObject.SetParent(_unityContext.RuntimeGenerationRoot);
            _layerRootObject.transform.localPosition = Vector3.zero;
            _layerRootObject.transform.position += _settings.Offset;
        }

        public void UpdateForView(CanonicalTileId canonicalTileId, IMapInformation information)
        {
            if (_results.TryGetValue(canonicalTileId, out var visuals))
            {
                foreach (var entity in visuals)
                {
                    _mapInformation.PositionObjectFor(canonicalTileId, out var position, out var scale);
                    entity.GameObject.transform.localPosition = new Vector3(
                        position.x - _layerRootObject.transform.position.x, 
                        entity.GameObject.transform.localPosition.y, 
                        position.z - _layerRootObject.transform.position.z);
                    entity.GameObject.transform.localScale = scale;
                }
            }
        }

        public void SetActive(CanonicalTileId canonicalTileId, bool isActive, IMapInformation mapInformation)
        {
            if (_results.TryGetValue(canonicalTileId, out var visuals))
            {
                foreach (var entity in visuals)
                {
                    if (_stackList.TryGetValue(entity.StackId, out var stack))
                    {
                        entity.GameObject.SetActive(isActive && stack.IsZinSupportedRange(_mapInformation.AbsoluteZoom));
                    }
                }
            }
        }
        
        public bool ContainsVisualFor(CanonicalTileId dataTileId)
        {
            return _results.ContainsKey(dataTileId);
        }

        public IEnumerator Initialize()
        {
            foreach (var stack in _stackList)
            {
                stack.Value.Initialize(_layerRootObject);
            }

            yield return null;
        }

        public void AddModifierStack(List<ModifierStack> stack)
        {
            foreach (var modifierStack in stack)
            {
                _stackList.Add(modifierStack.GetHashCode(), modifierStack);
            }
        }

        public virtual Dictionary<int, HashSet<MeshData>> CreateMesh(CanonicalTileId tileId, VectorTileLayer layer)
        {
            var meshData = new Dictionary<int, HashSet<MeshData>>();
            MeshModifications(tileId, layer, meshData);
            return meshData;
        }

        public virtual List<GameObject> CreateGo(CanonicalTileId tileId, Dictionary<int, HashSet<MeshData>> meshData)
        {
            var objects =  GameObjectModifications(tileId, meshData);
            return objects;
        }

        public virtual void UnregisterTile(CanonicalTileId tileId)
        {
            if (_results.ContainsKey(tileId))
            {
                foreach (var entity in _results[tileId])
                {
                    entity.GameObject.SetActive(false);
                    if (_stackList.TryGetValue(entity.StackId, out var stack))
                    {
                        stack.Finalize(entity);
                    }
                    else
                    {
                        Debug.Log("shouldn't happen");
                    }
                    
                    OnVectorMeshDestroyed(entity.GameObject);
                }

                _results.Remove(tileId);
            }

            foreach (var modifierStack in _stackList.Values)
            {
                modifierStack.UnregisterTile(tileId);
            }
        }
        
        public void OnDestroy()
        {
            foreach (var entities in _results.Values)
            {
                foreach (var entity in entities)
                {
                    OnVectorMeshDestroyed(entity.GameObject);
                    GameObject.Destroy(entity.GameObject);
                }
            }

            foreach (var stack in _stackList)
            {
                stack.Value.OnDestroy();
            }

            _results.Clear();
        }

        protected void MeshModifications(CanonicalTileId canonicalTileId, VectorTileLayer layer, Dictionary<int, HashSet<MeshData>> meshDataList)
        {
            for (int i = 0; i < layer.FeatureCount(); i++)
            {
                var featureResult = GetFeature(layer, i);
                if (featureResult == null)
                    continue;
                featureResult.TileId = canonicalTileId;

                foreach (var stack in _stackList)
                {
                    if (stack.Value.Filters != null && !stack.Value.Filters.Try(featureResult))
                        continue;
                    
                    var meshData = new MeshData();
                    meshData.Feature = featureResult;
                    meshData = stack.Value.RunMeshModifiers(featureResult, meshData, _mapInformation);
                    
                    if (!meshDataList.ContainsKey(stack.Key)) meshDataList.Add(stack.Key, new HashSet<MeshData>());
                    meshDataList[stack.Key].Add(meshData);
                    if (_settings.StackExecutionMode == ModifierStackExecutionMode.FirstHit)
                        break;
                }
            }

            // if (!tile.IsActive)
            //     return;

            foreach (var meshResult in meshDataList)
            {
                var stack = _stackList[meshResult.Key];
                if (stack.Settings.MergeObjects)
                {
                    var mergedData = CombineMeshData(meshResult.Value);
                    meshResult.Value.Clear();
                    meshResult.Value.Add(mergedData);
                }
            }
        }

        protected List<GameObject> GameObjectModifications(CanonicalTileId canonicalTileId, Dictionary<int, HashSet<MeshData>> meshDataList)
        {
            var objectList = new List<GameObject>();
            foreach (var pair in meshDataList)
            {
                foreach (var meshData in pair.Value)
                {
                    var entity = _stackList[pair.Key].CreateEntity(meshData);
                    entity.GameObject.transform.SetParent(_layerRootObject);
                    entity.StackId = pair.Key;
                    entity.Feature = meshData.Feature;
                    if(Application.isEditor) entity.GameObject.name = VectorLayerName + " " + canonicalTileId.ToString();
                    _stackList[pair.Key].RunGoModifiers(entity, _mapInformation);
                    objectList.Add(entity.GameObject);
                    
                    if(!_results.ContainsKey(canonicalTileId))
                        _results.Add(canonicalTileId, new List<VectorEntity>());
                    _results[canonicalTileId].Add(entity);
                    entity.Mesh.RecalculateBounds();
                    OnVectorMeshCreated(entity.GameObject);
                }
            }

            return objectList;
        }
        
        protected VectorFeatureUnity GetFeature(VectorTileLayer layer, int i)
        {
            var feature = layer.GetFeature(i);
            var layerExtent = (float)layer.Extent;
            var featureResult = new VectorFeatureUnity();
            featureResult.Properties = feature.GetProperties();

            var geometry = feature.Geometry<float>(0);
            var points = new List<List<Vector3>>();
            foreach (var t in geometry)
            {
                var pointCount = t.Count;
                var newPoints = new List<Vector3>(pointCount);
                for (int k = 0; k < pointCount; k++)
                {
                    var point = t[k];
                    newPoints.Add(new Vector3(
                    point.X / layerExtent,
                    0, 
                    -1 * (point.Y / layerExtent)));
                }

                points.Add(newPoints);
            }

            featureResult.Points = points;
            if (featureResult.Points.Count < 1)
            {
                return null;
            }
            featureResult.Data = feature;
            return featureResult;
        }

        protected MeshData CombineMeshData(HashSet<MeshData> meshDataList)
        {
            var mergedData = new MeshData();
            foreach (var currentData in meshDataList)
            {
                if (currentData.Vertices.Count <= 3)
                    continue;

                var st = mergedData.Vertices.Count;
                mergedData.Vertices.AddRange(currentData.Vertices);
                mergedData.Normals.AddRange(currentData.Normals);
                mergedData.Tangents.AddRange(currentData.Tangents);

                var c2 = currentData.UV.Count;
                for (int j = 0; j < c2; j++)
                {
                    if (mergedData.UV.Count <= j)
                    {
                        mergedData.UV.Add(new List<Vector2>(currentData.UV[j].Count));
                    }
                }

                c2 = currentData.UV.Count;
                for (int j = 0; j < c2; j++)
                {
                    mergedData.UV[j].AddRange(currentData.UV[j]);
                }

                c2 = currentData.Triangles.Count;
                for (int j = 0; j < c2; j++)
                {
                    if (mergedData.Triangles.Count <= j)
                    {
                        mergedData.Triangles.Add(new List<int>(currentData.Triangles[j].Count));
                    }
                }

                for (int j = 0; j < c2; j++)
                {
                    for (int k = 0; k < currentData.Triangles[j].Count; k++)
                    {
                        mergedData.Triangles[j].Add(currentData.Triangles[j][k] + st);
                    }
                }
            }

            return mergedData;
        }
        
        public Action<GameObject> OnVectorMeshCreated = list => { };
        public Action<GameObject> OnVectorMeshDestroyed = go => { };
    }
}
