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
	using NUnit.Framework;

	/// <summary>
	/// Test that Directions serializes and deserializes responses correctly.
	/// </summary>
	[TestFixture]
	internal class DirectionsTest
	{
		//private string basicResponse = "{\"routes\":[{\"legs\":[{\"steps\":[],\"summary\":\"\",\"duration\":214.4,\"distance\":1318.2}],\"geometry\":\"_urwFt}qbMuLp_@jWzPoHhRMK\",\"duration\":214.4,\"distance\":1318.2}],\"waypoints\":[{\"name\":\"East 13th Street\",\"location\":[-73.988909,40.733122]},{\"name\":\"6th Avenue\",\"location\":[-74.00001,40.733004]}],\"code\":\"Ok\"}";
		//private string responseWithSteps = "{\"routes\":[{\"legs\":[{\"steps\":[{\"intersections\":[{\"out\":0,\"entry\":[true],\"bearings\":[299],\"location\":[-73.988909,40.733122]},{\"out\":3,\"location\":[-73.989868,40.733528],\"bearings\":[15,120,195,300],\"entry\":[true,false,false,true],\"in\":1},{\"out\":3,\"location\":[-73.990945,40.733978],\"bearings\":[15,120,195,300],\"entry\":[false,false,true,true],\"in\":1},{\"out\":3,\"location\":[-73.992266,40.734532],\"bearings\":[30,120,210,300],\"entry\":[true,false,false,true],\"in\":1}],\"geometry\":\"_urwFt}qbMqA~DyAvEmBfG{CpJ\",\"maneuver\":{\"bearing_after\":299,\"type\":\"depart\",\"modifier\":\"left\",\"bearing_before\":0,\"location\":[-73.988909,40.733122],\"instruction\":\"Head northwest on East 13th Street\"},\"duration\":90.5,\"distance\":502.1,\"name\":\"East 13th Street\",\"mode\":\"driving\"},{\"intersections\":[{\"out\":2,\"location\":[-73.994118,40.735313],\"bearings\":[30,120,210,300],\"entry\":[false,false,true,true],\"in\":1},{\"out\":2,\"location\":[-73.994585,40.734672],\"bearings\":[30,120,210,300],\"entry\":[false,true,true,false],\"in\":0},{\"out\":2,\"location\":[-73.99505,40.734034],\"bearings\":[30,120,210,300],\"entry\":[false,false,true,true],\"in\":0},{\"out\":2,\"location\":[-73.995489,40.733437],\"bearings\":[30,120,210,300],\"entry\":[false,true,true,false],\"in\":0},{\"out\":2,\"location\":[-73.995914,40.732847],\"bearings\":[30,120,210,300],\"entry\":[false,false,true,true],\"in\":0},{\"out\":2,\"location\":[-73.996351,40.732255],\"bearings\":[30,120,210,300],\"entry\":[false,true,true,false],\"in\":0}],\"geometry\":\"ubswFf~rbM~B|A~BzAtBvAtBrAtBvAh@Vd@`@lAx@JH\",\"maneuver\":{\"bearing_after\":209,\"type\":\"turn\",\"modifier\":\"left\",\"bearing_before\":299,\"location\":[-73.994118,40.735313],\"instruction\":\"Turn left onto 5th Avenue\"},\"duration\":67.8,\"distance\":496.3,\"name\":\"5th Avenue\",\"mode\":\"driving\"},{\"intersections\":[{\"out\":2,\"location\":[-73.996976,40.731414],\"bearings\":[30,120,300],\"entry\":[false,true,true],\"in\":0}],\"geometry\":\"ijrwFbpsbMKPoChHEH\",\"maneuver\":{\"bearing_after\":305,\"type\":\"end of road\",\"modifier\":\"right\",\"bearing_before\":212,\"location\":[-73.996976,40.731414],\"instruction\":\"Turn right onto Washington Square North\"},\"duration\":21,\"distance\":164.2,\"name\":\"Washington Square North\",\"mode\":\"driving\"},{\"intersections\":[{\"out\":3,\"location\":[-73.998612,40.732215],\"bearings\":[30,120,210,300],\"entry\":[false,false,true,true],\"in\":1}],\"geometry\":\"korwFhzsbMmCbH\",\"maneuver\":{\"bearing_after\":303,\"type\":\"new name\",\"modifier\":\"straight\",\"bearing_before\":303,\"location\":[-73.998612,40.732215],\"instruction\":\"Continue straight onto Waverly Place\"},\"duration\":34.5,\"distance\":146,\"name\":\"Waverly Place\",\"mode\":\"driving\"},{\"intersections\":[{\"out\":0,\"location\":[-74.000066,40.732929],\"bearings\":[30,120,210,300],\"entry\":[true,false,false,true],\"in\":1}],\"geometry\":\"ysrwFlctbMMK\",\"maneuver\":{\"bearing_after\":30,\"type\":\"turn\",\"modifier\":\"right\",\"bearing_before\":303,\"location\":[-74.000066,40.732929],\"instruction\":\"Turn right onto 6th Avenue\"},\"duration\":0.6,\"distance\":9.6,\"name\":\"6th Avenue\",\"mode\":\"driving\"},{\"intersections\":[{\"in\":0,\"entry\":[true],\"bearings\":[210],\"location\":[-74.00001,40.733004]}],\"geometry\":\"gtrwF`ctbM\",\"maneuver\":{\"bearing_after\":0,\"location\":[-74.000066,40.732929],\"bearing_before\":30,\"type\":\"arrive\",\"instruction\":\"You have arrived at your destination\"},\"duration\":0,\"distance\":0,\"name\":\"6th Avenue\",\"mode\":\"driving\"}],\"summary\":\"East 13th Street, 5th Avenue\",\"duration\":214.4,\"distance\":1318.2}],\"geometry\":\"_urwFt}qbMuLp_@jWzPoHhRMK\",\"duration\":214.4,\"distance\":1318.2}],\"waypoints\":[{\"name\":\"East 13th Street\",\"location\":[-73.988909,40.733122]},{\"name\":\"6th Avenue\",\"location\":[-74.00001,40.733004]}],\"code\":\"Ok\"}";
		//private Directions directions = new Directions(new FileSource());

		[Test]
		[Ignore("not working in Unity as 'Directions' is 'internal sealed' and mapbox-sdk-cs and tests end up in 2 different aseemblies")]
		public void SerializesAndDeserializesBasic()
		{

			// TODO: directions.Deserialize doesn't work as Editor test because it is marked as 'internal' and
			// Editor tests end up in 'Assembly-CSharp-Editor.dll' => not the same where Mapobx.Directions ends up
			/*
			// First, deserialize the example response
			DirectionsResponse basicResp = this.directions.Deserialize(this.basicResponse);

			// Then deserialize it back to a string.
			string basicReserialized = JsonConvert.SerializeObject(basicResp);

			// Ensure the two match
			Assert.AreEqual(this.basicResponse, basicReserialized);
			*/
		}

		//TODO: implement a proper Json object comaparer
		/// <summary> This test will fail, see https://github.com/mapbox/mapbox-sdk-unity/issues/51. </summary>
		[Test]
		[Ignore("That's not working as the order of JSON properties is not guaranteed. We need a proper object comparer.")]
		public void SerializesAndDeserializesWithSteps()
		{
			// TODO: directions.Deserialize doesn't work as Editor test because it is marked as 'internal' and
			// Editor tests end up in 'Assembly-CSharp-Editor.dll' => not the same where Mapobx.Directions ends up
			/*
			// First, deserialize the example response.
			DirectionsResponse withStepsResp = this.directions.Deserialize(this.responseWithSteps);

			// Then deserialize it back to a string.
			string withStepsReserialized = JsonConvert.SerializeObject(withStepsResp);

			// Ensure the two match.
			Assert.AreEqual(this.responseWithSteps, withStepsReserialized);
			*/
		}
	}
}
