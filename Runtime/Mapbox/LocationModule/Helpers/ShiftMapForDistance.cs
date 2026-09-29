using System;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.LocationModule;
using UnityEngine;

namespace Mapbox.LocationModule.Helpers
{
    /// <summary>
    /// Describes one recenter performed by <see cref="ShiftMapForDistance"/>. Passed to
    /// <see cref="ShiftMapForDistance.OnRecentered"/>.
    ///
    /// A recenter moves the map's origin; it does not move the world. Every geographic point
    /// ends up at its old position minus the offset, and the offset is reported twice: once in
    /// the map's local space and once in world space. They are the same vector only when
    /// <c>MapRoot</c> sits at the world origin with identity rotation and unit scale.
    /// </summary>
    public readonly struct MapRecenteredArgs
    {
        /// <summary>The map centre before the recenter.</summary>
        public readonly LatitudeLongitude PreviousCenter;

        /// <summary>The map centre after the recenter, which is the location fix that triggered it.</summary>
        public readonly LatitudeLongitude NewCenter;

        /// <summary>
        /// How far every geographic point moved in the map's local (<c>MapRoot</c>) space.
        /// Subtract it from the <c>localPosition</c> of objects parented under <c>MapRoot</c>,
        /// i.e. objects placed with <c>ConvertLatLngToPosition</c> the way the coordinate
        /// conversion docs recommend. Its Y is always 0: a recenter never moves anything vertically.
        /// </summary>
        public readonly Vector3 LocalOffset;

        /// <summary>
        /// How far every geographic point moved in world space. Subtract it from the
        /// <c>position</c> of objects that are not parented under <c>MapRoot</c>.
        /// </summary>
        public readonly Vector3 WorldOffset;

        public MapRecenteredArgs(LatitudeLongitude previousCenter, LatitudeLongitude newCenter, Vector3 localOffset, Vector3 worldOffset)
        {
            PreviousCenter = previousCenter;
            NewCenter = newCenter;
            LocalOffset = localOffset;
            WorldOffset = worldOffset;
        }
    }

    /// <summary>
    /// Keeps a location-driven map centred on the player, so world coordinates stay small.
    ///
    /// The map's world origin is its centre. A player who walks away from it moves to ever
    /// larger world coordinates, and float precision falls with distance, which shows up as
    /// jittering meshes and cameras. When a location fix lands farther than
    /// <see cref="ShiftMapDistance"/> from the centre, this component makes that fix the new
    /// centre with <see cref="MapboxMap.ChangeView"/>.
    ///
    /// Tiles reposition themselves when the centre changes. Nothing else in the scene does,
    /// so anything else placed by geographic position has to be moved too. There are two ways
    /// to do that:
    /// <list type="bullet">
    /// <item><see cref="TransformsToShift"/>: set up in the Inspector, for the player, the camera
    /// rig, and anything a tile provider follows.</item>
    /// <item><see cref="OnRecentered"/>: for objects spawned from code, such as markers, route lines
    /// and multiplayer avatars, or for code that caches world positions.</item>
    /// </list>
    /// </summary>
    public class ShiftMapForDistance : MonoBehaviour
    {
        public MapBehaviourCore Map;
        public LocationProviderFactory LocationProviderFactory;
        [Tooltip("Recenter the map on the player when they are farther than this from the map centre (mercator units, roughly metres at the equator).")]
        public float ShiftMapDistance;
        [Tooltip("Player, camera rig and anything else placed in world space that must stay at the same geographic spot when the map recenters. List root objects only - a child of a listed object moves with it and would be shifted twice. Optional - leave empty if nothing needs to follow.")]
        public Transform[] TransformsToShift;

