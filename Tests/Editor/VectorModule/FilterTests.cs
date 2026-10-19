using System.Collections.Generic;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.Filters;
using Mapbox.VectorModule.MeshGeneration.Unity;
using NUnit.Framework;

namespace Mapbox.VectorModuleTests
{
    public class FilterTests
    {
        private class StubFilter : ILayerFeatureFilterComparer
        {
            private readonly bool _answer;
            public int TryCallCount;

            public StubFilter(bool answer) => _answer = answer;

            public void Initialize()
            {
            }

            public bool Try(VectorFeatureUnity feature)
            {
                TryCallCount++;
                return _answer;
            }
        }

        private static VectorFeatureUnity FeatureWith(params (string key, object value)[] properties)
        {
            var feature = new VectorFeatureUnity { Properties = new Dictionary<string, object>() };
            foreach (var (key, value) in properties)
            {
                feature.Properties.Add(key, value);
            }

            return feature;
        }

        private static VectorFilterStack StackOf(LayerFilterCombinerOperationType type,
            params ILayerFeatureFilterComparer[] filters)
        {
            return new VectorFilterStack
            {
                Type = type,
                Filters = new List<ILayerFeatureFilterComparer>(filters)
            };
        }

        [Test]
        public void Stack_WithNoFiltersPassesEverything()
        {
            var feature = FeatureWith();

            Assert.IsTrue(StackOf(LayerFilterCombinerOperationType.All).Try(feature));
            Assert.IsTrue(new VectorFilterStack { Filters = null }.Try(feature));
        }

        [Test]
        public void Stack_AnyPassesWhenOneFilterMatches()
        {
            var stack = StackOf(LayerFilterCombinerOperationType.Any, new StubFilter(false), new StubFilter(true));

            Assert.IsTrue(stack.Try(FeatureWith()));
        }

        [Test]
        public void Stack_AnyStopsAtTheFirstMatch()
        {
            var second = new StubFilter(true);
            var stack = StackOf(LayerFilterCombinerOperationType.Any, new StubFilter(true), second);

            Assert.IsTrue(stack.Try(FeatureWith()));
            Assert.AreEqual(0, second.TryCallCount);
        }

        [Test]
        public void Stack_AllFailsOnTheFirstMismatchAndStopsThere()
        {
            var second = new StubFilter(true);
            var stack = StackOf(LayerFilterCombinerOperationType.All, new StubFilter(false), second);

            Assert.IsFalse(stack.Try(FeatureWith()));
            Assert.AreEqual(0, second.TryCallCount);
        }

        [Test]
        public void Stack_NoneInvertsAny()
        {
            var feature = FeatureWith();

            Assert.IsFalse(StackOf(LayerFilterCombinerOperationType.None, new StubFilter(true)).Try(feature));
            Assert.IsTrue(StackOf(LayerFilterCombinerOperationType.None, new StubFilter(false)).Try(feature));
        }

        [Test]
        public void StringFilter_MatchesRegardlessOfCase()
        {
            var filter = new FeatureStringPropertyFilter(new FeatureStringPropertyFilterSettings
            {
                PropertyName = "class",
                CheckOperation = StringCheckOperation.Equals,
                FilterString = "Hillshade"
            });
            filter.Initialize();

            Assert.IsTrue(filter.Try(FeatureWith(("class", "hillshade"))));
            Assert.IsFalse(filter.Try(FeatureWith(("class", "water"))));
        }

        [Test]
        public void StringFilter_TreatsAMissingPropertyAsNoMatchRatherThanThrowing()
        {
            var filter = new FeatureStringPropertyFilter(new FeatureStringPropertyFilterSettings
            {
                PropertyName = "class",
                CheckOperation = StringCheckOperation.Equals,
                FilterString = "hillshade"
            });
            filter.Initialize();

            // Whole layers arrive with no tags at all; indexing the dictionary used to throw here.
            Assert.IsFalse(filter.Try(FeatureWith(("other", "value"))));
            Assert.IsFalse(filter.Try(new VectorFeatureUnity()));
        }

        [Test]
        public void StringFilter_InvertNegatesBothOutcomes()
        {
            var filter = new FeatureStringPropertyFilter(new FeatureStringPropertyFilterSettings
            {
                PropertyName = "class",
                CheckOperation = StringCheckOperation.Equals,
                FilterString = "hillshade",
                Invert = true
            });
            filter.Initialize();

            Assert.IsFalse(filter.Try(FeatureWith(("class", "hillshade"))));
            Assert.IsTrue(filter.Try(FeatureWith(("class", "water"))));
            Assert.IsTrue(filter.Try(FeatureWith(("other", "value"))));
        }

        [Test]
        public void StringFilter_ContainsMatchesAnyEntryInTheCommaSeparatedList()
        {
            var filter = new FeatureStringPropertyFilter(new FeatureStringPropertyFilterSettings
            {
                PropertyName = "class",
                CheckOperation = StringCheckOperation.Contains,
                FilterString = "wood, grass ,scrub"
            });
            filter.Initialize();

            Assert.IsTrue(filter.Try(FeatureWith(("class", "grass"))));
            Assert.IsTrue(filter.Try(FeatureWith(("class", "SCRUB"))));
            Assert.IsFalse(filter.Try(FeatureWith(("class", "water"))));
        }

        [Test]
        public void NumberFilter_ReadsTheTypesTheDecoderActuallyBoxes()
        {
            var filter = new FeatureNumberPropertyFilter(new FeatureNumberPropertyFilterSettings
            {
                PropertyName = "ele",
                checkOperation = FilterCheckOperation.LessThan,
                FilterValue = 0f
            });

            Assert.IsTrue(filter.Try(FeatureWith(("ele", -200L))), "boxed long, as the decoder produces");
            Assert.IsTrue(filter.Try(FeatureWith(("ele", -200d))), "boxed double");
            Assert.IsTrue(filter.Try(FeatureWith(("ele", -200f))), "boxed float");
            Assert.IsTrue(filter.Try(FeatureWith(("ele", "-200"))), "string, parsed invariantly");
            Assert.IsFalse(filter.Try(FeatureWith(("ele", 200L))));
        }

        [Test]
        public void NumberFilter_TreatsMissingAndUnreadableValuesAsNoMatch()
        {
            var filter = new FeatureNumberPropertyFilter(new FeatureNumberPropertyFilterSettings
            {
                PropertyName = "ele",
                checkOperation = FilterCheckOperation.MoreThan,
                FilterValue = -1000f
            });

            Assert.IsFalse(filter.Try(FeatureWith(("other", 5L))), "property absent");
            Assert.IsFalse(filter.Try(new VectorFeatureUnity()), "no properties at all");
            Assert.IsFalse(filter.Try(FeatureWith(("ele", "not a number"))));
        }

        [Test]
        public void HasPropertyFilter_HandlesAFeatureWithNoProperties()
        {
            var filter = new HasPropertyFilter("class");

            Assert.IsTrue(filter.Try(FeatureWith(("class", "water"))));
            Assert.IsFalse(filter.Try(FeatureWith(("other", "value"))));
            Assert.IsFalse(filter.Try(new VectorFeatureUnity()));
        }
    }
}
