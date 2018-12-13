namespace KdTree.Math
{
    // Via http://www.geodatasource.com/developers/c-sharp
    // This code is licensed under LGPLv3.
    public class GeoUtils
	{
		public static double Distance(double lat1, double lon1, double lat2, double lon2, char unit)
		{
			double theta = lon1 - lon2;
			double dist = System.Math.Sin(Deg2rad(lat1)) * System.Math.Sin(Deg2rad(lat2)) + System.Math.Cos(Deg2rad(lat1)) * System.Math.Cos(Deg2rad(lat2)) * System.Math.Cos(Deg2rad(theta));
			dist = System.Math.Acos(dist);
			dist = Rad2deg(dist);
			dist = dist * 60 * 1.1515;
			if (unit == 'K')
			{
				dist = dist * 1.609344;
			}
			else if (unit == 'N')
			{
				dist = dist * 0.8684;
			}
			return (dist);
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		//::  This function converts decimal degrees to radians             :::
		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		private static double Deg2rad(double deg)
		{
			return (deg * System.Math.PI / 180.0);
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		//::  This function converts radians to decimal degrees             :::
		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		private static double Rad2deg(double rad)
		{
			return (rad / System.Math.PI * 180.0);
		}

	}
}
