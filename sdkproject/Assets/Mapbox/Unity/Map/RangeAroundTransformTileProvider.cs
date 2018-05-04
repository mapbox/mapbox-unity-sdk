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
		RangeAroundTransformTileProviderOptions _rangeTileProviderOptions;

		private bool _initialized = false;
		private UnwrappedTileId _currentTile;
		private UnwrappedTileId _cachedTile;
		private int _counter;

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
		}

		protected virtual void Update()
		{
			if (!_initialized) return;

			var activeTiles = _activeTiles.Keys.ToList();

			List<UnwrappedTileId> tilesToRequest = new List<UnwrappedTileId>();
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
				Cleanup(_currentTile);

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

		private void Cleanup(UnwrappedTileId currentTile)
		{
			var _activeTilesKeys = _activeTiles.Keys.ToList();
			foreach (var tile in _activeTilesKeys)
			{
				bool dispose = false;
				dispose = tile.X > currentTile.X + _rangeTileProviderOptions.disposeBuffer || tile.X < _currentTile.X - _rangeTileProviderOptions.disposeBuffer;
				dispose = dispose || tile.Y > _currentTile.Y + _rangeTileProviderOptions.disposeBuffer || tile.Y < _currentTile.Y - _rangeTileProviderOptions.disposeBuffer;

				if (dispose)
				{
					RemoveTile(tile);
				}
			}
		}
	}
}