using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities;
using Mapbox.DirectionsApi.MapMatching;
using UnityEngine;

namespace Mapbox.DirectionsApi.Example
{
    /// <summary>
    /// Example script demonstrating the Mapbox Map Matching API.
    /// Map matching snaps GPS traces to the road network, cleaning up noisy GPS data.
    /// </summary>
    public class MapMatchingExample : MonoBehaviour
    {
        [Header("Map Reference")]
        public MapBehaviourCore MapCore;

        [Header("Map Matching Settings")]
        [Tooltip("Profile to use for matching (driving, walking, cycling, etc.)")]
        public Profile MatchingProfile = Profile.MapboxDriving;

        [Tooltip("Request timeout in seconds")]
        public int TimeoutSeconds = 30;

        [Header("GPS Trace Configuration")]
        [Tooltip("Generate a test GPS trace automatically on start")]
        public bool UseTestTrace = true;

        [Header("Test Trace (San Francisco area)")]
        [Tooltip("Simulated noisy GPS coordinates")]
        public Vector2d[] TestGPSTrace = new Vector2d[]
        {
            new Vector2d(37.7749, -122.4194),
            new Vector2d(37.7751, -122.4193),
            new Vector2d(37.7753, -122.4191),
            new Vector2d(37.7755, -122.4189),
            new Vector2d(37.7757, -122.4187),
            new Vector2d(37.7759, -122.4185),
            new Vector2d(37.7761, -122.4183),
            new Vector2d(37.7763, -122.4181)
        };

        [Header("Optional Parameters")]
        [Tooltip("GPS accuracy radius for each point (0 = use default). Higher for noisy traces.")]
        public uint DefaultRadius = 10;

        [Tooltip("Enable to remove clusters and resample traces")]
        public bool TidyTrace = false;

        [Tooltip("Request turn-by-turn steps")]
        public bool IncludeSteps = false;

        [Tooltip("Overview geometry detail")]
        public MapMatching.Overview GeometryOverview = MapMatching.Overview.Simplified;

        private MapboxMapMatcherApi _mapMatcherApi;

        void Start()
        {
            if (MapCore == null)
            {
                Debug.LogError("MapCore is not assigned! Please assign a MapBehaviourCore component.");
                return;
            }

            MapCore.Initialized += OnMapInitialized;
        }

        void OnMapInitialized(Mapbox.BaseModule.Map.MapboxMap map)
        {
            _mapMatcherApi = new MapboxMapMatcherApi(map.MapService.FileSource, TimeoutSeconds);
            Debug.Log("Map Matching API initialized.");

            if (UseTestTrace)
            {
                Debug.Log("Matching test GPS trace...");
                MatchGPSTrace(TestGPSTrace);
            }
        }

        /// <summary>
        /// Match a GPS trace to the road network.
        /// </summary>
        /// <param name="gpsTrace">Array of GPS coordinates to match</param>
        public void MatchGPSTrace(Vector2d[] gpsTrace)
        {
            if (_mapMatcherApi == null)
            {
                Debug.LogWarning("Map Matching API not initialized yet. Waiting for map...");
                return;
            }

            if (gpsTrace == null || gpsTrace.Length < 2)
            {
                Debug.LogError("GPS trace must have at least 2 coordinates");
                return;
            }

            if (gpsTrace.Length > 100)
            {
                Debug.LogError("GPS trace cannot have more than 100 coordinates");
                return;
            }

            // Create map matching request
            var mapMatchingResource = new MapMatchingResource
            {
                Coordinates = gpsTrace,
                Profile = MatchingProfile,
                Steps = IncludeSteps,
                Overview = GeometryOverview,
                Tidy = TidyTrace
            };

            // Set radiuses if specified
            if (DefaultRadius > 0)
            {
                uint[] radiuses = new uint[gpsTrace.Length];
                for (int i = 0; i < radiuses.Length; i++)
                {
                    radiuses[i] = DefaultRadius;
                }
                mapMatchingResource.Radiuses = radiuses;
            }

            Debug.Log($"Sending map matching request with {gpsTrace.Length} coordinates...");

            // Execute the map matching request
            _mapMatcherApi.Match(mapMatchingResource, HandleMapMatchingResponse);
        }

