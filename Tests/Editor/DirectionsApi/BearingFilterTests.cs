using System;
using Mapbox.Utils;
using NUnit.Framework;

namespace Mapbox.DirectionsApiTests
{
    [TestFixture]
    public class BearingFilterTests
    {
        [Test]
        public void Constructor_SetsValues()
        {
            var filter = new BearingFilter(45, 10);

            Assert.AreEqual(45, filter.Bearing);
            Assert.AreEqual(10, filter.Range);
        }

        [Test]
        public void Constructor_AcceptsNullValues()
        {
            var filter = new BearingFilter(null, null);

            Assert.IsNull(filter.Bearing);
            Assert.IsNull(filter.Range);
        }

        [Test]
        public void Constructor_ThrowsException_WhenBearingIsNegative()
        {
            Assert.Throws<Exception>(() => new BearingFilter(-1, 10));
        }

        [Test]
        public void Constructor_ThrowsException_WhenBearingIsGreaterThan360()
        {
            Assert.Throws<Exception>(() => new BearingFilter(361, 10));
        }

        [Test]
        public void Constructor_AcceptsBearing_AtZero()
        {
            var filter = new BearingFilter(0, 10);
            Assert.AreEqual(0, filter.Bearing);
        }

        [Test]
        public void Constructor_AcceptsBearing_At360()
        {
            var filter = new BearingFilter(360, 10);
            Assert.AreEqual(360, filter.Bearing);
        }

        [Test]
        public void Constructor_ThrowsException_WhenRangeIsNegative()
        {
            Assert.Throws<Exception>(() => new BearingFilter(45, -1));
        }

        [Test]
        public void Constructor_ThrowsException_WhenRangeIsGreaterThan180()
        {
            Assert.Throws<Exception>(() => new BearingFilter(45, 181));
        }

        [Test]
        public void Constructor_AcceptsRange_AtZero()
        {
            var filter = new BearingFilter(45, 0);
            Assert.AreEqual(0, filter.Range);
        }

        [Test]
        public void Constructor_AcceptsRange_At180()
        {
            var filter = new BearingFilter(45, 180);
            Assert.AreEqual(180, filter.Range);
        }

        [Test]
        public void ToString_ReturnsCorrectFormat()
        {
            var filter = new BearingFilter(45.5, 10.5);
            var result = filter.ToString();

            Assert.AreEqual("45.5,10.5", result);
        }

        [Test]
        public void ToString_ReturnsEmptyString_WhenBearingIsNull()
        {
            var filter = new BearingFilter(null, 10);
            var result = filter.ToString();

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void ToString_ReturnsEmptyString_WhenRangeIsNull()
        {
            var filter = new BearingFilter(45, null);
            var result = filter.ToString();

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void ToString_ReturnsEmptyString_WhenBothAreNull()
        {
            var filter = new BearingFilter(null, null);
            var result = filter.ToString();

            Assert.AreEqual(string.Empty, result);
        }

        [TestCase(0, 0, "0,0")]
        [TestCase(90, 45, "90,45")]
        [TestCase(180, 90, "180,90")]
        [TestCase(270, 135, "270,135")]
        [TestCase(360, 180, "360,180")]
        public void ToString_ReturnsCorrectFormat_ForVariousAngles(double bearing, double range, string expected)
        {
            var filter = new BearingFilter(bearing, range);
            var result = filter.ToString();

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Constructor_AcceptsDecimalValues()
        {
            var filter = new BearingFilter(123.456, 78.9);

            Assert.AreEqual(123.456, filter.Bearing);
            Assert.AreEqual(78.9, filter.Range);
        }

        [Test]
        public void Constructor_ThrowsException_OnlyWhenBearingIsInvalid()
        {
            // Valid bearing, invalid range should throw
            Assert.Throws<Exception>(() => new BearingFilter(45, 200));
        }
    }
}