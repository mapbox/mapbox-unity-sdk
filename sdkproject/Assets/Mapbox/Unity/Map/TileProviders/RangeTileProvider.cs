using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Unity.DataContainers;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;

namespace Mapbox.Unity.Map.TileProviders
{
	public class RangeTileProvider : AbstractTileProvider
	{
		private RangeTileProviderOptions _rangeTileProviderOptions;
		private bool _initialized = false;
		private float _zoomLevel;

		public override void OnInitialized()
		{
			if (Options != null)
			{
				_rangeTileProviderOptions = (RangeTileProviderOptions) Options;
			}
			else
			{
				_rangeTileProviderOptions = new RangeTileProviderOptions();
			}

			_initialized = true;
			_currentExtent.ActiveTiles = new HashSet<UnwrappedTileId>();
		}

		public override void UpdateTileExtent()
		{
			if (!_initialized || _rangeTileProviderOptions == null)
			{
				return;
			}

			if (_zoomLevel < _map.Zoom)
			{
				_currentExtent.ZoomState = ZoomState.ZoomIn;
			}
			else if (_zoomLevel > _map.Zoom)
			{
				_currentExtent.ZoomState = ZoomState.ZoomOut;
			}
			else
			{
				_currentExtent.ZoomState = ZoomState.NoChange;
			}

			//_currentExtent.Bounds = new Vector2dBounds();
			var centerTile = TileCover.CoordinateToTileId(_map.CenterLatitudeLongitude, _map.AbsoluteZoom);
			var bottomLeft = Conversions.TileIdToBounds(centerTile.X - 1, centerTile.Y + 1, centerTile.Z);
			var topRight = Conversions.TileIdToBounds(centerTile.X + 1, centerTile.Y - 1, centerTile.Z);
			_currentExtent.Bounds = new Vector2dBounds(Conversions.LatLonToMeters(bottomLeft.SouthWest.y, bottomLeft.SouthWest.x), Conversions.LatLonToMeters(topRight.NorthEast.y, topRight.NorthEast.x));
			_currentExtent.ActiveTiles.Clear();
			_currentExtent.ActiveTiles.Add(new UnwrappedTileId(_map.AbsoluteZoom, centerTile.X, centerTile.Y));

			for (int x = (centerTile.X - _rangeTileProviderOptions.west); x <= (centerTile.X + _rangeTileProviderOptions.east); x++)
			{
				for (int y = (centerTile.Y - _rangeTileProviderOptions.north); y <= (centerTile.Y + _rangeTileProviderOptions.south); y++)
				{
					_currentExtent.ActiveTiles.Add(new UnwrappedTileId(_map.AbsoluteZoom, x, y));
				}
			}

			_zoomLevel = _map.Zoom;
			OnExtentChanged();
		}

		public override bool Cleanup(UnwrappedTileId tile)
		{
			return (!_currentExtent.ActiveTiles.Contains(tile));
		}
	}
}