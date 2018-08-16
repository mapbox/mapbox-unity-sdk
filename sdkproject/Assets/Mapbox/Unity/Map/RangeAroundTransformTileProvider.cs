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

		//private List<UnwrappedTileId> _toRemove;
		//private HashSet<UnwrappedTileId> _tilesToRequest;

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
			//_toRemove = new List<UnwrappedTileId>(((_rangeTileProviderOptions.visibleBuffer * 2) + 1) * ((_rangeTileProviderOptions.visibleBuffer * 2) + 1));
			_currentExtent.activeTiles = new HashSet<UnwrappedTileId>();
			_map.OnInitialized += UpdateTileExtent;
			_map.OnUpdated += UpdateTileExtent;
		}

		public override void UpdateTileExtent()
		{
			if (!_initialized) return;

			_currentExtent.activeTiles.Clear();
			//_toRemove.Clear();
			_currentTile = TileCover.CoordinateToTileId(_map.WorldToGeoPosition(_rangeTileProviderOptions.targetTransform.localPosition), _map.AbsoluteZoom);

			if (!_currentTile.Equals(_cachedTile))
			{
				for (int x = _currentTile.X - _rangeTileProviderOptions.visibleBuffer; x <= (_currentTile.X + _rangeTileProviderOptions.visibleBuffer); x++)
				{
					for (int y = _currentTile.Y - _rangeTileProviderOptions.visibleBuffer; y <= (_currentTile.Y + _rangeTileProviderOptions.visibleBuffer); y++)
					{
						_currentExtent.activeTiles.Add(new UnwrappedTileId(_map.AbsoluteZoom, x, y));
					}
				}
				_cachedTile = _currentTile;
				OnExtentChanged();
			}
		}

		public virtual void Update()
		{
			if (_rangeTileProviderOptions != null && _rangeTileProviderOptions.targetTransform != null && _rangeTileProviderOptions.targetTransform.hasChanged)
			{
				UpdateTileExtent();
				_rangeTileProviderOptions.targetTransform.hasChanged = false;

			}
		}
	}
}
