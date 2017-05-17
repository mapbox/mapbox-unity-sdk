namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Unity.Utilities;
	using Mapbox.Utils;
	using System;
	using System.Collections;

	public class CameraBoundsTileProvider : AbstractTileProvider
	{
		[SerializeField]
		Camera _camera;

		[SerializeField]
		int _visibleBuffer;

		[SerializeField]
		int _disposeBuffer;

		[SerializeField]
		float _updateInterval;

		Plane _groundPlane;
		Ray _ray;
		float _hitDistance;

		Vector2d _currentLatitudeLongitude;
		UnwrappedTileId _cachedTile;
		UnwrappedTileId _currentTile;

		WaitForSeconds _wait;

		internal override void OnInitialized()
		{
			_groundPlane = new Plane(Vector3.up, Vector3.zero);
			_wait = new WaitForSeconds(_updateInterval);
			StartCoroutine(UpdateTileCover());
		}

		IEnumerator UpdateTileCover()
		{
			while (true)
			{
				_ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
				if (_groundPlane.Raycast(_ray, out _hitDistance))
				{
					_currentLatitudeLongitude = _ray.GetPoint(_hitDistance).GetGeoPosition(_map.CenterMercator, _map.WorldRelativeScale);
					_currentTile = TileCover.CoordinateToTileId(_currentLatitudeLongitude, _map.Zoom);

					// TODO: override?
					if (!_currentTile.Equals(_cachedTile))
					{
						for (int x = _currentTile.X - _visibleBuffer; x <= (_currentTile.X + _visibleBuffer); x++)
						{
							for (int y = _currentTile.Y - _visibleBuffer; y <= (_currentTile.Y + _visibleBuffer); y++)
							{
								AddTile(new UnwrappedTileId(_map.Zoom, x, y));
							}
						}
						_cachedTile = _currentTile;
						Cleanup(_currentTile);
					}
				}
				yield return _wait;
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
