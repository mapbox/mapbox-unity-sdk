namespace Mapbox.Unity.MeshGeneration.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Mapbox.VectorTile;
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Filters;
    using Mapbox.Unity.MeshGeneration.Data;
    using Mapbox.Unity.MeshGeneration.Modifiers;
    using Mapbox.Unity.Utilities;

    [Serializable]
    public class TypeVisualizerTuple
    {
        public string Type;
        public ModifierStackBase Stack;
    }


    /// <summary>
    /// VectorLayerVisualizer is a specialized layer visualizer working on polygon and line based vector data (i.e. building, road, landuse) using modifier stacks.
    /// Each feature is preprocessed and passed down to a modifier stack, which will create and return a game object for that given feature.
    /// Key is the name of the layer to be processed.
    /// Classification Key is the property name to be used for stack selection.
    /// It also supports filters; objects that goes over features and decides if it'll be visualized or not.
    /// Default Stack is the stack that'll be used for any feature that passes the filters but isn't matched to any special stack.
    /// 
    /// </summary>
    [CreateAssetMenu(menuName = "Mapbox/Layer Visualizer/Vector Layer Visualizer")]
    public class VectorLayerVisualizer : LayerVisualizerBase
    {
        private bool _subdivideLongEdges = true;
        private int _maxEdgeSectionCount = 40;
        private int _preferredEdgeSectionLength = 10;

        [SerializeField]
        private string _classificationKey;
        [SerializeField]
        private string _key;
        public override string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        [SerializeField]
        private List<FilterBase> Filters;

        [SerializeField]
        private ModifierStackBase _defaultStack;
        [SerializeField]
        private List<TypeVisualizerTuple> Stacks;

        private GameObject _container;

        /// <summary>
        /// Creates an object for each layer, extract and filter in/out the features and runs Build method on them.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="tile"></param>
        public override void Create(VectorTileLayer layer, UnityTile tile)
        {
            _container = new GameObject(Key + " Container");
            _container.transform.SetParent(tile.transform, false);

            //testing each feature with filters
            var fc = layer.FeatureCount();
            var filterOut = false;
            for (int i = 0; i < fc; i++)
            {
                filterOut = false;
                var feature = new VectorFeatureUnity(layer.GetFeature(i, 0), tile, layer.Extent);
                foreach (var filter in Filters)
                {
                    if (!string.IsNullOrEmpty(filter.Key) && !feature.Properties.ContainsKey(filter.Key))
                        continue;

                    if (!filter.Try(feature))
                    {
                        filterOut = true;
                        break;
                    }
                }

                if (!filterOut)
                    Build(feature, tile, _container);
            }

            var mergedStack = _defaultStack as MergedModifierStack;
            if (mergedStack != null)
            {
                mergedStack.End(tile, _container);
            }

            for (int i = 0; i < Stacks.Count; i++)
            {
                mergedStack = Stacks[i].Stack as MergedModifierStack;
                if (mergedStack != null)
                {
                    mergedStack.End(tile, _container);
                }
            }
        }

        /// <summary>
        /// Preprocess features, finds the relevant modifier stack and passes the feature to that stack
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="tile"></param>
        /// <param name="parent"></param>
        private void Build(VectorFeatureUnity feature, UnityTile tile, GameObject parent)
        {
            if (feature.Properties.ContainsKey("extrude") && !bool.Parse(feature.Properties["extrude"].ToString()))
                return;

            //we're not cutting out the holes yet
            foreach (var geometry in feature.Points)
            {
                var meshData = new MeshData();
                meshData.TileRect = tile.Rect;

                if (geometry.Count <= 1)
                    continue;

                //this will be improved in next version and will probably be replaced by filters
                string styleSelectorKey = "";
                if (string.IsNullOrEmpty(_classificationKey))
                {
                    if (feature.Properties.ContainsKey("type"))
                    {
                        styleSelectorKey = feature.Properties["type"].ToString().ToLowerInvariant();
                    }
                    else if (feature.Properties.ContainsKey("class"))
                    {
                        styleSelectorKey = feature.Properties["class"].ToString().ToLowerInvariant();
                    }
                }
                else if (feature.Properties.ContainsKey(_classificationKey))
                {
                    if (feature.Properties.ContainsKey(_classificationKey))
                    {
                        styleSelectorKey = feature.Properties[_classificationKey].ToString().ToLowerInvariant();
                    }
                }

                //we'll run all visualizers on MeshData here 
                var list = geometry;
                //.Select(x => Conversions.GeoToWorldPosition(x.Lat, x.Lng, tile.Rect.Center).ToVector3xz()).ToList();

                //long straight edges looks bad on bumpy terrain
                if (_subdivideLongEdges)
                {
                    var verts = new List<Vector3>();
                    if (list.Count > 1)
                    {
                        for (int i = 0; i < list.Count - 1; i++)
                        {
                            verts.Add(list[i]);
                            var dist = Vector3.Distance(list[i], list[i + 1]);
                            var step = Math.Min(_maxEdgeSectionCount, dist / _preferredEdgeSectionLength);
                            if (step > 1)
                            {
                                var counter = 1;
                                while (counter < step)
                                {
                                    var nv = Vector3.Lerp(list[i], list[i + 1], Mathf.Min(1, counter / step));
                                    verts.Add(nv);
                                    counter++;
                                }
                            }
                        }
                    }
                    verts.Add(list.Last());
                    list = verts;
                }

                //adding terrain & building min_height to vertices
                //we may move this into height modifier in the future
                meshData.Vertices = list.Select(vertex =>
                {
                    var h = tile.QueryHeightData((float)((vertex.x + tile.Rect.Size.x / 2) / tile.Rect.Size.x), (float)((vertex.z + tile.Rect.Size.y / 2) / tile.Rect.Size.y));
                    vertex += new Vector3(0, h, 0);

                    if (feature.Properties.ContainsKey("min_height"))
                    {
                        var min_height = Convert.ToSingle(feature.Properties["min_height"]);
                        vertex += new Vector3(0, min_height, 0);
                    }

                    return vertex;
                }).ToList();

                //and finally, running the modifier stack on the feature
                var mod = Stacks.FirstOrDefault(x => x.Type.Contains(styleSelectorKey));
                GameObject go;
                if (mod != null)
                {
                    go = mod.Stack.Execute(tile, feature, meshData, parent, mod.Type);
                }
                else
                {
                    if (_defaultStack != null)
                        go = _defaultStack.Execute(tile, feature, meshData, parent, _key);
                }
                //go.layer = LayerMask.NameToLayer(_key);
            }
        }
    }
}
