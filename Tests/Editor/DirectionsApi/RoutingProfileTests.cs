using Mapbox.DirectionsApi;
using NUnit.Framework;

namespace Mapbox.DirectionsApiTests
{
    [TestFixture]
    public class RoutingProfileTests
    {
        [Test]
        public void Driving_Profile_Exists()
        {
            Assert.IsNotNull(RoutingProfile.Driving);
        }

        [Test]
        public void Walking_Profile_Exists()
        {
            Assert.IsNotNull(RoutingProfile.Walking);
        }

        [Test]
        public void Cycling_Profile_Exists()
        {
            Assert.IsNotNull(RoutingProfile.Cycling);
        }

        [Test]
        public void Driving_ToString_ReturnsCorrectValue()
        {
            var profile = RoutingProfile.Driving;
            Assert.AreEqual("mapbox/driving/", profile.ToString());
        }

        [Test]
        public void Walking_ToString_ReturnsCorrectValue()
        {
            var profile = RoutingProfile.Walking;
            Assert.AreEqual("mapbox/walking/", profile.ToString());
        }

        [Test]
        public void Cycling_ToString_ReturnsCorrectValue()
        {
            var profile = RoutingProfile.Cycling;
            Assert.AreEqual("mapbox/cycling/", profile.ToString());
        }

        [Test]
        public void GetProfile_ReturnsDriving_ForDrivingOption()
        {
            var profile = RoutingProfile.GetProfile(RoutingProfile.RoutingProfileOptions.Driving);
            Assert.AreEqual(RoutingProfile.Driving, profile);
        }

        [Test]
        public void GetProfile_ReturnsWalking_ForWalkingOption()
        {
            var profile = RoutingProfile.GetProfile(RoutingProfile.RoutingProfileOptions.Walking);
            Assert.AreEqual(RoutingProfile.Walking, profile);
        }

        [Test]
        public void GetProfile_ReturnsCycling_ForCyclingOption()
        {
            var profile = RoutingProfile.GetProfile(RoutingProfile.RoutingProfileOptions.Cycling);
            Assert.AreEqual(RoutingProfile.Cycling, profile);
        }

        [Test]
        public void Profiles_AreUnique()
        {
            Assert.AreNotEqual(RoutingProfile.Driving, RoutingProfile.Walking);
            Assert.AreNotEqual(RoutingProfile.Driving, RoutingProfile.Cycling);
            Assert.AreNotEqual(RoutingProfile.Walking, RoutingProfile.Cycling);
        }

        [Test]
        public void Profile_IsSameInstance()
        {
            var driving1 = RoutingProfile.Driving;
            var driving2 = RoutingProfile.Driving;

            Assert.AreSame(driving1, driving2);
        }
    }
}