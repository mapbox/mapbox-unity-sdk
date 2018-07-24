namespace Mapbox.Unity.Map
{
	using System.Linq;
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Unity.Utilities;
	using Mapbox.Utils;
	using System.Collections.Generic;

	public class CameraBoundsTileProvider : AbstractTileProvider
	{
		private Plane _groundPlane;
		private Ray _ray;
		private float _hitDistance;
		private Vector3 _viewportTarget;
		private float _elapsedTime;
		private bool _shouldUpdate;

		private Vector2d _currentLatitudeLongitude;
		private UnwrappedTileId _cachedTile;
		private UnwrappedTileId _currentTile;
		private List<UnwrappedTileId> toRemove;

		private CameraBoundsTileProviderOptions _cbtpOptions;

		private UnwrappedTileId key;
		private bool _shouldDispose;

		public override void OnInitialized()
		{
			_cbtpOptions = (CameraBoundsTileProviderOptions)Options;
			_groundPlane = new Plane(Mapbox.Unity.Constants.Math.Vector3Up, Mapbox.Unity.Constants.Math.Vector3Zero);
			_viewportTarget = new Vector3(0.5f, 0.5f, 0);
			_shouldUpdate = true;
			_cachedTile = new UnwrappedTileId();
			toRemove = new List<UnwrappedTileId>();
		}

		protected virtual void Update()
		{
			if (!_shouldUpdate)
			{
				return;
			}

			_elapsedTime += Time.deltaTime;
			if (_elapsedTime >= _cbtpOptions.updateInterval)
			{
				_elapsedTime = 0f;
				_ray = _cbtpOptions.camera.ViewportPointToRay(_viewportTarget);
				if (_groundPlane.Raycast(_ray, out _hitDistance))
				{
					_currentLatitudeLongitude = _map.WorldToGeoPosition(_ray.GetPoint(_hitDistance));
					_currentTile = TileCover.CoordinateToTileId(_currentLatitudeLongitude, _map.AbsoluteZoom);

					if (!_currentTile.Equals(_cachedTile))
					{
						// FIXME: this results in bugs at world boundaries! Does not cleanly wrap. Negative tileIds are bad.
						for (int x = _currentTile.X - _cbtpOptions.visibleBuffer; x <= (_currentTile.X + _cbtpOptions.visibleBuffer); x++)
						{
							for (int y = _currentTile.Y - _cbtpOptions.visibleBuffer; y <= (_currentTile.Y + _cbtpOptions.visibleBuffer); y++)
							{
								AddTile(new UnwrappedTileId(_map.AbsoluteZoom, x, y));
							}
						}
						_cachedTile = _currentTile;
						Cleanup(_currentTile);
					}
				}
			}
		}

		void Cleanup(UnwrappedTileId currentTile)
		{
			toRemove.Clear();
			foreach (var tile in _activeTiles)
			{
				key = tile.Key;
				_shouldDispose = false;
				_shouldDispose = key.X > currentTile.X + _cbtpOptions.disposeBuffer || key.X < _currentTile.X - _cbtpOptions.disposeBuffer;
				_shouldDispose = _shouldDispose || key.Y > _currentTile.Y + _cbtpOptions.disposeBuffer || key.Y < _currentTile.Y - _cbtpOptions.disposeBuffer;

				if (_shouldDispose)
				{
					toRemove.Add(key);
				}
			}

			foreach (var item in toRemove)
			{
				RemoveTile(item);
			}
		}
	}
}
