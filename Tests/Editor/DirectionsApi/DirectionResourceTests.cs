using System;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.DirectionsApi;
using Mapbox.Utils;
using NUnit.Framework;

namespace Mapbox.DirectionsApiTests
{
    [TestFixture]
    public class DirectionResourceTests
    {
        private LatitudeLongitude[] _validCoordinates;

        [SetUp]
        public void Setup()
        {
            _validCoordinates = new LatitudeLongitude[]
            {
                new LatitudeLongitude(37.7749, -122.4194), // San Francisco
                new LatitudeLongitude(34.0522, -118.2437)  // Los Angeles
            };
        }

        [Test]
        public void Constructor_SetsCoordinatesAndProfile()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);

            Assert.AreEqual(2, resource.Coordinates.Length);
            Assert.AreEqual(RoutingProfile.Driving, resource.RoutingProfile);
        }

        [Test]
        public void Coordinates_ThrowsException_WhenLessThan2()
        {
            var invalidCoords = new LatitudeLongitude[]
            {
                new LatitudeLongitude(37.7749, -122.4194)
            };

            Assert.Throws<Exception>(() => new DirectionResource(invalidCoords, RoutingProfile.Driving));
        }

        [Test]
        public void Coordinates_ThrowsException_WhenMoreThan25()
        {
            var invalidCoords = new LatitudeLongitude[26];
            for (int i = 0; i < 26; i++)
            {
                invalidCoords[i] = new LatitudeLongitude(37.0 + i * 0.1, -122.0 + i * 0.1);
            }

            Assert.Throws<Exception>(() => new DirectionResource(invalidCoords, RoutingProfile.Driving));
        }

        [Test]
        public void Coordinates_AcceptsExactly2Elements()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Walking);
            Assert.AreEqual(2, resource.Coordinates.Length);
        }

        [Test]
        public void Coordinates_AcceptsExactly25Elements()
        {
            var coords = new LatitudeLongitude[25];
            for (int i = 0; i < 25; i++)
            {
                coords[i] = new LatitudeLongitude(37.0 + i * 0.1, -122.0 + i * 0.1);
            }

            var resource = new DirectionResource(coords, RoutingProfile.Cycling);
            Assert.AreEqual(25, resource.Coordinates.Length);
        }

        [Test]
        public void RoutingProfile_CanBeSet()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            resource.RoutingProfile = RoutingProfile.Walking;

            Assert.AreEqual(RoutingProfile.Walking, resource.RoutingProfile);
        }

        [Test]
        public void Alternatives_DefaultsToNull()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            Assert.IsNull(resource.Alternatives);
        }

        [Test]
        public void Alternatives_CanBeSet()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            resource.Alternatives = true;

            Assert.IsTrue(resource.Alternatives.Value);
        }

        [Test]
        public void Steps_DefaultsToNull()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            Assert.IsNull(resource.Steps);
        }

        [Test]
        public void Steps_CanBeSet()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            resource.Steps = true;

            Assert.IsTrue(resource.Steps.Value);
        }

        [Test]
        public void ContinueStraight_DefaultsToNull()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            Assert.IsNull(resource.ContinueStraight);
        }

        [Test]
        public void ContinueStraight_CanBeSet()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            resource.ContinueStraight = false;

            Assert.IsFalse(resource.ContinueStraight.Value);
        }

        [Test]
        public void Overview_DefaultsToNull()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            Assert.IsNull(resource.Overview);
        }

        [Test]
        public void Overview_CanBeSet()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            resource.Overview = Overview.Full;

            Assert.AreEqual(Overview.Full, resource.Overview);
        }

        [Test]
        public void Bearings_DefaultsToNull()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            Assert.IsNull(resource.Bearings);
        }

        [Test]
        public void Bearings_ThrowsException_WhenCountMismatchesCoordinates()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            var invalidBearings = new BearingFilter[]
            {
                new BearingFilter(45, 10)
            };

            Assert.Throws<Exception>(() => resource.Bearings = invalidBearings);
        }

        [Test]
        public void Bearings_AcceptsCorrectCount()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            var validBearings = new BearingFilter[]
            {
                new BearingFilter(45, 10),
                new BearingFilter(90, 20)
            };

            resource.Bearings = validBearings;
            Assert.AreEqual(2, resource.Bearings.Length);
        }

        [Test]
        public void Radiuses_DefaultsToNull()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            Assert.IsNull(resource.Radiuses);
        }

        [Test]
        public void Radiuses_ThrowsException_WhenCountMismatchesCoordinates()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            var invalidRadiuses = new double[] { 100.0 };

            Assert.Throws<Exception>(() => resource.Radiuses = invalidRadiuses);
        }

        [Test]
        public void Radiuses_ThrowsException_WhenValueIsZeroOrNegative()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            var invalidRadiuses = new double[] { 100.0, 0 };

            Assert.Throws<Exception>(() => resource.Radiuses = invalidRadiuses);
        }

        [Test]
        public void Radiuses_AcceptsPositiveValues()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            var validRadiuses = new double[] { 100.0, 200.0 };

            resource.Radiuses = validRadiuses;
            Assert.AreEqual(2, resource.Radiuses.Length);
            Assert.AreEqual(100.0, resource.Radiuses[0]);
            Assert.AreEqual(200.0, resource.Radiuses[1]);
        }

        [Test]
        public void GetUrl_ReturnsCorrectBaseUrl()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            var url = resource.GetUrl();

            Assert.IsTrue(url.Contains("directions/v5/"));
            Assert.IsTrue(url.Contains("mapbox/driving/"));
            Assert.IsTrue(url.EndsWith(".json"));
        }

        [Test]
        public void GetUrl_IncludesCoordinatesInCorrectFormat()
        {
            var coords = new LatitudeLongitude[]
            {
                new LatitudeLongitude(37.7749, -122.4194),
                new LatitudeLongitude(34.0522, -118.2437)
            };
            var resource = new DirectionResource(coords, RoutingProfile.Driving);
            var url = resource.GetUrl();

            // Coordinates should be in lon,lat format separated by semicolons
            Assert.IsTrue(url.Contains("-122"));
            Assert.IsTrue(url.Contains("37"));
            Assert.IsTrue(url.Contains(";"));
        }

        [Test]
        public void GetUrl_IncludesAlternativesWhenSet()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            resource.Alternatives = true;
            var url = resource.GetUrl();

            Assert.IsTrue(url.Contains("alternatives=true"));
        }

        [Test]
        public void GetUrl_IncludesStepsWhenSet()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            resource.Steps = true;
            var url = resource.GetUrl();

            Assert.IsTrue(url.Contains("steps=true"));
        }

        [Test]
        public void GetUrl_IncludesContinueStraightWhenSet()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            resource.ContinueStraight = false;
            var url = resource.GetUrl();

            Assert.IsTrue(url.Contains("continue_straight=false"));
        }

        [Test]
        public void GetUrl_IncludesOverviewWhenSet()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            resource.Overview = Overview.Full;
            var url = resource.GetUrl();

            Assert.IsTrue(url.Contains("overview=full"));
        }

        [Test]
        public void GetUrl_IncludesRadiusesWhenSet()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            resource.Radiuses = new double[] { 100.0, 200.0 };
            var url = resource.GetUrl();

            Assert.IsTrue(url.Contains("radiuses="));
        }

        [Test]
        public void GetUrl_IncludesBearingsWhenSet()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            resource.Bearings = new BearingFilter[]
            {
                new BearingFilter(45, 10),
                new BearingFilter(90, 20)
            };
            var url = resource.GetUrl();

            Assert.IsTrue(url.Contains("bearings="));
        }

        [Test]
        public void GetUrl_CombinesMultipleParameters()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Walking);
            resource.Alternatives = true;
            resource.Steps = true;
            resource.Overview = Overview.Simplified;

            var url = resource.GetUrl();

            Assert.IsTrue(url.Contains("alternatives=true"));
            Assert.IsTrue(url.Contains("steps=true"));
            Assert.IsTrue(url.Contains("overview=simplified"));
        }

        [Test]
        public void GetUrl_IncludesDrivingProfile()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Driving);
            var url = resource.GetUrl();

            Assert.IsTrue(url.Contains("mapbox/driving/"));
        }

        [Test]
        public void GetUrl_IncludesWalkingProfile()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Walking);
            var url = resource.GetUrl();

            Assert.IsTrue(url.Contains("mapbox/walking/"));
        }

        [Test]
        public void GetUrl_IncludesCyclingProfile()
        {
            var resource = new DirectionResource(_validCoordinates, RoutingProfile.Cycling);
            var url = resource.GetUrl();

            Assert.IsTrue(url.Contains("mapbox/cycling/"));
        }
    }
}