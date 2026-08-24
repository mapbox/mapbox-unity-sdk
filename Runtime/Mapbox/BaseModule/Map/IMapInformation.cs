using System;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;

namespace Mapbox.BaseModule.Map
{
    /// <summary>
    /// Elevation lookup that can say "I don't know". A delegate type rather than a Func because
    /// Func cannot carry an out parameter, and the whole point is returning the value and its
    /// validity separately instead of overloading 0 to mean both.
    /// </summary>
    public delegate bool TryQueryElevationDelegate(CanonicalTileId tileId, float x, float y, out float elevation);

    public interface IMapInformation
    {
        LatitudeLongitude LatitudeLongitude { get; }
        float Pitch { get; }
        float Bearing { get; }
        float Scale { get; }
        int AbsoluteZoom { get; }
        float Zoom { get; }
        Vector2d CenterMercator { get; }
        void Initialize();
        void Initialize(LatitudeLongitude latitudeLongitude);
        void SetLatitudeLongitude(LatitudeLongitude latlng);
        void SetInformation(LatitudeLongitude? latlng, float? zoom = null, float? pitch = null, float? bearing = null, float? scale = null);
        event Action<IMapInformation> SetView;
        event Action<IMapInformation> ViewChanged; 
        event Action<IMapInformation> LatitudeLongitudeChanged;
        event Action<IMapInformation> WorldScaleChanged;
        /// <summary>
        /// Elevation lookup, where 0 means both "sea level" and "no data loaded". Prefer
        /// <see cref="TryQueryElevation"/> when those two need telling apart.
        /// </summary>
        Func<CanonicalTileId, float, float, float> QueryElevation { get; set; }

        /// <summary>
        /// Elevation lookup that reports whether the value is real. Returns false when no loaded
        /// terrain data covers the point, so callers that would otherwise bake a placeholder 0
        /// into geometry can wait or skip instead.
        /// </summary>
        TryQueryElevationDelegate TryQueryElevation { get; set; }
        TerrainInfo Terrain { get; }
        float GetLatitudeCompensationForLocation { get; }
        float GetScaleFor(float zoomValue);
    }
}