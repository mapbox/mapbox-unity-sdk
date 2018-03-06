using System.Linq;
using UnityEngine;
using Mapbox.Map;

namespace Mapbox.Unity.Map
{

	public class ARTableTileProvider : AbstractTileProvider
	{

		[SerializeField]
		private AbstractMap _basicMap;

		[SerializeField]
		private Transform _targetTransform;

		[SerializeField]
		private int _visibleBuffer;

		[SerializeField]
		private int _disposeBuffer;

		private bool _initialized = false;
		private UnwrappedTileId _currentTile;
		private UnwrappedTileId _cachedTile;


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
		}

		private void Update()
		{

			if (!_initialized) return;

			_currentTile = TileCover.CoordinateToTileId(_basicMap.WorldToGeoPosition(_targetTransform.position), (int)_map.Zoom);

			if (!_currentTile.Equals(_cachedTile))
			{
				for (int x = _currentTile.X - _visibleBuffer; x <= (_currentTile.X + _visibleBuffer); x++)
				{
					for (int y = _currentTile.Y - _visibleBuffer; y <= (_currentTile.Y + _visibleBuffer); y++)
					{
						AddTile(new UnwrappedTileId((int)_map.Zoom, x, y));
					}
				}
				_cachedTile = _currentTile;
				Cleanup(_currentTile);
			}
		}

		private void Cleanup(UnwrappedTileId currentTile)
		{
			var keys = _activeTiles.Keys.ToList();
			for (int i = 0; i < keys.Count; i++)
			{
				var tile = keys[i];
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