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
		//[SerializeField]
		//Camera _camera;

		//// TODO: change to Vector4 to optimize for different aspect ratios.
		//[SerializeField]
		//int _visibleBuffer;

		//[SerializeField]
		//int _disposeBuffer;

		//[SerializeField]
		//float _updateInterval;

		Plane _groundPlane;
		Ray _ray;
		float _hitDistance;
		Vector3 _viewportTarget;
		float _elapsedTime;
		bool _shouldUpdate;

		Vector2d _currentLatitudeLongitude;
		UnwrappedTileId _cachedTile;
		UnwrappedTileId _currentTile;
		List<UnwrappedTileId> toRemove;

		CameraBoundsTileProviderOptions _cbtpOptions;

		public override void OnInitialized()
		{
			_cbtpOptions = (CameraBoundsTileProviderOptions)Options;
			_groundPlane = new Plane(Mapbox.Unity.Constants.Math.Vector3Up, Mapbox.Unity.Constants.Math.Vector3Zero);
			_viewportTarget = new Vector3(0.5f, 0.5f, 0);
			_shouldUpdate = true;
			_cachedTile = new UnwrappedTileId();
			toRemove = new List<UnwrappedTileId>();
		}

		void Update()
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
			var _activeTilesKeys = _activeTiles.Keys.ToList();
			foreach (var tile in _activeTilesKeys)
			{
				bool dispose = false;
				dispose = tile.X > currentTile.X + _cbtpOptions.disposeBuffer || tile.X < _currentTile.X - _cbtpOptions.disposeBuffer;
				dispose = dispose || tile.Y > _currentTile.Y + _cbtpOptions.disposeBuffer || tile.Y < _currentTile.Y - _cbtpOptions.disposeBuffer;

				if (dispose)
				{
					toRemove.Add(tile);
				}
			}

			foreach (var item in toRemove)
			{
				RemoveTile(item);
			}
		}
	}
}
