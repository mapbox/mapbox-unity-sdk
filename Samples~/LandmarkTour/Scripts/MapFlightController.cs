using System;
using System.Collections;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.Example.Scripts.MapInput;
using UnityEngine;
// The camera namespace and its base input class share the name MapInput, so the clamp
// constants are reached through an alias rather than an ambiguous bare reference.
using MapInputBase = Mapbox.Example.Scripts.MapInput.MapInput;

namespace Mapbox.Samples.LandmarkTour
{
    /// <summary>
    /// Flies the map view from wherever it is to a target view the way a globe viewer does
    /// it: tilt to looking straight down, climb until the whole route is on screen, travel
    /// at that altitude, descend, then tilt onto the destination's own pitch.
    ///
    /// The phases are explicit rather than blended, and that is the point in two separate
    /// ways. First, a single eased interpolation from one close-up to another never
    /// actually climbs — both ends are at zoom 17, so the map pans a thousand kilometres at
    /// street level and asks the tile pipeline for every z16/z17 tile along the corridor.
    /// Pinning the centre during the climb and descent, and only moving it at cruise
    /// altitude, means the tiles requested are a few dozen coarse ones per zoom level over
    /// two fixed locations. Second, zoom and pitch are never animated in the same phase —
    /// tilting always happens at a fixed zoom, and zoom-walking always happens at a fixed
    /// pitch. See the note on <see cref="WalkZoom"/> for why: blending them let
    /// <c>UnityTileProvider</c>'s quadtree split decision flip between "split into 4
    /// children" and "don't split" across consecutive frames — and each flip drops tiles out
    /// of the retained set for a frame, which is enough for the fetch pipeline to cancel an
    /// in-flight request that happens to be sitting in it. That silently killed landmark
    /// building-tile fetches mid-flight, with nothing to retry them afterward (nothing calls
    /// <see cref="MapboxMap.ChangeView"/> again once the flight ends until the user provides
    /// real input), so landmarks would only ever appear after a manual pan or click, never
    /// from the scripted flight itself — manual mouse-wheel zoom never touches pitch, so it
    /// never triggers the flip-flop.
    ///
    /// The camera is never moved directly. Every frame this pushes an interpolated view
    /// into <see cref="MapboxMap.ChangeView"/>, and the camera follows because
    /// <see cref="Moving3dCamera"/> subscribes to the map information's ViewChanged and
    /// LatitudeLongitudeChanged events when it initializes. Those subscriptions live on
    /// IMapInformation rather than in the behaviour's Update, which is why disabling the
    /// camera behaviour for the duration suppresses user input without stopping the camera
    /// from tracking the flight.
    /// </summary>
    public class MapFlightController : MonoBehaviour
    {
        [Tooltip("The map to fly. Found in the scene if left empty.")]
        public MapBehaviourCore MapBehaviour;

        [Tooltip("Interactive camera to suspend while a flight plays, so a stray drag or scroll cannot fight it. Also supplies the distance curve used to pick the cruise altitude. Found in the scene if left empty.")]
        public Moving3dCameraBehaviour CameraBehaviour;

        [Tooltip("Zoom levels between tile-cover refreshes while climbing or descending. This no longer chops up the camera motion — the climb is one continuous sweep either way — it only sets how often the tile pipeline is nudged along the way. Keep it at 2 or below: a 512px tile stretched over a 3-level lag still leaves ~148 source pixels on screen, but 5 levels leaves 37 and reads as a smear.")]
        public float ZoomLevelsPerStep = 2f;

        [Tooltip("Seconds of camera motion per zoom step. The climb or descent lasts this times the number of steps it spans, run as one uninterrupted eased sweep.")]
        public float SecondsPerStep = 0.35f;

        [Tooltip("Longest a background tile refresh may run before it is abandoned, in seconds. 0 disables tile refreshing entirely. The camera never waits on this — it only bounds how long a refresh keeps polling.")]
        public float TileWaitTimeout = 0.8f;

        [Tooltip("Seconds of cruise per 1000 km travelled, before the range clamp below.")]
        public float CruiseSecondsPerThousandKilometres = 1.5f;

        [Tooltip("Shortest and longest the cruise phase may last, in seconds. Total flight time is ascent + cruise + descent.")]
        public Vector2 CruiseDurationRange = new Vector2(0.6f, 4f);

        [Tooltip("How much wider than the route the cruise view is. 1 fits the route exactly edge to edge; 1.4 leaves a comfortable margin. Raising this zooms further out and loads fewer tiles.")]
        public float CruisePadding = 1.4f;

