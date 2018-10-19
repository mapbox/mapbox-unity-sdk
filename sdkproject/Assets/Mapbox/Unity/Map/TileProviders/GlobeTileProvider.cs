using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Utils;

namespace Mapbox.Unity.Map.TileProviders
{
	public class GlobeTileProvider : AbstractTileProvider
	{
		public override void OnInitialized()
		{
			_currentExtent.activeTiles = new HashSet<UnwrappedTileId>();
		}

		public override void UpdateTileExtent()
		{
			// HACK: don't allow too many tiles to be requested.
			if (_map.AbsoluteZoom > 5)
			{
				throw new System.Exception("Too many tiles! Use a lower zoom level!");
			}

			var tileCover = TileCover.Get(Vector2dBounds.World(), _map.AbsoluteZoom);
			foreach (var tile in tileCover)
			{
				_currentExtent.activeTiles.Add(new UnwrappedTileId(tile.Z, tile.X, tile.Y));
			}
			OnExtentChanged();
		}
		public override bool Cleanup(UnwrappedTileId tile)
		{
			return (!_currentExtent.activeTiles.Contains(tile));
		}
	}
}
