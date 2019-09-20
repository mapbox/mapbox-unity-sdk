//-----------------------------------------------------------------------
// <copyright file="DirectionsTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.MapboxSdkCs.UnitTest
{

	using Mapbox.Directions;
	using Mapbox.Json;
	using Mapbox.Platform;
	using NUnit;
	using NUnit.Framework;
	using UnityEngine;


	/// <summary>
	/// Test that Directions serializes and deserializes responses correctly.
	/// </summary>
	[TestFixture]
	internal class DirectionsTest
	{

		private string _basicResponse = "{\"routes\":[{\"legs\":[{\"steps\":[],\"summary\":\"\",\"duration\":214.4,\"distance\":1318.2,\"annotation\":null}],\"geometry\":\"_urwFt}qbMuLp_@jWzPoHhRMK\",\"duration\":214.4,\"distance\":1318.2,\"weight\":0.0,\"weight_name\":null}],\"waypoints\":[{\"name\":\"East 13th Street\",\"location\":[-73.988909,40.733122]},{\"name\":\"6th Avenue\",\"location\":[-74.00001,40.733004]}],\"code\":\"Ok\"}";
		private string _responseWithSteps = "{\"routes\":[{\"legs\":[{\"steps\":[{\"intersections\":[{\"out\":0,\"entry\":[true],\"bearings\":[299],\"location\":[-73.988909,40.733122]},{\"out\":3,\"entry\":[true,false,false,true],\"bearings\":[15,120,195,300],\"location\":[-73.989868,40.733528],\"in\":1},{\"out\":3,\"entry\":[false,false,true,true],\"bearings\":[15,120,195,300],\"location\":[-73.990945,40.733978],\"in\":1},{\"out\":3,\"entry\":[true,false,false,true],\"bearings\":[30,120,210,300],\"location\":[-73.992266,40.734532],\"in\":1}],\"geometry\":\"_urwFt}qbMqA~DyAvEmBfG{CpJ\",\"maneuver\":{\"bearing_after\":299,\"type\":\"depart\",\"modifier\":\"left\",\"bearing_before\":0,\"Location\":[40.733122,-73.988909],\"instruction\":\"Head northwest on East 13th Street\"},\"duration\":90.5,\"distance\":502.1,\"name\":\"East 13th Street\",\"mode\":\"driving\"},{\"intersections\":[{\"out\":2,\"entry\":[false,false,true,true],\"bearings\":[30,120,210,300],\"location\":[-73.994118,40.735313],\"in\":1},{\"out\":2,\"entry\":[false,true,true,false],\"bearings\":[30,120,210,300],\"location\":[-73.994585,40.734672],\"in\":0},{\"out\":2,\"entry\":[false,false,true,true],\"bearings\":[30,120,210,300],\"location\":[-73.99505,40.734034],\"in\":0},{\"out\":2,\"entry\":[false,true,true,false],\"bearings\":[30,120,210,300],\"location\":[-73.995489,40.733437],\"in\":0},{\"out\":2,\"entry\":[false,false,true,true],\"bearings\":[30,120,210,300],\"location\":[-73.995914,40.732847],\"in\":0},{\"out\":2,\"entry\":[false,true,true,false],\"bearings\":[30,120,210,300],\"location\":[-73.996351,40.732255],\"in\":0}],\"geometry\":\"ubswFf~rbM~B|A~BzAtBvAtBrAtBvAh@Vd@`@lAx@JH\",\"maneuver\":{\"bearing_after\":209,\"type\":\"turn\",\"modifier\":\"left\",\"bearing_before\":299,\"Location\":[40.735313,-73.994118],\"instruction\":\"Turn left onto 5th Avenue\"},\"duration\":67.8,\"distance\":496.3,\"name\":\"5th Avenue\",\"mode\":\"driving\"},{\"intersections\":[{\"out\":2,\"entry\":[false,true,true],\"bearings\":[30,120,300],\"location\":[-73.996976,40.731414],\"in\":0}],\"geometry\":\"ijrwFbpsbMKPoChHEH\",\"maneuver\":{\"bearing_after\":305,\"type\":\"end of road\",\"modifier\":\"right\",\"bearing_before\":212,\"Location\":[40.731414,-73.996976],\"instruction\":\"Turn right onto Washington Square North\"},\"duration\":21.0,\"distance\":164.2,\"name\":\"Washington Square North\",\"mode\":\"driving\"},{\"intersections\":[{\"out\":3,\"entry\":[false,false,true,true],\"bearings\":[30,120,210,300],\"location\":[-73.998612,40.732215],\"in\":1}],\"geometry\":\"korwFhzsbMmCbH\",\"maneuver\":{\"bearing_after\":303,\"type\":\"new name\",\"modifier\":\"straight\",\"bearing_before\":303,\"Location\":[40.732215,-73.998612],\"instruction\":\"Continue straight onto Waverly Place\"},\"duration\":34.5,\"distance\":146.0,\"name\":\"Waverly Place\",\"mode\":\"driving\"},{\"intersections\":[{\"out\":0,\"entry\":[true,false,false,true],\"bearings\":[30,120,210,300],\"location\":[-74.000066,40.732929],\"in\":1}],\"geometry\":\"ysrwFlctbMMK\",\"maneuver\":{\"bearing_after\":30,\"type\":\"turn\",\"modifier\":\"right\",\"bearing_before\":303,\"Location\":[40.732929,-74.000066],\"instruction\":\"Turn right onto 6th Avenue\"},\"duration\":0.6,\"distance\":9.6,\"name\":\"6th Avenue\",\"mode\":\"driving\"},{\"intersections\":[{\"out\":0,\"entry\":[true],\"bearings\":[210],\"location\":[-74.00001,40.733004],\"in\":0}],\"geometry\":\"gtrwF`ctbM\",\"maneuver\":{\"bearing_after\":0,\"type\":\"arrive\",\"modifier\":null,\"bearing_before\":30,\"Location\":[40.732929,-74.000066],\"instruction\":\"You have arrived at your destination\"},\"duration\":0.0,\"distance\":0.0,\"name\":\"6th Avenue\",\"mode\":\"driving\"}],\"summary\":\"East 13th Street, 5th Avenue\",\"duration\":214.4,\"distance\":1318.2,\"annotation\":null}],\"geometry\":\"_urwFt}qbMuLp_@jWzPoHhRMK\",\"duration\":214.4,\"distance\":1318.2,\"weight\":0.0,\"weight_name\":null}],\"waypoints\":[{\"name\":\"East 13th Street\",\"location\":[-73.988909,40.733122]},{\"name\":\"6th Avenue\",\"location\":[-74.00001,40.733004]}],\"code\":\"Ok\"}";
		private Directions _directions = new Directions(new FileSource(Unity.MapboxAccess.Instance.Configuration.GetMapsSkuToken, Unity.MapboxAccess.Instance.Configuration.AccessToken));



		[Test]
		public void SerializesAndDeserializesBasic()
		{
			// First, deserialize the example response
			DirectionsResponse basicResp = _directions.Deserialize(_basicResponse);

			// Then deserialize it back to a string.
			string basicReserialized = _directions.Serialize(basicResp);

			// Ensure the two match
			Assert.AreEqual(_basicResponse, basicReserialized);
		}


		//TODO: implement a proper Json object comaparer
		/// <summary> This test will fail, see https://github.com/mapbox/mapbox-sdk-unity/issues/51. </summary>
		[Test]
		public void SerializesAndDeserializesWithSteps()
		{
			// First, deserialize the example response.
			DirectionsResponse withStepsResp = _directions.Deserialize(_responseWithSteps);

			// Then deserialize it back to a string.
			string withStepsReserialized = _directions.Serialize(withStepsResp);

			// Ensure the two match.
			Assert.AreEqual(_responseWithSteps, withStepsReserialized);
		}



	}
}
