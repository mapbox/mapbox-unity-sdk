using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Mapbox.VectorModule.MeshGeneration.MeshModifiers
{
    [Serializable]
    public class HeightModifier : MeshModifier<BasicHeightModifierCore>
    {
        private GeometryExtrusionOptions _basicOptions;
        
        public HeightModifier(GeometryExtrusionOptions options = null)
        {
            _basicOptions = options ?? new GeometryExtrusionOptions();
        }

        protected override BasicHeightModifierCore CreateCore() => new BasicHeightModifierCore(_basicOptions);

        public override void Run(VectorFeatureUnity feature, MeshData md, IMapInformation mapInfo)
        {
	        Core.Run(feature, md, mapInfo);
        }
    }

    [Serializable]
    public class GeometryExtrusionOptions
    {
        public ExtrusionType extrusionType = ExtrusionType.PropertyHeight;
        public ExtrusionGeometryType extrusionGeometryType = ExtrusionGeometryType.RoofAndSide;

        [Tooltip("Property name in feature layer to use for extrusion.")]
        public string propertyName = "height";

        public string propertyDescription = "";
        public float minimumHeight = 0f;
        public float maximumHeight = 1000f;

        [Tooltip("Scale factor to multiply the extrusion value of the feature.")]
        public float extrusionScaleFactor = 1f;
    }

    public sealed class BasicHeightModifierCore
    {
        GeometryExtrusionOptions _options;
        private bool _separateSubmesh = false;
        private float _scale = 0.01f;
        private int _counter = 0;
        float height = 0.0f;

        public BasicHeightModifierCore(GeometryExtrusionOptions options)
        {
            _options = options;
        }

        public void Run(VectorFeatureUnity feature, MeshData md, IMapInformation mapInformation)
        {
            _counter = 0;
            if (md.Vertices.Count == 0 || feature == null || feature.Points.Count < 1)
                return;

            var tileSize = Conversions.TileEdgeSizeInMercator(feature.TileId);
            _scale = mapInformation.Scale;

            float maxHeight = 1.0f;
            float minHeight = 0.0f;

            QueryHeight(feature, md, out maxHeight, out minHeight);

            maxHeight = (maxHeight * _options.extrusionScaleFactor / tileSize) / mapInformation.GetLatitudeCompensationForLocation;
            minHeight = (minHeight * _options.extrusionScaleFactor / tileSize) / mapInformation.GetLatitudeCompensationForLocation;
            height = (maxHeight - minHeight);

            //Set roof height
            GenerateRoofMesh(md, minHeight, maxHeight);
            if (_options.extrusionGeometryType == ExtrusionGeometryType.SideOnly)
            {
                md.Triangles[0].Clear();
            }

            GenerateWallMesh(md);
        }

        public void GenerateWallMesh(MeshData md)
        {
            md.Vertices.Capacity = _counter + md.Edges.Count * 2;
            float d = 0f;
            Vector3 v1;
            Vector3 v2;
            int ind = 0;
            Vector3 wallDir;

            if (_options.extrusionGeometryType != ExtrusionGeometryType.RoofOnly)
            {
                _counter = md.Edges.Count;
                var wallTri = new List<int>(_counter * 3);
                var wallUv = new List<Vector2>(_counter * 2);
                Vector3 norm = Constants.Math.Vector3Zero;

                md.Vertices.Capacity = md.Vertices.Count + _counter * 2;
                md.Normals.Capacity = md.Normals.Count + _counter * 2;

                for (int i = 0; i < _counter; i += 2)
                {
                    v1 = md.Vertices[md.Edges[i]];
                    v2 = md.Vertices[md.Edges[i + 1]];
                    ind = md.Vertices.Count;
                    md.Vertices.Add(v1);
                    md.Vertices.Add(v2);
                    md.Vertices.Add(new Vector3(v1.x, v1.y - height, v1.z));
                    md.Vertices.Add(new Vector3(v2.x, v2.y - height, v2.z));

                    d = (v2 - v1).magnitude;
                    norm = Vector3.Normalize(Vector3.Cross(v2 - v1, md.Vertices[ind + 2] - v1));
                    md.Normals.Add(norm);
                    md.Normals.Add(norm);
                    md.Normals.Add(norm);
                    md.Normals.Add(norm);

                    wallDir = (v2 - v1).normalized;
                    md.Tangents.Add(wallDir);
                    md.Tangents.Add(wallDir);
                    md.Tangents.Add(wallDir);
                    md.Tangents.Add(wallDir);

                    wallUv.Add(new Vector2(0, 0));
                    wallUv.Add(new Vector2(d, 0));
                    wallUv.Add(new Vector2(0, -height));
                    wallUv.Add(new Vector2(d, -height));

                    wallTri.Add(ind);
                    wallTri.Add(ind + 1);
                    wallTri.Add(ind + 2);

                    wallTri.Add(ind + 1);
                    wallTri.Add(ind + 3);
                    wallTri.Add(ind + 2);
                }

                // TODO: Do we really need this?
                if (_separateSubmesh)
                {
                    md.Triangles.Add(wallTri);
                }
                else
                {
                    md.Triangles.Capacity = md.Triangles.Count + wallTri.Count;
                    md.Triangles[0].AddRange(wallTri);
                }

                md.UV[0].AddRange(wallUv);
            }
        }

        public void GenerateRoofMesh(MeshData md, float minHeight, float maxHeight)
        {
            _counter = md.Vertices.Count;
            switch (_options.extrusionType)
            {
                case ExtrusionType.None:
                    break;
                case ExtrusionType.PropertyHeight:
                    for (int i = 0; i < _counter; i++)
                    {
                        md.Vertices[i] = new Vector3(md.Vertices[i].x, md.Vertices[i].y + maxHeight, md.Vertices[i].z);
                    }

                    break;
                case ExtrusionType.MinHeight:
                {
                    var minmax = MinMaxPair.GetMinMaxHeight(md.Vertices);
                    for (int i = 0; i < _counter; i++)
                    {
                        md.Vertices[i] = new Vector3(md.Vertices[i].x, minmax.min + maxHeight, md.Vertices[i].z);
                    }
                }
                    break;
                case ExtrusionType.MaxHeight:
                {
                    var minmax = MinMaxPair.GetMinMaxHeight(md.Vertices);
                    for (int i = 0; i < _counter; i++)
                    {
                        md.Vertices[i] = new Vector3(md.Vertices[i].x, minmax.max + maxHeight, md.Vertices[i].z);
                    }

                    height += (minmax.max - minmax.min);
                }
                    break;
                case ExtrusionType.RangeHeight:
                    for (int i = 0; i < _counter; i++)
                    {
                        md.Vertices[i] = new Vector3(md.Vertices[i].x, md.Vertices[i].y + maxHeight, md.Vertices[i].z);
                    }

                    break;
                case ExtrusionType.AbsoluteHeight:
                    for (int i = 0; i < _counter; i++)
                    {
                        md.Vertices[i] = new Vector3(md.Vertices[i].x, md.Vertices[i].y + maxHeight, md.Vertices[i].z);
                    }

                    break;
                default:
                    break;
            }
        }

        public void QueryHeight(VectorFeatureUnity feature, MeshData md, out float maxHeight, out float minHeight)
        {
            minHeight = 0.0f;
            maxHeight = 0.0f;

            switch (_options.extrusionType)
            {
                case ExtrusionType.None:
                    break;
                case ExtrusionType.PropertyHeight:
                case ExtrusionType.MinHeight:
                case ExtrusionType.MaxHeight:
                    if (feature.Properties != null && feature.Properties.ContainsKey(_options.propertyName))
                    {
                        try
                        {
                            maxHeight = Convert.ToSingle(feature.Properties[_options.propertyName]);
                        }
                        catch (Exception)
                        {
                            Debug.LogError("Property: '" + _options.propertyName + "' must contain a numerical value for extrusion.");
                            return;
                        }

                        if (feature.Properties.ContainsKey("min_height"))
                        {
                            minHeight = Convert.ToSingle(feature.Properties["min_height"]);
                        }
                    }
                    break;
                case ExtrusionType.RangeHeight:
                    if (feature.Properties != null && feature.Properties.ContainsKey(_options.propertyName))
                    {
                        if (_options.minimumHeight > _options.maximumHeight)
                        {
                            Debug.LogError("Maximum Height less than Minimum Height.Swapping values for extrusion.");
                            var temp = _options.minimumHeight;
                            _options.minimumHeight = _options.maximumHeight;
                            _options.maximumHeight = temp;
                        }

                        float featureHeight;
                        try
                        {
                            featureHeight = Convert.ToSingle(feature.Properties[_options.propertyName]);
                        }
                        catch (Exception)
                        {
                            Debug.LogError("Property: '" + _options.propertyName + "' must contain a numerical value for extrusion.");
                            return;
                        }

                        maxHeight = Math.Min(Math.Max(_options.minimumHeight, featureHeight), _options.maximumHeight);
                        if (feature.Properties.ContainsKey("min_height"))
                        {
                            var featureMinHeight = Convert.ToSingle(feature.Properties["min_height"]);
                            minHeight = Math.Min(featureMinHeight, _options.maximumHeight);
                        }
                    }

                    break;
                case ExtrusionType.AbsoluteHeight:
                    maxHeight = _options.maximumHeight;
                    break;
                default:
                    break;
            }
        }
        
        private class MinMaxPair
        {
            public float min, max;

            public static MinMaxPair GetMinMaxHeight(List<Vector3> vertices)
            {
                int counter = vertices.Count;
                MinMaxPair returnValue = new MinMaxPair
                {
                    max = float.MinValue,
                    min = float.MaxValue
                };

                for (int i = 0; i < counter; i++)
                {
                    if (vertices[i].y > returnValue.max)
                        returnValue.max = vertices[i].y;
                    else if (vertices[i].y < returnValue.min)
                        returnValue.min = vertices[i].y;
                }

                return returnValue;
            }
        }
    }
}