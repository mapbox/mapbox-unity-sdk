using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Interfaces;
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

namespace Mapbox.VectorModule.ComponentSystem.BuildingComponentVisualizer
{
    public class BuildingComponentVisualizer : MapboxComponentVisualizer
    {
        private BuildingComponentSettings _buildingSettings;
        
        public BuildingComponentVisualizer(string name, IMapInformation mapInformation,
            UnityContext unityContext = null, BuildingComponentSettings settings = null) : base(name, mapInformation,
            unityContext)
        {
            _buildingSettings = settings ?? new BuildingComponentSettings();
        }

        public override MeshData CreateMesh(CanonicalTileId tileId, VectorTileLayer layer)
        {
            int heightTagIndex = -1;
            int minHeightTagIndex = -1;
            int extrudeTagIndex = -1;
            
            for (var i = 0; i < layer.Keys.Count; i++)
            {
                var key = layer.Keys[i];
                if (key == "height") heightTagIndex = i;
                if (key == "min_height") minHeightTagIndex = i;
                if (key == "extrude") extrudeTagIndex = i;
            }

            var arrayPolygon = new ArrayPolygon();
            ArraySnapTerrain arraySnapTerrain = null;
            if (_buildingSettings.EnableTerrainSnapping)
            {
                arraySnapTerrain = new ArraySnapTerrain(_mapInformation);
            }
            IPerformanceExtrusion arrayHeight;
            IHeightFilter settings;
            if (_buildingSettings.RoundBuildingCorners)
            {
                arrayHeight = new ArrayChamferHeight(_buildingSettings.ChamferExtrusionSettings);
                settings = _buildingSettings.ChamferExtrusionSettings;
            }
            else
            {
                arrayHeight = new ArrayHeight(_buildingSettings.BasicExtrusionSettings);
                settings = _buildingSettings.BasicExtrusionSettings;
            }
            
            var featureCount = layer.FeatureCount();
            var info = new StackMeshInfo(featureCount);

            var tileSize = Conversions.TileSizeInUnitySpace(tileId.Z, _mapInformation.Scale);
            var featureArray = new BuildingFeatureUnity[featureCount];

            for (var i = 0; i < featureCount; i++)
            {
                //for some reason passing ref values here effects performance quite a bit
                var feature = GetFeature(layer, i, ref heightTagIndex, ref minHeightTagIndex, ref extrudeTagIndex);
                if (feature == null) continue;
                featureArray[i] = feature;

                info.vertexRanges[i] = info.TotalPointCount;
                var vertNeeded = feature.VertexData.VertexCount * 5;
                info.vertexSize[i] = vertNeeded;
                info.TotalPointCount += vertNeeded;
            }

            var meshData = new MeshData(info, info.TotalPointCount);

            var poolSize = 10002;
            var triList = new int[poolSize];
            var triIndex = 0;
            var baseTriIndex = 0;
            var retryCount = 0;
            for (int i = 0; i < featureCount; i++)
            {
                var featureResult = featureArray[i];
                if (featureResult == null || !featureResult.DoExtrude)
                {
                    //ArrayPool<Vector3>.Shared.Return(featureResult.VertexData.Vertices);
                    continue;
                }

                var featurePreTriIndex = triIndex;

                var vertices = meshData.Vertices.AsSpan(meshData.MeshInfo.vertexRanges[i], meshData.MeshInfo.vertexSize[i]);
                var normals = meshData.Normals.AsSpan(meshData.MeshInfo.vertexRanges[i], meshData.MeshInfo.vertexSize[i]);
                var vertexAnchorIndex = meshData.MeshInfo.vertexRanges[i];

                triIndex = arrayPolygon.Polygonize(vertices, normals, vertexAnchorIndex, triList, triIndex, featureResult.VertexData);
                if (triIndex == -1)
                {
                    for (int j = featurePreTriIndex; j < triList.Length; j++)
                    {
                        triList[j] = 0;
                    }

                    meshData.Triangles.Add(triList);
                    baseTriIndex += triList.Length;

                    // On the first failure, size the replacement to what this feature can
                    // actually need rather than to poolSize again. A fresh buffer of the
                    // same size is no help to a feature whose own triangulation exceeds it:
                    // the retry overflows identically and the feature is dropped without a
                    // word. Building footprints are far below the limit in practice, so this
                    // is a guard rather than an observed failure — but it is the same defect
                    // that lost low-zoom water in AreaComponentVisualizer, and the extrusion
                    // path below already sizes to its requirement this way.
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

                    triList = new int[replacementCapacity];
                    triIndex = 0;
                    if (retryCount++ < 1)
                    {
                        i--;
                        continue;
                    }

                    // Sized to this feature's worst case and still short, so the geometry is
                    // degenerate rather than merely large. Skip it, but say so.
                    Debug.LogWarning(
                        $"BuildingComponentVisualizer: skipped a '{VectorLayerName}' feature in tile {tileId} that could not be " +
                        $"triangulated into {attemptedCapacity} indices ({featureResult.VertexData.VertexCount} vertices, " +
                        $"{Mathf.Max(0, featureResult.VertexData.Submeshes.Count - 1)} rings).");
                    retryCount = 0;
                    continue;
                }
                retryCount = 0;

                if (_buildingSettings.EnableTerrainSnapping)
                {
                    arraySnapTerrain.SnapTerrain(vertices, vertices.Length / 5, tileId, tileSize);
                }
                
                var triSpaceRequired = arrayHeight.CalculateTriCountFor(featureResult.VertexData.VertexCount);
                if (triSpaceRequired + triIndex >= triList.Length)
                {
                    for (int j = featurePreTriIndex; j < triList.Length; j++)
                    {
                        triList[j] = 0;
                    }

                    meshData.Triangles.Add(triList);
                    baseTriIndex += triList.Length;
                    triList = new int[triSpaceRequired + triIndex + 3];
                    triIndex = 0;
                    if (retryCount++ < 1)
                    {
                        i--;
                        continue;
                    }
                    // Feature requires more space than expected — skip it
                    retryCount = 0;
                    continue;
                }
                else
                {
                    var height = featureResult.Height;
                    var minHeight = featureResult.MinHeight;
                    settings.FilterHeight(ref height, ref minHeight);

                    triIndex = arrayHeight.Run(vertices, 
                        normals, 
                        vertexAnchorIndex, 
                        triList, 
                        triIndex, 
                        height,
                        minHeight,
                        featureResult.VertexData,
                        tileSize, 
                        _mapInformation.Scale);
                }

                info.triRanges[i] = baseTriIndex + triIndex;
                //ArrayPool<Vector3>.Shared.Return(featureResult.VertexData.Vertices);
            }

            if (triIndex < triList.Length)
            {
                Array.Resize(ref triList, triIndex);
            }
            meshData.Triangles.Add(triList);
            return meshData;
        }

