//-----------------------------------------------------------------------
// <copyright file="GeocoderTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.MapboxSdkCs.UnitTest
{

	using Geocoding;
	using Mapbox.Json;
	using Mapbox.Platform;
	using Mapbox.Unity;
	using Mapbox.Utils.JsonConverters;
	using NUnit.Framework;

	/// <summary>
	/// Test that Geocoder serializes and deserializes responses correctly.
	/// </summary>
	[TestFixture]
	internal class GeocoderTest
	{
		private readonly Geocoder _geocoder = new Geocoder(new FileSource(Unity.MapboxAccess.Instance.Configuration.GetMapsSkuToken, Unity.MapboxAccess.Instance.Configuration.AccessToken)); //MapboxAccess.Instance.Geocoder;
		private string _forwardResponse = "{\"type\":\"FeatureCollection\",\"query\":[\"minneapolis\"],\"features\":[{\"id\":\"place.12871500125885940\",\"type\":\"Feature\",\"text\":\"Minneapolis\",\"place_name\":\"Minneapolis, Minnesota, United States\",\"relevance\":0.99,\"properties\":{\"wikidata\":\"Q36091\"},\"bbox\":[-93.5226520099878,44.7853029900244,-93.1424209928836,45.2129100099882],\"center\":[-93.2655,44.9773],\"geometry\":{\"type\":\"Point\",\"coordinates\":[-93.2655,44.9773]},\"context\":[{\"id\":\"postcode.11389548391063390\",\"text\":\"55415\"},{\"id\":\"region.12225983719702200\",\"text\":\"Minnesota\",\"short_code\":\"US-MN\",\"wikidata\":\"Q1527\"},{\"id\":\"country.12862386939497690\",\"text\":\"United States\",\"short_code\":\"us\",\"wikidata\":\"Q30\"}]},{\"id\":\"poi.15555644443768740\",\"type\":\"Feature\",\"text\":\"Minneapolis City Hall\",\"place_name\":\"Minneapolis City Hall, Minneapolis, Minnesota 55415, United States\",\"relevance\":0.99,\"properties\":{\"wikidata\":\"Q1384874\",\"landmark\":true,\"tel\":null,\"address\":null,\"category\":\"other\"},\"center\":[-93.265277777778,44.977222222222],\"geometry\":{\"type\":\"Point\",\"coordinates\":[-93.265277777778,44.977222222222]},\"context\":[{\"id\":\"neighborhood.13081559486410050\",\"text\":\"Greater Central\"},{\"id\":\"place.12871500125885940\",\"text\":\"Minneapolis\",\"wikidata\":\"Q36091\"},{\"id\":\"postcode.11389548391063390\",\"text\":\"55415\"},{\"id\":\"region.12225983719702200\",\"text\":\"Minnesota\",\"short_code\":\"US-MN\",\"wikidata\":\"Q1527\"},{\"id\":\"country.12862386939497690\",\"text\":\"United States\",\"short_code\":\"us\",\"wikidata\":\"Q30\"}]},{\"id\":\"poi.6527299549845510\",\"type\":\"Feature\",\"text\":\"Minneapolis Grain Exchange\",\"place_name\":\"Minneapolis Grain Exchange, Minneapolis, Minnesota 55415, United States\",\"relevance\":0.99,\"properties\":{\"wikidata\":\"Q1540984\",\"landmark\":true,\"tel\":null,\"address\":null,\"category\":\"other\"},\"center\":[-93.2636,44.9775],\"geometry\":{\"type\":\"Point\",\"coordinates\":[-93.2636,44.9775]},\"context\":[{\"id\":\"neighborhood.13081559486410050\",\"text\":\"Greater Central\"},{\"id\":\"place.12871500125885940\",\"text\":\"Minneapolis\",\"wikidata\":\"Q36091\"},{\"id\":\"postcode.11389548391063390\",\"text\":\"55415\"},{\"id\":\"region.12225983719702200\",\"text\":\"Minnesota\",\"short_code\":\"US-MN\",\"wikidata\":\"Q1527\"},{\"id\":\"country.12862386939497690\",\"text\":\"United States\",\"short_code\":\"us\",\"wikidata\":\"Q30\"}]},{\"id\":\"poi.12655750184890630\",\"type\":\"Feature\",\"text\":\"Minneapolis Armory\",\"place_name\":\"Minneapolis Armory, Minneapolis, Minnesota 55415, United States\",\"relevance\":0.99,\"properties\":{\"wikidata\":\"Q745327\",\"landmark\":true,\"tel\":null,\"address\":null,\"category\":\"other\"},\"center\":[-93.263278,44.975092],\"geometry\":{\"type\":\"Point\",\"coordinates\":[-93.263278,44.975092]},\"context\":[{\"id\":\"neighborhood.13081559486410050\",\"text\":\"Greater Central\"},{\"id\":\"place.12871500125885940\",\"text\":\"Minneapolis\",\"wikidata\":\"Q36091\"},{\"id\":\"postcode.11389548391063390\",\"text\":\"55415\"},{\"id\":\"region.12225983719702200\",\"text\":\"Minnesota\",\"short_code\":\"US-MN\",\"wikidata\":\"Q1527\"},{\"id\":\"country.12862386939497690\",\"text\":\"United States\",\"short_code\":\"us\",\"wikidata\":\"Q30\"}]},{\"id\":\"poi.4855757554573390\",\"type\":\"Feature\",\"text\":\"Minneapolis Chain of Lakes Park\",\"place_name\":\"Minneapolis Chain of Lakes Park, Minneapolis, Minnesota 55405, United States\",\"relevance\":0.99,\"properties\":{\"wikidata\":null,\"landmark\":true,\"tel\":null,\"address\":null,\"category\":\"park\",\"maki\":\"picnic-site\"},\"bbox\":[-93.330260720104,44.9504758437682,-93.3013567328453,44.969400319872],\"center\":[-93.310259,44.959942],\"geometry\":{\"type\":\"Point\",\"coordinates\":[-93.310259,44.959942]},\"context\":[{\"id\":\"neighborhood.12530456224376080\",\"text\":\"Kenwood\"},{\"id\":\"place.12871500125885940\",\"text\":\"Minneapolis\",\"wikidata\":\"Q36091\"},{\"id\":\"postcode.10829535691218220\",\"text\":\"55405\"},{\"id\":\"region.12225983719702200\",\"text\":\"Minnesota\",\"short_code\":\"US-MN\",\"wikidata\":\"Q1527\"},{\"id\":\"country.12862386939497690\",\"text\":\"United States\",\"short_code\":\"us\",\"wikidata\":\"Q30\"}]}],\"attribution\":\"NOTICE: \u00A9 2016 Mapbox and its suppliers. All rights reserved. Use of this data is subject to the Mapbox Terms of Service (https://www.mapbox.com/about/maps/). This response and the information it contains may not be retained.\"}";
		private string _reverseResponse = "{\"type\":\"FeatureCollection\",\"query\":[-77.0268808,38.925326999999996],\"features\":[{\"id\":\"address.5375777428110760\",\"type\":\"Feature\",\"text\":\"11th St NW\",\"place_name\":\"2717 11th St NW, Washington, District of Columbia 20001, United States\",\"relevance\":1.0,\"properties\":{},\"center\":[-77.026824,38.925306],\"geometry\":{\"type\":\"Point\",\"coordinates\":[-77.026824,38.925306]},\"address\":\"2717\",\"context\":[{\"id\":\"neighborhood.11736072639395000\",\"text\":\"Pleasant Plains\"},{\"id\":\"place.12334081418246050\",\"text\":\"Washington\",\"wikidata\":\"Q61\"},{\"id\":\"postcode.3526019892841050\",\"text\":\"20001\"},{\"id\":\"region.6884744206035790\",\"text\":\"District of Columbia\",\"short_code\":\"US-DC\",\"wikidata\":\"Q61\"},{\"id\":\"country.12862386939497690\",\"text\":\"United States\",\"wikidata\":\"Q30\",\"short_code\":\"us\"}]},{\"id\":\"neighborhood.11736072639395000\",\"type\":\"Feature\",\"text\":\"Pleasant Plains\",\"place_name\":\"Pleasant Plains, Washington, 20001, District of Columbia, United States\",\"relevance\":1.0,\"properties\":{},\"bbox\":[-77.0367101373528,38.9177500315001,-77.0251464843832,38.9273657639],\"center\":[-77.0303,38.9239],\"geometry\":{\"type\":\"Point\",\"coordinates\":[-77.0303,38.9239]},\"context\":[{\"id\":\"place.12334081418246050\",\"text\":\"Washington\",\"wikidata\":\"Q61\"},{\"id\":\"postcode.3526019892841050\",\"text\":\"20001\"},{\"id\":\"region.6884744206035790\",\"text\":\"District of Columbia\",\"short_code\":\"US-DC\",\"wikidata\":\"Q61\"},{\"id\":\"country.12862386939497690\",\"text\":\"United States\",\"wikidata\":\"Q30\",\"short_code\":\"us\"}]},{\"id\":\"place.12334081418246050\",\"type\":\"Feature\",\"text\":\"Washington\",\"place_name\":\"Washington, District of Columbia, United States\",\"relevance\":1.0,\"properties\":{\"wikidata\":\"Q61\"},\"bbox\":[-77.1197590084041,38.8031129900659,-76.90939299,38.9955480080759],\"center\":[-77.0366,38.895],\"geometry\":{\"type\":\"Point\",\"coordinates\":[-77.0366,38.895]},\"context\":[{\"id\":\"postcode.3526019892841050\",\"text\":\"20001\"},{\"id\":\"region.6884744206035790\",\"text\":\"District of Columbia\",\"short_code\":\"US-DC\",\"wikidata\":\"Q61\"},{\"id\":\"country.12862386939497690\",\"text\":\"United States\",\"wikidata\":\"Q30\",\"short_code\":\"us\"}]},{\"id\":\"postcode.3526019892841050\",\"type\":\"Feature\",\"text\":\"20001\",\"place_name\":\"20001, District of Columbia, United States\",\"relevance\":1.0,\"properties\":{},\"bbox\":[-77.028082,38.890834,-77.007177,38.929058],\"center\":[-77.018017,38.909197],\"geometry\":{\"type\":\"Point\",\"coordinates\":[-77.018017,38.909197]},\"context\":[{\"id\":\"region.6884744206035790\",\"text\":\"District of Columbia\",\"short_code\":\"US-DC\",\"wikidata\":\"Q61\"},{\"id\":\"country.12862386939497690\",\"text\":\"United States\",\"wikidata\":\"Q30\",\"short_code\":\"us\"}]},{\"id\":\"region.6884744206035790\",\"type\":\"Feature\",\"text\":\"District of Columbia\",\"place_name\":\"District of Columbia, United States\",\"relevance\":1.0,\"properties\":{\"short_code\":\"US-DC\",\"wikidata\":\"Q61\"},\"bbox\":[-77.2081379659453,38.7177026348658,-76.909393,38.995548],\"center\":[-76.990661,38.89657],\"geometry\":{\"type\":\"Point\",\"coordinates\":[-76.990661,38.89657]},\"context\":[{\"id\":\"country.12862386939497690\",\"text\":\"United States\",\"wikidata\":\"Q30\",\"short_code\":\"us\"}]},{\"id\":\"country.12862386939497690\",\"type\":\"Feature\",\"text\":\"United States\",\"place_name\":\"United States\",\"relevance\":1.0,\"properties\":{\"wikidata\":\"Q30\",\"short_code\":\"us\"},\"bbox\":[-179.330950579,18.765563302,179.959578044,71.540723637],\"center\":[-97.922211,39.381266],\"geometry\":{\"type\":\"Point\",\"coordinates\":[-97.922211,39.381266]}}],\"attribution\":\"NOTICE: © 2016 Mapbox and its suppliers. All rights reserved. Use of this data is subject to the Mapbox Terms of Service (https://www.mapbox.com/about/maps/). This response and the information it contains may not be retained.\"}";

		[Test]
		public void SerializesAndDeserializesReverse()
		{
			// First, deserialize the example response
			ReverseGeocodeResponse reverseResp = _geocoder.Deserialize<ReverseGeocodeResponse>(_reverseResponse);

			// Then deserialize it back to a string.
			string reverseReserialized = JsonConvert.SerializeObject(reverseResp, JsonConverters.Converters);

			// Ensure the two match
			Assert.AreEqual(_reverseResponse, reverseReserialized);
		}


		[Test]
		public void SerializesAndDeserializesForward()
		{
			// First, deserialize the example response
			ForwardGeocodeResponse forwardResp = _geocoder.Deserialize<ForwardGeocodeResponse>(_forwardResponse);

			// Then deserialize it back to a string.
			string forwardReserialized = JsonConvert.SerializeObject(forwardResp, JsonConverters.Converters);

			// Ensure the two match
			Assert.AreEqual(_forwardResponse, forwardReserialized);
		}
	}
}
