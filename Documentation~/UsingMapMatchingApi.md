
# Using the Mapbox Map Matching API

The Mapbox Map Matching API snaps GPS traces (sequences of coordinates) to the road network and returns the matched route. This is useful for:
- Cleaning up noisy GPS traces
- Matching recorded GPS tracks to actual roads
- Analyzing vehicle or pedestrian movement patterns
- Creating accurate route visualizations from tracking data

For complete API reference and details, see the [official Mapbox Map Matching API documentation](https://docs.mapbox.com/api/navigation/map-matching/).

## What is Map Matching?

Map matching takes a series of coordinates (typically from GPS tracking) and "snaps" them to the nearest roads, producing a clean, road-network-constrained path. Unlike the Directions API which calculates an optimal route between points, Map Matching respects the actual path taken and corrects for GPS inaccuracies.

## Basic Usage

```csharp
using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.DirectionsApi.MapMatching;
using Mapbox.BaseModule.Data.Vector2d;

public class MapMatchingExample : MonoBehaviour
{
    public MapBehaviourCore MapCore;
    private MapboxMapMatcherApi _mapMatcherApi;

    void Start()
    {
        MapCore.Initialized += map =>
        {
            _mapMatcherApi = new MapboxMapMatcherApi(map.MapService.FileSource, 30);
            MatchGPSTrace();
        };
    }

    void MatchGPSTrace()
    {
        // Example: noisy GPS coordinates from a recorded trip
        var gpsTrace = new Vector2d[]
        {
            new Vector2d(37.7749, -122.4194),
            new Vector2d(37.7751, -122.4193),
            new Vector2d(37.7753, -122.4191),
            new Vector2d(37.7755, -122.4189),
            new Vector2d(37.7757, -122.4187)
        };

        // Create map matching request
        var mapMatchingResource = new MapMatchingResource
        {
            Coordinates = gpsTrace,
            Profile = Profile.MapboxDriving
        };

        // Query the Map Matching API
        _mapMatcherApi.Match(mapMatchingResource, (MapMatchingResponse response) =>
        {
            if (response.HasMatchingError)
            {
                Debug.LogError($"Map matching error: {response.MatchingError}");
                return;
            }

            if (response.Matchings == null || response.Matchings.Length == 0)
            {
                Debug.Log("No matches found");
                return;
            }

            var match = response.Matchings[0];
            Debug.Log($"Match confidence: {match.Confidence:F2}");
            Debug.Log($"Distance: {match.Distance} meters");
            Debug.Log($"Duration: {match.Duration} seconds");
            Debug.Log($"Matched geometry points: {match.Geometry.Count}");
        });
    }
}
```

## What This Does
- Takes a sequence of GPS coordinates (simulating a tracked route)
- Sends them to the Map Matching API
- Returns a cleaned, road-snapped version of the route
- Includes a confidence score indicating how well the trace matched to roads

## Required Parameters

### Coordinates
An array of 2-100 coordinates representing the GPS trace:
```csharp
mapMatchingResource.Coordinates = new Vector2d[]
{
    new Vector2d(lat1, lng1),
    new Vector2d(lat2, lng2),
    // ... more points
};
```

### Profile
The routing profile to use for matching:
```csharp
Profile.MapboxDriving        // Match to driving roads
Profile.MapboxDrivingTraffic // Match with traffic considerations
Profile.MapboxWalking        // Match to pedestrian paths
Profile.MapboxCycling        // Match to cycling routes
```

## Optional Parameters

### Radiuses
Specify the GPS accuracy for each coordinate (in meters). Higher values for noisy traces:
```csharp
mapMatchingResource.Radiuses = new uint[] { 10, 10, 15, 20, 10 };
// Values between 1-30: lower (1-10) for clean traces, higher (20-30) for noisy
```

### Timestamps
Unix timestamps for each coordinate (useful for speed calculations):
```csharp
long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
mapMatchingResource.Timestamps = new long[]
{
    now,
    now + 5,   // 5 seconds later
    now + 10,  // 10 seconds later
    now + 15,
    now + 20
};
```

### Steps
Get turn-by-turn instructions:
```csharp
mapMatchingResource.Steps = true;
```

### Overview
Control the geometry detail level:
```csharp
mapMatchingResource.Overview = Overview.Full;        // Most detailed
mapMatchingResource.Overview = Overview.Simplified;  // Simplified (default)
mapMatchingResource.Overview = Overview.None;        // No geometry
```

### Tidy
Remove GPS trace clusters and re-sample for better matching:
```csharp
mapMatchingResource.Tidy = true;
```

### Annotations
Request additional metadata:
```csharp
mapMatchingResource.Annotations = Annotations.Duration | Annotations.Distance | Annotations.Speed;
```

### Geometries
Specify the geometry format:
```csharp
mapMatchingResource.Geometries = Geometries.Polyline;   // Default, precision 5
mapMatchingResource.Geometries = Geometries.Polyline6;  // Precision 6
mapMatchingResource.Geometries = Geometries.GeoJson;    // GeoJSON format
```

## Response Data

The `MapMatchingResponse` contains:

### Error Checking
```csharp
if (response.HasMatchingError)
{
    Debug.LogError(response.MatchingError);
    return;
}
```

### Matchings Array
Each `MatchObject` includes:
- `Confidence` - Float between 0 (low) and 1 (high) indicating match quality
- `Distance` - Total matched route distance in meters
- `Duration` - Estimated travel time in seconds
- `Geometry` - The road-snapped route as Vector2d points
- `Weight` - Route weight (duration-based)
- `Legs` - Individual segments if multiple waypoints

### Tracepoints Array
Information about each input coordinate:
- `Location` - The snapped location on the road network
- `Name` - Name of the matched street/road
- `WaypointIndex` - Index in the matched route
- `MatchingsIndex` - Which matching this belongs to
- `AlternativesCount` - Number of alternative matches (0 = unambiguous match)

## Common Use Cases

### Clean GPS Tracking Data
```csharp
// Convert noisy GPS logs to clean routes
var gpsLog = LoadGPSLog(); // Your GPS coordinates
var matchRequest = new MapMatchingResource
{
    Coordinates = gpsLog,
    Profile = Profile.MapboxDriving,
    Tidy = true,  // Remove clusters
    Radiuses = GetRadiuses(gpsLog)  // Set based on GPS accuracy
};
```

### Analyze Traveled Routes
```csharp
// Get detailed information about a traveled route
var matchRequest = new MapMatchingResource
{
    Coordinates = recordedPath,
    Profile = Profile.MapboxDriving,
    Annotations = Annotations.Duration | Annotations.Distance | Annotations.Speed,
    Steps = true  // Get turn-by-turn of actual path taken
};
```

### Visualize Movement Patterns
```csharp
// Match and visualize user movement
var matchRequest = new MapMatchingResource
{
    Coordinates = userPath,
    Profile = Profile.MapboxWalking,
    Overview = Overview.Full  // Get full geometry for drawing
};
```

## Differences from Directions API

| Feature | Directions API | Map Matching API |
|---------|---------------|------------------|
| Purpose | Find optimal route | Match GPS trace to roads |
| Input | 2-25 waypoints | 2-100 coordinates |
| Output | Optimal calculated route | Road-snapped actual path |
| Use Case | Navigation | GPS trace analysis |
| Path Adherence | Optimized | Follows input path |

---
