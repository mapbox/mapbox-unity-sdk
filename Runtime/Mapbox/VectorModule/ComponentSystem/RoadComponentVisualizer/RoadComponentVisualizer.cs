using System;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.ComponentSystem.Data;
using Mapbox.VectorTile.Contants;
using Mapbox.VectorTile.Geometry;
using UnityEngine;
using UnityEngine.Rendering;
using Random = System.Random;

namespace Mapbox.VectorModule.ComponentSystem.RoadComponentVisualizer
{
    public class RoadComponentVisualizer : MapboxComponentVisualizer
    {
        private RoadComponentSettings _settings;

        [ThreadStatic] private static Random _threadRandom;
        private static Random GetRandom() => _threadRandom ??= new Random();

        public RoadComponentVisualizer(string name, IMapInformation mapInformation, UnityContext unityContext = null,
            RoadComponentSettings settings = null) : base(name, mapInformation, unityContext)
        {
            _settings = settings ?? new RoadComponentSettings();
            // localPosition (not position) so the y-offset stays in MapRoot-local space
            // and composes with any parent transform applied to the map hierarchy.
            _layerRootObject.transform.localPosition = new Vector3(
                _layerRootObject.transform.localPosition.x,
                _settings.GameObjectOffset,
                _layerRootObject.transform.localPosition.z);
        }

        private enum Turn
        {
            Start,
            Right,
            Left
        }
        
        private class MaterialTrianglePair
        {
            public int[] Triangles;
            public int TotalTriSize;
            public int[] TriSizes;
            public int[] TriRanges;
        }
        
