namespace Mapbox.Unity.Map
{
	using Mapbox.Map;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;

	[CreateAssetMenu(menuName = "Mapbox/MapVisualizer/DynamicZoomMapVisualizer")]
	public class DynamicZoomMapVisualizer : AbstractMapVisualizer
	{
		protected override void PlaceTile(UnwrappedTileId tileId, UnityTile tile, IMapReadable map)
		{
			//get the tile covering the center (Unity 0,0,0) of current extent
			UnwrappedTileId centerTile = TileCover.WebMercatorToTileId(map.CenterMercator, _map.Zoom);
			//get center WebMerc corrdinates of tile covering the center (Unity 0,0,0)
			Vector2d centerTileCenter = Conversions.TileIdToCenterWebMercator(centerTile.X, centerTile.Y, _map.Zoom);
			//calculate distance between WebMerc center coordinates of center tile and WebMerc coordinates exactly at center
			Vector2d shift = map.CenterMercator - centerTileCenter;
			var unityTileSize = map.UnityTileSize;
			// get factor at equator to avoid shifting errors at higher latitudes
			float factor = Conversions.GetTileScaleInMeters(0f, _map.Zoom) * 256 / unityTileSize;

			Vector3 unityTileScale = new Vector3(unityTileSize, 1, unityTileSize);

			//position the tile relative to the center tile of the current viewport using the tile id
			//multiply by tile size Unity units (unityTileScale)
			//shift by distance of current viewport center to center of center tile
			Vector3 position = new Vector3(
				(tileId.X - centerTile.X) * unityTileSize - (float)shift.x / factor
				, 0
				, (centerTile.Y - tileId.Y) * unityTileSize - (float)shift.y / factor);
			tile.transform.localPosition = position;
			tile.transform.localScale = unityTileScale;
		}
	}
}