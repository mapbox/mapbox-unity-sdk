//-----------------------------------------------------------------------
// <copyright file="PolylineUtilsTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.UnitTest
{
    using System.Collections.Generic;
    using Mapbox.Utils;
    using NUnit.Framework;

    /// <summary>
    /// Polyline utils test.
    /// </summary>
    [TestFixture]
    public class PolylineUtilsTest
    {
        /// <summary>
        /// Tests the decode.
        /// </summary>
        /// <remarks>
        /// Sample values from https://developers.google.com/maps/documentation/utilities/polylinealgorithm.
        /// </remarks>    
        [Test]
        public void TestDecode()
        {
            // _p~iF~ps|U_ulLnnqC_mqNvxq`@
            List<Vector2d> path = PolylineUtils.Decode(
              "_p~iF~ps|U_ulLnnqC_mqNvxq`@");

            // (38.5, -120.2), (40.7, -120.95), (43.252, -126.453)
            Assert.AreEqual(-120.2, path[0].y);
            Assert.AreEqual(38.5, path[0].x);
            Assert.AreEqual(-120.95, path[1].y);
            Assert.AreEqual(40.7, path[1].x);
            Assert.AreEqual(-126.453, path[2].y);
            Assert.AreEqual(43.252, path[2].x);
        }

        /// <summary>
        /// Tests the encode.
        /// </summary>
        [Test]
        public void TestEncode()
        {
            // (38.5, -120.2), (40.7, -120.95), (43.252, -126.453)
            var path = new List<Vector2d>();
            path.Add(new Vector2d(38.5, -120.2));
            path.Add(new Vector2d(40.7, -120.95));
            path.Add(new Vector2d(43.252, -126.453));

            // _p~iF~ps|U_ulLnnqC_mqNvxq`@
            Assert.AreEqual("_p~iF~ps|U_ulLnnqC_mqNvxq`@", PolylineUtils.Encode(path));
        }
    }
}