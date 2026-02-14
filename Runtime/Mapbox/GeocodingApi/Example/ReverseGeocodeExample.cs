using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;
using Mapbox.GeocodingApi;

/// <summary>
/// Example script demonstrating reverse geocoding - converting coordinates to place names.
/// Reverse geocoding takes latitude/longitude coordinates and returns human-readable addresses.
/// </summary>
public class ReverseGeocodeExample : MonoBehaviour
{
    [Header("Map Reference")]
    public MapBehaviourCore MapCore;

    [Header("Coordinates to Reverse Geocode")]
    [Tooltip("Latitude of the location to look up (default: Golden Gate Bridge)")]
    public double Latitude = 37.8199;

    [Tooltip("Longitude of the location to look up (default: Golden Gate Bridge)")]
    public double Longitude = -122.4783;

    [Header("Optional Filters")]
    [Tooltip("Filter by place type (e.g., 'address', 'poi', 'place', 'neighborhood')")]
    public string[] TypeFilter;

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
        Debug.Log($"Geocoding API initialized. Looking up coordinates: ({Latitude}, {Longitude})");
        GetAddressFromCoordinates();
    }

    /// <summary>
    /// Perform a reverse geocoding lookup with the configured coordinates.
    /// </summary>
    public void GetAddressFromCoordinates()
    {
        if (_geocodingApi == null)
        {
            Debug.LogWarning("Geocoding API not initialized yet. Waiting for map...");
            return;
        }

        // Create the location to reverse geocode
        var location = new LatitudeLongitude(Latitude, Longitude);

        // Create reverse geocode request
        var reverseGeocodeResource = new ReverseGeocodeResource(location);

        // Apply optional type filter
        if (TypeFilter != null && TypeFilter.Length > 0)
        {
            reverseGeocodeResource.Types = TypeFilter;
        }

        // Execute the reverse geocoding request
        _geocodingApi.Geocode(reverseGeocodeResource, HandleReverseGeocodeResponse);
    }

    void HandleReverseGeocodeResponse(ReverseGeocodeResponse response)
    {
        if (response == null)
        {
            Debug.LogError("Reverse geocoding API returned null response");
            return;
        }

        if (response.Features == null || response.Features.Count == 0)
        {
            Debug.LogWarning($"No address found for coordinates: ({Latitude}, {Longitude})");
            return;
        }

        Debug.Log($"=== Found {response.Features.Count} Feature(s) at Location ===");
        Debug.Log($"Query Coordinates: [{response.Query[0]}, {response.Query[1]}] (Lng, Lat)");

        // Display all features at this location
        for (int i = 0; i < response.Features.Count; i++)
        {
            var feature = response.Features[i];
            Debug.Log($"\n--- Feature {i + 1} ---");
            Debug.Log($"Place Name: {feature.PlaceName}");
            Debug.Log($"Text: {feature.Text}");
            Debug.Log($"Place Type: {string.Join(", ", feature.PlaceType)}");
            Debug.Log($"Relevance: {feature.Relevance:F2}");

            if (!string.IsNullOrEmpty(feature.Address))
            {
                Debug.Log($"Street Address: {feature.Address}");
            }

            // Display hierarchical context (neighborhood, city, state, country, etc.)
            if (feature.Context != null && feature.Context.Count > 0)
            {
                Debug.Log("Location Context:");
                foreach (var context in feature.Context)
                {
                    foreach (var kvp in context)
                    {
                        // Common keys: 'text' (name), 'short_code' (code)
                        if (kvp.Key == "text")
                        {
                            Debug.Log($"  {kvp.Value}");
                        }
                    }
                }
            }
        }

        // Highlight the most specific result (usually first)
        var primaryFeature = response.Features[0];
        Debug.Log($"\n=== Primary Address ===");
        Debug.Log(primaryFeature.PlaceName);

        // Build a simplified address string
        string simpleAddress = primaryFeature.Text;
        if (primaryFeature.Context != null)
        {
            foreach (var context in primaryFeature.Context)
            {
                if (context.ContainsKey("text"))
                {
                    simpleAddress += ", " + context["text"];
                }
            }
        }
        Debug.Log($"Simple Address: {simpleAddress}");
    }

    void OnDestroy()
    {
        if (MapCore != null)
        {
            MapCore.Initialized -= OnMapInitialized;
        }
    }

    // Optional: Helper method to reverse geocode from a Unity world position
    // Useful for getting addresses from map clicks
    public void GetAddressFromWorldPosition(Vector3 worldPosition)
    {
        if (MapCore == null || MapCore.InitializationStatus <= InitializationStatus.Initialized)
        {
            Debug.LogWarning("Map not initialized yet");
            return;
        }

        // Convert world position to lat/lng
        var latLng = MapCore.MapInformation.ConvertPositionToLatLng(worldPosition);

        // Update the inspector values
        Latitude = latLng.Latitude;
        Longitude = latLng.Longitude;

        // Perform reverse geocoding
        GetAddressFromCoordinates();
    }
}
