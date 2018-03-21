namespace Mapbox.Unity.Map
{
	using System;

	public static class MapboxDefaultElevation
	{
		public static Style GetParameters(ElevationSourceType defaultElevation)
		{
			Style defaultStyle = new Style();
			switch (defaultElevation)
			{
				case ElevationSourceType.MapboxTerrain:
					defaultStyle = new Style
					{
						Id = "mapbox.terrain-rgb",
						Name = "Mapbox Terrain"
					};

					break;
				case ElevationSourceType.Custom:
					throw new Exception("Invalid type : Custom");
				default:
					break;
			}

			return defaultStyle;
		}
	}
}
