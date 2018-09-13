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
#if !ENABLE_WINMD_SUPPORT
			[Description("location service enabled")]
#endif
			LocationServiceEnabled = 0,
#if !ENABLE_WINMD_SUPPORT
			[Description("location service intializing")]
#endif
			LocationServiceInitializing = 1,
#if !ENABLE_WINMD_SUPPORT
			[Description("location updated")]
#endif
			LocationUpdated = 2,
#if !ENABLE_WINMD_SUPPORT
			[Description("userheading updated")]
#endif
			UserHeadingUpdated = 3,
#if !ENABLE_WINMD_SUPPORT
			[Description("location provider")]
#endif
			LocationProvider = 4,
#if !ENABLE_WINMD_SUPPORT
			[Description("location provider class")]
#endif
			LocationProviderClass = 5,
#if !ENABLE_WINMD_SUPPORT
			[Description("time device [utc]")]
#endif
			UtcTimeDevice = 6,
#if !ENABLE_WINMD_SUPPORT
			[Description("time location [utc]")]
#endif
			UtcTimeOfLocation = 7,
#if !ENABLE_WINMD_SUPPORT
			[Description("latitude")]
#endif
			Latitude = 8,
#if !ENABLE_WINMD_SUPPORT
			[Description("longitude")]
#endif
			Longitude = 9,
#if !ENABLE_WINMD_SUPPORT
			[Description("accuracy [m]")]
#endif
			Accuracy = 10,
#if !ENABLE_WINMD_SUPPORT
			[Description("user heading [°]")]
#endif
			UserHeading = 11,
#if !ENABLE_WINMD_SUPPORT
			[Description("device orientation [°]")]
#endif
			DeviceOrientation = 12,
#if !ENABLE_WINMD_SUPPORT
			[Description("speed [km/h]")]
#endif
			Speed = 13,
#if !ENABLE_WINMD_SUPPORT
			[Description("has gps fix")]
#endif
			HasGpsFix = 14,
#if !ENABLE_WINMD_SUPPORT
			[Description("satellites used")]
#endif
			SatellitesUsed = 15,
#if !ENABLE_WINMD_SUPPORT
			[Description("satellites in view")]
#endif
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
