using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.ComponentSystem.Data;
using Mapbox.VectorModule.ComponentSystem.Modifiers;
using Mapbox.VectorTile.Contants;
using Mapbox.VectorTile.Geometry;
using UnityEngine;
using DecodeGeometry = Mapbox.VectorModule.ComponentSystem.Data.DecodeGeometry;

namespace Mapbox.VectorModule.ComponentSystem.AreaComponent
{
    public class AreaComponentVisualizer : MapboxComponentVisualizer
    {
        private AreaComponentSettings _settings;
        
        public AreaComponentVisualizer(string name, IMapInformation mapInformation,
            UnityContext unityContext = null, AreaComponentSettings settings = null) : base(name, mapInformation,
            unityContext)
        {
            _settings = settings ?? new AreaComponentSettings();
            // localPosition (not position) so the y-offset stays in MapRoot-local space
            // and composes with any parent transform applied to the map hierarchy.
            _layerRootObject.transform.localPosition = new Vector3(
                _layerRootObject.transform.localPosition.x,
                _settings.GameObjectOffset,
                _layerRootObject.transform.localPosition.z);
        }

        public override MeshData CreateMesh(CanonicalTileId tileId, VectorTileLayer layer)
        {
            int classTagIndex = -1;
            int typeTagIndex = -1;
            for (var i = 0; i < layer.Keys.Count; i++)
            {
                var key = layer.Keys[i];
                if (key == "class") classTagIndex = i;
                if (key == "type") typeTagIndex = i;
            }
            
            var arrayPolygon = new ArrayPolygon();
            var featureCount = layer.FeatureCount();
            var info = new StackMeshInfo(featureCount);
            var featureArray = new AreaFeatureUnity[featureCount];

            for (var i = 0; i < featureCount; i++)
            {
                //for some reason passing ref values here effects performance quite a bit
                var feature = GetFeature(layer, i, ref classTagIndex, ref typeTagIndex);
                if (feature == null) continue;
                featureArray[i] = feature;

                info.vertexRanges[i] = info.TotalPointCount;
                var vertNeeded = feature.VertexData.VertexCount * 5;
                info.vertexSize[i] = vertNeeded;
                info.TotalPointCount += vertNeeded;
            }

            var meshData = new MeshData(info, info.TotalPointCount);

            var poolSize = 10002;
            var styleCount = _settings.StyleDictionary.Count;
            var triangles = new List<int[]>();
            var triIndices = new int[styleCount];
            var baseTriIndices = new int[styleCount];
            for (var i = 0; i < styleCount; i++)
            {
                triangles.Add(new int[poolSize]);
            }

            AreaStyle style = null;
            var retryCount = 0;
            for (int i = 0; i < featureCount; i++)
            {
                var featureResult = featureArray[i];
                if (featureResult == null)
                {
                    continue;
                }

                if (_settings.LayerName == "water")
                {
                    style = _settings.Styles[0];
                }
                else if (!_settings.StyleDictionary.TryGetValue(featureResult.Class, out style))
                {
                    continue;
                }

                var si = style.Index;
                var featurePreTriIndex = triIndices[si];

                var vertices = meshData.Vertices.AsSpan(meshData.MeshInfo.vertexRanges[i], meshData.MeshInfo.vertexSize[i]);
                var normals = meshData.Normals.AsSpan(meshData.MeshInfo.vertexRanges[i], meshData.MeshInfo.vertexSize[i]);
                var vertexAnchorIndex = meshData.MeshInfo.vertexRanges[i];

                var triList = triangles[si];
                triIndices[si] = arrayPolygon.Polygonize(vertices, normals, vertexAnchorIndex, triList, triIndices[si], featureResult.VertexData, style.PushUp);
                if (triIndices[si] == -1)
                {
                    for (int j = featurePreTriIndex; j < triList.Length; j++)
                    {
                        triList[j] = 0;
                    }

                    meshData.Triangles.Add(triList);
                    meshData.Materials.Add(style.Material);

                    // Replace the buffer that was just handed to the mesh. On the first
                    // failure, size it to what this feature can actually need rather than
                    // to poolSize again: a fresh buffer of the same size is no help to a
                    // feature whose own triangulation exceeds it, so the retry overflowed
                    // identically and the feature was dropped without a word. Low-zoom
                    // coastline and ocean rings are exactly that shape, which is why water
                    // went missing in patches when zoomed out.
                    // The capacity that actually just failed, captured before the buffer is
                    // replaced. Not replacementCapacity: on the second failure the sizing
                    // block above is skipped, so that variable holds poolSize rather than the
                    // worst-case capacity the retry was really attempted against - which made
                    // the warning report a number the code had never tried.
                    var attemptedCapacity = triList.Length;

                    var replacementCapacity = poolSize;
                    if (retryCount == 0)
                    {
                        replacementCapacity = Mathf.Max(poolSize, ArrayPolygon.WorstCaseIndexCount(featureResult.VertexData));
                    }

                    triangles[si] = new int[replacementCapacity];
                    baseTriIndices[si] += triList.Length;
                    triIndices[si] = 0;
                    if (retryCount++ < 1)
                    {
                        i--;
                        continue;
                    }

                    // Retried against a buffer sized for this feature's worst case and it
                    // still would not fit, so the geometry is degenerate rather than merely
                    // large. Skip it, but say so — silently dropping features is what made
                    // the low-zoom water problem hard to see.
                    Debug.LogWarning(
                        $"AreaComponentVisualizer: skipped a '{VectorLayerName}' feature in tile {tileId} that could not be " +
                        $"triangulated into {attemptedCapacity} indices ({featureResult.VertexData.VertexCount} vertices, " +
                        $"{Mathf.Max(0, featureResult.VertexData.Submeshes.Count - 1)} rings).");
                    retryCount = 0;
                    continue;
                }
                retryCount = 0;

                info.triRanges[i] = baseTriIndices[si] + triIndices[si];
            }

            for (var i = 0; i < triangles.Count; i++)
            {
                var submeshTri = triangles[i];
                meshData.Triangles.Add(submeshTri);
                meshData.Materials.Add(_settings.Styles[i].Material);
            }

            return meshData;
        }

