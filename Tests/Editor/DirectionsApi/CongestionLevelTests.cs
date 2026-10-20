using Mapbox.DirectionsApi;
using NUnit.Framework;

namespace Mapbox.DirectionsApiTests
{
    [TestFixture]
    public class CongestionLevelTests
    {
        [Test]
        public void LevelForNumeric_Null_ReturnsUnknown()
        {
            Assert.AreEqual(CongestionLevel.Unknown, CongestionLevelUtility.LevelForNumeric(null));
        }

        [TestCase(0, CongestionLevel.Low)]
        [TestCase(39, CongestionLevel.Low)]
        [TestCase(40, CongestionLevel.Moderate)]
        [TestCase(59, CongestionLevel.Moderate)]
        [TestCase(60, CongestionLevel.Heavy)]
        [TestCase(79, CongestionLevel.Heavy)]
        [TestCase(80, CongestionLevel.Severe)]
        [TestCase(100, CongestionLevel.Severe)]
        public void LevelForNumeric_BucketsAtBoundaries(int value, CongestionLevel expected)
        {
            Assert.AreEqual(expected, CongestionLevelUtility.LevelForNumeric(value));
        }
    }
}
