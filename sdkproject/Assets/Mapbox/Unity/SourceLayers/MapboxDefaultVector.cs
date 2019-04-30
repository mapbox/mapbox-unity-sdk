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
						Name = "Mapbox Streets v7"
					};

					break;
				case VectorSourceType.MapboxStreetsV8:
					defaultStyle = new Style
					{
						Id = "mapbox.mapbox-streets-v8",
						Name = "Mapbox Streets v8"
					};

					break;
				case VectorSourceType.MapboxStreetsWithBuildingIds:
					defaultStyle = new Style
					{
						Id = "mapbox.3d-buildings,mapbox.mapbox-streets-v7",
						Name = "Mapbox Streets With Building Ids"
					};

					break;
				case VectorSourceType.MapboxStreetsV8WithBuildingIds:
					defaultStyle = new Style
					{
						Id = "mapbox.3d-buildings,mapbox.mapbox-streets-v8",
						Name = "Mapbox Streets v8 With Building Ids"
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
