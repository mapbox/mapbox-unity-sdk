//-----------------------------------------------------------------------
// <copyright file="ForwardGeocodeResourceTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.MapboxSdkCs.UnitTest
{

	using System;
	using Mapbox.Utils;
	using NUnit.Framework;


	[TestFixture]
	internal class ForwardGeocodeResourceTest
	{
		private const string _query = "Minneapolis, MN";
		private Geocoding.ForwardGeocodeResource _forwardGeocodeResource;

		[SetUp]
		public void SetUp()
		{
			_forwardGeocodeResource = new Geocoding.ForwardGeocodeResource(_query);
		}

		public void BadType()
		{
			_forwardGeocodeResource.Types = new string[] { "fake" };
		}

		public void BadTypeWithGoodType()
		{
			_forwardGeocodeResource.Types = new string[] { "place", "fake" };
		}

		public void BadCountry()
		{
			_forwardGeocodeResource.Types = new string[] { "zz" };
		}

		public void BadCountryWithGoodType()
		{
			_forwardGeocodeResource.Types = new string[] { "zz", "ar" };
		}

		[Test]
		public void SetInvalidTypes()
		{
			Assert.Throws<Exception>(BadType);
			Assert.Throws<Exception>(BadTypeWithGoodType);
		}

		[Test]
		public void SetInvalidCountries()
		{
			Assert.Throws<Exception>(BadCountry);
			Assert.Throws<Exception>(BadCountryWithGoodType);
		}

		[Test]
		public void GetUrl()
		{
			// With only constructor
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis%2C%20MN.json", _forwardGeocodeResource.GetUrl());

			// With autocomplete
			_forwardGeocodeResource.Autocomplete = false;
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis%2C%20MN.json?autocomplete=false", _forwardGeocodeResource.GetUrl());

			// With bbox
			_forwardGeocodeResource.Bbox = new Vector2dBounds(new Vector2d(15, 10), new Vector2d(25, 20));
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis%2C%20MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000", _forwardGeocodeResource.GetUrl());

			// With one country
			_forwardGeocodeResource.Country = new string[] { "ar" };
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis%2C%20MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000&country=ar", _forwardGeocodeResource.GetUrl());

			// With multiple countries
			_forwardGeocodeResource.Country = new string[] { "ar", "fi" };
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis%2C%20MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000&country=ar%2Cfi", _forwardGeocodeResource.GetUrl());

			// With proximity
			_forwardGeocodeResource.Proximity = new Vector2d(10, 5);
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis%2C%20MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000&country=ar%2Cfi&proximity=5.00000%2C10.00000", _forwardGeocodeResource.GetUrl());

			// With one types
			_forwardGeocodeResource.Types = new string[] { "country" };
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis%2C%20MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000&country=ar%2Cfi&proximity=5.00000%2C10.00000&types=country", _forwardGeocodeResource.GetUrl());

			// With multiple types
			_forwardGeocodeResource.Types = new string[] { "country", "region" };
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis%2C%20MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000&country=ar%2Cfi&proximity=5.00000%2C10.00000&types=country%2Cregion", _forwardGeocodeResource.GetUrl());

			// Set all to null
			_forwardGeocodeResource.Autocomplete = null;
			_forwardGeocodeResource.Bbox = null;
			_forwardGeocodeResource.Country = null;
			_forwardGeocodeResource.Proximity = null;
			_forwardGeocodeResource.Types = null;
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis%2C%20MN.json", _forwardGeocodeResource.GetUrl());
		}
	}
}