        [Tooltip("Never climb above this zoom, however far apart the two places are. Stops a long flight pulling back to a view of the whole planet.")]
        [Range(0f, 22f)]
        public float MinimumCruiseZoom = 3f;

        [Tooltip("Camera tilt held during the cruise. 90 is straight down, which is both the natural travelling view and the cheapest, since a tilted camera at cruise altitude sweeps a far larger area of tiles into the frustum.")]
        [Range(15f, 90f)]
        public float CruisePitch = 90f;

        public bool IsFlying => _flight != null;

        private MapboxMap _map;
        private Coroutine _flight;
        private Coroutine _coverWait;
        private Coroutine _backgroundRefresh;

        private void Awake()
        {
            if (MapBehaviour == null) MapBehaviour = FindObjectOfType<MapBehaviourCore>();
            if (CameraBehaviour == null) CameraBehaviour = FindObjectOfType<Moving3dCameraBehaviour>();

            if (MapBehaviour == null)
            {
                Debug.LogError($"{nameof(MapFlightController)} found no {nameof(MapBehaviourCore)} in the scene. Assign the Map Behaviour field; flights will not run.", this);
                enabled = false;
                return;
            }

            MapBehaviour.Initialized += OnMapInitialized;
        }

        private void OnDestroy()
        {
            if (MapBehaviour != null) MapBehaviour.Initialized -= OnMapInitialized;
        }

        private void OnMapInitialized(MapboxMap map) => _map = map;

        /// <summary>
        /// Starts a flight to the given view. A flight already in progress is cancelled and
        /// the new one starts from wherever the view had reached, so rapid button presses
        /// chain instead of fighting.
        /// </summary>
        public void FlyTo(LatitudeLongitude target, float zoom, float pitch, float bearing)
        {
            if (_map == null)
            {
                Debug.LogWarning($"{nameof(MapFlightController)}.{nameof(FlyTo)} was called before the map finished initializing. Ignoring.", this);
                return;
            }

            Cancel();
            _flight = StartCoroutine(Fly(target, zoom, pitch, bearing));
        }

        /// <summary>Stops the current flight where it is and hands control back to the interactive camera.</summary>
        public void Cancel()
        {
            if (_flight == null) return;

            StopCoroutine(_flight);
            _flight = null;

            if (_coverWait != null)
            {
                StopCoroutine(_coverWait);
                _coverWait = null;
            }

            if (_backgroundRefresh != null)
            {
                StopCoroutine(_backgroundRefresh);
                _backgroundRefresh = null;
            }

            EndFlight(_map.MapInformation.Zoom);
        }

