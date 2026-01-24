using System;
using System.Text;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.DirectionsApi;
using Mapbox.DirectionsApi.Response;
using NUnit.Framework;

namespace Mapbox.DirectionsApiTests
{
    [TestFixture]
    public class MapboxDirectionsApiTests
    {
        private class MockFileSource : IFileSource
        {
            private readonly string _responseJson;
            private readonly bool _shouldError;

            public MockFileSource(string responseJson, bool shouldError = false)
            {
                _responseJson = responseJson;
                _shouldError = shouldError;
            }

            public IAsyncRequest Request(string uri, Action<Response> callback, int timeout = 10)
            {
                if (!_shouldError)
                {
                    var response = new Response
                    {
                        Data = Encoding.UTF8.GetBytes(_responseJson)
                    };
                    callback(response);
                }

                return new MockAsyncRequest();
            }

            public IWebRequest MapboxImageRequest(string uri, Action<WebRequestResponse> callback, string etag = "", int timeout = 10, bool isNonReadable = true)
            {
                throw new NotImplementedException("Not needed for Directions API tests");
            }

            public IWebRequest CustomImageRequest(string uri, Action<WebRequestResponse> callback, string etag = null, int timeout = 10, bool isNonReadable = true)
            {
                throw new NotImplementedException("Not needed for Directions API tests");
            }

            public IWebRequest MapboxDataRequest(string uri, Action<WebRequestResponse> callback, string etag = "", int timeout = 10)
            {
                throw new NotImplementedException("Not needed for Directions API tests");
            }

            public void OnDestroy()
            {
                // No cleanup needed for mock
            }
        }

        private class MockAsyncRequest : IAsyncRequest
        {
            public bool IsCompleted { get; set; }
            public HttpRequestType RequestType { get; set; }

            public void Cancel()
            {
                IsCompleted = true;
            }
        }

        private string _validResponseJson = @"{
            ""routes"": [
                {
                    ""distance"": 10000.0,
                    ""duration"": 500.0,
                    ""weight"": 520.0
                }
            ],
            ""waypoints"": [
                {
                    ""name"": ""Start Point"",
                    ""location"": [-122.4194, 37.7749]
                },
                {
                    ""name"": ""End Point"",
                    ""location"": [-118.2437, 34.0522]
                }
            ],
            ""code"": ""Ok""
        }";

        [Test]
        public void Constructor_AcceptsFileSource()
        {
            var mockSource = new MockFileSource(_validResponseJson);
            var api = new MapboxDirectionsApi(mockSource);

            Assert.IsNotNull(api);
        }

        [Test]
        public void Query_ExecutesCallback()
        {
            var mockSource = new MockFileSource(_validResponseJson);
            var api = new MapboxDirectionsApi(mockSource);

            var coords = new LatitudeLongitude[]
            {
                new LatitudeLongitude(37.7749, -122.4194),
                new LatitudeLongitude(34.0522, -118.2437)
            };
            var resource = new DirectionResource(coords, RoutingProfile.Driving);

            DirectionsResponse capturedResponse = null;
            api.Query(resource, response =>
            {
                capturedResponse = response;
            });

            Assert.IsNotNull(capturedResponse);
        }

        [Test]
        public void Query_ReturnsValidResponse()
        {
            var mockSource = new MockFileSource(_validResponseJson);
            var api = new MapboxDirectionsApi(mockSource);

            var coords = new LatitudeLongitude[]
            {
                new LatitudeLongitude(37.7749, -122.4194),
                new LatitudeLongitude(34.0522, -118.2437)
            };
            var resource = new DirectionResource(coords, RoutingProfile.Driving);

            DirectionsResponse capturedResponse = null;
            api.Query(resource, response =>
            {
                capturedResponse = response;
            });

            Assert.IsNotNull(capturedResponse);
            Assert.AreEqual("Ok", capturedResponse.Code);
            Assert.AreEqual(1, capturedResponse.Routes.Count);
            Assert.AreEqual(2, capturedResponse.Waypoints.Count);
        }

        [Test]
        public void Query_ParsesRouteData()
        {
            var mockSource = new MockFileSource(_validResponseJson);
            var api = new MapboxDirectionsApi(mockSource);

            var coords = new LatitudeLongitude[]
            {
                new LatitudeLongitude(37.7749, -122.4194),
                new LatitudeLongitude(34.0522, -118.2437)
            };
            var resource = new DirectionResource(coords, RoutingProfile.Driving);

            DirectionsResponse capturedResponse = null;
            api.Query(resource, response =>
            {
                capturedResponse = response;
            });

            Assert.AreEqual(10000.0, capturedResponse.Routes[0].Distance);
            Assert.AreEqual(500.0, capturedResponse.Routes[0].Duration);
            Assert.AreEqual(520.0, capturedResponse.Routes[0].Weight);
        }

