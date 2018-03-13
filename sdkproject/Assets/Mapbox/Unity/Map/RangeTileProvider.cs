namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using Mapbox.Map;

	public class RangeTileProvider : AbstractTileProvider
	{
		//[SerializeField]
		//private int _west = 2;
		//[SerializeField]
		//private int _north = 2;
		//[SerializeField]
		//private int _east = 2;
		//[SerializeField]
		//private int _south = 2;
		RangeTileProviderOptions _rangeTileProviderOptions;

		public override void OnInitialized()
		{
			if (Options != null)
			{
				_rangeTileProviderOptions = (RangeTileProviderOptions)Options;
			}
			else
			{
				_rangeTileProviderOptions = new RangeTileProviderOptions();
			}
			var centerTile = TileCover.CoordinateToTileId(_map.CenterLatitudeLongitude, _map.AbsoluteZoom);
			AddTile(new UnwrappedTileId(_map.AbsoluteZoom, centerTile.X, centerTile.Y));
			for (int x = (int)(centerTile.X - _rangeTileProviderOptions.west); x <= (centerTile.X + _rangeTileProviderOptions.east); x++)
			{
				for (int y = (int)(centerTile.Y - _rangeTileProviderOptions.north); y <= (centerTile.Y + _rangeTileProviderOptions.south); y++)
				{
					AddTile(new UnwrappedTileId(_map.AbsoluteZoom, x, y));
				}
			}
		}
	}
}
