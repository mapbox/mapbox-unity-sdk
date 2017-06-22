//-----------------------------------------------------------------------
// <copyright file="DirectionResourceTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.MapboxSdkCs.UnitTest
{
	using System;
	using Mapbox.Utils;
	using NUnit.Framework;

	[TestFixture]
	internal class DirectionResourceTest
	{
		private Vector2d[] _coordinates = { new Vector2d(10, 10), new Vector2d(20, 20) };
		private Directions.RoutingProfile _profile = Directions.RoutingProfile.Driving;
		private Directions.DirectionResource _directionResource;

		[SetUp]
		public void SetUp()
		{
			_directionResource = new Directions.DirectionResource(_coordinates, _profile);
		}

		public void MismatchedBearings()
		{
			_directionResource.Bearings = new BearingFilter[] { new BearingFilter(10, 10) };
		}

		public void MismatchedRadiuses()
		{
			_directionResource.Radiuses = new double[] { 10 };
		}

		public void TooSmallRadius()
		{
			_directionResource.Radiuses = new double[] { 10, -1 };
		}

		[Test]
		public void SetInvalidBearings()
		{
			Assert.Throws<Exception>(MismatchedBearings);
		}

		[Test]
		public void SetInvalidRadiuses_Mismatched()
		{
			Assert.Throws<Exception>(MismatchedRadiuses);
		}

		[Test]
		public void SetInvalidRadiuses_TooSmall()
		{
			Assert.Throws<Exception>(TooSmallRadius);
		}

		[Test]
		public void GetUrl()
		{
			// With only constructor
			Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json", _directionResource.GetUrl());

			// With alternatives
			_directionResource.Alternatives = false;
			Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json?alternatives=false", _directionResource.GetUrl());

			// With bearings
			_directionResource.Bearings = new BearingFilter[] { new BearingFilter(90, 45), new BearingFilter(90, 30) };
			Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json?alternatives=false&bearings=90%2C45%3B90%2C30", _directionResource.GetUrl());

			// Bearings are nullable
			_directionResource.Bearings = new BearingFilter[] { new BearingFilter(90, 45), new BearingFilter(null, null) };
			Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json?alternatives=false&bearings=90%2C45%3B", _directionResource.GetUrl());

			// With continue straight
			_directionResource.ContinueStraight = false;
			Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json?alternatives=false&bearings=90%2C45%3B&continue_straight=false", _directionResource.GetUrl());

			// With overview
			_directionResource.Overview = Directions.Overview.Full;
			Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json?alternatives=false&bearings=90%2C45%3B&continue_straight=false&overview=full", _directionResource.GetUrl());

			// With steps
			_directionResource.Radiuses = new double[] { 30, 30 };
			Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json?alternatives=false&bearings=90%2C45%3B&continue_straight=false&overview=full&radiuses=30%2C30", _directionResource.GetUrl());

			// With steps
			_directionResource.Steps = false;
			Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json?alternatives=false&bearings=90%2C45%3B&continue_straight=false&overview=full&radiuses=30%2C30&steps=false", _directionResource.GetUrl());

			// Set all to null
			_directionResource.Alternatives = null;
			_directionResource.Bearings = null;
			_directionResource.ContinueStraight = null;
			_directionResource.Overview = null;
			_directionResource.Radiuses = null;
			_directionResource.Steps = null;
			Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json", _directionResource.GetUrl());
		}
	}
}