        void HandleMapMatchingResponse(MapMatchingResponse response)
        {
            // Check for request errors
            if (response.HasRequestError)
            {
                Debug.LogError("Map matching request failed:");
                foreach (var ex in response.RequestExceptions)
                {
                    Debug.LogError($"  {ex.Message}");
                }
                return;
            }

            // Check for matching errors
            if (response.HasMatchingError)
            {
                Debug.LogError($"Map matching error: {response.MatchingError}");
                Debug.LogError($"Error code: {response.Code}");
                if (!string.IsNullOrEmpty(response.Message))
                {
                    Debug.LogError($"Message: {response.Message}");
                }
                return;
            }

            // Check if we got any matches
            if (response.Matchings == null || response.Matchings.Length == 0)
            {
                Debug.LogWarning("No matches found for GPS trace");
                return;
            }

            Debug.Log($"=== Map Matching Results ===");
            Debug.Log($"Found {response.Matchings.Length} matching(s)");

            // Display each matching
            for (int i = 0; i < response.Matchings.Length; i++)
            {
                var match = response.Matchings[i];
                Debug.Log($"\n--- Matching {i + 1} ---");
                Debug.Log($"Confidence: {match.Confidence:F2} (0=low, 1=high)");
                Debug.Log($"Distance: {match.Distance:F2} meters ({match.Distance / 1000:F2} km)");
                Debug.Log($"Duration: {match.Duration:F0} seconds ({match.Duration / 60:F1} minutes)");
                Debug.Log($"Weight: {match.Weight}");

                if (match.Geometry != null)
                {
                    Debug.Log($"Matched geometry points: {match.Geometry.Count}");
                    Debug.Log($"Start: ({match.Geometry[0].x:F6}, {match.Geometry[0].y:F6})");
                    Debug.Log($"End: ({match.Geometry[match.Geometry.Count - 1].x:F6}, {match.Geometry[match.Geometry.Count - 1].y:F6})");
                }

                if (match.Legs != null && match.Legs.Count > 0)
                {
                    Debug.Log($"Route legs: {match.Legs.Count}");
                }
            }

            // Display tracepoint information
            if (response.Tracepoints != null && response.Tracepoints.Length > 0)
            {
                Debug.Log($"\n=== Tracepoints ({response.Tracepoints.Length}) ===");
                int unambiguousMatches = 0;

                for (int i = 0; i < response.Tracepoints.Length; i++)
                {
                    var tracepoint = response.Tracepoints[i];
                    if (tracepoint == null)
                    {
                        Debug.Log($"Tracepoint {i}: Omitted (removed by Tidy or unmatched)");
                        continue;
                    }

                    if (tracepoint.AlternativesCount == 0)
                    {
                        unambiguousMatches++;
                    }

                    Debug.Log($"Tracepoint {i}:");
                    Debug.Log($"  Name: {tracepoint.Name}");
                    Debug.Log($"  Location: ({tracepoint.Location[1]}, {tracepoint.Location[0]})"); // lat, lng
                    Debug.Log($"  Waypoint Index: {tracepoint.WaypointIndex}");
                    Debug.Log($"  Alternatives Count: {tracepoint.AlternativesCount} {(tracepoint.AlternativesCount == 0 ? "(unambiguous)" : "")}");
                }

                Debug.Log($"\nUnambiguous matches: {unambiguousMatches}/{response.Tracepoints.Length}");
            }

            // Summary
            var bestMatch = response.Matchings[0];
            Debug.Log($"\n=== Summary ===");
            Debug.Log($"Best match confidence: {bestMatch.Confidence:F2}");
            Debug.Log($"GPS trace cleaned and snapped to {bestMatch.Geometry.Count} road points");
        }

        void OnDestroy()
        {
            if (MapCore != null)
            {
                MapCore.Initialized -= OnMapInitialized;
            }
        }
    }
}
