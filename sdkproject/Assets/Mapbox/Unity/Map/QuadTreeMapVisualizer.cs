namespace Mapbox.Unity.Map
{
	using Mapbox.Map;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;
	//TODO : obsolete.
	[CreateAssetMenu(menuName = "Mapbox/MapVisualizer/QuadTreeMapVisualizer")]
	public class QuadTreeMapVisualizer : AbstractMapVisualizer
	{
		protected override void PlaceTile(UnwrappedTileId tileId, UnityTile tile, IMapReadable map)
		{
			//get the tile covering the center (Unity 0,0,0) of current extent
			UnwrappedTileId centerTile = TileCover.CoordinateToTileId(map.CenterLatitudeLongitude, map.AbsoluteZoom);
			//get center WebMerc corrdinates of tile covering the center (Unity 0,0,0)
			Vector2d centerTileCenter = Conversions.TileIdToCenterWebMercator(centerTile.X, centerTile.Y, map.AbsoluteZoom);
			//calculate distance between WebMerc center coordinates of center tile and WebMerc coordinates exactly at center
			Vector2d shift = map.CenterMercator - centerTileCenter;
			var unityTileSize = map.UnityTileSize;
			// get factor at equator to avoid shifting errors at higher latitudes
			float factor = Conversions.GetTileScaleInMeters(0f, map.AbsoluteZoom) * 256.0f / unityTileSize;
			var scaleFactor = Mathf.Pow(2, (map.InitialZoom - map.AbsoluteZoom));

			//position the tile relative to the center tile of the current viewport using the tile id
			//multiply by tile size Unity units (unityTileScale)
			//shift by distance of current viewport center to center of center tile
			float shiftX = (float)shift.x / factor;
			float shiftY = (float)shift.y / factor;
			Vector3 position = new Vector3(
				((tileId.X - centerTile.X) * unityTileSize - shiftX) * scaleFactor
				, 0
				, ((centerTile.Y - tileId.Y) * unityTileSize - shiftY) * scaleFactor);
			tile.transform.localPosition = position;
		}
	}
}