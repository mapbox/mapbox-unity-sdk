//-----------------------------------------------------------------------
// <copyright file="LonLatToVector2dConverterTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.MapboxSdkCs.UnitTest
{

	using Mapbox.Json;
	using Mapbox.Utils;
	using Mapbox.Utils.JsonConverters;
	using NUnit.Framework;


	[TestFixture]
	internal class LonLatToVector2dConverterTest
	{

		// Mapbox API returns longitude,latitude
		private string _lonLatStr = "[-77.0295,38.9165]";

		// In Unity, x = latitude, y = longitude
		private Vector2d _latLonObject = new Vector2d(y: -77.0295, x: 38.9165);


		[Test]
		public void Deserialize()
		{
			Vector2d deserializedLonLat = JsonConvert.DeserializeObject<Vector2d>(_lonLatStr, JsonConverters.Converters);
			Assert.AreEqual(_latLonObject.ToString(), deserializedLonLat.ToString());
		}


		[Test]
		public void Serialize()
		{
			string serializedLonLat = JsonConvert.SerializeObject(_latLonObject, JsonConverters.Converters);
			Assert.AreEqual(_lonLatStr, serializedLonLat);
		}


	}
}
