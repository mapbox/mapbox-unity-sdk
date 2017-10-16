﻿namespace Mapbox.Unity.Map
{
	using Mapbox.Utils;
	using Mapbox.Map;

	public class GlobeTileProvider : AbstractTileProvider
	{
		public override void OnInitialized()
		{
			// HACK: don't allow too many tiles to be requested.
			if (_map.Zoom > 5)
			{
				throw new System.Exception("Too many tiles! Use a lower zoom level!");
			}

			var tileCover = TileCover.Get(Vector2dBounds.World(), _map.Zoom);
			foreach (var tile in tileCover)
			{
				AddTile(new UnwrappedTileId(tile.Z, tile.X, tile.Y));
			}
		}
	}
}
