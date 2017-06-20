//-----------------------------------------------------------------------
// <copyright file="BearingFilterTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.UnitTest
{
    using System;
    using Mapbox;
    using NUnit.Framework;

    [TestFixture]
    internal class BearingFilterTest
    {
        private BearingFilter bf;

        [SetUp]
        public void SetUp()
        {
            this.bf = new BearingFilter(10, 10);
        }

        public void BearingTooLarge()
        {
            this.bf = new BearingFilter(361, 10);
        }

        public void BearingTooSmall()
        {
            this.bf = new BearingFilter(-1, 10);
        }

        public void RangeTooLarge()
        {
            this.bf = new BearingFilter(10, 181);
        }

        public void RangeTooSmall()
        {
            this.bf = new BearingFilter(10, -1);
        }

        [Test]
        public void InvalidValues()
        {
            Assert.Throws<Exception>(this.BearingTooLarge);
            Assert.Throws<Exception>(this.BearingTooSmall);
            Assert.Throws<Exception>(this.RangeTooSmall);
            Assert.Throws<Exception>(this.RangeTooLarge);
        }

        [Test]
        public void ToStringTest()
        {
            Assert.AreEqual(this.bf.ToString(), "10,10");

            this.bf = new BearingFilter(null, null);
            Assert.AreEqual(this.bf.ToString(), string.Empty);
        }
    }
}