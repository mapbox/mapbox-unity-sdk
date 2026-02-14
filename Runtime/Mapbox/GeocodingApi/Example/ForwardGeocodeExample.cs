using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.GeocodingApi.Example
{
    /// <summary>
    /// Example script demonstrating forward geocoding - converting place names to coordinates.
    /// Forward geocoding takes a text query (address, place name, etc.) and returns geographic coordinates.
    /// </summary>
    public class ForwardGeocodeExample : MonoBehaviour
    {
        [Header("Map Reference")]
        public MapBehaviourCore MapCore;

        [Header("Search Query")]
        [Tooltip("The place name or address to search for")]
        public string SearchQuery = "San Francisco, CA";

        [Header("Optional Filters")]
        [Tooltip("Limit results to specific countries (ISO 3166-1 alpha-2 codes, e.g., 'us', 'gb', 'de')")]
        public string[] CountryFilter;

        [Tooltip("Enable autocomplete for partial queries")]
        public bool EnableAutocomplete = false;

        [Tooltip("Bias results near this location (latitude)")]
        public double ProximityLatitude = 0;

        [Tooltip("Bias results near this location (longitude)")]
        public double ProximityLongitude = 0;

        [Tooltip("Use proximity bias")]
        public bool UseProximity = false;

        private MapboxGeocodingApi _geocodingApi;

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
            _geocodingApi = new MapboxGeocodingApi(map.MapService.FileSource);
            Debug.Log("Geocoding API initialized. Searching for: " + SearchQuery);
            SearchPlace();
        }

        /// <summary>
        /// Perform a forward geocoding search with the configured query and filters.
        /// </summary>
        public void SearchPlace()
        {
            if (_geocodingApi == null)
            {
                Debug.LogWarning("Geocoding API not initialized yet. Waiting for map...");
                return;
            }

            if (string.IsNullOrEmpty(SearchQuery))
            {
                Debug.LogWarning("Search query is empty!");
                return;
            }

            // Create forward geocode request
            var forwardGeocodeResource = new ForwardGeocodeResource(SearchQuery);

            // Apply optional filters
            if (CountryFilter != null && CountryFilter.Length > 0)
            {
                forwardGeocodeResource.Country = CountryFilter;
            }

            if (EnableAutocomplete)
            {
                forwardGeocodeResource.Autocomplete = true;
            }

            if (UseProximity)
            {
                forwardGeocodeResource.Proximity = new Vector2d(ProximityLatitude, ProximityLongitude);
            }

            // Execute the geocoding request
            _geocodingApi.Geocode(forwardGeocodeResource, HandleForwardGeocodeResponse);
        }

        void HandleForwardGeocodeResponse(ForwardGeocodeResponse response)
        {
            if (response == null)
            {
                Debug.LogError("Geocoding API returned null response");
                return;
            }

            if (response.Features == null || response.Features.Count == 0)
            {
                Debug.LogWarning($"No results found for query: {SearchQuery}");
                return;
            }

            Debug.Log($"=== Found {response.Features.Count} Result(s) ===");

            // Display all results (typically ordered by relevance)
            for (int i = 0; i < response.Features.Count; i++)
            {
                var feature = response.Features[i];
                Debug.Log($"\n--- Result {i + 1} ---");
                Debug.Log($"Place Name: {feature.PlaceName}");
                Debug.Log($"Text: {feature.Text}");
                Debug.Log($"Coordinates: Lat {feature.Center.x}, Lng {feature.Center.y}");
                Debug.Log($"Place Type: {string.Join(", ", feature.PlaceType)}");
                Debug.Log($"Relevance: {feature.Relevance:F2}");

                if (!string.IsNullOrEmpty(feature.Address))
                {
                    Debug.Log($"Address: {feature.Address}");
                }

                // Display context (city, state, country, etc.)
                if (feature.Context != null && feature.Context.Count > 0)
                {
                    Debug.Log("Context:");
                    foreach (var context in feature.Context)
                    {
                        foreach (var kvp in context)
                        {
                            Debug.Log($"  {kvp.Key}: {kvp.Value}");
                        }
                    }
                }
            }

            // Highlight the best match
            var bestMatch = response.Features[0];
            Debug.Log($"\n=== Best Match ===");
            Debug.Log($"{bestMatch.PlaceName}");
            Debug.Log($"Location: ({bestMatch.Center.x}, {bestMatch.Center.y})");
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