        public override MeshData CreateMesh(CanonicalTileId tileId, VectorTileLayer layer)
        {
            if (_settings.RoadStyleSheet == null || _settings.RoadStyleSheet.Styles.Count == 0)
                return null;
            
            int classTagIndex = -1;
            int typeTagIndex = -1;
            int structureTagIndex = -1;
            for (var i = 0; i < layer.Keys.Count; i++)
            {
                var key = layer.Keys[i];
                if (key == "class") classTagIndex = i;
                if (key == "type") typeTagIndex = i;
                if (key == "structure") structureTagIndex = i;
            }

            var rnd = GetRandom();
            var featureCount = layer.FeatureCount();
            var info = new StackMeshInfo(featureCount);

            var triangleGroups = new Dictionary<Material, MaterialTrianglePair>();
            foreach (var style in _settings.RoadStyleSheet.Styles)
            {
                if (!triangleGroups.ContainsKey(style.Material))
                {
                    triangleGroups.Add(style.Material, new MaterialTrianglePair()
                    {
                        TriSizes = new int[featureCount],
                        TriRanges = new int[featureCount]
                    });
                }
            }
            
            var tileSize = Conversions.TileSizeInUnitySpace(tileId.Z, _mapInformation.Scale);
            var featureArray = new RoadFeatureUnity[featureCount];
            var styleArray = new RoadStyle[featureCount];

            for (var i = 0; i < featureCount; i++)
            {
                var feature = GetFeature(layer, i, ref classTagIndex, ref typeTagIndex, ref structureTagIndex);
                if (feature == null) continue;
                if (feature.GeometryType != GeomType.LINESTRING) continue;
                if (!_settings.RoadStyleSheet.TryGetStyle(feature, out var style)) continue;

                feature.VertexData = feature.Geometry(new Vector3(layer.Extent, 0, -layer.Extent));
                if (feature.VertexData.VertexCount <= 1) continue;
                
                featureArray[i] = feature;
                styleArray[i] = style;

                triangleGroups[style.Material].TriRanges[i] = triangleGroups[style.Material].TotalTriSize;
                info.vertexRanges[i] = info.TotalPointCount;

                var vertNeeded = VertexNeed(feature.VertexData);
                info.TotalPointCount += vertNeeded;
                info.vertexSize[i] = vertNeeded;
                if (feature.VertexData.VertexCount == 2)
                {
                    triangleGroups[style.Material].TriSizes[i] = 12 + 6;
                    triangleGroups[style.Material].TotalTriSize  += 12 + 6;
                    info.TotalTriangleCount += 12 + 6; //2 tri 3 vert + 12 for caps
                }
                else
                {
                    //12 tri for start/end caps
                    //6 tri end line segment
                    // 12 per middle vertex
                    var triSize = 12 + 6 + ((feature.VertexData.VertexCount - 2) * 12);
                    triangleGroups[style.Material].TriSizes[i] = triSize;
                    triangleGroups[style.Material].TotalTriSize  += triSize;
                    info.TotalTriangleCount += triSize;
                }
            }
            
            foreach (var pair in triangleGroups)
            {
                pair.Value.Triangles = new int[pair.Value.TotalTriSize];
            }

            var meshData = new MeshData(info, info.TotalPointCount)
            {
                UVs = new Vector2[info.TotalPointCount]
            };

            var featureTriIndex = 0;
            for (int i = 0; i < featureArray.Length; i++)
            {
                var featureResult = featureArray[i];
                var style = styleArray[i];
                
                if (featureResult == null) continue;
                if (featureResult.VertexData.VertexCount <= 1) continue;
                
                var group = triangleGroups[style.Material];
                
                var scaledRoadWidth = style.Width / _mapInformation.Scale / tileSize;
                var designedVertCount = meshData.MeshInfo.vertexSize[i];
                if (designedVertCount <= 2 || group.TriSizes[i] < 3) continue;

                var vertices = meshData.Vertices.AsSpan(meshData.MeshInfo.vertexRanges[i], designedVertCount);
                var normals = meshData.Normals.AsSpan(meshData.MeshInfo.vertexRanges[i], designedVertCount);
                var uvs = meshData.UVs.AsSpan(meshData.MeshInfo.vertexRanges[i], designedVertCount);
                var tris = group.Triangles.AsSpan(group.TriRanges[i], group.TriSizes[i]);
                
                var lastTurnWas = Turn.Start;
                var prevFirst = -2;
                var prevSecond = -1;
                var subVertIndex = 0;
                var subTriIndex = 0;
                
                for (var j = 0; j < featureResult.VertexData.Submeshes.Count - 1; j++)
                {
                    var subSize = featureResult.VertexData.Submeshes[j + 1] - featureResult.VertexData.Submeshes[j];
                    var startVertex = featureResult.VertexData.Submeshes[j];

                    var elevation = style.PushUp + (float)rnd.NextDouble() * _settings.RandomOffsetRange;
                    var sideNormal = new Vector3(0, 0, 0);
                    var finishLine = false;
                    var distance = 0f;
                    Vector3 dirNext = new Vector3(0, 0, 0);
                    Vector3 dirPrev;
                    for (int k = 0; k < subSize - 1; k++)
                    {
                        var current = featureResult.VertexData.Vertices[startVertex + k];
                        var next = featureResult.VertexData.Vertices[startVertex + k + 1];

                        
                        
                        if(k > 0)
                        {
                            //var prev = featureResult.VertexData.Vertices[startVertex + k - 1];
                            var movement = next - current;
                            dirPrev = -dirNext; //(prev - current).normalized;
                            dirNext = movement.normalized;
                            var dirInside = (dirNext + dirPrev).normalized * scaledRoadWidth;
                            var prevSideNormal = sideNormal;
                            sideNormal = new Vector3(dirNext.z  * scaledRoadWidth, 0, -dirNext.x  * scaledRoadWidth);
                            
                            // decide previous triangle indices based on turnState
                            if (lastTurnWas == Turn.Right)
                            {
                                prevFirst = -1;
                                prevSecond = -2;
                            }
                            else if (lastTurnWas == Turn.Left || lastTurnWas == Turn.Start)
                            {
                                prevFirst = -2;
                                prevSecond = -1;
                            }

                            int baseVertIndex = subVertIndex;
                            int baseTriIndex = featureTriIndex + baseVertIndex;
                            
                            //featureTriIndex is the start of the feature
                            //baseVertIndex is the start of this section (like corner)
                            
                            // precompute side offsets with sign depending on isRight
                            var isRight = Vector3.Dot(prevSideNormal, dirNext) > 0;
                            int sign = isRight ? -1 : 1;

                            // v0: current ± prevSideNormal
                            float v0x = current.x + sign * prevSideNormal.x;
                            float v0z = current.z + sign * prevSideNormal.z;

                            // v2: current ± sideNormal
                            float v2x = current.x + sign * sideNormal.x;
                            float v2z = current.z + sign * sideNormal.z;

                            // v1: current - dirInside
                            float v1x = current.x - dirInside.x;
                            float v1z = current.z - dirInside.z;

                            // v3: current + dirInside
                            float v3x = current.x + dirInside.x;
                            float v3z = current.z + dirInside.z;

                            // write vertices
                            vertices[baseVertIndex    ] = new Vector3(v0x, elevation, v0z);
                            vertices[baseVertIndex + 1] = new Vector3(v1x, elevation, v1z);
                            vertices[baseVertIndex + 2] = new Vector3(v2x, elevation, v2z);
                            vertices[baseVertIndex + 3] = new Vector3(v3x, elevation, v3z);

                            // write normals (all up)
                            normals[baseVertIndex    ] =Vector3.up;
                            normals[baseVertIndex + 1] =Vector3.up;
                            normals[baseVertIndex + 2] =Vector3.up;
                            normals[baseVertIndex + 3] = Vector3.up;
                            
                            // write uvs
                            uvs[baseVertIndex    ] = new Vector2(sign == 1 ? 0 : 1, distance);
                            uvs[baseVertIndex + 1] = new Vector2(sign == 1 ? 0 : 1, distance);
                            uvs[baseVertIndex + 2] = new Vector2(sign == 1 ? 0 : 1, distance);
                            uvs[baseVertIndex + 3] = new Vector2(sign == 1 ? 1 : 0, distance);

                            // handy local vars for indices
                            int i0 = baseTriIndex + 0;
                            int i1 = baseTriIndex + 1;
                            int i2 = baseTriIndex + 2;
                            int i3 = baseTriIndex + 3;
                            int ipf = baseTriIndex + prevFirst;
                            int ips = baseTriIndex + prevSecond;

                            // prev tris + corner tris
                            if (isRight)
                            {
                                // prev tris
                                tris[subTriIndex++] = ipf;
                                tris[subTriIndex++] = ips;
                                tris[subTriIndex++] = i0;

                                tris[subTriIndex++] = i0;
                                tris[subTriIndex++] = i3;
                                tris[subTriIndex++] = ipf;

                                // corner tris
                                tris[subTriIndex++] = i0;
                                tris[subTriIndex++] = i1;
                                tris[subTriIndex++] = i3;

                                tris[subTriIndex++] = i1;
                                tris[subTriIndex++] = i2;
                                tris[subTriIndex++] = i3;

                                lastTurnWas = Turn.Right;
                            }
                            else
                            {
                                // prev tris
                                tris[subTriIndex++] = i0;
                                tris[subTriIndex++] = ipf;
                                tris[subTriIndex++] = ips;

                                tris[subTriIndex++] = i3;
                                tris[subTriIndex++] = i0;
                                tris[subTriIndex++] = ips;

                                // corner tris
                                tris[subTriIndex++] = i0;
                                tris[subTriIndex++] = i3;
                                tris[subTriIndex++] = i1;

                                tris[subTriIndex++] = i2;
                                tris[subTriIndex++] = i1;
                                tris[subTriIndex++] = i3;

                                lastTurnWas = Turn.Left;
                            }


                            subVertIndex += 4;

                            if (k == subSize - 2)
                            {
                                finishLine = true;
                            }

                            distance += movement.magnitude;
                        }
                        else if (k == 0)
                        {
                            distance = 0;
                            var movement = next - current;
                            dirNext = movement.normalized;
                            sideNormal = new Vector3(dirNext.z * scaledRoadWidth, 0, -dirNext.x * scaledRoadWidth);

                            // round caps are width/2 back and then width/2 to sides
                            var capSide1x = current.x - (dirNext.x * scaledRoadWidth/2) + (sideNormal.x/2);
                            var capSide1z = current.z - (dirNext.z * scaledRoadWidth/2) + (sideNormal.z/2);
                            var capSide2x = current.x - (dirNext.x * scaledRoadWidth/2) - (sideNormal.x/2);
                            var capSide2z = current.z - (dirNext.z * scaledRoadWidth/2) - (sideNormal.z/2);
                            var side1x = current.x + sideNormal.x;
                            var side1z = current.z + sideNormal.z;
                            var side2x = current.x - sideNormal.x;
                            var side2z = current.z - sideNormal.z;
                            
                            vertices[subVertIndex    ] = new Vector3(capSide1x, elevation, capSide1z);
                            vertices[subVertIndex + 1] = new Vector3(capSide2x, elevation, capSide2z);
                            vertices[subVertIndex + 2] = new Vector3(side1x, elevation, side1z);
                            vertices[subVertIndex + 3] = new Vector3(side2x, elevation, side2z);

                            normals[subVertIndex    ] = Vector3.up;
                            normals[subVertIndex + 1] = Vector3.up;
                            normals[subVertIndex + 2] = Vector3.up;
                            normals[subVertIndex + 3] = Vector3.up;
                            
                            //0.25 and 0.75 are just pushing uv coordinates inside a litte to prevent ugly stretching on caps
                            uvs[subVertIndex    ] = new Vector2(0.25f, 0);
                            uvs[subVertIndex + 1] = new Vector2(0.75f, 0);
                            uvs[subVertIndex + 2] = new Vector2(0, 0);
                            uvs[subVertIndex + 3] = new Vector2(1, 0);

                            var baseTriIndex = featureTriIndex + subVertIndex;
                            tris[subTriIndex++] = baseTriIndex + 0;
                            tris[subTriIndex++] = baseTriIndex + 1;
                            tris[subTriIndex++] = baseTriIndex + 2;

                            tris[subTriIndex++] = baseTriIndex + 2;
                            tris[subTriIndex++] = baseTriIndex + 1;
                            tris[subTriIndex++] = baseTriIndex + 3;
                            
                            subVertIndex += 4;
                            lastTurnWas = 0;

                            if (subSize == 2)
                                finishLine = true;
                            
                            distance += movement.magnitude;
                        }
                        
                        if (finishLine)
                        {
                            current = featureResult.VertexData.Vertices[startVertex + k + 1];
                            var dir = (current - featureResult.VertexData.Vertices[startVertex + k]).normalized;
                            
                            
                            if (lastTurnWas == Turn.Left || lastTurnWas == 0)
                            {
                                prevFirst = -1;
                                prevSecond = -2;
                            }
                            else if (lastTurnWas == Turn.Right)
                            {
                                prevFirst = -2;
                                prevSecond = -1;
                            }

                            var side1x = current.x + sideNormal.x;
                            var side1z = current.z + sideNormal.z;
                            var side2x = current.x - sideNormal.x;
                            var side2z = current.z - sideNormal.z;

                            var capSide1x = current.x + (dir.x * scaledRoadWidth / 2) + (sideNormal.x / 2);
                            var capSide1z = current.z + (dir.z * scaledRoadWidth / 2) + (sideNormal.z / 2);
                            var capSide2x = current.x + (dir.x * scaledRoadWidth / 2) - (sideNormal.x / 2);
                            var capSide2z = current.z + (dir.z * scaledRoadWidth / 2) - (sideNormal.z / 2);
                            
                            vertices[subVertIndex    ] = new Vector3(side1x, elevation, side1z);
                            vertices[subVertIndex + 1] = new Vector3(side2x, elevation, side2z);
                            vertices[subVertIndex + 2] = new Vector3(capSide1x, elevation, capSide1z);
                            vertices[subVertIndex + 3] = new Vector3(capSide2x, elevation, capSide2z);

                            normals[subVertIndex    ] = Vector3.up;
                            normals[subVertIndex + 1] = Vector3.up;
                            normals[subVertIndex + 2] = Vector3.up;
                            normals[subVertIndex + 3] = Vector3.up;
                            
                            uvs[subVertIndex    ] = new Vector2(0, distance);
                            uvs[subVertIndex + 1] = new Vector2(1, distance);
                            //0.25 and 0.75 are just pushing uv coordinates inside a litte to prevent ugly stretching on caps
                            uvs[subVertIndex + 2] = new Vector2(0.25f, distance + scaledRoadWidth/2);
                            uvs[subVertIndex + 3] = new Vector2(0.75f, distance + scaledRoadWidth/2);

                            var baseTriIndex = featureTriIndex + subVertIndex;
                            tris[subTriIndex++] = baseTriIndex + prevSecond;
                            tris[subTriIndex++] = baseTriIndex + prevFirst;
                            tris[subTriIndex++] = baseTriIndex + 0;

                            tris[subTriIndex++] = baseTriIndex + 0;
                            tris[subTriIndex++] = baseTriIndex + prevFirst;
                            tris[subTriIndex++] = baseTriIndex + 1;
                            
                            tris[subTriIndex++] = baseTriIndex + 0;
                            tris[subTriIndex++] = baseTriIndex + 1;
                            tris[subTriIndex++] = baseTriIndex + 2;

                            tris[subTriIndex++] = baseTriIndex + 2;
                            tris[subTriIndex++] = baseTriIndex + 1;
                            tris[subTriIndex++] = baseTriIndex + 3;

                            subVertIndex += 4;
                        }
                    }
                }

                featureTriIndex += designedVertCount;
            }

            foreach (var pair in triangleGroups)
            {
                meshData.Materials.Add(pair.Key);
                meshData.Triangles.Add(pair.Value.Triangles);
            }
            
            //TODO we are keeping triangle information inside groups 
            //and not pass it into stackinfo
            //this'll probably break mouse click interactions
            //planned with meshinfo.tri information in mind
            //currently when user clicks and we get triIndex info from hit
            //there'll be no data to match it, and highlight selection or something

            return meshData;
        }

