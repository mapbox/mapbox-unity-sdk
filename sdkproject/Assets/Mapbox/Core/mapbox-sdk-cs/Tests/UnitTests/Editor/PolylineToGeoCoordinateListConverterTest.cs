//-----------------------------------------------------------------------
// <copyright file="PolylineToVector2dListConverterTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.MapboxSdkCs.UnitTest
{

	using System.Collections.Generic;
	using Mapbox.Json;
	using Mapbox.Utils;
	using Mapbox.Utils.JsonConverters;
	using NUnit.Framework;


	[TestFixture]
	internal class PolylineToVector2dListConverterTest
	{

		// (38.5, -120.2), (40.7, -120.95), (43.252, -126.453)
		private readonly List<Vector2d> _polyLineObj = new List<Vector2d>()
		{
			new Vector2d(38.5, -120.2),
			new Vector2d(40.7, -120.95),
			new Vector2d(43.252, -126.453)
		};

		private string _polyLineString = "\"_p~iF~ps|U_ulLnnqC_mqNvxq`@\"";


		[Test]
		public void Deserialize()
		{
			List<Vector2d> deserializedLine = JsonConvert.DeserializeObject<List<Vector2d>>(_polyLineString, JsonConverters.Converters);
			Assert.AreEqual(_polyLineObj, deserializedLine);
		}


		[Test]
		public void Serialize()
		{
			string serializedLine = JsonConvert.SerializeObject(_polyLineObj, JsonConverters.Converters);
			Assert.AreEqual(_polyLineString, serializedLine);
		}


	}
}
