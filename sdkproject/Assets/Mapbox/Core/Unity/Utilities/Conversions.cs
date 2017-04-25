//-----------------------------------------------------------------------
// <copyright file="Conversions.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Unity.Utilities
{
    using System;
    using Mapbox.Utils;
    using UnityEngine;

    /// <summary>
    /// A set of Geo and Terrain Conversion utils.
    /// </summary>
    public static class Conversions
    {
        private const int TileSize = 256;
        private const int EarthRadius = 6378137;
        private const double InitialResolution = 2 * Math.PI * EarthRadius / TileSize;
        private const double OriginShift = 2 * Math.PI * EarthRadius / 2;

        /// <summary>
        /// Converts <see cref="T:Mapbox.Utils.Vector2d"/> struct, WGS84
        /// lat/lon to Spherical Mercator EPSG:900913 xy meters.
        /// </summary>
        /// <param name="v"> The <see cref="T:Mapbox.Utils.Vector2d"/>. </param>
        /// <returns> A <see cref="T:UnityEngine.Vector2d"/> of coordinates in meters. </returns>
        private static Vector2d LatLonToMeters(Vector2d v)
        {
            return LatLonToMeters(v.x, v.y);
        }

        /// <summary>
        /// Converts WGS84 lat/lon to Spherical Mercator EPSG:900913 xy meters.
        /// SOURCE: http://stackoverflow.com/questions/12896139/geographic-coordinates-converter.
        /// </summary>
        /// <param name="lat"> The latitude. </param>
        /// <param name="lon"> The longitude. </param>
        /// <returns> A <see cref="T:UnityEngine.Vector2d"/> of xy meters. </returns>
        private static Vector2d LatLonToMeters(double lat, double lon)
        {
            var posx = lon * OriginShift / 180;
            var posy = Math.Log(Math.Tan((90 + lat) * Math.PI / 360)) / (Math.PI / 180);
            posy = posy * OriginShift / 180;
            //losing precision by converting to float here if lat/lng is too high/low.
            //prefer using GeoToWorldPosition 
            return new Vector2d((float)posx, (float)posy);
        }

        /// <summary>
        /// Converts WGS84 lat/lon to x/y meters in reference to a center point
        /// </summary>
        /// <param name="lat"> The latitude. </param>
        /// <param name="lon"> The longitude. </param>
        /// <param name="refPoint"> A <see cref="T:UnityEngine.Vector2d"/> center point to offset resultant xy</param>
        /// <param name="scale"> Scale in meters. (default scale = 1) </param>
        /// <returns> A <see cref="T:UnityEngine.Vector2d"/> xy tile ID. </returns>
        /// <example>
        /// Converts a Lat/Lon of (37.7749, 122.4194) into Unity coordinates for a map centered at (10,10) and a scale of 2.5 meters for every 1 Unity unit 
        /// <code>
        /// var worldPosition = Conversions.GeoToWorldPosition(37.7749, 122.4194, new Vector2d(10, 10), (float)2.5);
        /// // worldPosition = ( 11369163.38585, 34069138.17805 )
        /// </code>
        /// </example>
        public static Vector2d GeoToWorldPosition(double lat, double lon, Vector2d refPoint, float scale = 1)
        {
            var posx = lon * OriginShift / 180;
            var posy = Math.Log(Math.Tan((90 + lat) * Math.PI / 360)) / (Math.PI / 180);
            posy = posy * OriginShift / 180;
            return new Vector2d((posx - refPoint.x) * scale, (posy - refPoint.y) * scale);
        }

        public static Vector2d GeoToWorldPosition(Vector2d latLong, Vector2d refPoint, float scale = 1)
        {
            return GeoToWorldPosition(latLong.x, latLong.y, refPoint, scale);
        }

        /// <summary>
        /// Converts Spherical Mercator EPSG:900913 in xy meters to WGS84 lat/lon.
        /// Inverse of LatLonToMeters.
        /// </summary>
        /// <param name="m"> A <see cref="T:UnityEngine.Vector2d"/> of coordinates in meters.  </param>
        /// <returns> The <see cref="T:Mapbox.Utils.Vector2d"/> in lat/lon. </returns>

        /// <example>
        /// Converts EPSG:900913 xy meter coordinates to lat lon 
        /// <code>
        /// var worldPosition =  new Vector2d (4547675.35434,13627665.27122);
        /// var latlon = Conversions.MetersToLatLon(worldPosition);
        /// // latlon = ( 37.77490, 122.41940 )
        /// </code>
        /// </example>
        public static Vector2d MetersToLatLon(Vector2d m)
        {
            var vx = (m.x / OriginShift) * 180;
            var vy = (m.y / OriginShift) * 180;
            vy = 180 / Math.PI * (2 * Math.Atan(Math.Exp(vy * Math.PI / 180)) - Math.PI / 2);
            return new Vector2d(vy, vx);
        }

        /// <summary>
        /// Gets the xy tile ID from Spherical Mercator EPSG:900913 xy coords.
        /// </summary>
        /// <param name="m"> <see cref="T:UnityEngine.Vector2d"/> XY coords in meters. </param>
        /// <param name="zoom"> Zoom level. </param>
        /// <returns> A <see cref="T:UnityEngine.Vector2d"/> xy tile ID. </returns>
        /// 
        /// <example>
        /// Converts EPSG:900913 xy meter coordinates to web mercator tile XY coordinates at zoom 12.
        /// <code>
        /// var meterXYPosition = new Vector2d (4547675.35434,13627665.27122);
        /// var tileXY = Conversions.MetersToTile (meterXYPosition, 12);
        /// // tileXY = ( 655, 2512 )
        /// </code>
        /// </example>
        public static Vector2 MetersToTile(Vector2d m, int zoom)
        {
            var p = MetersToPixels(m, zoom);
            return PixelsToTile(p);
        }

        /// <summary>
        /// Gets the tile bounds in Spherical Mercator EPSG:900913 meters from an xy tile ID.
        /// </summary>
        /// <param name="tileCoordinate"> XY tile ID. </param>
        /// <param name="zoom"> Zoom level. </param>
        /// <returns> A <see cref="T:UnityEngine.Rect"/> in meters. </returns>
        public static RectD TileBounds(Vector2 tileCoordinate, int zoom)
        {
            var min = PixelsToMeters(new Vector2d(tileCoordinate.x * TileSize, tileCoordinate.y * TileSize), zoom);
            var max = PixelsToMeters(new Vector2d((tileCoordinate.x + 1) * TileSize, (tileCoordinate.y + 1) * TileSize), zoom);
            return new RectD(min, max - min);
        }

        /// <summary>
        /// Gets the xy tile ID at the requested zoom that contains the WGS84 lat/lon point.
        /// See: http://wiki.openstreetmap.org/wiki/Slippy_map_tilenames.
        /// </summary>
        /// <param name="latitude"> The latitude. </param>
        /// <param name="longitude"> The longitude. </param>
        /// <param name="zoom"> Zoom level. </param>
        /// <returns> A <see cref="T:UnityEngine.Vector2d"/> xy tile ID. </returns>
        public static Vector2d LatitudeLongitudeToTileId(float latitude, float longitude, int zoom)
        {
            var x = (int)Math.Floor((longitude + 180.0) / 360.0 * Math.Pow(2.0, zoom));
            var y = (int)Math.Floor((1.0 - Math.Log(Math.Tan(latitude * Math.PI / 180.0)
                    + 1.0 / Math.Cos(latitude * Math.PI / 180.0)) / Math.PI) / 2.0 * Math.Pow(2.0, zoom));

            return new Vector2d(x, y);
        }

        /// <summary>
        /// Gets the WGS84 longitude of the northwest corner from a tile's X position and zoom level.
        /// See: http://wiki.openstreetmap.org/wiki/Slippy_map_tilenames.
        /// </summary>
        /// <param name="x"> Tile X position. </param>
        /// <param name="zoom"> Zoom level. </param>
        /// <returns> NW Longitude. </returns>
        public static double TileXToNWLongitude(int x, int zoom)
        {
            var n = Math.Pow(2.0, zoom);
            var lon_deg = x / n * 360.0 - 180.0;
            return lon_deg;
        }

        /// <summary>
        /// Gets the WGS84 latitude of the northwest corner from a tile's Y position and zoom level.
        /// See: http://wiki.openstreetmap.org/wiki/Slippy_map_tilenames.
        /// </summary>
        /// <param name="y"> Tile Y position. </param>
        /// <param name="zoom"> Zoom level. </param>
        /// <returns> NW Latitude. </returns>
        public static double TileYToNWLatitude(int y, int zoom)
        {
            var n = Math.Pow(2.0, zoom);
            var lat_rad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / n)));
            var lat_deg = lat_rad * 180.0 / Math.PI;
            return lat_deg;
        }

        /// <summary>
        /// Gets the <see cref="T:Mapbox.Utils.Vector2dBounds"/> of a tile.
        /// </summary>
        /// <param name="x"> Tile X position. </param>
        /// <param name="y"> Tile Y position. </param>
        /// <param name="zoom"> Zoom level. </param>
        /// <returns> The <see cref="T:Mapbox.Utils.Vector2dBounds"/> of the tile. </returns>
        public static Vector2dBounds TileIdToBounds(int x, int y, int zoom)
        {
            var sw = new Vector2d(TileYToNWLatitude(y, zoom), TileXToNWLongitude(x + 1, zoom));
            var ne = new Vector2d(TileYToNWLatitude(y + 1, zoom), TileXToNWLongitude(x, zoom));
            return new Vector2dBounds(sw, ne);
        }

        /// <summary>
        /// Gets the WGS84 lat/lon of the center of a tile.
        /// </summary>
        /// <param name="x"> Tile X position. </param>
        /// <param name="y"> Tile Y position. </param>
        /// <param name="zoom"> Zoom level. </param>
        /// <returns>A <see cref="T:UnityEngine.Vector2d"/> of lat/lon coordinates.</returns>
        public static Vector2d TileIdToCenterLatitudeLongitude(int x, int y, int zoom)
        {
            var bb = TileIdToBounds(x, y, zoom);
            var center = bb.Center;
            return new Vector2d((float)center.x, (float)center.y);
        }

        /// <summary>
        /// Gets the meters per pixels at given latitude and zoom level for a 256x256 tile.
        /// See: http://wiki.openstreetmap.org/wiki/Zoom_levels.
        /// </summary>
        /// <param name="latitude"> The latitude. </param>
        /// <param name="zoom"> Zoom level. </param>
        /// <returns> Meters per pixel. </returns>
        public static float GetTileScaleInMeters(float latitude, int zoom)
        {
            return 40075000 * Mathf.Cos(Mathf.Deg2Rad * latitude) / Mathf.Pow(2f, zoom + 8);
        }

        /// <summary>
        /// Gets height from terrain-rgb adjusted for a given scale.
        /// </summary>
        /// <param name="color"> The <see cref="T:UnityEngine.Color"/>. </param>
        /// <param name="relativeScale"> Relative scale. </param>
        /// <returns> Adjusted height in meters. </returns>
        public static float GetRelativeHeightFromColor(Color color, float relativeScale)
        {
            return GetAbsoluteHeightFromColor(color) * relativeScale;
        }

        /// <summary>
        /// Specific formula for mapbox.terrain-rgb to decode height values from pixel values.
        /// See: https://www.mapbox.com/blog/terrain-rgb/.
        /// </summary>
        /// <param name="color"> The <see cref="T:UnityEngine.Color"/>. </param>
        /// <returns> Height in meters. </returns>
        public static float GetAbsoluteHeightFromColor(Color color)
        {
            return (float)(-10000 + ((color.r * 255 * 256 * 256 + color.g * 255 * 256 + color.b * 255) * 0.1));
        }

        private static double Resolution(int zoom)
        {
            return InitialResolution / Math.Pow(2, zoom);
        }

        private static Vector2d PixelsToMeters(Vector2d p, int zoom)
        {
            var res = Resolution(zoom);
            var met = new Vector2d();
            met.x = (float)(p.x * res - OriginShift);
            met.y = (float)-(p.y * res - OriginShift);
            return met;
        }

        private static Vector2d MetersToPixels(Vector2d m, int zoom)
        {
            var res = Resolution(zoom);
            var pix = new Vector2d((float)((m.x + OriginShift) / res), (float)((-m.y + OriginShift) / res));
            return pix;
        }

        private static Vector2 PixelsToTile(Vector2d p)
        {
            var t = new Vector2((int)Math.Ceiling(p.x / (double)TileSize) - 1, (int)Math.Ceiling(p.y / (double)TileSize) - 1);
            return t;
        }
    }
}