        private int VertexNeed(FeatureVertexData data)
        {
            var total = 0;
            for (var i = 0; i < data.Submeshes.Count - 1; i++)
            {
                var size = data.Submeshes[i + 1] - data.Submeshes[i];
                total += 4 * size; //(size - 2) * 4 + (2 * 2); //two vertices for cap, 4 for each mid vertex
            }

            return total;
        }

        public override List<GameObject> CreateGo(CanonicalTileId tileId, MeshData meshData)
        {
            var objectList = new List<GameObject>();
            var entity = _buildingObjectPool.GetObject();
            var mats = new Material[meshData.Triangles.Count];
            // for (int i = 0; i < meshData.Triangles.Count; i++)
            // {
            //     mats[i] = _settings.Material;
            // }
            for (var i = 0; i < meshData.Triangles.Count; i++)
            {
                mats[i] = meshData.Materials[i];
            }

            entity.MeshRenderer.materials = mats;

            entity.GameObject.transform.SetParent(_layerRootObject, worldPositionStays: false);
            entity.StackId = 0;

            var mesh = entity.Mesh;
            mesh.Clear();
            WarnOnNonFiniteVertices(tileId, meshData);
            mesh.SetVertices(meshData.Vertices);
            mesh.SetNormals(meshData.Normals);
            mesh.SetUVs(0, meshData.UVs);
            mesh.indexFormat = IndexFormat.UInt32;

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

            // foreach (var vertex in meshData.Vertices)
            // {
            //     var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //     go.transform.position = vertex;
            //     go.transform.localScale = Vector3.one * 0.001f;
            //     go.transform.SetParent(entity.Transform);
            // }

            return objectList;
        }

        private RoadFeatureUnity GetFeature(VectorTileLayer layer, int i, ref int classTagIndex, ref int typeTagIndex, ref int structureTagIndex)
        {
            var view = layer.GetViewFor(i);
            var layerData = layer.Data.Slice(view.x, view.y);
            var featureReader = new PbfReader(layerData);
            var feature = new RoadFeatureUnity();
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
                            throw new Exception(string.Format("Layer [{0}], feature already has a geometry",
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

            feature.SetProperties(ref layer, ref classTagIndex, ref typeTagIndex, ref structureTagIndex);
            return feature;
        }
    }
}
