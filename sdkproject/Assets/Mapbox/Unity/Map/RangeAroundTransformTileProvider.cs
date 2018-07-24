namespace Mapbox.Unity.Map
{
	using System.Linq;
	using UnityEngine;
	using Mapbox.Map;
	using System.Collections.Generic;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;

	public class RangeAroundTransformTileProvider : AbstractTileProvider
	{
		private RangeAroundTransformTileProviderOptions _rangeTileProviderOptions;

		private bool _initialized = false;
		private UnwrappedTileId _currentTile;
		private UnwrappedTileId _cachedTile;
		private int _counter;
		private List<UnwrappedTileId> toRemove;
		private HashSet<UnwrappedTileId> tilesToRequest;

		public override void OnInitialized()
		{
			_rangeTileProviderOptions = (RangeAroundTransformTileProviderOptions)Options;

			if (_rangeTileProviderOptions.targetTransform == null)
			{
				Debug.LogError("TransformTileProvider: No location marker transform specified.");
				Destroy(this);
			}
			else
			{
				_initialized = true;
			}
			_cachedTile = new UnwrappedTileId();
			toRemove = new List<UnwrappedTileId>(((_rangeTileProviderOptions.visibleBuffer*2) + 1) * ((_rangeTileProviderOptions.visibleBuffer * 2) + 1));
			tilesToRequest = new HashSet<UnwrappedTileId>();
		}

		protected virtual void Update()
		{
			if (!_initialized) return;

			tilesToRequest.Clear();
			toRemove.Clear();
			_currentTile = TileCover.CoordinateToTileId(_map.WorldToGeoPosition(_rangeTileProviderOptions.targetTransform.localPosition), _map.AbsoluteZoom);

			if (!_currentTile.Equals(_cachedTile))
			{
				for (int x = _currentTile.X - _rangeTileProviderOptions.visibleBuffer; x <= (_currentTile.X + _rangeTileProviderOptions.visibleBuffer); x++)
				{
					for (int y = _currentTile.Y - _rangeTileProviderOptions.visibleBuffer; y <= (_currentTile.Y + _rangeTileProviderOptions.visibleBuffer); y++)
					{
						tilesToRequest.Add(new UnwrappedTileId(_map.AbsoluteZoom, x, y));
					}
				}
				_cachedTile = _currentTile;

				foreach (var item in _activeTiles)
				{
					if (!tilesToRequest.Contains(item.Key))
					{
						toRemove.Add(item.Key);
					}
				}

				foreach (var t2r in toRemove)
				{
					RemoveTile(t2r);
				}

				foreach (var tile in _activeTiles)
				{
					// Reposition tiles in case we panned.
					RepositionTile(tile.Key);
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
}
