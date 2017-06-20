//-----------------------------------------------------------------------
// <copyright file="ReverseGeocodeResourceTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.UnitTest
{
    using System;
    using Mapbox.Utils;
    using NUnit.Framework;

    [TestFixture]
    internal class ReverseGeocodeResourceTest
    {
        private const string Base = "https://api.mapbox.com/geocoding/v5/mapbox.places/";
        private Vector2d query = new Vector2d(10, 10);
        private string expectedQueryString = "10.00000,10.00000";
        private Geocoding.ReverseGeocodeResource rgr;

        [SetUp]
        public void SetUp()
        {
            this.rgr = new Geocoding.ReverseGeocodeResource(this.query);
        }

        public void BadType()
        {
            this.rgr.Types = new string[] { "fake" };
        }

        public void BadTypeWithGoodType()
        {
            this.rgr.Types = new string[] { "place", "fake" };
        }

        [Test]
        public void SetInvalidTypes()
        {
            Assert.Throws<Exception>(this.BadType);
            Assert.Throws<Exception>(this.BadTypeWithGoodType);
        }

        [Test]
        public void GetUrl()
        {
            // With only constructor
            Assert.AreEqual(Base + this.expectedQueryString + ".json", this.rgr.GetUrl());

            // With one types
            this.rgr.Types = new string[] { "country" };
            Assert.AreEqual(Base + this.expectedQueryString + ".json?types=country", this.rgr.GetUrl());

            // With multiple types
            this.rgr.Types = new string[] { "country", "region" };
            Assert.AreEqual(Base + this.expectedQueryString + ".json?types=country%2Cregion", this.rgr.GetUrl());

            // Set all to null
            this.rgr.Types = null;
            Assert.AreEqual(Base + this.expectedQueryString + ".json", this.rgr.GetUrl());
        }
    }
}