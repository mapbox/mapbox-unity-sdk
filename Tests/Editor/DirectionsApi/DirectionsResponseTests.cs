using System.Collections.Generic;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities.JsonConverters;
using Mapbox.DirectionsApi.Response;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Mapbox.DirectionsApiTests
{
    [TestFixture]
    public class DirectionsResponseTests
    {
        private string _sampleResponseJson = @"{
            ""routes"": [
                {
                    ""distance"": 12345.6,
                    ""duration"": 789.0,
                    ""weight"": 800.0
                }
            ],
            ""waypoints"": [
                {
                    ""name"": ""Main Street"",
                    ""location"": [-122.4194, 37.7749]
                },
                {
                    ""name"": ""Market Street"",
                    ""location"": [-118.2437, 34.0522]
                }
            ],
            ""code"": ""Ok""
        }";

        [Test]
        public void Deserialize_ParsesValidResponse()
        {
            var response = JsonConvert.DeserializeObject<DirectionsResponse>(_sampleResponseJson, JsonConverters.Converters);

            Assert.IsNotNull(response);
            Assert.AreEqual("Ok", response.Code);
        }

        [Test]
        public void Deserialize_ParsesRoutes()
        {
            var response = JsonConvert.DeserializeObject<DirectionsResponse>(_sampleResponseJson, JsonConverters.Converters);

            Assert.IsNotNull(response.Routes);
            Assert.AreEqual(1, response.Routes.Count);
            Assert.AreEqual(12345.6, response.Routes[0].Distance);
            Assert.AreEqual(789.0, response.Routes[0].Duration);
        }

        [Test]
        public void Deserialize_ParsesWaypoints()
        {
            var response = JsonConvert.DeserializeObject<DirectionsResponse>(_sampleResponseJson, JsonConverters.Converters);

            Assert.IsNotNull(response.Waypoints);
            Assert.AreEqual(2, response.Waypoints.Count);
            Assert.AreEqual("Main Street", response.Waypoints[0].Name);
            Assert.AreEqual("Market Street", response.Waypoints[1].Name);
        }

        [Test]
        public void Deserialize_ParsesCode()
        {
            var response = JsonConvert.DeserializeObject<DirectionsResponse>(_sampleResponseJson, JsonConverters.Converters);

            Assert.AreEqual("Ok", response.Code);
        }

        [Test]
        public void Serialize_CreatesValidJson()
        {
            var response = new DirectionsResponse
            {
                Code = "Ok",
                Routes = new List<Route>(),
                Waypoints = new List<Waypoint>()
            };

            var json = JsonConvert.SerializeObject(response, JsonConverters.Converters);

            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("\"code\""));
            Assert.IsTrue(json.Contains("\"routes\""));
            Assert.IsTrue(json.Contains("\"waypoints\""));
        }

        [Test]
        public void Deserialize_HandlesEmptyRoutes()
        {
            var json = @"{
                ""routes"": [],
                ""waypoints"": [],
                ""code"": ""NoRoute""
            }";

            var response = JsonConvert.DeserializeObject<DirectionsResponse>(json, JsonConverters.Converters);

            Assert.IsNotNull(response.Routes);
            Assert.AreEqual(0, response.Routes.Count);
            Assert.AreEqual("NoRoute", response.Code);
        }

        [Test]
        public void Deserialize_HandlesMultipleRoutes()
        {
            var json = @"{
                ""routes"": [
                    {
                        ""distance"": 1000.0,
                        ""duration"": 100.0,
                        ""weight"": 110.0
                    },
                    {
                        ""distance"": 1200.0,
                        ""duration"": 120.0,
                        ""weight"": 130.0
                    }
                ],
                ""waypoints"": [],
                ""code"": ""Ok""
            }";

            var response = JsonConvert.DeserializeObject<DirectionsResponse>(json, JsonConverters.Converters);

            Assert.AreEqual(2, response.Routes.Count);
            Assert.AreEqual(1000.0, response.Routes[0].Distance);
            Assert.AreEqual(1200.0, response.Routes[1].Distance);
        }

        [Test]
        public void SerializeDeserialize_RoundTrip()
        {
            var original = new DirectionsResponse
            {
                Code = "Ok",
                Routes = new List<Route>
                {
                    new Route
                    {
                        Distance = 5000.0,
                        Duration = 300.0,
                        Weight = 320.0f,
                        Geometry = new List<Vector2d>()
                    }
                },
                Waypoints = new List<Waypoint>
                {
                    new Waypoint
                    {
                        Name = "Test Waypoint"
                    }
                }
            };

            var json = JsonConvert.SerializeObject(original, JsonConverters.Converters);
            var deserialized = JsonConvert.DeserializeObject<DirectionsResponse>(json, JsonConverters.Converters);

            Assert.AreEqual(original.Code, deserialized.Code);
            Assert.AreEqual(original.Routes.Count, deserialized.Routes.Count);
            Assert.AreEqual(original.Routes[0].Distance, deserialized.Routes[0].Distance);
            Assert.AreEqual(original.Waypoints[0].Name, deserialized.Waypoints[0].Name);
        }

        [Test]
        public void Deserialize_HandlesNullFields()
        {
            var json = @"{
                ""code"": ""Ok""
            }";

            var response = JsonConvert.DeserializeObject<DirectionsResponse>(json, JsonConverters.Converters);

            Assert.IsNotNull(response);
            Assert.AreEqual("Ok", response.Code);
        }
    }
}