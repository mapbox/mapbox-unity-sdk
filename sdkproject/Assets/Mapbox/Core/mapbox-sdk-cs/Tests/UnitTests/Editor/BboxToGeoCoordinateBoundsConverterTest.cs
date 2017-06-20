//-----------------------------------------------------------------------
// <copyright file="BboxToVector2dBoundsConverterTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.UnitTest
{
    using Mapbox.Json;
    using Mapbox.Utils;
    using Mapbox.Utils.JsonConverters;
    using NUnit.Framework;

    [TestFixture]
    internal class BboxToVector2dBoundsConverterTest
    {
        private string geoCoordinateBoundsStr = "[38.9165,-77.0295,30.2211,-80.5521]";

        private Vector2dBounds geoCoordinateBoundsObj = new Vector2dBounds(
            sw: new Vector2d(y: -77.0295, x: 38.9165),
            ne: new Vector2d(y: -80.5521, x: 30.2211));

        [Test]
        public void Deserialize()
        {
            Vector2dBounds deserializedVector2dBounds = JsonConvert.DeserializeObject<Vector2dBounds>(this.geoCoordinateBoundsStr, JsonConverters.Converters);
            Assert.AreEqual(this.geoCoordinateBoundsObj.ToString(), deserializedVector2dBounds.ToString());
        }

        [Test]
        public void Serialize()
        {
            string serializedVector2dBounds = JsonConvert.SerializeObject(this.geoCoordinateBoundsObj, JsonConverters.Converters);
            Assert.AreEqual(this.geoCoordinateBoundsStr, serializedVector2dBounds);
        }
    }
}