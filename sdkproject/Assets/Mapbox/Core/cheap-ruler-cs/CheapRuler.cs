namespace Mapbox.CheapRulerCs
{

	using System;


	public enum CheapRulerUnits
	{
		Kilometers,
		Miles,
		NauticalMiles,
		Meters,
		Yards,
		Feet,
		Inches
	}

	public class CheapRuler
	{


		private double _kx;
		private double _ky;


		/// <summary>
		/// Creates a ruler object that will approximate measurements around the given latitude. Units are one of: kilometers
		/// </summary>
		/// <param name="outputUnits"></param>
		public CheapRuler(double latitude, CheapRulerUnits outputUnits = CheapRulerUnits.Kilometers)
		{

			double factor;

			switch (outputUnits)
			{
				case CheapRulerUnits.Kilometers:
					factor = 1.0d;
					break;
				case CheapRulerUnits.Miles:
					factor = 1000.0d / 1609.344;
					break;
				case CheapRulerUnits.NauticalMiles:
					factor = 1000.0d / 1852.0d;
					break;
				case CheapRulerUnits.Meters:
					factor = 1000.0d;
					break;
				case CheapRulerUnits.Yards:
					factor = 1000.0d / 0.9144;
					break;
				case CheapRulerUnits.Feet:
					factor = 1000.0d / 0.3048;
					break;
				case CheapRulerUnits.Inches:
					factor = 1000.0d / 0.0254;
					break;
				default:
					factor = 1.0d;
					break;
			}

			var cos = Math.Cos(latitude * Math.PI / 180);
			var cos2 = 2 * cos * cos - 1;
			var cos3 = 2 * cos * cos2 - cos;
			var cos4 = 2 * cos * cos3 - cos2;
			var cos5 = 2 * cos * cos4 - cos3;

			// multipliers for converting longitude and latitude degrees into distance (http://1.usa.gov/1Wb1bv7)
			_kx = factor * (111.41513 * cos - 0.09455 * cos3 + 0.00012 * cos5);
			_ky = factor * (111.13209 - 0.56605 * cos2 + 0.0012 * cos4);
		}


		/// <summary>
		/// Creates a ruler object from tile coordinates.
		/// </summary>
		/// <param name="y">Y TileId</param>
		/// <param name="z">Zoom Level</param>
		/// <param name="units"></param>
		/// <returns></returns>
		public static CheapRuler FromTile(int y, int z, CheapRulerUnits units = CheapRulerUnits.Kilometers)
		{
			var n = Math.PI * (1 - 2 * (y + 0.5) / Math.Pow(2, z));
			var lat = Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n))) * 180 / Math.PI;
			return new CheapRuler(lat, units);
		}


		/// <summary>
		/// Given two points returns the distance.
		/// </summary>
		/// <param name="a">point [longitude, latitude]</param>
		/// <param name="b">point [longitude, latitude]</param>
		/// <returns>Distance</returns>
		public double Distance(double[] a, double[] b)
		{
			var dx = (a[0] - b[0]) * _kx;
			var dy = (a[1] - b[1]) * _ky;
			return Math.Sqrt(dx * dx + dy * dy);
		}


		/// <summary>
		/// Returns the bearing between two points in angles.
		/// </summary>
		/// <param name="a">a point [longitude, latitude]</param>
		/// <param name="b">b point [longitude, latitude]</param>
		/// <returns>Bearing</returns>
		public double Bearing(double[] a, double[] b)
		{
			var dx = (b[0] - a[0]) * _kx;
			var dy = (b[1] - a[1]) * _ky;
			if (dx == 0 && dy == 0)
			{
				return 0;
			}
			var bearing = Math.Atan2(dx, dy) * 180 / Math.PI;
			if (bearing > 180)
			{
				bearing -= 360;
			}
			return bearing;
		}


		/// <summary>
		/// Returns a new point given distance and bearing from the starting point.
		/// </summary>
		/// <param name="p"></param>
		/// <param name="distance"></param>
		/// <param name="bearing">point [longitude, latitude]</param>
		/// <returns></returns>
		public double[] Destination(double[] p, double distance, double bearing)
		{
			var a = (90 - bearing) * Math.PI / 180;
			return offset(
				p
				, Math.Cos(a) * distance
				, Math.Sin(a) * distance
			);
		}


		/// <summary>
		/// Returns a new point given easting and northing offsets (in ruler units) from the starting point.
		/// </summary>
		/// <param name="p">point [longitude, latitude]</param>
		/// <param name="dx">dx easting</param>
		/// <param name="dy">dy northing</param>
		/// <returns>point [longitude, latitude]</returns>
		private double[] offset(double[] p, double dx, double dy)
		{
			return new double[]
			{
				p[0] + dx / _kx,
				p[1] + dy / _ky
			};
		}



	}
}
