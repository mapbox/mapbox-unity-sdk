//-----------------------------------------------------------------------
// <copyright file="VectorExtensions.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Unity.Utilities
{
    using Mapbox.Utils;
    using UnityEngine;

    public static class VectorExtensions
    {
        public static Vector3 ToVector3xz(this Vector2 v)
        {
            return new Vector3(v.x, 0, v.y);
        }

        public static Vector3 ToVector3xz(this Vector2d v)
        {
            return new Vector3((float)v.x, 0, (float)v.y);
        }

        public static Vector2 ToVector2xz(this Vector3 v)
        {
            return new Vector3(v.x, v.z);
        }

        public static Vector2d ToVector2d(this Vector3 v)
        {
            return new Vector2d(v.x, v.z);
        }

        public static void MoveToGeocoordinate(this Transform t, double lat, double lng, Vector2d refPoint, float scale = 1)
        {
            t.position = Conversions.GeoToWorldPosition(lat, lng, refPoint, scale).ToVector3xz();
        }

        public static void MoveToGeocoordinate(this Transform t, Vector2d latLon, Vector2d refPoint, float scale = 1)
        {
            t.MoveToGeocoordinate(latLon.x, latLon.y, refPoint, scale);
        }

        public static Vector3 AsUnityPosition(this Vector2 latLon, Vector2d refPoint, float scale = 1)
        {
            return Conversions.GeoToWorldPosition(latLon.x, latLon.y, refPoint, scale).ToVector3xz();
        }

        public static Vector2d GetGeoPosition(this Transform t, Vector2d refPoint, float scale = 1)
        {
            var pos = refPoint + (t.position / scale).ToVector2d();
            return Conversions.MetersToLatLon(pos);
        }

        // TODO: add ability to get geo position from a vector2 or vector 3, as well (not just transform).
    }
}
