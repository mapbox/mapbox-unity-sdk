using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Utilities;
using Mapbox.LandmarksModule;
using NUnit.Framework;
using UnityEngine;

namespace Mapbox.LandmarksModuleTests
{
    public class LandmarkPolygonFilterTests
    {
        // z14 tile space: x runs 0..1 west to east, z runs 0..-1 north to south. The north
        // neighbour is Y - 1, the east neighbour X + 1.
        private static readonly CanonicalTileId TileA = new CanonicalTileId(14, 8000, 5000);
        private static readonly CanonicalTileId EastOfA = new CanonicalTileId(14, 8001, 5000);
        private static readonly CanonicalTileId NorthOfA = new CanonicalTileId(14, 8000, 4999);
        private static readonly CanonicalTileId NorthEastOfA = new CanonicalTileId(14, 8001, 4999);

        private LandmarkPolygonFilter _filter;

        [SetUp]
        public void SetUp()
        {
            _filter = new LandmarkPolygonFilter();
        }

        // Axis-aligned rectangle ring in tile space, wound the same way as a footprint ring.
        private static Vector3[] Rect(float minX, float maxX, float minZ, float maxZ)
        {
            return new[]
            {
                new Vector3(minX, 0, minZ),
                new Vector3(maxX, 0, minZ),
                new Vector3(maxX, 0, maxZ),
                new Vector3(minX, 0, maxZ),
            };
        }

        private static VectorFeatureUnity Feature(CanonicalTileId tileId, float minX, float maxX, float minZ, float maxZ)
        {
            var feature = new VectorFeatureUnity { TileId = tileId };
            feature.Points.Add(new List<Vector3>(Rect(minX, maxX, minZ, maxZ)));
            return feature;
        }

        private IReadOnlyCollection<CanonicalTileId> Add(CanonicalTileId tileId, params Vector3[][] polygons)
        {
            return _filter.AddLandmarkPolygons(tileId, new List<Vector3[]>(polygons));
        }

        [Test]
        public void Try_FeatureOverlappingFootprintInSameTile_IsRejected()
        {
            Add(TileA, Rect(0.4f, 0.6f, -0.6f, -0.4f));

            Assert.IsFalse(_filter.Try(Feature(TileA, 0.5f, 0.7f, -0.5f, -0.3f)));
        }

        [Test]
        public void Try_FeatureAwayFromFootprintInSameTile_Passes()
        {
            Add(TileA, Rect(0.4f, 0.6f, -0.6f, -0.4f));

            Assert.IsTrue(_filter.Try(Feature(TileA, 0.7f, 0.8f, -0.8f, -0.7f)));
        }

        [Test]
        public void Try_FootprintInsideTileNearEastEdge_RejectsNeighbourFeatureOverhangingIntoTile()
        {
            // Wholly inside A (max x 0.995 < 1), but within the margin of its east edge.
            Add(TileA, Rect(0.95f, 0.995f, -0.6f, -0.4f));

            // B's copy of a building straddling the A/B edge: the vector tile buffer puts part of it
            // at x < 0 in B space, which is x 0.988..1 in A space - overlapping the footprint.
            Assert.IsFalse(_filter.Try(Feature(EastOfA, -0.012f, 0.05f, -0.55f, -0.45f)));
        }

        [Test]
        public void Try_FootprintStraddlingNorthEdge_RejectsFeatureInNorthNeighbour()
        {
            Add(TileA, Rect(0.4f, 0.6f, -0.05f, 0.05f));

            // The part past A's north edge is z -1..-0.95 in the north neighbour's space.
            Assert.IsFalse(_filter.Try(Feature(NorthOfA, 0.45f, 0.55f, -0.99f, -0.9f)));
        }

        [Test]
        public void Try_FootprintFarFromEdges_IsNotReplicatedToNeighbour()
        {
            var changed = Add(TileA, Rect(0.4f, 0.6f, -0.6f, -0.4f));

            CollectionAssert.AreEquivalent(new[] { TileA }, changed);
            Assert.IsTrue(_filter.Try(Feature(EastOfA, -0.012f, 0.05f, -0.55f, -0.45f)));
            Assert.IsTrue(_filter.Try(Feature(NorthOfA, 0.45f, 0.55f, -0.99f, -0.9f)));
        }

        [Test]
        public void AddLandmarkPolygons_FootprintNearCorner_ReturnsSourceAndTouchedNeighboursOnly()
        {
            // One footprint in the middle and one within the margin of the north-east corner: the
            // second touches the east, north and north-east neighbours, nothing touches west or south.
            var changed = Add(TileA,
                Rect(0.4f, 0.6f, -0.6f, -0.4f),
                Rect(0.97f, 0.99f, -0.03f, -0.01f));

            CollectionAssert.AreEquivalent(new[] { TileA, EastOfA, NorthOfA, NorthEastOfA }, changed);
        }

        [Test]
        public void AddLandmarkPolygons_EmptyOrNullInput_ReturnsEmptySet()
        {
            Assert.AreEqual(0, _filter.AddLandmarkPolygons(TileA, new List<Vector3[]>()).Count);
            Assert.AreEqual(0, _filter.AddLandmarkPolygons(TileA, null).Count);
            Assert.AreEqual(0, _filter.AddLandmarkPolygons(TileA, new List<Vector3[]> { null, new Vector3[0] }).Count);

            // Nothing was stored either.
            Assert.IsTrue(_filter.Try(Feature(TileA, 0f, 1f, -1f, 0f)));
        }

        [Test]
        public void RemoveLandmarkPolygonsFromSource_RemovesReplicasInNeighbours()
        {
            Add(TileA, Rect(0.95f, 0.995f, -0.6f, -0.4f));
            var neighbourFeature = Feature(EastOfA, -0.012f, 0.05f, -0.55f, -0.45f);
            Assert.IsFalse(_filter.Try(neighbourFeature), "Precondition: the replica should reject the neighbour feature.");

            _filter.RemoveLandmarkPolygonsFromSource(TileA);

            Assert.IsTrue(_filter.Try(neighbourFeature));
            Assert.IsTrue(_filter.Try(Feature(TileA, 0.9f, 1f, -0.6f, -0.4f)));
        }

        [Test]
        public void Try_Z16FeatureAtTopLeftOfTile_IsTransformedIntoZ14Space()
        {
            // The top-left z16 child of A covers A's 0..0.25 in both axes.
            var topLeftZ16 = new CanonicalTileId(16, TileA.X * 4, TileA.Y * 4);
            Add(TileA, Rect(0.1f, 0.2f, -0.2f, -0.1f));

            // 0.5..0.7 in z16 space is 0.125..0.175 in A: inside the footprint.
            Assert.IsFalse(_filter.Try(Feature(topLeftZ16, 0.5f, 0.7f, -0.7f, -0.5f)));

            // 0.9..1 in z16 space is 0.225..0.25 in A: just east of the footprint, so the far edge
            // of the z16 tile really does land at a quarter of A.
            Assert.IsTrue(_filter.Try(Feature(topLeftZ16, 0.9f, 1f, -0.2f, -0.1f)));

            // The first z16 geometry read as z14 coordinates directly misses the footprint, so its
            // rejection above came from the transform, not from the raw numbers.
            Assert.IsTrue(_filter.Try(Feature(TileA, 0.5f, 0.7f, -0.7f, -0.5f)));
        }
    }
}
