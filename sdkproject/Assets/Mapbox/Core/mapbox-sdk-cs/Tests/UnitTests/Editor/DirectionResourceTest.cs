//-----------------------------------------------------------------------
// <copyright file="DirectionResourceTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.UnitTest
{
    using System;
    using Mapbox.Utils;
    using NUnit.Framework;

    [TestFixture]
    internal class DirectionResourceTest
    {
        private Vector2d[] coordinates = { new Vector2d(10, 10), new Vector2d(20, 20) };
        private Directions.RoutingProfile profile = Directions.RoutingProfile.Driving;
        private Directions.DirectionResource dr;

        [SetUp]
        public void SetUp()
        {
            this.dr = new Directions.DirectionResource(this.coordinates, this.profile);
        }

        public void MismatchedBearings()
        {
            this.dr.Bearings = new BearingFilter[] { new BearingFilter(10, 10) };
        }

        public void MismatchedRadiuses()
        {
            this.dr.Radiuses = new double[] { 10 };
        }

        public void TooSmallRadius()
        {
            this.dr.Radiuses = new double[] { 10, -1 };
        }

        [Test]
        public void SetInvalidBearings()
        {
            Assert.Throws<Exception>(this.MismatchedBearings);
        }

        [Test]
        public void SetInvalidRadiuses_Mismatched()
        {
            Assert.Throws<Exception>(this.MismatchedRadiuses);
        }

        [Test]
        public void SetInvalidRadiuses_TooSmall()
        {
            Assert.Throws<Exception>(this.TooSmallRadius);
        }

        [Test]
        public void GetUrl()
        {
            // With only constructor
            Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json", this.dr.GetUrl());

            // With alternatives
            this.dr.Alternatives = false;
            Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json?alternatives=false", this.dr.GetUrl());

            // With bearings
            this.dr.Bearings = new BearingFilter[] { new BearingFilter(90, 45), new BearingFilter(90, 30) };
            Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json?alternatives=false&bearings=90%2C45%3B90%2C30", this.dr.GetUrl());

            // Bearings are nullable
            this.dr.Bearings = new BearingFilter[] { new BearingFilter(90, 45), new BearingFilter(null, null) };
            Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json?alternatives=false&bearings=90%2C45%3B", this.dr.GetUrl());

            // With continue straight
            this.dr.ContinueStraight = false;
            Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json?alternatives=false&bearings=90%2C45%3B&continue_straight=false", this.dr.GetUrl());

            // With overview
            this.dr.Overview = Directions.Overview.Full;
            Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json?alternatives=false&bearings=90%2C45%3B&continue_straight=false&overview=full", this.dr.GetUrl());

            // With steps
            this.dr.Radiuses = new double[] { 30, 30 };
            Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json?alternatives=false&bearings=90%2C45%3B&continue_straight=false&overview=full&radiuses=30%2C30", this.dr.GetUrl());

            // With steps
            this.dr.Steps = false;
            Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json?alternatives=false&bearings=90%2C45%3B&continue_straight=false&overview=full&radiuses=30%2C30&steps=false", this.dr.GetUrl());

            // Set all to null
            this.dr.Alternatives = null;
            this.dr.Bearings = null;
            this.dr.ContinueStraight = null;
            this.dr.Overview = null;
            this.dr.Radiuses = null;
            this.dr.Steps = null;
            Assert.AreEqual("https://api.mapbox.com/directions/v5/mapbox/driving/10.00000,10.00000;20.00000,20.00000.json", this.dr.GetUrl());
        }
    }
}