        private IEnumerator Fly(LatitudeLongitude target, float targetZoom, float targetPitch, float targetBearing)
        {
            var information = _map.MapInformation;
            var start = information.LatitudeLongitude;
            var startZoom = information.Zoom;
            var startPitch = information.Pitch;
            var startBearing = information.Bearing;

            targetZoom = Mathf.Clamp(targetZoom, 0f, MapInputBase.MaxClampZoom);
            // ChangeView bypasses the camera's own ClampPitch. A pitch near the horizon
            // makes the camera's ground-plane raycasts divide by a near-zero ray direction,
            // so the same limits are applied here.
            targetPitch = Mathf.Clamp(targetPitch, MinimumPitch, MaximumPitch);
            var cruisePitch = Mathf.Clamp(CruisePitch, MinimumPitch, MaximumPitch);

            var cruiseZoom = GetCruiseZoom(information, start, target, startZoom, targetZoom);

            var distanceKilometres = (float)Conversions.GeoDistance(start.Longitude, start.Latitude, target.Longitude, target.Latitude);
            var cruise = Mathf.Clamp(
                distanceKilometres / 1000f * CruiseSecondsPerThousandKilometres,
                Mathf.Min(CruiseDurationRange.x, CruiseDurationRange.y),
                Mathf.Max(CruiseDurationRange.x, CruiseDurationRange.y));

            if (CameraBehaviour != null) CameraBehaviour.enabled = false;

            // Phase 1a — tilt to looking straight down, before any zoom change, at the
            // starting point. See the note on UnityTileProvider's near-90 pitch behaviour
            // on TiltPitch and WalkZoom below for why this has to be a separate phase
            // rather than blended with the zoom-out.
            yield return TiltPitch(start, startZoom, startPitch, cruisePitch, startBearing);

            // Phase 1b — climb over the starting point to cruise altitude, one step at a
            // time, pitch held at cruisePitch throughout.
            yield return WalkZoom(start, startZoom, cruiseZoom, cruisePitch, startBearing);

            // Phase 2 — travel at cruise altitude. The only phase that moves the centre, so
            // the only one that can stream a corridor of tiles, and it runs at the zoom where
            // the whole route fits on screen. Bearing swings here, where it is free.
            yield return RunPhase(cruise, progress =>
            {
                var eased = Mathf.SmoothStep(0f, 1f, progress);
                _map.ChangeView(
                    new LatitudeLongitude(
                        start.Latitude + (target.Latitude - start.Latitude) * eased,
                        start.Longitude + (target.Longitude - start.Longitude) * eased),
                    cruiseZoom,
                    cruisePitch,
                    Mathf.LerpAngle(startBearing, targetBearing, eased));
            });

            // Nudge the destination's coarse tiles along before starting down, but do not
            // hold the camera for them: a blocking wait here is a dead stop of up to
            // TileWaitTimeout right at the top of the arc, which is the most visible place
            // in the whole flight to stop moving.
            RefreshTilesInBackground();

            // Phase 3a — descend onto the destination, centre already fixed, pitch held at
            // cruisePitch throughout.
            yield return WalkZoom(target, cruiseZoom, targetZoom, cruisePitch, targetBearing);

            // Phase 3b — tilt onto the destination's own pitch only once zoom has settled.
            yield return TiltPitch(target, targetZoom, cruisePitch, targetPitch, targetBearing);

            // Land exactly on the requested view rather than wherever the last frame fell.
            _map.ChangeView(target, targetZoom, targetPitch, targetBearing);

            // One more settling pass now that the view has completely stopped moving. A
            // tile whose fetch got cancelled by cover churn during the final animated
            // frames (RetainTiles cancels anything that drops out of the retained set for
            // even a single frame, and a fixed pitch alone doesn't rule that out near a
            // split threshold) never gets retried on its own: nothing calls ChangeView
            // again after a flight ends until the user provides real input, so a cancelled
            // request just sits dead. This call re-requests it against a now-static view,
            // where nothing can cancel it a second time. Landmarks and base tiles both pick
            // the result up automatically once it lands (LandmarksLayerModule's generation
            // callback fires on its own; MapboxMapVisualizer's per-frame temp-tile retry
            // promotes the tile) even if this wait itself times out first — the retry
            // outlives the wait, only the cancellation risk needed removing.
            yield return WaitForTiles();

            _flight = null;
            EndFlight(targetZoom);
        }