        public override List<GameObject> CreateGo(CanonicalTileId tileId, MeshData meshData)
        {
            var objectList = new List<GameObject>();
            var entity = _buildingObjectPool.GetObject();
            var mats = new Material[meshData.Triangles.Count];
            for (int i = 0; i < meshData.Triangles.Count; i++)
            {
                mats[i] = _buildingSettings.Material;
            }

            entity.MeshRenderer.materials = mats;

            entity.GameObject.transform.SetParent(_layerRootObject, worldPositionStays: false);
            entity.StackId = 0;

            var mesh = entity.Mesh;
            mesh.Clear();
            WarnOnNonFiniteVertices(tileId, meshData);
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

        private BuildingFeatureUnity GetFeature(VectorTileLayer layer, int i, ref int heightTagIndex, ref int minHeightTagIndex, ref int extrudeTagIndex)
        {
            var view = layer.GetViewFor(i);
            var layerData = layer.Data.Slice(view.x, view.y);
            var featureReader = new PbfReader(layerData);
            var feature = new BuildingFeatureUnity();
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

            feature.SetProperties(ref layer, ref heightTagIndex, ref minHeightTagIndex, ref extrudeTagIndex);
            feature.VertexData = feature.Geometry(new Vector3(layer.Extent, 0, -layer.Extent));
            return feature;
        }

        private class BuildingFeatureUnity
        {
            public ulong Id;
            public GeomType GeometryType;
            public uint[] GeometryCommands;
            public FeatureVertexData VertexData;
            public int[] Tags;
            public float Height;
            public float MinHeight;
            public bool DoExtrude;

            public void SetProperties(ref VectorTileLayer layer, ref int heightTagIndex, ref int minHeightTagIndex, ref int extrudeTagIndex)
            {
                if (Tags == null) return; // Some features have no tags

                var tagCount = Tags.Length;
                
                for (int i = 0; i < tagCount - 1; i += 2)
                {
                    if (heightTagIndex != -1 && Tags[i] == heightTagIndex)
                    {
                        Height = Convert.ToSingle(layer.Values[Tags[i + 1]]);
                    }
                    else if (minHeightTagIndex != -1 && Tags[i] == minHeightTagIndex)
                    {
                        MinHeight = Convert.ToSingle(layer.Values[Tags[i + 1]]);
                    }
                    else if (extrudeTagIndex != -1 && Tags[i] == extrudeTagIndex)
                    {
                        DoExtrude = Convert.ToBoolean(layer.Values[Tags[i + 1]]);
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