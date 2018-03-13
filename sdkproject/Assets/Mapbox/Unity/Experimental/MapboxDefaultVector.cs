namespace Mapbox.Unity.Map
{
	using System;

	public static class MapboxDefaultVector
	{
		public static Style GetParameters(VectorSourceType defaultElevation)
		{
			Style defaultStyle = new Style();
			switch (defaultElevation)
			{
				case VectorSourceType.MapboxStreets:
					defaultStyle = new Style
					{
						Id = "mapbox.mapbox-streets-v7",
						Name = "Mapbox Terrain"
					};

					break;
				case VectorSourceType.Custom:
					throw new Exception("Invalid type : Custom");
				default:
					break;
			}

			return defaultStyle;
		}
	}
}
