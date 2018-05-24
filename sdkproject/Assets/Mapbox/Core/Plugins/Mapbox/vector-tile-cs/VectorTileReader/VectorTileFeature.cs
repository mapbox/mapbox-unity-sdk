using System.Collections.Generic;
using Mapbox.VectorTile.Geometry;
using System;


namespace Mapbox.VectorTile
{


	public class VectorTileFeature
	{


		/// <summary>
		/// Initialize VectorTileFeature
		/// </summary>
		/// <param name="layer">Parent <see cref="VectorTileLayer"/></param>
		public VectorTileFeature(VectorTileLayer layer, uint? clipBuffer = null, float scale = 1.0f)
		{
			_layer = layer;
			_clipBuffer = clipBuffer;
			_scale = scale;
			Tags = new List<int>();
		}


		private VectorTileLayer _layer;
		// TODO: how to cache without using object
		// may a dictionary with parameters clip and scale as key to keep different requests fast
		private object _cachedGeometry;
		private uint? _clipBuffer;
		private float? _scale;
		private float? _previousScale; //cache previous scale to not return


		/// <summary>Id of this feature https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L32</summary>
		public ulong Id { get; set; }


		/// <summary>Parent <see cref="VectorTileLayer"/> this feature belongs too</summary>
		public VectorTileLayer Layer { get { return _layer; } }


		/// <summary><see cref="GeomType"/> of this feature</summary>
		public GeomType GeometryType { get; set; }


		/// <summary>Geometry in internal tile coordinates</summary>
		public List<uint> GeometryCommands { get; set; }


		public List<List<Point2d<T>>> Geometry<T>(
			uint? clipBuffer = null
			, float? scale = null
		)
		{

			// parameters passed to this method override parameters passed to the constructor
			if (_clipBuffer.HasValue && !clipBuffer.HasValue) { clipBuffer = _clipBuffer; }
			if (_scale.HasValue && !scale.HasValue) { scale = _scale; }

			// TODO: how to cache 'finalGeom' without making whole class generic???
			// and without using an object (boxing) ???
			List<List<Point2d<T>>> finalGeom = _cachedGeometry as List<List<Point2d<T>>>;
			if (null != finalGeom && scale == _previousScale)
			{
				return finalGeom;
			}

			//decode commands and coordinates
			List<List<Point2d<long>>> geom = DecodeGeometry.GetGeometry(
				_layer.Extent
				, GeometryType
				, GeometryCommands
				, scale.Value
			);
			if (clipBuffer.HasValue)
			{
				// HACK !!!
				// work around a 'feature' of clipper where the ring order gets mixed up
				// with multipolygons containing holes
				if (geom.Count < 2 || GeometryType != GeomType.POLYGON)
				{
					// work on points, lines and single part polygons as before
					geom = UtilGeom.ClipGeometries(geom, GeometryType, (long)_layer.Extent, clipBuffer.Value, scale.Value);
				}
				else
				{
					// process every ring of a polygon in a separate loop
					List<List<Point2d<long>>> newGeom = new List<List<Point2d<long>>>();
					int geomCount = geom.Count;
					for (int i = 0; i < geomCount; i++)
					{
						List<Point2d<long>> part = geom[i];
						List<List<Point2d<long>>> tmp = new List<List<Point2d<long>>>();
						// flip order of inner rings to look like outer rings
						bool isInner = signedPolygonArea(part) >= 0;
						if (isInner) { part.Reverse(); }
						tmp.Add(part);
						tmp = UtilGeom.ClipGeometries(tmp, GeometryType, (long)_layer.Extent, clipBuffer.Value, scale.Value);
						// ring was completely outside of clip border
						if (0 == tmp.Count)
						{
							continue;
						}
						// flip winding order of inner rings back
						if (isInner) { tmp[0].Reverse(); }
						newGeom.Add(tmp[0]);
					}
					geom = newGeom;
				}
			}

			//HACK: use 'Scale' to convert to <T> too
			finalGeom = DecodeGeometry.Scale<T>(geom, scale.Value);

			//set field needed for next iteration
			_previousScale = scale;
			_cachedGeometry = finalGeom;

			return finalGeom;
		}


		private float signedPolygonArea(List<Point2d<long>> vertices)
		{
			int num_points = vertices.Count - 1;
			float area = 0;
			for (int i = 0; i < num_points; i++)
			{
				area +=
					(vertices[i + 1].X - vertices[i].X) *
					(vertices[i + 1].Y + vertices[i].Y) / 2;
			}
			return area;
		}


		/// <summary>Tags to resolve properties https://github.com/mapbox/vector-tile-spec/tree/master/2.1#44-feature-attributes</summary>
		public List<int> Tags { get; set; }


		/// <summary>
		/// Get properties of this feature. Throws exception if there is an uneven number of feature tag ids
		/// </summary>
		/// <returns>Dictionary of this feature's properties</returns>
		public Dictionary<string, object> GetProperties()
		{

			if (0 != Tags.Count % 2)
			{
				throw new Exception(string.Format("Layer [{0}]: uneven number of feature tag ids", _layer.Name));
			}
			Dictionary<string, object> properties = new Dictionary<string, object>();
			int tagCount = Tags.Count;
			for (int i = 0; i < tagCount; i += 2)
			{
				properties.Add(_layer.Keys[Tags[i]], _layer.Values[Tags[i + 1]]);
			}
			return properties;
		}


		/// <summary>
		/// Get property by name
		/// </summary>
		/// <param name="key">Name of the property to request</param>
		/// <returns>Value of the requested property</returns>
		public object GetValue(string key)
		{

			var idxKey = _layer.Keys.IndexOf(key);
			if (-1 == idxKey)
			{
				throw new Exception(string.Format("Key [{0}] does not exist", key));
			}

			int tagCount = Tags.Count;
			for (int i = 0; i < tagCount; i++)
			{
				if (idxKey == Tags[i])
				{
					return _layer.Values[Tags[i + 1]];
				}
			}
			return null;
		}



	}
}
