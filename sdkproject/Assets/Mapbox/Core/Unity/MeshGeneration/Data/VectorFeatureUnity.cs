namespace Mapbox.Unity.MeshGeneration.Data
{
    using Mapbox.VectorTile;
    using Mapbox.VectorTile.ExtensionMethods;
    using System.Collections.Generic;
    using Mapbox.VectorTile.Geometry;
    using UnityEngine;

    public class VectorFeatureUnity
    {
        public VectorTileFeature Data { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public List<List<Vector3>> Points;

        public VectorFeatureUnity(VectorTileFeature feature, UnityTile tile, float layerExtent)
        {
            Data = feature;
            Properties = Data.GetProperties();
            Points = new List<List<Vector3>>();

            for (int i = 0; i < feature.Geometry.Count; i++)
            {
                var nl = new List<Vector3>(feature.Geometry[i].Count);
                for (int j = 0; j < feature.Geometry[i].Count; j++)
                {
                    var point = feature.Geometry[i][j];
                    nl.Add(new Vector3((float)(point.X / layerExtent * tile.Rect.Size.x - (tile.Rect.Size.x/2)), 0, (float)((layerExtent - point.Y) / layerExtent * tile.Rect.Size.y - (tile.Rect.Size.y / 2))));
                }
                Points.Add(nl);
            }
        }
    }
}
