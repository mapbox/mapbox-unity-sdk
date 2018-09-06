namespace Mapbox.Utils
{


	using System.Collections.Generic;
	using Mapbox.VectorTile.Geometry;


	public static class PolygonUtils
	{
		/// <summary>
		/// <para>Method to check if a point is contained inside a polygon, ignores vertical axis (y axis)</para>
		/// <para>from https://stackoverflow.com/a/7123291</para>
		/// </summary>
		/// <returns><c>true</c>, if point lies inside the constructed polygon, <c>false</c> otherwise.</returns>
		/// <param name="polygon">Polygon points.</param>
		/// <param name="p">The point that is to be tested.</param>
		public static bool PointInPolygon(Point2d<float> p, List<List<Point2d<float>>> polygon)
		{
			List<Point2d<float>> poly = polygon[0];

			Point2d<float> p1;
			Point2d<float> p2;
			bool inside = false;

			if (poly.Count < 3)
			{
				return inside;
			}

			var oldPoint = new Point2d<float>(
				poly[poly.Count - 1].X
				, poly[poly.Count - 1].Y
			);

			for (int i = 0; i < poly.Count; i++)
			{
				var newPoint = new Point2d<float>(poly[i].X, poly[i].Y);

				if (newPoint.X > oldPoint.X)
				{
					p1 = oldPoint;
					p2 = newPoint;
				}
				else
				{
					p1 = newPoint;
					p2 = oldPoint;
				}

				if (
					(newPoint.X < p.X) == (p.X <= oldPoint.X)
					&& (p.Y - (long)p1.Y) * (p2.X - p1.X) < (p2.Y - (long)p1.Y) * (p.X - p1.X)
				)
				{
					inside = !inside;
				}

				oldPoint = newPoint;
			}

			return inside;
		}



	}

}