        [Test]
        public void Query_ParsesWaypointData()
        {
            var mockSource = new MockFileSource(_validResponseJson);
            var api = new MapboxDirectionsApi(mockSource);

            var coords = new LatitudeLongitude[]
            {
                new LatitudeLongitude(37.7749, -122.4194),
                new LatitudeLongitude(34.0522, -118.2437)
            };
            var resource = new DirectionResource(coords, RoutingProfile.Driving);

            DirectionsResponse capturedResponse = null;
            api.Query(resource, response =>
            {
                capturedResponse = response;
            });

            Assert.AreEqual("Start Point", capturedResponse.Waypoints[0].Name);
            Assert.AreEqual("End Point", capturedResponse.Waypoints[1].Name);
        }

        [Test]
        public void Query_ReturnsAsyncRequest()
        {
            var mockSource = new MockFileSource(_validResponseJson);
            var api = new MapboxDirectionsApi(mockSource);

            var coords = new LatitudeLongitude[]
            {
                new LatitudeLongitude(37.7749, -122.4194),
                new LatitudeLongitude(34.0522, -118.2437)
            };
            var resource = new DirectionResource(coords, RoutingProfile.Driving);

            var asyncRequest = api.Query(resource, response => { });

            Assert.IsNotNull(asyncRequest);
        }

        [Test]
        public void Serialize_CreatesJson()
        {
            var mockSource = new MockFileSource(_validResponseJson);
            var api = new MapboxDirectionsApi(mockSource);

            var response = new DirectionsResponse
            {
                Code = "Ok",
                Routes = new System.Collections.Generic.List<Route>(),
                Waypoints = new System.Collections.Generic.List<Waypoint>()
            };

            var json = api.Serialize(response);

            Assert.IsNotNull(json);
            Assert.IsTrue(json.Length > 0);
        }

        [Test]
        public void Query_HandlesDifferentProfiles()
        {
            var mockSource = new MockFileSource(_validResponseJson);
            var api = new MapboxDirectionsApi(mockSource);

            var coords = new LatitudeLongitude[]
            {
                new LatitudeLongitude(37.7749, -122.4194),
                new LatitudeLongitude(34.0522, -118.2437)
            };

            // Test with different routing profiles
            var walkingResource = new DirectionResource(coords, RoutingProfile.Walking);
            DirectionsResponse walkingResponse = null;
            api.Query(walkingResource, response => { walkingResponse = response; });

            Assert.IsNotNull(walkingResponse);
            Assert.AreEqual("Ok", walkingResponse.Code);
        }

        [Test]
        public void Query_HandlesEmptyRoutes()
        {
            var emptyRoutesJson = @"{
                ""routes"": [],
                ""waypoints"": [],
                ""code"": ""NoRoute""
            }";

            var mockSource = new MockFileSource(emptyRoutesJson);
            var api = new MapboxDirectionsApi(mockSource);

            var coords = new LatitudeLongitude[]
            {
                new LatitudeLongitude(37.7749, -122.4194),
                new LatitudeLongitude(34.0522, -118.2437)
            };
            var resource = new DirectionResource(coords, RoutingProfile.Driving);

            DirectionsResponse capturedResponse = null;
            api.Query(resource, response =>
            {
                capturedResponse = response;
            });

            Assert.IsNotNull(capturedResponse);
            Assert.AreEqual("NoRoute", capturedResponse.Code);
            Assert.AreEqual(0, capturedResponse.Routes.Count);
        }

        [Test]
        public void Query_HandlesMultipleWaypoints()
        {
            var mockSource = new MockFileSource(_validResponseJson);
            var api = new MapboxDirectionsApi(mockSource);

            var coords = new LatitudeLongitude[]
            {
                new LatitudeLongitude(37.7749, -122.4194),
                new LatitudeLongitude(36.1699, -115.1398), // Las Vegas
                new LatitudeLongitude(34.0522, -118.2437)
            };
            var resource = new DirectionResource(coords, RoutingProfile.Driving);

            DirectionsResponse capturedResponse = null;
            api.Query(resource, response =>
            {
                capturedResponse = response;
            });

            Assert.IsNotNull(capturedResponse);
        }
    }
}