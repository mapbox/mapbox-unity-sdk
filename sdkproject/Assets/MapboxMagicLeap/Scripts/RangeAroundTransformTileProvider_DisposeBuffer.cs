namespace Mapbox.Examples.MagicLeap
{
	using Mapbox.Unity.Map;
	using System.Linq;
	using UnityEngine;
	using Mapbox.Map;
	using System.Collections.Generic;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;

	public class RangeAroundTransformTileProvider_DisposeBuffer : AbstractTileProvider
	{
		[SerializeField]
		private Transform targetTransform;
		[SerializeField]
		private int visibleBuffer;

		private bool _initialized = false;
		private UnwrappedTileId _currentTile;
		private UnwrappedTileId _cachedTile;

		public override void OnInitialized()
		{

			if (targetTransform == null)
			{
				Debug.LogError("TransformTileProvider: No location marker transform specified.");
				Destroy(this);
			}
			else
			{
				_initialized = true;
			}
			_cachedTile = new UnwrappedTileId();
			_currentExtent.activeTiles = new HashSet<UnwrappedTileId>();
			_map.OnInitialized += UpdateTileExtent;
			_map.OnUpdated += UpdateTileExtent;
		}

		public override void UpdateTileExtent()
		{
			if (!_initialized) return;

			_currentTile = TileCover.CoordinateToTileId(_map.WorldToGeoPosition(targetTransform.localPosition), _map.AbsoluteZoom);

			if (!_currentTile.Equals(_cachedTile))
			{
				//add new tiles to current extent
				for (int x = _currentTile.X - visibleBuffer; x <= (_currentTile.X + visibleBuffer); x++)
				{
					for (int y = _currentTile.Y - visibleBuffer; y <= (_currentTile.Y + visibleBuffer); y++)
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
			if (targetTransform != null
				&& (targetTransform.hasChanged || _map.Root.transform.hasChanged))
			{
				UpdateTileExtent();
				targetTransform.hasChanged = false;

			}
		}
	}
}
