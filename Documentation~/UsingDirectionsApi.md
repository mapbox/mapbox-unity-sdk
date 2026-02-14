
# Using the Mapbox Directions API

The Mapbox Directions API allows you to calculate routes between locations. You can get turn-by-turn directions, route geometry, distance, and duration for different routing profiles (driving, walking, cycling).

For complete API reference and details, see the [official Mapbox Directions API documentation](https://docs.mapbox.com/api/navigation/directions/).

## Basic Setup

To use the Directions API, you need to:

1. Reference the `MapBehaviourCore` component to access the map's file source.
2. Create a `MapboxDirectionsApi` instance with the file source.
3. Build a `DirectionResource` with your origin and destination coordinates.
4. Query the API and handle the response in a callback.

Here's a simple example:

```csharp
using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.DirectionsApi;
using Mapbox.DirectionsApi.Response;
using Mapbox.Utils;

public class DirectionsExample : MonoBehaviour
{
    public MapBehaviourCore MapCore;
    private MapboxDirectionsApi _directionsApi;

    void Start()
    {
        MapCore.Initialized += map =>
        {
            _directionsApi = new MapboxDirectionsApi(map.MapService.FileSource);
            GetRoute();
        };
    }

    void GetRoute()
    {
        // Define two coordinates: San Francisco to Los Angeles
        var origin = new LatitudeLongitude(37.7749, -122.4194);
        var destination = new LatitudeLongitude(34.0522, -118.2437);

        // Create a directions request with driving profile
        var directionResource = new DirectionResource(
            new[] { origin, destination },
            RoutingProfile.Driving
        );

        // Query the API
        _directionsApi.Query(directionResource, response =>
        {
            if (response == null || response.Routes == null || response.Routes.Count == 0)
            {
                Debug.Log("No route found");
                return;
            }

            var route = response.Routes[0];
            Debug.Log($"Route found!");
            Debug.Log($"Distance: {route.Distance} meters");
            Debug.Log($"Duration: {route.Duration} seconds");
            Debug.Log($"Geometry points: {route.Geometry.Count}");
        });
    }
}
```

## What This Does

- The script waits for the map to initialize and gets access to the file source.
- Creates a `MapboxDirectionsApi` instance for making routing queries.
- Defines a route from San Francisco to Los Angeles using latitude/longitude coordinates.
- Requests a driving route between the two points.
- When the response arrives, it logs the route distance (in meters), duration (in seconds), and number of geometry points.

## Routing Profiles

You can choose from three routing profiles:

- `RoutingProfile.Driving` - Optimized for car travel
- `RoutingProfile.Walking` - Optimized for pedestrians
- `RoutingProfile.Cycling` - Optimized for bicycle travel

## Optional Parameters

The `DirectionResource` supports several optional parameters:

```csharp
var directionResource = new DirectionResource(coordinates, RoutingProfile.Driving)
{
    Alternatives = true,        // Get alternative routes
    Steps = true,              // Include turn-by-turn instructions
    Overview = Overview.Full   // Get full route geometry
};
```

## Response Data

The `DirectionsResponse` contains:
- `Routes` - List of possible routes (usually one, unless alternatives are requested)
- `Waypoints` - Snapped waypoint locations
- `Code` - Response status code ("Ok" if successful)

Each `Route` includes:
- `Distance` - Total route distance in meters
- `Duration` - Estimated travel time in seconds
- `Geometry` - List of lat/lng coordinates forming the route path
- `Weight` - Route weight (typically duration-based)

---
