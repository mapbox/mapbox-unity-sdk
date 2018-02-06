namespace Mapbox.Unity.Map
{
	using System.Linq;
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;

	public class RangeAroundTransformTileProvider : AbstractTileProvider
	{
		//[SerializeField]
		//private Transform _targetTransform;

		//[SerializeField]
		//private int _visibleBuffer;

		//[SerializeField]
		//private int _disposeBuffer;

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

		private void Update()
		{
			if (!_initialized) return;

			_currentTile = TileCover.CoordinateToTileId(_rangeTileProviderOptions.targetTransform.localPosition.GetGeoPosition(_map.CenterMercator, _map.WorldRelativeScale), _map.AbsoluteZoom);

			if (!_currentTile.Equals(_cachedTile))
			{
				for (int x = _currentTile.X - _rangeTileProviderOptions.visibleBuffer; x <= (_currentTile.X + _rangeTileProviderOptions.visibleBuffer); x++)
				{
					for (int y = _currentTile.Y - _rangeTileProviderOptions.visibleBuffer; y <= (_currentTile.Y + _rangeTileProviderOptions.visibleBuffer); y++)
					{
						AddTile(new UnwrappedTileId(_map.AbsoluteZoom, x, y));
					}
				}
				_cachedTile = _currentTile;
				Cleanup(_currentTile);
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