using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities;
using Mapbox.DirectionsApi.Response;
using UnityEngine;

namespace Mapbox.DirectionsApi.Example
{
    /// <summary>
    /// Example script demonstrating basic usage of the Mapbox Directions API.
    /// Calculates a route between two coordinates and logs the result.
    /// </summary>
    public class DirectionsExample : MonoBehaviour
    {
        [Header("Map Reference")]
        public MapBehaviourCore MapCore;

        [Header("Route Settings")]
        public RoutingProfile.RoutingProfileOptions Profile = RoutingProfile.RoutingProfileOptions.Driving;

        [Header("Test Coordinates (Default: SF to LA)")]
        public double OriginLatitude = 37.7749;
        public double OriginLongitude = -122.4194;
        public double DestinationLatitude = 34.0522;
        public double DestinationLongitude = -118.2437;

        private MapboxDirectionsApi _directionsApi;

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
            _directionsApi = new MapboxDirectionsApi(map.MapService.FileSource);
            Debug.Log("Directions API initialized. Getting route...");
            GetRoute();
        }

        public void GetRoute()
        {
            if (_directionsApi == null)
            {
                Debug.LogWarning("Directions API not initialized yet. Waiting for map...");
                return;
            }

            // Create coordinates
            var origin = new LatitudeLongitude(OriginLatitude, OriginLongitude);
            var destination = new LatitudeLongitude(DestinationLatitude, DestinationLongitude);

            // Create a directions request
            var directionResource = new DirectionResource(
                new[] { origin, destination },
                RoutingProfile.GetProfile(Profile)
            )
            {
                Alternatives = false,
                Steps = false
            };

            // Query the API
            _directionsApi.Query(directionResource, HandleDirectionsResponse);
        }

        void HandleDirectionsResponse(DirectionsResponse response)
        {
            if (response == null)
            {
                Debug.LogError("Directions API returned null response");
                return;
            }

            if (response.Routes == null || response.Routes.Count == 0)
            {
                Debug.LogWarning($"No route found. Response code: {response.Code}");
                return;
            }

            // Log the first route
            var route = response.Routes[0];
            Debug.Log("=== Route Found ===");
            Debug.Log($"Profile: {Profile}");
            Debug.Log($"Distance: {route.Distance:F2} meters ({route.Distance / 1000:F2} km)");
            Debug.Log($"Duration: {route.Duration:F0} seconds ({route.Duration / 60:F1} minutes)");
            Debug.Log($"Weight: {route.Weight}");
            Debug.Log($"Geometry points: {route.Geometry.Count}");

            // Log first and last geometry points
            if (route.Geometry != null && route.Geometry.Count > 0)
            {
                var firstPoint = route.Geometry[0];
                var lastPoint = route.Geometry[route.Geometry.Count - 1];
                Debug.Log($"Start point: ({firstPoint.x}, {firstPoint.y})");
                Debug.Log($"End point: ({lastPoint.x}, {lastPoint.y})");
            }
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
