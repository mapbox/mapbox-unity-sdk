namespace Mapbox.Unity.Location
{


	using System;
	using System.ComponentModel;
	using System.Globalization;
	using Mapbox.VectorTile.ExtensionMethods;


	/// <summary>
	/// Base class for reading/writing location logs
	/// </summary>
	public abstract class LocationLogAbstractBase
	{


		public readonly string Delimiter = ";";
		protected readonly CultureInfo _invariantCulture = CultureInfo.InvariantCulture;


		public enum LogfileColumns
		{
			[Description("location service enabled")]
			LocationServiceEnabled = 0,
			[Description("location service intializing")]
			LocationServiceInitializing = 1,
			[Description("location updated")]
			LocationUpdated = 2,
			[Description("userheading updated")]
			UserHeadingUpdated = 3,
			[Description("location provider")]
			LocationProvider = 4,
			[Description("location provider class")]
			LocationProviderClass = 5,
			[Description("time device [utc]")]
			UtcTimeDevice = 6,
			[Description("time location [utc]")]
			UtcTimeOfLocation = 7,
			[Description("latitude")]
			Latitude = 8,
			[Description("longitude")]
			Longitude = 9,
			[Description("accuracy [m]")]
			Accuracy = 10,
			[Description("user heading [°]")]
			UserHeading = 11,
			[Description("device orientation [°]")]
			DeviceOrientation = 12,
			[Description("speed [km/h]")]
			Speed = 13,
			[Description("has gps fix")]
			HasGpsFix = 14,
			[Description("satellites used")]
			SatellitesUsed = 15,
			[Description("satellites in view")]
			SatellitesInView = 16
		}


		public string[] HeaderNames
		{
			get
			{
				Type enumType = typeof(LogfileColumns);
				Array arrEnumVals = Enum.GetValues(enumType);
				string[] hdrs = new string[arrEnumVals.Length];
				for (int i = 0; i < arrEnumVals.Length; i++)
				{
					hdrs[i] = ((LogfileColumns)Enum.Parse(enumType, arrEnumVals.GetValue(i).ToString())).Description();

				}
				return hdrs;
			}
		}



	}
}
