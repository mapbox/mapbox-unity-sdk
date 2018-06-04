namespace Mapbox.Unity.MeshGeneration.Data
{
	using Mapbox.VectorTile;
	using System.Collections.Generic;
	using Mapbox.VectorTile.Geometry;
	using UnityEngine;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;

	public class VectorFeatureUnity
	{
		public VectorTileFeature Data;
		public Dictionary<string, object> Properties;
		public List<List<Vector3>> Points = new List<List<Vector3>>();
		public UnityTile Tile;

		private double _rectSizex;
		private double _rectSizey;
		private int _geomCount;
		private int _pointCount;
		private List<Vector3> _newPoints = new List<Vector3>();
		private List<List<Point2d<float>>> _geom;

		public VectorFeatureUnity()
		{
			Points = new List<List<Vector3>>();
		}

		public VectorFeatureUnity(VectorTileFeature feature, UnityTile tile, float layerExtent, bool buildingsWithUniqueIds = false)
		{
			Data = feature;
			Properties = Data.GetProperties();
			Points.Clear();
			Tile = tile;

			//this is a temp hack until we figure out how streets ids works
			if (buildingsWithUniqueIds == true) //ids from building dataset is big ulongs 
			{
				_geom = feature.Geometry<float>(); //and we're not clipping by passing no parameters
			}
			else //streets ids, will require clipping
			{
				_geom = feature.Geometry<float>(0); //passing zero means clip at tile edge
			}

			_rectSizex = tile.Rect.Size.x;
			_rectSizey = tile.Rect.Size.y;

			_geomCount = _geom.Count;
			for (int i = 0; i < _geomCount; i++)
			{
				_pointCount = _geom[i].Count;
				_newPoints = new List<Vector3>(_pointCount);
				for (int j = 0; j < _pointCount; j++)
				{
					var point = _geom[i][j];
					_newPoints.Add(new Vector3((float)(point.X / layerExtent * _rectSizex - (_rectSizex / 2)) * tile.TileScale, 0, (float)((layerExtent - point.Y) / layerExtent * _rectSizey - (_rectSizey / 2)) * tile.TileScale));
				}
				Points.Add(_newPoints);
			}
		}

		public bool ContainsLatLon(Vector2d coord)
		{
			////first check tile
			var coordinateTileId = Conversions.LatitudeLongitudeToTileId(
				coord.x, coord.y, Tile.InitialZoom);
			if (!coordinateTileId.Canonical.Equals(Tile.CanonicalTileId))
			{
				return false;
			}

			//then check polygon
			var point = Conversions.LatitudeLongitudeToVectorTilePosition(coord, Tile.InitialZoom);
			var output = PolygonUtils.PointInPolygon(new Point2d<float>(point.x, point.y), _geom);

			return output;
		}
 
	}
}
