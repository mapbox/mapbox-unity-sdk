using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;

namespace Mapbox.BaseModule.Map
{
    public static class MapboxMapExtensions
    {
        public static bool TryGetMapTile(this MapboxMap map, UnwrappedTileId tileId, out UnityMapTile tile)
        {
            if (map.MapVisualizer.ActiveTiles.TryGetValue(tileId, out tile))
            {
                return true;
            }

            tile = null;
            return false;
        }
        
        public static bool TryGetMapTile(this MapboxMap map, LatitudeLongitude coordinates, out UnityMapTile tile, int maxZoomLevel = 20)
        {
            var maxTileId = Conversions.LatitudeLongitudeToTileId(coordinates, maxZoomLevel);
            for (int i = maxZoomLevel; i > 1; i--)
            {
                if (map.MapVisualizer.ActiveTiles.TryGetValue(maxTileId, out tile))
                {
                    return true;
                }
            }

            tile = null;
            return false;
        }

        /// <summary>
        /// Only queries active tiles / visible world
        /// </summary>
        /// <param name="map"></param>
        /// <param name="location"></param>
        /// <param name="elevation"></param>
        /// <param name="maxZoomLevel"></param>
        /// <param name="minZoomLevel"></param>
        /// <returns></returns>
        public static bool TryGetElevation(this MapboxMap map, LatitudeLongitude location, out float elevation, int maxZoomLevel = 20, int minZoomLevel = 2)
        {
            var maxTileId = Conversions.LatitudeLongitudeToTileId(location, maxZoomLevel);
            UnityMapTile tile = null;
            for (int i = maxZoomLevel; i >= minZoomLevel; i--)
            {
                if (map.MapVisualizer.ActiveTiles.TryGetValue(maxTileId, out tile))
                {
                    break;
                }
            }

            if (tile != null)
            {
                var tilePos = Conversions.LatitudeLongitudeToInTile01(location, tile.TerrainContainer.TerrainData.TileId);
                elevation = tile.TerrainContainer.QueryHeightData(tilePos.x, tilePos.y);
                return true;
            }

            elevation = float.MinValue;
            return false;
        }
        
        public static void ClearCachedData(this MapboxMap map)
        {
            map.MapService.ClearCachedData();
        }
    }
}