        /// <summary>
        /// Raised once per recenter, after the map has moved to its new centre and after
        /// <see cref="TransformsToShift"/> have been shifted.
        ///
        /// <para><b>When it fires.</b> It fires synchronously, from inside the location provider's
        /// <c>OnLocationUpdated</c>. By then <c>MapInformation</c> already reports the new
        /// centre, so <c>ConvertLatLngToPosition</c> returns new-frame positions. Handlers can
        /// either subtract the offset or place objects again from their stored coordinates;
        /// both give the same result.</para>
        ///
        /// <para><b>Too late for tile providers.</b> The tile cover for the new centre has already
        /// been built by the time handlers run. If a tile provider follows a transform (as
        /// <c>TransformBasedTileProvider</c> does), put that transform in
        /// <see cref="TransformsToShift"/>, which is shifted before the cover is built. If it is
        /// moved only from this event, the map loads tiles around the wrong spot until the next
        /// redraw.</para>
        ///
        /// <para><b>What to use.</b> Use <see cref="MapRecenteredArgs.LocalOffset"/> for objects
        /// under <c>MapRoot</c> and <see cref="MapRecenteredArgs.WorldOffset"/> for everything
        /// else. Anything parented under a tile has already moved with the tile; do not shift it
        /// again.</para>
        ///
        /// <para><b>What the offset means.</b> The offset is the one this recenter applied, not a
        /// running total. It becomes invalid if the map scale changes before a handler uses it.
        /// Recentering never changes the scale, but a handler that stores the offset for later
        /// has to take that into account.</para>
        /// </summary>
        public event Action<MapRecenteredArgs> OnRecentered = delegate { };

        private MapboxMap _map;
        private ILocationProvider _locationProvider;

        private void Start()
        {
            Map.Initialized += map =>
            {
                _map = map;
                _locationProvider = LocationProviderFactory.DefaultLocationProvider;
                if (_locationProvider != null)
                {
                    _locationProvider.OnLocationUpdated += LocationUpdated;
                }
            };
        }

        private void OnDestroy()
        {
            if (_locationProvider != null)
            {
                _locationProvider.OnLocationUpdated -= LocationUpdated;
            }
        }

        private void LocationUpdated(Location location)
        {
            // Map isn't ready to be recentered yet (still initializing, or a previous
            // LoadMapView(Coroutine) is mid-flight) - skip this update rather than issue
            // a ChangeView against a map that isn't in a stable state.
            if (_map == null || _map.Status < InitializationStatus.ReadyForUpdates)
            {
                return;
            }

            var mapCenterMercator = _map.MapInformation.CenterMercator;
            var locationMercator = Conversions.LatitudeLongitudeToWebMercator(location.LatitudeLongitude);
            var distanceFromCenter = Vector2d.Distance(mapCenterMercator, locationMercator);
            if (distanceFromCenter > ShiftMapDistance)
            {
                Debug.Log(string.Format("ShiftMapForDistance: player is {0} from map centre (limit {1}), recentering map", distanceFromCenter, ShiftMapDistance));

                var previousCenter = _map.MapInformation.LatitudeLongitude;

                // The position the player's new location currently sits at, measured against
                // the OLD map centre. Once ChangeView below makes `location` the new centre, this
                // is where the new origin falls - so it's exactly what every geographic point
                // moves by. Exact because ChangeView without a zoom leaves Scale untouched; pass a
                // zoom here and this would need the old and new scale both.
                //
                // ConvertLatLngToPosition works in MapRoot's local space (tiles are placed with
                // localPosition), so the world-space equivalent goes through MapRoot. With the
                // default MapRoot at the origin, unrotated and unscaled, the two are identical.
                var localOffset = _map.MapInformation.ConvertLatLngToPosition(location.LatitudeLongitude);
                var mapRoot = _map.UnityContext != null ? _map.UnityContext.MapRoot : null;
                var worldOffset = mapRoot != null ? mapRoot.TransformVector(localOffset) : localOffset;

                // Shift BEFORE ChangeView, not after: ChangeView recomputes the tile cover
                // synchronously, and a transform-driven tile provider (TransformBasedTileProvider)
                // reads its transform's position during that call. Shifted afterwards, the cover
                // would be built around a spot `offset` away from the player and stay there until
                // something else forced a redraw. Nothing runs between the two calls, so the
                // transforms are never observed out of step with the map.
                ShiftTransforms(worldOffset);

                _map.ChangeView(location.LatitudeLongitude);

                // Raised last, so handlers see the map, its tiles and TransformsToShift all in
                // the new frame. See OnRecentered for why tile-provider transforms can't use it.
                OnRecentered(new MapRecenteredArgs(previousCenter, location.LatitudeLongitude, localOffset, worldOffset));
            }
        }

        private void ShiftTransforms(Vector3 worldOffset)
        {
            if (TransformsToShift == null)
            {
                return;
            }

            for (var i = 0; i < TransformsToShift.Length; i++)
            {
                var t = TransformsToShift[i];
                if (t == null)
                {
                    continue;
                }

                t.position -= worldOffset;
            }
        }
    }
}