        public override List<GameObject> CreateGo(CanonicalTileId tileId, MeshData meshData)
        {
            var objectList = new List<GameObject>();
            var entity = _buildingObjectPool.GetObject();
            var mats = new Material[meshData.Triangles.Count];
            for (var i = 0; i < meshData.Triangles.Count; i++)
            {
                mats[i] = meshData.Materials[i];
            }

            entity.MeshRenderer.materials = mats;

            entity.GameObject.transform.SetParent(_layerRootObject, worldPositionStays: false);
            entity.StackId = 0;

            var mesh = entity.Mesh;
            mesh.Clear();
            mesh.SetVertices(meshData.Vertices);
            mesh.SetNormals(meshData.Normals);
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.subMeshCount = meshData.Triangles.Count;
            for (var index = 0; index < meshData.Triangles.Count; index++)
            {
                var submesh = meshData.Triangles[index];
                mesh.SetTriangles(submesh, index);
            }

            entity.MeshFilter.sharedMesh = mesh;
            objectList.Add(entity.GameObject);

            if (!_results.ContainsKey(tileId))
                _results.Add(tileId, new List<VectorEntity>());
            _results[tileId].Add(entity);
            OnBuildingMeshCreated(tileId, entity.GameObject, meshData);

            return objectList;
        }

        private AreaFeatureUnity GetFeature(VectorTileLayer layer, int i, ref int classTagIndex, ref int typeTagIndex)
        {
            var view = layer.GetViewFor(i);
            var layerData = layer.Data.Slice(view.x, view.y);
            var featureReader = new PbfReader(layerData);
            var feature = new AreaFeatureUnity();
            bool geomTypeSet = false;
            while (featureReader.NextByte())
            {
                int featureType = featureReader.Tag;
                switch ((FeatureType)featureType)
                {
                    case FeatureType.Id:
                        feature.Id = (ulong)featureReader.Varint();
                        break;
                    case FeatureType.Tags:
                        var tags = featureReader.GetPackedInt();
                        feature.Tags = tags;
                        break;
                    case FeatureType.Type:
                        int geomType = (int)featureReader.Varint();
                        feature.GeometryType = (GeomType)geomType;
                        geomTypeSet = true;
                        break;
                    case FeatureType.Geometry:
                        if (null != feature.GeometryCommands)
                        {
                            throw new System.Exception(string.Format("Layer [{0}], feature already has a geometry",
                                layer.Name));
                        }

                        //get raw array of commands and coordinates
                        feature.GeometryCommands = featureReader.GetPackedUnit32();
                        break;
                    default:
                        featureReader.Skip();
                        break;
                }
            }

            // Discard features with missing/malformed geometry
            if (feature.GeometryCommands == null) return null;

            feature.SetProperties(ref layer, ref classTagIndex, ref typeTagIndex);
            feature.VertexData = feature.Geometry(new Vector3(layer.Extent, 0, -layer.Extent));
            return feature;
        }

        private class AreaFeatureUnity
        {
            public ulong Id;
            public GeomType GeometryType;
            public uint[] GeometryCommands;
            public FeatureVertexData VertexData;
            public int[] Tags;
            public string Class;
            public string Type;

            public void SetProperties(ref VectorTileLayer layer, ref int classTagIndex, ref int typeTagIndex)
            {
                if (Tags == null) return; //water has no tags
                
                var tagCount = Tags.Length;
                //some features have odd number of tags
                //not sure if it's a bug or data issue
                //so -1 here to skip last single tag
                for (int i = 0; i < tagCount - 1; i += 2)
                {
                    if (classTagIndex != -1 && Tags[i] == classTagIndex)
                    {
                        Class = layer.Values[Tags[i + 1]].ToString();
                    }
                    else if (typeTagIndex != -1 && Tags[i] == typeTagIndex)
                    {
                        Type = layer.Values[Tags[i + 1]].ToString();
                    }
                }
            }

            public FeatureVertexData Geometry(Vector3 scale)
            {
                return DecodeGeometry.GetGeometry(GeometryCommands, scale);
            }
        }
    }
}