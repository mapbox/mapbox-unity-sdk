namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using Mapbox.Map;
	using System.Collections.Generic;
	using System.Linq;

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


			//foreach (var tile in tilesToRequest)
			//{
			//	AddTile(tile);
			//}
		}

		private void Update()
		{
			if (Options == null)
			{
				return;
			}
			var activeTiles = _activeTiles.Keys.ToList();

			List<UnwrappedTileId> tilesToRequest = new List<UnwrappedTileId>();
			var centerTile = TileCover.CoordinateToTileId(_map.CenterLatitudeLongitude, _map.AbsoluteZoom);
			tilesToRequest.Add(new UnwrappedTileId(_map.AbsoluteZoom, centerTile.X, centerTile.Y));

			for (int x = (int)(centerTile.X - _rangeTileProviderOptions.west); x <= (centerTile.X + _rangeTileProviderOptions.east); x++)
			{
				for (int y = (int)(centerTile.Y - _rangeTileProviderOptions.north); y <= (centerTile.Y + _rangeTileProviderOptions.south); y++)
				{
					tilesToRequest.Add(new UnwrappedTileId(_map.AbsoluteZoom, x, y));
				}
			}

			List<UnwrappedTileId> toRemove = activeTiles.Except(tilesToRequest).ToList();
			foreach (var t2r in toRemove) { RemoveTile(t2r); }
			var finalTilesNeeded = tilesToRequest.Except(activeTiles);

			foreach (var tile in activeTiles)
			{
				// Reposition tiles in case we panned.
				RepositionTile(tile);
			}

			foreach (var tile in finalTilesNeeded)
			{
				AddTile(tile);
			}
		}
	}
}
