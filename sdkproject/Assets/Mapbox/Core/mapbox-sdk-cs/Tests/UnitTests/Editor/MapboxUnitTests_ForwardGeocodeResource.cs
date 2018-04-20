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
			// TODO: find proper fix for encoded url parameters crashing on some iPhones
#if UNITY_IOS
			UnityEngine.Debug.LogWarning("ForwardGeocodeResourceTest.GetUrl() TODO: find proper fix for encoded url parameters crashing on some iPhones");
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis++MN.json", _forwardGeocodeResource.GetUrl());
#else
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis%2C%20MN.json", _forwardGeocodeResource.GetUrl());
#endif


			// With autocomplete
			_forwardGeocodeResource.Autocomplete = false;
#if UNITY_IOS
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis++MN.json?autocomplete=false", _forwardGeocodeResource.GetUrl());
#else
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis%2C%20MN.json?autocomplete=false", _forwardGeocodeResource.GetUrl());
#endif


			// With bbox
			_forwardGeocodeResource.Bbox = new Vector2dBounds(new Vector2d(15, 10), new Vector2d(25, 20));
#if UNITY_IOS
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis++MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000".ToLower(), _forwardGeocodeResource.GetUrl().ToLower());
#else
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis++MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000", _forwardGeocodeResource.GetUrl());
#endif


			// With one country
			_forwardGeocodeResource.Country = new string[] { "ar" };
#if UNITY_IOS
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis++MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000&country=ar".ToLower(), _forwardGeocodeResource.GetUrl().ToLower());
#else
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis%2C%20MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000&country=ar", _forwardGeocodeResource.GetUrl());
#endif


			// With multiple countries
			_forwardGeocodeResource.Country = new string[] { "ar", "fi" };
#if UNITY_IOS
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis++MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000&country=ar%2Cfi".ToLower(), _forwardGeocodeResource.GetUrl().ToLower());
#else
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis%2C%20MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000&country=ar%2Cfi", _forwardGeocodeResource.GetUrl());
#endif


			// With proximity
			_forwardGeocodeResource.Proximity = new Vector2d(10, 5);
#if UNITY_IOS
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis++MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000&country=ar%2Cfi&proximity=5.00000%2C10.00000".ToLower(), _forwardGeocodeResource.GetUrl().ToLower());
#else
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis%2C%20MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000&country=ar%2Cfi&proximity=5.00000%2C10.00000", _forwardGeocodeResource.GetUrl());
#endif


			// With one types
			_forwardGeocodeResource.Types = new string[] { "country" };
#if UNITY_IOS
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis++MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000&country=ar%2Cfi&proximity=5.00000%2C10.00000&types=country".ToLower(), _forwardGeocodeResource.GetUrl().ToLower());
#else
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis%2C%20MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000&country=ar%2Cfi&proximity=5.00000%2C10.00000&types=country", _forwardGeocodeResource.GetUrl());
#endif


			// With multiple types
			_forwardGeocodeResource.Types = new string[] { "country", "region" };
#if UNITY_IOS
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis++MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000&country=ar%2Cfi&proximity=5.00000%2C10.00000&types=country%2Cregion".ToLower(), _forwardGeocodeResource.GetUrl().ToLower());
#else
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis%2C%20MN.json?autocomplete=false&bbox=10.00000%2C15.00000%2C20.00000%2C25.00000&country=ar%2Cfi&proximity=5.00000%2C10.00000&types=country%2Cregion", _forwardGeocodeResource.GetUrl());
#endif

			// Set all to null
			_forwardGeocodeResource.Autocomplete = null;
			_forwardGeocodeResource.Bbox = null;
			_forwardGeocodeResource.Country = null;
			_forwardGeocodeResource.Proximity = null;
			_forwardGeocodeResource.Types = null;

#if UNITY_IOS
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis++MN.json", _forwardGeocodeResource.GetUrl());
#else
			Assert.AreEqual("https://api.mapbox.com/geocoding/v5/mapbox.places/Minneapolis%2C%20MN.json", _forwardGeocodeResource.GetUrl());
#endif
		}
	}
}
