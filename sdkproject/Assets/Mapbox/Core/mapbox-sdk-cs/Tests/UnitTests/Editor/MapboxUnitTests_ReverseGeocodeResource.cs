//-----------------------------------------------------------------------
// <copyright file="ReverseGeocodeResourceTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.MapboxSdkCs.UnitTest
{

	using System;
	using Mapbox.Utils;
	using NUnit.Framework;


	[TestFixture]
	internal class ReverseGeocodeResourceTest
	{

		private const string _baseUrl = "https://api.mapbox.com/geocoding/v5/mapbox.places/";
		private Vector2d _queryLocation = new Vector2d(10, 10);
		private string _expectedQueryString = "10.00000,10.00000";
		private Geocoding.ReverseGeocodeResource _reverseGeocodeResource;


		[SetUp]
		public void SetUp()
		{
			_reverseGeocodeResource = new Geocoding.ReverseGeocodeResource(_queryLocation);
		}

		public void BadType()
		{
			_reverseGeocodeResource.Types = new string[] { "fake" };
		}

		public void BadTypeWithGoodType()
		{
			_reverseGeocodeResource.Types = new string[] { "place", "fake" };
		}

		[Test]
		public void SetInvalidTypes()
		{
			Assert.Throws<Exception>(BadType);
			Assert.Throws<Exception>(BadTypeWithGoodType);
		}

		[Test]
		public void GetUrl()
		{
			// With only constructor
			Assert.AreEqual(_baseUrl + _expectedQueryString + ".json", _reverseGeocodeResource.GetUrl());

			// With one types
			_reverseGeocodeResource.Types = new string[] { "country" };
			Assert.AreEqual(_baseUrl + _expectedQueryString + ".json?types=country", _reverseGeocodeResource.GetUrl());

			// With multiple types
			_reverseGeocodeResource.Types = new string[] { "country", "region" };
			Assert.AreEqual(_baseUrl + _expectedQueryString + ".json?types=country%2Cregion", _reverseGeocodeResource.GetUrl());

			// Set all to null
			_reverseGeocodeResource.Types = null;
			Assert.AreEqual(_baseUrl + _expectedQueryString + ".json", _reverseGeocodeResource.GetUrl());
		}


	}
}
