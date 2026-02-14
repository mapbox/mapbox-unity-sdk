
# Using the Mapbox Geocoding API

The Mapbox Geocoding API provides two main functions:
- **Forward Geocoding**: Convert addresses or place names into geographic coordinates (lat/lng)
- **Reverse Geocoding**: Convert geographic coordinates into human-readable addresses

For complete API reference and details, see the [official Mapbox Geocoding API documentation](https://docs.mapbox.com/api/search/geocoding/).

## Forward Geocoding

Forward geocoding converts a text query (like "San Francisco" or "1600 Pennsylvania Avenue") into coordinates.

### Basic Example

```csharp
using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.GeocodingApi;

public class ForwardGeocodeExample : MonoBehaviour
{
    public MapBehaviourCore MapCore;
    private MapboxGeocodingApi _geocodingApi;

    void Start()
    {
        MapCore.Initialized += map =>
        {
            _geocodingApi = new MapboxGeocodingApi(map.MapService.FileSource);
            SearchPlace("San Francisco, CA");
        };
    }

    void SearchPlace(string placeName)
    {
        var forwardGeocodeResource = new ForwardGeocodeResource(placeName);

        _geocodingApi.Geocode(forwardGeocodeResource, (ForwardGeocodeResponse response) =>
        {
            if (response == null || response.Features == null || response.Features.Count == 0)
            {
                Debug.Log("No results found");
                return;
            }

            var firstResult = response.Features[0];
            Debug.Log($"Found: {firstResult.PlaceName}");
            Debug.Log($"Coordinates: {firstResult.Center.x}, {firstResult.Center.y}");
        });
    }
}
```

### What This Does
- Waits for the map to initialize
- Creates a geocoding request for "San Francisco, CA"
- Returns the place name and coordinates (latitude, longitude)
- The `Center` property contains the location as a `Vector2d` with x=latitude, y=longitude

### Optional Parameters

You can refine your forward geocoding searches:

```csharp
var forwardGeocodeResource = new ForwardGeocodeResource("pizza")
{
    Country = new[] { "us" },              // Limit to United States
    Proximity = new Vector2d(37.7, -122.4), // Bias results near San Francisco
    Autocomplete = true,                    // Enable autocomplete suggestions
    Types = new[] { "poi" }                 // Only return points of interest
};
```

### Response Data

The `ForwardGeocodeResponse` contains:
- `Features` - List of matching places (usually ordered by relevance)
- `Query` - The original query string
- `Attribution` - Mapbox attribution text

Each `Feature` includes:
- `PlaceName` - Full address or place name
- `Text` - Short name of the place
- `Center` - Coordinates as Vector2d (x=lat, y=lng)
- `PlaceType` - Type of place (e.g., "address", "poi", "place")
- `Address` - Street address (if applicable)
- `Relevance` - Match score (0-1)
- `Context` - Additional location context (city, state, country, etc.)

---

## Reverse Geocoding

Reverse geocoding converts coordinates into a human-readable address or place name.

### Basic Example

```csharp
using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.GeocodingApi;
using Mapbox.Utils;

public class ReverseGeocodeExample : MonoBehaviour
{
    public MapBehaviourCore MapCore;
    private MapboxGeocodingApi _geocodingApi;

    void Start()
    {
        MapCore.Initialized += map =>
        {
            _geocodingApi = new MapboxGeocodingApi(map.MapService.FileSource);

            // Reverse geocode the Golden Gate Bridge location
            var location = new LatitudeLongitude(37.8199, -122.4783);
            GetAddressFromCoordinates(location);
        };
    }

    void GetAddressFromCoordinates(LatitudeLongitude location)
    {
        var reverseGeocodeResource = new ReverseGeocodeResource(location);

        _geocodingApi.Geocode(reverseGeocodeResource, (ReverseGeocodeResponse response) =>
        {
            if (response == null || response.Features == null || response.Features.Count == 0)
            {
                Debug.Log("No address found");
                return;
            }

            var firstResult = response.Features[0];
            Debug.Log($"Address: {firstResult.PlaceName}");
            Debug.Log($"Type: {string.Join(", ", firstResult.PlaceType)}");
        });
    }
}
```

### What This Does
- Waits for the map to initialize
- Creates a reverse geocoding request for coordinates (37.8199, -122.4783)
- Returns the human-readable address or place name at that location
- The first feature typically contains the most specific address

### Optional Parameters

You can filter reverse geocoding results by type:

```csharp
var reverseGeocodeResource = new ReverseGeocodeResource(location)
{
    Types = new[] { "address" }  // Only return street addresses
};
```

Common types: `country`, `region`, `postcode`, `place`, `locality`, `neighborhood`, `address`, `poi`

### Response Data

The `ReverseGeocodeResponse` contains:
- `Features` - List of places at or near the coordinates
- `Query` - The original coordinates as [longitude, latitude]
- `Attribution` - Mapbox attribution text

Each `Feature` includes the same fields as forward geocoding (see above).

---

## Common Use Cases

### Interactive Map Click
Convert clicked map positions to addresses:
```csharp
// Get world position from mouse click, convert to lat/lng, then reverse geocode
var worldPos = GetClickedWorldPosition();
var latLng = mapInformation.ConvertPositionToLatLng(worldPos);
var reverseResource = new ReverseGeocodeResource(latLng);
geocodingApi.Geocode(reverseResource, HandleReverseGeocodeResponse);
```

### Search Box
Implement a location search:
```csharp
var forwardResource = new ForwardGeocodeResource(searchText)
{
    Autocomplete = true,
    Country = new[] { "us" }
};
geocodingApi.Geocode(forwardResource, DisplaySearchResults);
```

---
