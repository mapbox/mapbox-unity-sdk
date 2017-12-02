namespace Mapbox.Unity.Map
{
	using System.Linq;
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;

	public class RangeAroundTransformTileProvider : AbstractTileProvider
	{
		[SerializeField]
		private Transform _targetTransform;

		[SerializeField]
		private int _visibleBuffer;

		[SerializeField]
		private int _disposeBuffer;

		private bool _initialized = false;
		private UnwrappedTileId _currentTile;
		private UnwrappedTileId _cachedTile;
		private int _counter;

		public override void OnInitialized()
		{
			if (_targetTransform == null)
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

			_currentTile = TileCover.CoordinateToTileId(_targetTransform.localPosition.GetGeoPosition(_map.CenterMercator, _map.WorldRelativeScale), _map.AbsoluteZoom);

			if (!_currentTile.Equals(_cachedTile))
			{
				for (int x = _currentTile.X - _visibleBuffer; x <= (_currentTile.X + _visibleBuffer); x++)
				{
					for (int y = _currentTile.Y - _visibleBuffer; y <= (_currentTile.Y + _visibleBuffer); y++)
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
				dispose = tile.X > currentTile.X + _disposeBuffer || tile.X < _currentTile.X - _disposeBuffer;
				dispose = dispose || tile.Y > _currentTile.Y + _disposeBuffer || tile.Y < _currentTile.Y - _disposeBuffer;

				if (dispose)
				{
					RemoveTile(tile);
				}
			}
		}
	}
}