        /// <summary>
        /// Sweeps zoom from one value to another in a single continuous eased motion, with
        /// the centre AND the pitch held still throughout — see the pitch note below.
        ///
        /// The sweep is deliberately one phase rather than one phase per zoom step. Stepping
        /// the camera was an earlier attempt at the tile problem below, and it cost far more
        /// than it bought: each step ran its own <see cref="Mathf.SmoothStep"/>, and
        /// SmoothStep has zero derivative at both ends, so zoom velocity dropped to zero at
        /// every single step boundary. A four-step climb was four accelerate-decelerate
        /// cycles, which is exactly the ratcheting "zooming in and out step by step" the
        /// stepping was supposed to hide. Easing once across the whole span keeps velocity
        /// continuous from the first frame to the last.
        ///
        /// The tile problem itself is real and is still handled, just without stopping the
        /// camera. The tile cover changes on every integer zoom crossing, and the fetch
        /// pipeline holds a queued request for 0.2s before it may start and then starts only
        /// one per frame, so a fast sweep can have each level's requests aborted at roughly
        /// the moment they become eligible to run. So the sweep still fires a background
        /// cover refresh every ZoomLevelsPerStep levels as it passes them — the pipeline gets
        /// the same periodic nudge it got from stepping, and the camera never waits for it.
        /// Detail lags the camera by up to a step's worth of levels while it catches up,
        /// which is the trade: a lagging texture reads far better than a stalling camera.
        ///
        /// Pitch is a single fixed value, not a from/to pair, and that is deliberate.
        /// UnityTileProvider.ShouldSplit special-cases a camera looking straight down: with
        /// no horizontal forward component, every tile's corners project to ~zero distance
        /// along that forward, which the split test treats as "behind the camera" and
        /// force-splits regardless of actual distance. As pitch tilts away from that, some
        /// corners cross to "in front" and the tile falls back to ordinary distance-based
        /// splitting — a real, but one-time and monotonic, transition. Doing that at the
        /// same time zoom is changing (thus changing tile size, thus the split threshold
        /// itself, every single step) let the two shift against each other, and the split
        /// decision for a tile could flip between "split into 4 children" and "don't split"
        /// across consecutive frames. Each flip is fully valid the instant its frame's
        /// ChangeView runs, but it also means a tile can drop out of the retained set for
        /// just that one frame — and PbfSource.RetainTiles cancels any in-flight fetch for a
        /// tile that isn't retained, so a landmark's building-tile request sitting in that
        /// window gets killed mid-flight, permanently: nothing retries it once the flight
        /// ends, since nothing calls ChangeView again until the user provides real input.
        /// That is why landmarks would silently fail to load on the scripted flight and only
        /// ever appear after a manual pan or click — manual mouse-wheel zoom never touches
        /// pitch at all, so it never triggers the flip-flop. Callers tilt to the cruise pitch
        /// before starting a zoom walk and tilt away from it only after the walk finishes, so
        /// pitch and zoom are never both moving in the same frame.
        /// </summary>
        private IEnumerator WalkZoom(
            LatitudeLongitude center,
            float fromZoom, float toZoom,
            float pitch,
            float bearing)
        {
            var span = Mathf.Abs(toZoom - fromZoom);
            var perStep = Mathf.Max(0.25f, ZoomLevelsPerStep);
            var steps = Mathf.Max(1, Mathf.CeilToInt(span / perStep));

            // Steps now only pace the tile refreshes, so the sweep keeps the same overall
            // duration it had when each step was animated separately.
            var duration = steps * Mathf.Max(0f, SecondsPerStep);
            var nextRefreshStep = 1;

            RefreshTilesInBackground();

            yield return RunPhase(duration, progress =>
            {
                var t = Mathf.SmoothStep(0f, 1f, progress);
                _map.ChangeView(
                    center,
                    Mathf.Lerp(fromZoom, toZoom, t),
                    pitch,
                    bearing);

                // Fired as the sweep passes each step boundary rather than on a timer, so
                // the refreshes track zoom levels actually crossed. The easing means the
                // middle boundaries are crossed quickly and the outer ones slowly, which is
                // the right shape anyway: the cover barely changes at the eased ends.
                if (t * steps >= nextRefreshStep)
                {
                    nextRefreshStep++;
                    RefreshTilesInBackground();
                }
            });
        }

        /// <summary>
        /// Tilts pitch at a fixed zoom and centre. Kept separate from WalkZoom so the two
        /// never animate at once — see the pitch note on WalkZoom for why that matters.
        /// </summary>
        private IEnumerator TiltPitch(LatitudeLongitude center, float zoom, float fromPitch, float toPitch, float bearing)
        {
            if (Mathf.Approximately(fromPitch, toPitch)) yield break;

            yield return RunPhase(SecondsPerStep, progress =>
            {
                var eased = Mathf.SmoothStep(0f, 1f, progress);
                _map.ChangeView(center, zoom, Mathf.Lerp(fromPitch, toPitch, eased), bearing);
            });

            // Background, not awaited: a tilt is immediately followed by a zoom sweep (or by
            // the landing settle), and stopping between the two is what made the flight read
            // as a sequence of moves rather than one flight.
            RefreshTilesInBackground();
        }

        /// <summary>
        /// Waits until the current view's tiles are in memory, or until the timeout runs out.
        /// </summary>
        private IEnumerator WaitForTiles()
        {
            if (TileWaitTimeout <= 0f || _map == null) yield break;

            var loaded = false;
            _coverWait = StartCoroutine(LoadCurrentCover(() => loaded = true));

            var waited = 0f;
            while (!loaded && waited < TileWaitTimeout)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (loaded)
            {
                _coverWait = null;
                yield break;
            }

            // Out of patience. Stop waiting, but the fetches themselves are hosted on the
            // SDK's own coroutine runner rather than on this component, so they keep going
            // and the next step still gets the benefit.
            StopCoroutine(_coverWait);
            _coverWait = null;
        }

        private IEnumerator LoadCurrentCover(Action onComplete)
        {
            yield return _map.LoadTileCoverToMemory(_map.TileCover);
            onComplete();
        }

