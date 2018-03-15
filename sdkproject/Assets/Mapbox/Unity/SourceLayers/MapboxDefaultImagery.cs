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
				case ImagerySourceType.MapboxStreets:
					defaultStyle = new Style
					{
						Id = "mapbox://styles/mapbox/streets-v10",
						Name = "Streets"
					};

					break;
				case ImagerySourceType.MapboxOutdoors:
					defaultStyle = new Style
					{
						Id = "mapbox://styles/mapbox/outdoors-v10",
						Name = "Streets"
					};

					break;
				case ImagerySourceType.MapboxDark:
					defaultStyle = new Style
					{
						Id = "mapbox://styles/mapbox/dark-v9",
						Name = "Dark"
					};

					break;
				case ImagerySourceType.MapboxLight:
					defaultStyle = new Style
					{
						Id = "mapbox://styles/mapbox/light-v9",
						Name = "Light"
					};

					break;
				case ImagerySourceType.MapboxSatellite:
					defaultStyle = new Style
					{
						Id = "mapbox.satellite",
						Name = "Satellite"
					};

					break;
				case ImagerySourceType.MapboxSatelliteStreet:
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
}
