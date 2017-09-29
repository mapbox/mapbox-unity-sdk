namespace Mapbox.Unity.MeshGeneration.Data
{
	using Mapbox.VectorTile;
	using System.Collections.Generic;
	using Mapbox.VectorTile.Geometry;
	using UnityEngine;

	public class VectorFeatureUnity
	{
		public VectorTileFeature Data { get; set; }
		public Dictionary<string, object> Properties { get; set; }
		public List<List<Vector3>> Points;

		public VectorFeatureUnity()
		{
			Points = new List<List<Vector3>>();
		}

		public VectorFeatureUnity(VectorTileFeature feature, UnityTile tile, float layerExtent)
		{
			Data = feature;
			Properties = Data.GetProperties();
			Points = new List<List<Vector3>>();

			double unityTileSizeX = tile.Rect.Size.x;
			double unityTileSizeY = tile.Rect.Size.y;

			List<List<Point2d<float>>> geom = feature.Geometry<float>(0);
			var geomCount = geom.Count;
			for (int i = 0; i < geomCount; i++)
			{
				var pointCount = geom[i].Count;
				var nl = new List<Vector3>(pointCount);
				for (int j = 0; j < pointCount; j++)
				{
					var point = geom[i][j];
					nl.Add(new Vector3((float)(point.X / layerExtent * unityTileSizeX - (unityTileSizeX / 2))* tile.TileScale, 0, (float)((layerExtent - point.Y) / layerExtent * unityTileSizeY - (unityTileSizeY / 2)) * tile.TileScale));
				}
				Points.Add(nl);
			}
		}
	}
}
