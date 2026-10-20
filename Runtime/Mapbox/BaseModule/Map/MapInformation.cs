using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities;
using Mapbox.BaseModule.Utilities.Attributes;
using UnityEngine;

namespace Mapbox.BaseModule.Map
{
    [Serializable]
    public class MapInformation : IMapInformation
    {
        private LatitudeLongitude _latitudeLongitude;
        private Vector2d _centerMercator;
        [Tooltip("Initial camera position pitch angle")]
        [SerializeField] private float _pitch;
        [Tooltip("Initial camera position bearing")]
        [SerializeField] private float _bearing;
        [Tooltip("Initial world scale")]
        [SerializeField] protected float _scale = 1000;
        
        [Tooltip("World center latitude longitude as comma separated values")]
        [Geocode] [SerializeField] protected string _latitudeLongitudeString;
        [Tooltip("Initial zoom value for the map")]
        [SerializeField] private float _zoom;
        protected bool _isInitialized = false;
        
        public MapInformation(string latitudeLongitudeString)
        {
            _latitudeLongitudeString = latitudeLongitudeString;
        }
        
        public MapInformation(string latitudeLongitudeString, float pitch, float scale, float zoom)
        {
            _latitudeLongitudeString = latitudeLongitudeString;
            _pitch = pitch;
            _scale = scale;
            _zoom = zoom;
        }
        
        public virtual void Initialize()
        {
            if(_isInitialized) return;
            Initialize(Conversions.StringToLatLon(_latitudeLongitudeString));
        }
        
        public virtual void Initialize(LatitudeLongitude latitudeLongitude)
        {
            if(_isInitialized) return;

            SetLatitudeLongitude(latitudeLongitude);
            _isInitialized = true;
            // Back to the no-terrain defaults: 0 everywhere, with the Try form reporting false so
            // callers can tell "no terrain in this scene" from a genuine sea-level reading. The
            // field initializers cover instances whose Initialize override never runs this; these
            // assignments cover an instance being reused for a fresh session.
            QueryElevation = NoElevation;
            TryQueryElevation = NoElevationAvailable;
            // Reset terrain bounds to TerrainInfo defaults so a previous map's Min/Max
            // never bleeds into a fresh session. Field initializers already set defaults
            // on new MapInformation instances, but explicitly resetting here guards
            // against future paths that reuse an existing instance.
            Terrain.MinElevation = TerrainInfo.DefaultMinElevation;
            Terrain.MaxElevation = TerrainInfo.DefaultMaxElevation;
        }

        //PROPERTIES
        public LatitudeLongitude LatitudeLongitude => _latitudeLongitude;
        public float Pitch
        {
            get => _pitch;
            private set => _pitch = value;
        }
        public float Bearing
        {
            get => _bearing;
            private set => _bearing = value;
        }
        public virtual float Scale
        {
            get => _scale; 
            protected set => _scale = value;
        }
        
        public int AbsoluteZoom => (int) Math.Floor(Zoom);
        public virtual float Zoom
        {
            get => _zoom;
            private set => _zoom = value;
        }
        public Vector2d CenterMercator => _centerMercator;
        
        //METHODS
        public virtual float GetScaleFor(float zoomValue) => _scale;

        public virtual float GetLatitudeCompensationForLocation => Conversions.LatitudeElevationCompensation(_latitudeLongitude.Latitude);
        
        public void SetLatitudeLongitude(LatitudeLongitude latlng)
        {
            _latitudeLongitude = latlng;
            _latitudeLongitudeString = _latitudeLongitude.ToString();
            _centerMercator = Conversions.LatitudeLongitudeToWebMercator(LatitudeLongitude);
            LatitudeLongitudeChanged?.Invoke(this);
        }

        public virtual void SetInformation(LatitudeLongitude? latlng, float? zoom = null, float? pitch = null, float? bearing = null, float? scale = null)
        {
            if(latlng.HasValue) SetLatitudeLongitude(latlng.Value);
            if(zoom.HasValue) Zoom = zoom.Value;
            if (pitch.HasValue) Pitch = pitch.Value;
            if (bearing.HasValue) Bearing = bearing.Value;
            if (scale.HasValue && !Mathf.Approximately(scale.Value, Scale))
            {
                Scale = scale.Value;
                OnWorldScaleChanged();
            }

            if (SetView != null)
            {
                SetView(this);
            }

            if (ViewChanged != null)
            {
                ViewChanged(this);
            }
        }

        /// <summary>
        /// Both elevation lookups are defaulted here rather than only in Initialize, because
        /// DynamicScalingMapInformation overrides Initialize without calling base - so anything
        /// assigned only there is silently skipped for that subclass, and QueryElevation was left
        /// null. Most callers null-check it, but MapInformationStaticMethods.ConvertLatLngToPosition
        /// invokes it directly. A field initializer means neither can be null whichever path ran,
        /// and a terrain module simply overwrites them.
        /// </summary>
        public Func<CanonicalTileId, float, float, float> QueryElevation { get; set; } = NoElevation;

        public TryQueryElevationDelegate TryQueryElevation { get; set; } = NoElevationAvailable;

        /// <summary>Default lookup for a map with no terrain module: nothing is known anywhere.</summary>
        private static bool NoElevationAvailable(CanonicalTileId tileId, float x, float y, out float elevation)
        {
            elevation = 0f;
            return false;
        }

        /// <summary>
        /// Lossy counterpart to <see cref="NoElevationAvailable"/>, defined in terms of it so the
        /// two defaults cannot drift apart - the same relationship TerrainLayerModule's real
        /// QueryElevation has with its TryQueryElevation.
        /// </summary>
        private static float NoElevation(CanonicalTileId tileId, float x, float y)
        {
            NoElevationAvailable(tileId, x, y, out var elevation);
            return elevation;
        }

        public TerrainInfo Terrain { get; } = new TerrainInfo();
        public event Action<IMapInformation> SetView = (t) => {};
        public event Action<IMapInformation> ViewChanged = (t) => {};
        public event Action<IMapInformation> LatitudeLongitudeChanged = (t) => {};
        public event Action<IMapInformation> WorldScaleChanged = (t) => {};
        
        protected void OnWorldScaleChanged() => WorldScaleChanged?.Invoke(this);
    }
}