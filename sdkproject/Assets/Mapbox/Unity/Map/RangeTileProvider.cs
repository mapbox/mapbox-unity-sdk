namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using Mapbox.Map;
	using System.Collections.Generic;

	public class RangeTileProvider : AbstractTileProvider
	{
		RangeTileProviderOptions _rangeTileProviderOptions;
		private bool _initialized = false;
		List<UnwrappedTileId> toRemove;
		HashSet<UnwrappedTileId> tilesToRequest;

		private int _activeTileCount;
		private UnwrappedTileId centerTile;

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

			_initialized = true;
			toRemove = new List<UnwrappedTileId>((_rangeTileProviderOptions.east + _rangeTileProviderOptions.west) * (_rangeTileProviderOptions.north + _rangeTileProviderOptions.south));
			tilesToRequest = new HashSet<UnwrappedTileId>();
		}


		protected virtual void Update()
		{
			if (!_initialized)
			{
				return;
			}

			if (Options == null)
			{
				return;
			}

			tilesToRequest.Clear();
			centerTile = TileCover.CoordinateToTileId(_map.CenterLatitudeLongitude, _map.AbsoluteZoom);
			tilesToRequest.Add(new UnwrappedTileId(_map.AbsoluteZoom, centerTile.X, centerTile.Y));

			for (int x = (int)(centerTile.X - _rangeTileProviderOptions.west); x <= (centerTile.X + _rangeTileProviderOptions.east); x++)
			{
				for (int y = (int)(centerTile.Y - _rangeTileProviderOptions.north); y <= (centerTile.Y + _rangeTileProviderOptions.south); y++)
				{
					tilesToRequest.Add(new UnwrappedTileId(_map.AbsoluteZoom, x, y));
				}
			}

			foreach (var item in _activeTiles)
			{
				if(!tilesToRequest.Contains(item.Key))
				{
					toRemove.Add(item.Key);
				}
			}

			foreach (var t2r in toRemove)
			{
				RemoveTile(t2r);
			}

			foreach (var tile in _activeTiles.Keys)
			{
				// Reposition tiles in case we panned.
				RepositionTile(tile);
			}

			foreach (var tile in tilesToRequest)
			{
				if (!_activeTiles.ContainsKey(tile))
				{
					AddTile(tile);
				}
			}
		}
	}
}