        /// <summary>
        /// Fire-and-forget counterpart to <see cref="WaitForTiles"/>: starts the same
        /// tile-cover refresh but never holds up the caller. This is what every mid-flight
        /// call site uses; the blocking <see cref="WaitForTiles"/> is left only for the
        /// landing settle, where the camera has already stopped and a wait costs nothing.
        ///
        /// Only one runs at a time. Refreshes are fired from inside the zoom sweep, so
        /// without the guard a slow one would still be polling when the next boundary came
        /// round and they would stack up, each one re-walking the whole cover through
        /// <see cref="MapboxMap.LoadTileCoverToMemory"/> every frame. Skipping while one is
        /// already in flight loses nothing: it re-reads <see cref="MapboxMap.TileCover"/>
        /// when it starts, so the next one picks up wherever the view has got to by then.
        /// </summary>
        private void RefreshTilesInBackground()
        {
            if (TileWaitTimeout <= 0f || _map == null || _backgroundRefresh != null) return;
            _backgroundRefresh = StartCoroutine(BackgroundLoadCurrentCover());
        }

        private IEnumerator BackgroundLoadCurrentCover()
        {
            var loaded = false;
            var load = StartCoroutine(LoadCurrentCover(() => loaded = true));

            var waited = 0f;
            while (!loaded && waited < TileWaitTimeout)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (!loaded) StopCoroutine(load);
            _backgroundRefresh = null;
        }

        private static IEnumerator RunPhase(float duration, Action<float> apply)
        {
            if (duration <= 0f)
            {
                apply(1f);
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                apply(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
        }

        /// <summary>
        /// Lowest zoom the flight needs to climb to for the whole route to fit on screen,
        /// which is what keeps the pan away from close-up tiles. Never climbs above the
        /// closer of the two views, so a short hop between neighbouring landmarks does not
        /// pointlessly pull back.
        /// </summary>
        private float GetCruiseZoom(IMapInformation information, LatitudeLongitude start, LatitudeLongitude target, float startZoom, float targetZoom)
        {
            // Web Mercator rather than ground distance, because the view width below is
            // also in Mercator units — so the latitude stretch cancels instead of needing
            // a correction factor.
            var from = Conversions.LatitudeLongitudeToWebMercator(start);
            var to = Conversions.LatitudeLongitudeToWebMercator(target);
            var span = Math.Sqrt((to.x - from.x) * (to.x - from.x) + (to.y - from.y) * (to.y - from.y));

            var ceiling = Mathf.Min(startZoom, targetZoom);
            if (span <= 0d) return ceiling;

            var required = span * Mathf.Max(1f, CruisePadding);
            var floor = Mathf.Clamp(MinimumCruiseZoom, 0f, ceiling);

            // Step down a level at a time rather than inverting the distance curve, so this
            // holds for whatever curve the camera is actually configured with.
            for (var zoom = Mathf.FloorToInt(ceiling); zoom > floor; zoom--)
            {
                if (GetViewWidthInMercator(information, zoom) >= required) return zoom;
            }

            return floor;
        }

        /// <summary>How wide a slice of the world is on screen at a given zoom, in Web Mercator units.</summary>
        private float GetViewWidthInMercator(IMapInformation information, float zoom)
        {
            var core = CameraBehaviour != null ? CameraBehaviour.Core : null;
            var camera = CameraBehaviour != null ? CameraBehaviour.Camera : null;

            if (core?.CameraCurve == null || camera == null)
            {
                // Without the camera to ask, fall back on the shipped curve's shape: the
                // camera sits about two tile widths above the map at any zoom.
                return 2f * Conversions.TileSizeInUnitySpace(Mathf.FloorToInt(zoom), 1f);
            }

            // Camera distance is in Unity units and one Unity unit is Scale Mercator units,
            // so multiplying by Scale is unnecessary: the curve output is already the
            // distance in Mercator units before the Scale division the camera applies.
            var distance = core.DistanceScale * core.CameraCurve.Evaluate(zoom);
            return 2f * distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }

        private void EndFlight(float arrivalZoom)
        {
            if (CameraBehaviour == null) return;

            // Moving3dCamera keeps its own ZoomValue and only updates it inside its own
            // Zoom(). SetCamera copies pitch and bearing back from the map, but not zoom,
            // so without this the first scroll after a flight jumps to whatever zoom the
            // camera last saw a user input at.
            if (CameraBehaviour.Core != null) CameraBehaviour.Core.ZoomValue = arrivalZoom;
            CameraBehaviour.enabled = true;
        }

        // Mirrors MapInput.ClampPitch, which is protected and so not reachable from here.
        private const float MinimumPitch = 15f;
        private const float MaximumPitch = 90f;
    }
}
