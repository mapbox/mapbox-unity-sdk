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

        [Test]
        public void Deserialize_ParsesMaxSpeed_SpeedUnknownAndNoneElements()
        {
            var json = @"{ ""maxspeed"": [
                { ""speed"": 50, ""unit"": ""km/h"" },
                { ""unknown"": true },
                { ""none"": true }
            ] }";

            var annotation = JsonConvert.DeserializeObject<Annotation>(json);

            Assert.IsNotNull(annotation.MaxSpeed);
            Assert.AreEqual(3, annotation.MaxSpeed.Length);

            Assert.AreEqual(50.0, annotation.MaxSpeed[0].Speed);
            Assert.AreEqual("km/h", annotation.MaxSpeed[0].Unit);
            Assert.IsFalse(annotation.MaxSpeed[0].Unknown);
            Assert.IsFalse(annotation.MaxSpeed[0].None);

            Assert.IsNull(annotation.MaxSpeed[1].Speed);
            Assert.IsNull(annotation.MaxSpeed[1].Unit);
            Assert.IsTrue(annotation.MaxSpeed[1].Unknown);
            Assert.IsFalse(annotation.MaxSpeed[1].None);

            Assert.IsNull(annotation.MaxSpeed[2].Speed);
            Assert.IsNull(annotation.MaxSpeed[2].Unit);
            Assert.IsFalse(annotation.MaxSpeed[2].Unknown);
            Assert.IsTrue(annotation.MaxSpeed[2].None);
        }

        [Test]
        public void Deserialize_MaxSpeed_DefaultsToNull_WhenAbsent()
        {
            var json = @"{ ""congestion_numeric"": [0, 45] }";

            var annotation = JsonConvert.DeserializeObject<Annotation>(json);

            Assert.IsNull(annotation.MaxSpeed);
        }
    }
}
