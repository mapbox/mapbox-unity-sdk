//-----------------------------------------------------------------------
// <copyright file="LonLatToVector2dConverterTest.cs" company="Mapbox">
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
    internal class LonLatToVector2dConverterTest
    {
		// Mapbox API returns longitude,latitude
        private string lonLatStr = "[-77.0295,38.9165]";

		// In Unity, x = latitude, y = longitude
		private Vector2d latLonObject = new Vector2d(y: -77.0295, x: 38.9165);
		
        [Test]
        public void Deserialize()
        {
            Vector2d deserializedLonLat = JsonConvert.DeserializeObject<Vector2d>(this.lonLatStr, JsonConverters.Converters);
            Assert.AreEqual(this.latLonObject.ToString(), deserializedLonLat.ToString());
        }

        [Test]
        public void Serialize()
        {
            string serializedLonLat = JsonConvert.SerializeObject(this.latLonObject, JsonConverters.Converters);
            Assert.AreEqual(this.lonLatStr, serializedLonLat);
        }
    }
}