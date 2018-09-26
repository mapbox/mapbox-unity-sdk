namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;

	[Serializable]
	public class MapExtentOptions : MapboxDataProperty
	{
		public MapExtentType extentType = MapExtentType.CameraBounds;
		public DefaultMapExtents defaultExtents = new DefaultMapExtents();

		public MapExtentOptions(MapExtentType type)
		{
			extentType = type;
		}

		public ExtentOptions GetTileProviderOptions()
		{
			ExtentOptions options = new ExtentOptions();
			switch (extentType)
			{
				case MapExtentType.CameraBounds:
					options = defaultExtents.cameraBoundsOptions;
					break;
				case MapExtentType.RangeAroundCenter:
					options = defaultExtents.rangeAroundCenterOptions;
					break;
				case MapExtentType.RangeAroundTransform:
					options = defaultExtents.rangeAroundTransformOptions;
					break;
				default:
					break;
			}
			return options;
		}
	}


	[Serializable]
	public class DefaultMapExtents : MapboxDataProperty
	{
		public CameraBoundsTileProviderOptions cameraBoundsOptions = new CameraBoundsTileProviderOptions();
		public RangeTileProviderOptions rangeAroundCenterOptions = new RangeTileProviderOptions();
		public RangeAroundTransformTileProviderOptions rangeAroundTransformOptions = new RangeAroundTransformTileProviderOptions();
	}
}
