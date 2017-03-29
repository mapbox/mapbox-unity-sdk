namespace Mapbox.Unity.MeshGeneration.Data
{
    using Mapbox.VectorTile;
    using Mapbox.VectorTile.ExtensionMethods;
    using System.Collections.Generic;
    using Mapbox.VectorTile.Geometry;

    public class VectorFeatureUnity
    {
        public VectorTileFeature Data { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public List<List<LatLng>> Points;

        public VectorFeatureUnity(VectorTileFeature feature, UnityTile tile)
        {
            Data = feature;
            Properties = Data.GetProperties();
            Points = feature.GeometryAsWgs84((ulong)tile.Zoom, (ulong)tile.TileCoordinate.x, (ulong)tile.TileCoordinate.y);
        }
    }
}
