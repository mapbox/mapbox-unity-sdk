//-----------------------------------------------------------------------
// <copyright file="Vector2dBoundsTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.UnitTest
{
    using Mapbox.Utils;
    using NUnit.Framework;

    [TestFixture]
    internal class Vector2dBoundsTest
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void SmallBounds()
        {
            var a = new Vector2d(0, 0);
            var b = new Vector2d(10, 10);
            var bounds = new Vector2dBounds(a, b);
            Assert.AreEqual("0.00000,0.00000,10.00000,10.00000", bounds.ToString());
        }

        [Test]
        public void Extend()
        {
            var bounds1 = new Vector2dBounds(new Vector2d(-10, -10), new Vector2d(10, 10));
            var bounds2 = new Vector2dBounds(new Vector2d(-20, -20), new Vector2d(20, 20));

            bounds1.Extend(bounds2);

            Assert.AreEqual(bounds1.South, bounds2.South);
            Assert.AreEqual(bounds1.West, bounds2.West);
            Assert.AreEqual(bounds1.North, bounds2.North);
            Assert.AreEqual(bounds1.East, bounds2.East);
        }

        [Test]
        public void Hull()
        {
            var bounds1 = new Vector2dBounds(new Vector2d(-10, -10), new Vector2d(10, 10));
            var bounds2 = Vector2dBounds.FromCoordinates(new Vector2d(10, 10), new Vector2d(-10, -10));

            Assert.AreEqual(bounds1.South, bounds2.South);
            Assert.AreEqual(bounds1.West, bounds2.West);
            Assert.AreEqual(bounds1.North, bounds2.North);
            Assert.AreEqual(bounds1.East, bounds2.East);
        }

        [Test]
        public void World()
        {
            var bounds = Vector2dBounds.World();

            Assert.AreEqual(bounds.South, -90);
            Assert.AreEqual(bounds.West, -180);
            Assert.AreEqual(bounds.North, 90);
            Assert.AreEqual(bounds.East, 180);
        }

        [Test]
        public void CardinalLimits()
        {
            var bounds = new Vector2dBounds(new Vector2d(10, 20), new Vector2d(30, 40));

            // SouthWest, first parameter.
            Assert.AreEqual(bounds.South, 10);
            Assert.AreEqual(bounds.West, 20);

            // NorthEast, second parameter.
            Assert.AreEqual(bounds.North, 30);
            Assert.AreEqual(bounds.East, 40);
        }

        [Test]
        public void IsEmpty()
        {
            var bounds1 = new Vector2dBounds(new Vector2d(10, 10), new Vector2d(0, 0));
            Assert.IsTrue(bounds1.IsEmpty());

            var bounds2 = new Vector2dBounds(new Vector2d(0, 0), new Vector2d(0, 0));
            Assert.IsFalse(bounds2.IsEmpty());

            var bounds3 = new Vector2dBounds(new Vector2d(0, 0), new Vector2d(10, 10));
            Assert.IsFalse(bounds3.IsEmpty());
        }

        [Test]
        public void Center()
        {
            var bounds1 = new Vector2dBounds(new Vector2d(0, 0), new Vector2d(0, 0));
            Assert.AreEqual(bounds1.Center, new Vector2d(0, 0));

            bounds1.Center = new Vector2d(10, 10);
            Assert.AreEqual(new Vector2dBounds(new Vector2d(10, 10), new Vector2d(10, 10)), bounds1);

            var bounds2 = new Vector2dBounds(new Vector2d(-10, -10), new Vector2d(10, 10));
            Assert.AreEqual(bounds2.Center, new Vector2d(0, 0));

            bounds2.Center = new Vector2d(10, 10);
            Assert.AreEqual(new Vector2dBounds(new Vector2d(0, 0), new Vector2d(20, 20)), bounds2);

            var bounds3 = new Vector2dBounds(new Vector2d(0, 0), new Vector2d(20, 40));
            Assert.AreEqual(bounds3.Center, new Vector2d(10, 20));

            bounds3.Center = new Vector2d(10, 10);
            Assert.AreEqual(new Vector2dBounds(new Vector2d(0, -10), new Vector2d(20, 30)), bounds3);
        }
    }
}
