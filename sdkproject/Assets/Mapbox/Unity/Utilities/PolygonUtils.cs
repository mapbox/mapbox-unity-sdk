using UnityEngine;
using System.Collections.Generic;
using Mapbox.VectorTile.Geometry;
using Mapbox.VectorTile.Geometry.InteralClipperLib;

namespace Mapbox.Utils
{

	using Polygon = List<InternalClipper.IntPoint>;
	using Polygons = List<List<InternalClipper.IntPoint>>;


	public static class PolygonUtils
	{
		/// <summary>
		/// Method to check if a point is contained inside a polygon, ignores vertical axis (y axis),
		/// </summary>
		/// <returns><c>true</c>, if point lies inside the constructed polygon, <c>false</c> otherwise.</returns>
		/// <param name="polyPoints">Polygon points.</param>
		/// <param name="p">The point that is to be tested.</param>
		public static bool PointInPolygon(Point2d<float> coord, List<List<Point2d<float>>> poly)
		{
			var point = new InternalClipper.IntPoint(coord.X, coord.Y);
			var polygon = new Polygon();


			foreach (var vert in poly[0])
			{
				polygon.Add(new InternalClipper.IntPoint(vert.X, vert.Y));
			}

			//then check the actual polygon
			int result = InternalClipper.Clipper.PointInPolygon(point, polygon);
			return (result == 1) ? true : false;
		}
	}

}

