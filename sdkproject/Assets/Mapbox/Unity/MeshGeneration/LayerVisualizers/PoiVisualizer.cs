namespace Mapbox.Unity.MeshGeneration.Interfaces
{
    using System.Linq;
    using Mapbox.VectorTile;
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Data;
    using Mapbox.Unity.MeshGeneration.Components;
    using System;
    using Mapbox.Unity.Utilities;

    [CreateAssetMenu(menuName = "Mapbox/Layer Visualizer/Poi Layer Visualizer")]
    public class PoiVisualizer : LayerVisualizerBase
    {
        [SerializeField]
        private string _key;
        public override string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        public GameObject PoiPrefab;
        private GameObject _container;

        [SerializeField]
        private bool _scaleDownWithWorld = true;

        public override void Create(VectorTileLayer layer, UnityTile tile)
        {
            _container = new GameObject(Key + " Container");
            _container.transform.SetParent(tile.transform, false);

            var fc = layer.FeatureCount();
            for (int i = 0; i < fc; i++)
            {
                var feature = new VectorFeatureUnity(layer.GetFeature(i, 0), tile, layer.Extent);
                Build(feature, tile, _container);
            }
        }

        private void Build(VectorFeatureUnity feature, UnityTile tile, GameObject parent)
        {
            if (!feature.Points.Any())
                return;

            int selpos = feature.Points[0].Count / 2;
            var met = feature.Points[0][selpos];
            if (Math.Abs(met.x) > Math.Abs(tile.Rect.Size.x) / 2 || Math.Abs(met.y) > Math.Abs(tile.Rect.Size.y) / 2)
                return;
            if (!feature.Properties.ContainsKey("name"))
                return;

            var go = Instantiate(PoiPrefab);
            go.name = _key + " " + feature.Data.Id.ToString();

            var rx = (met.x - tile.Rect.Min.x) / tile.Rect.Size.x;
            var ry = 1 - (met.z - tile.Rect.Min.y) / tile.Rect.Size.y;
            var h = tile.QueryHeightData((int)rx, (int)ry);
            met.y += h;
            go.transform.position = met;
            go.transform.SetParent(parent.transform, false);

			if (!_scaleDownWithWorld)
			{
				go.transform.localScale = Vector3.one / go.transform.lossyScale.x;
			}

            var bd = go.AddComponent<FeatureBehaviour>();
            bd.Init(feature);

            var tm = go.GetComponent<IFeaturePropertySettable>();
			if (tm != null)
			{
				tm.Set(feature.Properties);
			}
        }

        private float GetHeightFromColor(Color c)
        {
            //additional *256 to switch from 0-1 to 0-256
            return (float)(-10000 + ((c.r * 256 * 256 * 256 + c.g * 256 * 256 + c.b * 256) * 0.1));
        }
    }
}
