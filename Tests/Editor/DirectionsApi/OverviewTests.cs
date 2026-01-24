using Mapbox.DirectionsApi;
using NUnit.Framework;

namespace Mapbox.DirectionsApiTests
{
    [TestFixture]
    public class OverviewTests
    {
        [Test]
        public void Full_Overview_Exists()
        {
            Assert.IsNotNull(Overview.Full);
        }

        [Test]
        public void Simplified_Overview_Exists()
        {
            Assert.IsNotNull(Overview.Simplified);
        }

        [Test]
        public void False_Overview_Exists()
        {
            Assert.IsNotNull(Overview.False);
        }

        [Test]
        public void Full_ToString_ReturnsCorrectValue()
        {
            var overview = Overview.Full;
            Assert.AreEqual("full", overview.ToString());
        }

        [Test]
        public void Simplified_ToString_ReturnsCorrectValue()
        {
            var overview = Overview.Simplified;
            Assert.AreEqual("simplified", overview.ToString());
        }

        [Test]
        public void False_ToString_ReturnsCorrectValue()
        {
            var overview = Overview.False;
            Assert.AreEqual("false", overview.ToString());
        }

        [Test]
        public void Overviews_AreUnique()
        {
            Assert.AreNotEqual(Overview.Full, Overview.Simplified);
            Assert.AreNotEqual(Overview.Full, Overview.False);
            Assert.AreNotEqual(Overview.Simplified, Overview.False);
        }

        [Test]
        public void Overview_IsSameInstance()
        {
            var full1 = Overview.Full;
            var full2 = Overview.Full;

            Assert.AreSame(full1, full2);
        }
    }
}