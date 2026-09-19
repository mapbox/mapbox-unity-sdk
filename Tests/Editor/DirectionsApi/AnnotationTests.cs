using Mapbox.DirectionsApi.Response;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Mapbox.DirectionsApiTests
{
    [TestFixture]
    public class AnnotationTests
    {
        [Test]
        public void Deserialize_ParsesCongestionNumeric()
        {
            var json = @"{ ""congestion_numeric"": [0, 45, 100] }";

            var annotation = JsonConvert.DeserializeObject<Annotation>(json);

            Assert.IsNotNull(annotation.CongestionNumeric);
            Assert.AreEqual(3, annotation.CongestionNumeric.Length);
            Assert.AreEqual(0, annotation.CongestionNumeric[0]);
            Assert.AreEqual(45, annotation.CongestionNumeric[1]);
            Assert.AreEqual(100, annotation.CongestionNumeric[2]);
        }

        [Test]
        public void Deserialize_ParsesCongestionNumeric_WithEmbeddedNulls()
        {
            var json = @"{ ""congestion_numeric"": [10, null, 90, null] }";

            var annotation = JsonConvert.DeserializeObject<Annotation>(json);

            Assert.IsNotNull(annotation.CongestionNumeric);
            Assert.AreEqual(4, annotation.CongestionNumeric.Length);
            Assert.AreEqual(10, annotation.CongestionNumeric[0]);
            Assert.IsNull(annotation.CongestionNumeric[1]);
            Assert.AreEqual(90, annotation.CongestionNumeric[2]);
            Assert.IsNull(annotation.CongestionNumeric[3]);
        }

        [Test]
        public void Deserialize_CongestionNumeric_DefaultsToNull_WhenAbsent()
        {
            var json = @"{ ""distance"": [1.0, 2.0] }";

            var annotation = JsonConvert.DeserializeObject<Annotation>(json);

            Assert.IsNull(annotation.CongestionNumeric);
        }

        [Test]
        public void Deserialize_LegWithAnnotation_ParsesCongestionNumericArray()
        {
            var json = @"{
                ""duration"": 100.0,
                ""distance"": 500.0,
                ""annotation"": {
                    ""congestion_numeric"": [5, null, 85]
                }
            }";

            var leg = JsonConvert.DeserializeObject<Leg>(json);

            Assert.IsNotNull(leg.Annotation);
            Assert.IsNotNull(leg.Annotation.CongestionNumeric);
            Assert.AreEqual(3, leg.Annotation.CongestionNumeric.Length);
            Assert.AreEqual(5, leg.Annotation.CongestionNumeric[0]);
            Assert.IsNull(leg.Annotation.CongestionNumeric[1]);
            Assert.AreEqual(85, leg.Annotation.CongestionNumeric[2]);
        }
    }
}
