namespace Mapbox.Unity.Map
{
	using UnityEngine;

	public interface ITileProviderOptions
	{

	}

	public class TileProviderOptions : ITileProviderOptions
	{
		public static ITileProviderOptions RangeAroundCenterOptions(int northRange, int southRange, int eastRange, int westRange)
		{
			return new RangeTileProviderOptions()
			{
				west = westRange,
				north = northRange,
				east = eastRange,
				south = southRange
			};
		}

		public static ITileProviderOptions RangeAroundTransformOptions(Transform tgtTransform, int visibleRange, int disposeRange)
		{
			return new RangeAroundTransformTileProviderOptions
			{
				targetTransform = tgtTransform,
				visibleBuffer = visibleRange,
				disposeBuffer = disposeRange,
			};
		}
		public static ITileProviderOptions CameraBoundsProviderOptions(Camera camera, int visibleRange, int disposeRange, float updateTime)
		{
			return new CameraBoundsTileProviderOptions
			{
				camera = camera,
				visibleBuffer = visibleRange,
				disposeBuffer = disposeRange,
				updateInterval = updateTime
			};
		}
	}
}
