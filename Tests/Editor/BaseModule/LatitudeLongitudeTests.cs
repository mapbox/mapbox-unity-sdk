using System.Globalization;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.BaseModule.Utilities.JsonConverters;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Mapbox.BaseModuleTests
{
    public class LatitudeLongitudeTests
    {
        [Test]
        public void Constructor_SetsFieldsCorrectly()
        {
            var ll = new LatitudeLongitude(10.5, -20.25);
            Assert.AreEqual(10.5, ll.Latitude);
            Assert.AreEqual(-20.25, ll.Longitude);
        }

        [Test]
        public void Invalid_ReturnsValuesOutsideRange()
        {
            var invalid = LatitudeLongitude.Invalid;
            Assert.AreEqual(LatitudeLongitude.MAX_LATITUDE * 2, invalid.Latitude);
            Assert.AreEqual(LatitudeLongitude.MAX_LONGITUDE * 2, invalid.Longitude);
            Assert.IsFalse(invalid.IsValid());
        }

        [Test]
        public void ToString_ReturnsExpectedFormat()
        {
            var ll = new LatitudeLongitude(12.345, -67.89);
            var str = ll.ToString();
            Assert.AreEqual("12.345,-67.89", str);
        }

        [Test]
        public void ToStringLonLat_ReturnsFormattedWith5Decimals()
        {
            var ll = new LatitudeLongitude(45.1234567, -89.9876543);
            var str = ll.ToStringLonLat();
            Assert.AreEqual(
                string.Format(CultureInfo.InvariantCulture, "{0:F5},{1:F5}", -89.9876543, 45.1234567),
                str);
        }
        
        [Test]
        public void AlmostEqual_WithinTolerance_ReturnsTrue()
        {
            Assert.IsTrue(LatitudeLongitude.AlmostEqual(1.00000001, 1.00000002, 1e-6));
        }

        [Test]
        public void AlmostEqual_OutsideTolerance_ReturnsFalse()
        {
            Assert.IsFalse(LatitudeLongitude.AlmostEqual(1.0, 1.1, 1e-6));
        }

        [Test]
        public void Equals_ReturnsTrueForEqualValues()
        {
            var a = new LatitudeLongitude(10, 20);
            var b = new LatitudeLongitude(10, 20);
            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
        }

        [Test]
        public void Equals_ReturnsFalseForDifferentValues()
        {
            var a = new LatitudeLongitude(10, 20);
            var b = new LatitudeLongitude(10.0001, 20);
            Assert.IsFalse(a.Equals(b));
            Assert.IsFalse(a == b);
            Assert.IsTrue(a != b);
        }

        [Test]
        public void IsValid_ReturnsTrueForInRangeValues()
        {
            var ll = new LatitudeLongitude(45, -45);
            Assert.IsTrue(ll.IsValid());
        }

        [TestCase(91, 0)]   // Latitude too high
        [TestCase(-91, 0)]  // Latitude too low
        [TestCase(0, 91)]   // Longitude too high
        [TestCase(0, -91)]  // Longitude too low
        public void IsValid_ReturnsFalseForOutOfRangeValues(double lat, double lon)
        {
            var ll = new LatitudeLongitude(lat, lon);
            Assert.IsFalse(ll.IsValid());
        }
    }
}