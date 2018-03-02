namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Unity.MeshGeneration.Factories;
	public static class MapboxDefaultImagery
	{
		public static Style GetParameters(ImagerySourceType defaultImagery)
		{
			Style defaultStyle = new Style();
			switch (defaultImagery)
			{
				case ImagerySourceType.Streets:
					defaultStyle = new Style
					{
						Id = "mapbox://styles/mapbox/streets-v10",
						Name = "Streets"
					};

					break;
				case ImagerySourceType.Outdoors:
					defaultStyle = new Style
					{
						Id = "mapbox://styles/mapbox/outdoors-v10",
						Name = "Streets"
					};

					break;
				case ImagerySourceType.Dark:
					defaultStyle = new Style
					{
						Id = "mapbox://styles/mapbox/dark-v9",
						Name = "Dark"
					};

					break;
				case ImagerySourceType.Light:
					defaultStyle = new Style
					{
						Id = "mapbox://styles/mapbox/light-v9",
						Name = "Light"
					};

					break;
				case ImagerySourceType.Satellite:
					defaultStyle = new Style
					{
						Id = "mapbox.satellite",
						Name = "Satellite"
					};

					break;
				case ImagerySourceType.SatelliteStreet:
					defaultStyle = new Style
					{
						Id = "mapbox://styles/mapbox/satellite-streets-v10",
						Name = "Satellite Streets"
					};

					break;
				case ImagerySourceType.Custom:
					throw new Exception("Invalid type : Custom");
				case ImagerySourceType.None:
					throw new Exception("Invalid type : None");
				default:
					break;
			}

			return defaultStyle;
		}
	}

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
						Id = "mapbox.terrain - rgb",
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
