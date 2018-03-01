namespace Mapbox.Unity.Map
{
	using System;
	[Serializable]
	public class MapExtentOptions
	{
		public MapExtentType extentType = MapExtentType.CameraBounds;

		public CameraBoundsTileProviderOptions cameraBoundsOptions;
		public RangeTileProviderOptions rangeAroundCenterOptions;
		public RangeAroundTransformTileProviderOptions rangeAroundTransformOptions;

		public MapExtentOptions(MapExtentType type)
		{
			extentType = type;
		}

		public ITileProviderOptions GetTileProviderOptions()
		{
			ITileProviderOptions options = new TileProviderOptions();
			switch (extentType)
			{
				case MapExtentType.CameraBounds:
					options = cameraBoundsOptions;// TileProviderOptions.CameraBoundsProviderOptions(camera, visibleBuffer, disposeBuffer, updateInterval);
					break;
				case MapExtentType.RangeAroundCenter:
					options = rangeAroundCenterOptions;// TileProviderOptions.RangeAroundCenterOptions(north, south, east, west);
					break;
				case MapExtentType.RangeAroundTransform:
					options = rangeAroundTransformOptions; //TileProviderOptions.RangeAroundTransformOptions(targetTransform, visibleBuffer, disposeBuffer);
					break;
				default:
					break;
			}
			return options;
		}
	}
}
