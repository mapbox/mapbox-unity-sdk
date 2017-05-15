namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Unity.Utilities;
	using Mapbox.Utils;

	public class CameraBoundsTileProvider : AbstractTileProvider
	{
		[SerializeField]
		Camera _camera;

		[SerializeField]
		int _visibleBuffer;

		[SerializeField]
		int _disposeBuffer;

		Plane _groundPlane;
		Ray _ray;
		float _hitDistance;

		Vector2d _currentLatitudeLongitude;
		UnwrappedTileId _cachedTile;
		UnwrappedTileId _currentTile;

		internal override void OnInitialized()
		{
			_groundPlane = new Plane(Vector3.up, Vector3.zero);
		}

		void Update()
		{
			_ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
			if (_groundPlane.Raycast(_ray, out _hitDistance))
			{
				_currentLatitudeLongitude = _ray.GetPoint(_hitDistance).GetGeoPosition(_mapController.ReferenceTileRect.Center, _mapController.WorldScaleFactor);
				_currentTile = TileCover.CoordinateToTileId(_currentLatitudeLongitude, _mapController.Zoom);

				if (!_currentTile.Equals(_cachedTile))
				{
					for (int x = _currentTile.X - _visibleBuffer; x <= (_currentTile.X + _visibleBuffer); x++)
					{
						for (int y = _currentTile.Y - _visibleBuffer; y <= (_currentTile.Y + _visibleBuffer); y++)
						{
							AddTile(new UnwrappedTileId(_mapController.Zoom, x, y));
						}
					}
					_cachedTile = _currentTile;
					Cleanup(_currentTile);
				}
			}
		}

		void Cleanup(UnwrappedTileId currentTile)
		{
			var count = _activeTiles.Count;
			for (int i = count - 1; i >= 0; i--)
			{
				var tile = _activeTiles[i];
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
