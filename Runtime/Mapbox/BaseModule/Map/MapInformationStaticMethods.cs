using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.BaseModule.Map
{
    public static class MapInformationStaticMethods
    {
        public static LatitudeLongitude ConvertPositionToLatLng(this IMapInformation mapInfo, Vector3 position)
        {
            return ConvertPositionToLatLngForScale(mapInfo, position, mapInfo.Scale);
        }
        
        public static LatitudeLongitude ConvertPositionToLatLngForScale(this IMapInformation mapInfo, Vector3 position, float scale)
        {
            var unscaledPos = position * scale;
            var currentMercator = mapInfo.CenterMercator;
            var mercator = new Vector2d(currentMercator.x + unscaledPos.x, currentMercator.y + unscaledPos.z);
            return Conversions.WebMercatorToLatLon(mercator);
        }
        
        /// <summary>
        /// The point the camera is actually looking at: where a ray through the centre of the
        /// viewport meets the map plane. Reports false when there is no such point, rather than
        /// returning a placeholder - a camera pitched at or above the horizon has a centre ray that
        /// is parallel to the plane or points away from it, and the honest answer is "nowhere".
        ///
        /// Needed because the map's own centre is not always the view centre. A camera that pans by
        /// recentring the map keeps the two together; one that keeps the map origin fixed and moves
        /// itself does not, and then <see cref="IMapInformation.CenterMercator"/> is the world
        /// origin rather than anything the user is looking at.
        ///
        /// Deliberately computed on demand instead of being stored on the map: a cached view centre
        /// needs an owner to refresh it every frame and is wrong for any reader that runs before
        /// that owner, which is the failure this exists to avoid.
        ///
        /// The plane is world y = 0, matching the rest of the SDK's screen-to-map math, so a MapRoot
        /// that has been moved, rotated or parented (an AR anchor, say) is not accounted for. Terrain
        /// height is ignored too - this is the flat map plane, not the ground surface.
        /// </summary>
        public static bool TryGetViewCenterPosition(this IMapInformation mapInfo, Camera camera, out Vector3 position)
        {
            position = Vector3.zero;
            if (camera == null)
            {
                return false;
            }

            var plane = new Plane(Vector3.up, Vector3.zero);
            var screenCenter = camera.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0f));
            return camera.GetPlaneIntersection(plane, screenCenter, out position);
        }

        /// <summary>
        /// <see cref="TryGetViewCenterPosition"/> in web mercator, using the same
        /// position-to-mercator relation as <see cref="ConvertPositionToLatLngForScale"/>.
        /// </summary>
        public static bool TryGetViewCenterMercator(this IMapInformation mapInfo, Camera camera, out Vector2d mercator)
        {
            mercator = default;
            if (!mapInfo.TryGetViewCenterPosition(camera, out var position))
            {
                return false;
            }

            var unscaled = position * mapInfo.Scale;
            mercator = new Vector2d(mapInfo.CenterMercator.x + unscaled.x, mapInfo.CenterMercator.y + unscaled.z);
            return true;
        }

        /// <summary>
        /// <see cref="TryGetViewCenterPosition"/> as a coordinate.
        /// </summary>
        public static bool TryGetViewCenterLatLng(this IMapInformation mapInfo, Camera camera, out LatitudeLongitude latlng)
        {
            latlng = default;
            if (!mapInfo.TryGetViewCenterMercator(camera, out var mercator))
            {
                return false;
            }

            latlng = Conversions.WebMercatorToLatLon(mercator);
            return true;
        }

        public static Vector3 ConvertLatLngToPositionForScale(this IMapInformation mapInfo, LatitudeLongitude latlng, float scale)
        {
            var mercator = Conversions.LatitudeLongitudeToWebMercator(latlng);
            var deltaMercator = mercator - mapInfo.CenterMercator;
            var scaledDeltaMercator = deltaMercator / scale;
            var scaledDeltaMercatorVector3 = scaledDeltaMercator.ToVector3xz();
            
            return scaledDeltaMercatorVector3;
        }
        
        public static Vector3 ConvertLatLngToPosition(this IMapInformation mapInfo, LatitudeLongitude latlng, bool queryElevation = false)
        {
            var mercator = Conversions.LatitudeLongitudeToWebMercator(latlng);
            var deltaMercator = mercator - mapInfo.CenterMercator;
            var scaledDeltaMercator = deltaMercator / mapInfo.Scale;
            var elevation = 0f;
            
            if (queryElevation)
            {
                var tileId = Conversions.LatitudeLongitudeToTileId(latlng, 14).Canonical;
                var pointPosition = Conversions.LatitudeLongitudeToInTile01(latlng, tileId);
                elevation = mapInfo.QueryElevation(tileId, pointPosition.x, pointPosition.y);
            }
            
            var scaledDeltaMercatorVector3 = new Vector3((float)scaledDeltaMercator.x, elevation, (float)scaledDeltaMercator.y);
            
            return scaledDeltaMercatorVector3;
        }
        
        public static void PositionObjectFor(this IMapInformation mapInfo, GameObject go, CanonicalTileId tileId)
        {
            var topLeftValues = Conversions.TileTopLeftInUnitySpace(tileId, mapInfo.CenterMercator, mapInfo.Scale);
            var tileSize = Conversions.TileSizeInUnitySpace(tileId.Z, mapInfo.Scale);
            go.transform.localPosition = topLeftValues;
            go.transform.localScale = Vector3.one * tileSize;
        }
        
        public static void PositionObjectFor(this IMapInformation mapInfo, CanonicalTileId tileId, out Vector3 position, out Vector3 scale)
        {
            var topLeftValues = Conversions.TileTopLeftInUnitySpace(tileId, mapInfo.CenterMercator, mapInfo.Scale);
            var tileSize = Conversions.TileSizeInUnitySpace(tileId.Z, mapInfo.Scale);
            position = topLeftValues;
            scale = Vector3.one * tileSize;
        }
    }
}