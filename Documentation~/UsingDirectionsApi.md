
# Using the Mapbox Directions API

The Mapbox Directions API allows you to calculate routes between locations. You can get turn-by-turn directions, route geometry, distance, and duration for different routing profiles (driving, driving with traffic, walking, cycling).

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

You can choose from four routing profiles:

- `RoutingProfile.Driving` - Optimized for car travel
- `RoutingProfile.DrivingTraffic` - Car travel with live traffic conditions factored in (`mapbox/driving-traffic`)
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
- `Legs` - One leg per pair of consecutive waypoints; each `Leg` carries the `Annotation` described below when annotations are requested

## Annotations

Annotations are per-segment data returned alongside the route geometry: one entry for each pair of consecutive coordinates. Request them by setting `DirectionResource.Annotations` to one or more `AnnotationOptions` flags, combined with `|`:

- `AnnotationOptions.Distance` - Distance of each segment, in meters
- `AnnotationOptions.Duration` - Duration of each segment, in seconds
- `AnnotationOptions.Speed` - Speed along each segment, in meters per second
- `AnnotationOptions.Congestion` - Congestion level of each segment, as a string (`DrivingTraffic` only)
- `AnnotationOptions.CongestionNumeric` - Congestion level of each segment, as a number (`DrivingTraffic` only)
- `AnnotationOptions.MaxSpeed` - Posted speed limit of each segment (`Driving` and `DrivingTraffic` only)

Annotations require `Overview = Overview.Full`, because they line up with the full-resolution geometry. The API rejects any other combination, so `GetUrl()` throws before the request is sent.

```csharp
var directionResource = new DirectionResource(coordinates, RoutingProfile.DrivingTraffic)
{
    Overview = Overview.Full,
    Annotations = AnnotationOptions.CongestionNumeric | AnnotationOptions.MaxSpeed
};
```

The values arrive on each `Leg.Annotation`. Two fields need more explanation than the others:

- `CongestionNumeric` - An `int?[]` from 0 (free-flowing) to 100 (maximum congestion). An entry is `null` where congestion is unknown.
- `MaxSpeed` - A `MaxSpeed[]`. Each entry is a known limit (`Speed` and `Unit`, for example 50 and `km/h`), an unknown limit (`Unknown` is true), or a segment with no legal limit (`None` is true). `Speed` is `null` in the last two cases, so check the flags first.

To bucket a numeric congestion value, use `CongestionLevelUtility.LevelForNumeric`, which returns a `CongestionLevel`: `Low` (0-39), `Moderate` (40-59), `Heavy` (60-79), `Severe` (80-100), or `Unknown` for `null`.

The DirectionsApiDemo sample colors its route this way: `CongestionRouteRenderer` concatenates each leg's `CongestionNumeric`, buckets it into `CongestionLevel` runs and builds one colored line mesh per level.

---
