using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Utilities;
using NUnit.Framework;

namespace Mapbox.BaseModuleTests
{
    public class MvtPropertiesTests
    {
        [Test]
        public void NormalizeIntegers_RestoresTheSignOfANegativeInt64()
        {
            // What a contour band at -200 m actually arrives as, read through a decoder that
            // treats every varint as unsigned.
            var properties = new Dictionary<string, object> { { "ele", 18446744073709551416UL } };

            MvtProperties.NormalizeIntegers(properties);

            Assert.AreEqual(-200L, properties["ele"]);
            Assert.AreEqual(-200, Convert.ToInt32(properties["ele"]));
        }

        [Test]
        public void NormalizeIntegers_LeavesSmallUnsignedValuesUnchangedInValue()
        {
            var properties = new Dictionary<string, object> { { "ele", 1200UL } };

            MvtProperties.NormalizeIntegers(properties);

            Assert.AreEqual(1200L, properties["ele"]);
            Assert.IsInstanceOf<long>(properties["ele"]);
        }

        [Test]
        public void NormalizeIntegers_TouchesOnlyUnsignedValues()
        {
            var properties = new Dictionary<string, object>
            {
                { "class", "hillshade" },
                { "index", 5L },
                { "height", 12.5d },
                { "visible", true },
                { "ele", 18446744073709551416UL }
            };

            MvtProperties.NormalizeIntegers(properties);

            Assert.AreEqual("hillshade", properties["class"]);
            Assert.AreEqual(5L, properties["index"]);
            Assert.AreEqual(12.5d, properties["height"]);
            Assert.AreEqual(true, properties["visible"]);
            Assert.AreEqual(-200L, properties["ele"]);
        }

        [Test]
        public void NormalizeIntegers_RewritesEveryUnsignedValuePresent()
        {
            var properties = new Dictionary<string, object>
            {
                { "ele", 18446744073709551416UL },
                { "min_ele", 18446744073709551216UL }
            };

            MvtProperties.NormalizeIntegers(properties);

            Assert.AreEqual(-200L, properties["ele"]);
            Assert.AreEqual(-400L, properties["min_ele"]);
        }

        [Test]
        public void NormalizeIntegers_HandlesEmptyAndNullDictionaries()
        {
            var empty = new Dictionary<string, object>();

            Assert.DoesNotThrow(() => MvtProperties.NormalizeIntegers(empty));
            Assert.DoesNotThrow(() => MvtProperties.NormalizeIntegers(null));
            Assert.AreEqual(0, empty.Count);
        }
    }
}
