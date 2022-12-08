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
			bool inside = false;
			int j = poly.Count - 1;
			for (int i = 0; i < poly.Count; i++)
			{
				if (poly[i].Y < p.Y && poly[j].Y >= p.Y || poly[j].Y < p.Y && poly[i].Y >= p.Y)
				{
					if (poly[i].X + (p.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) * (poly[j].X - poly[i].X) < p.X)
					{
						inside = !inside;
					}
				}
				j = i;
			}

			return inside;
		}
	}
}
