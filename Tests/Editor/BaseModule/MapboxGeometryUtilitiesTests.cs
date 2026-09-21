using System.Collections.Generic;
using Mapbox.BaseModule.Utilities;
using NUnit.Framework;
using UnityEngine;

namespace Mapbox.BaseModuleTests
{
    public class MapboxGeometryUtilitiesTests
    {
        private readonly List<Mesh> _meshes = new List<Mesh>();

        [TearDown]
        public void TearDown()
        {
            foreach (var mesh in _meshes)
            {
                if (mesh != null) Object.DestroyImmediate(mesh);
            }
            _meshes.Clear();
        }

        private Mesh CreateMesh(Vector3[] vertices, int[] triangles)
        {
            var mesh = new Mesh { vertices = vertices, triangles = triangles };
            _meshes.Add(mesh);
            return mesh;
        }

        private static bool VectorsEqual(Vector3 a, Vector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;

        private static bool ContainsEdge(List<(Vector3 From, Vector3 To)> allowed, Vector3 from, Vector3 to)
        {
            foreach (var edge in allowed)
            {
                if (VectorsEqual(edge.From, from) && VectorsEqual(edge.To, to)) return true;
            }
            return false;
        }

        // Asserts that every consecutive (wrapping) pair in `ring` is one of the directed
        // edges in `allowedEdges`. Rotation-independent: it does not assume which vertex the
        // walk started from, only that the walk always moves forward along real mesh edges.
        private static void AssertRingIsCyclicWalkOf(Vector3[] ring, List<(Vector3 From, Vector3 To)> allowedEdges)
        {
            Assert.AreEqual(allowedEdges.Count, ring.Length, "Ring length should match the number of expected boundary edges.");
            for (var i = 0; i < ring.Length; i++)
            {
                var from = ring[i];
                var to = ring[(i + 1) % ring.Length];
                Assert.IsTrue(ContainsEdge(allowedEdges, from, to),
                    $"Edge {from}->{to} at ring position {i} is not one of the expected boundary edges.");
            }
        }

        [Test]
        public void ExtractBoundaryRings_Quad_ReturnsSingleRingInBoundaryOrder()
        {
            // Two triangles sharing the A-C diagonal. The shared diagonal must NOT show up in
            // the ring; only the four outer quad edges should.
            var a = new Vector3(0, 0, 0);
            var b = new Vector3(1, 0, 0);
            var c = new Vector3(1, 0, 1);
            var d = new Vector3(0, 0, 1);
            var mesh = CreateMesh(
                new[] { a, b, c, d },
                new[] { 0, 1, 2, 0, 2, 3 });

            var results = new List<Vector3[]>();
            var count = MapboxGeometryUtilities.ExtractBoundaryRings(mesh, results);

            Assert.AreEqual(1, count);
            Assert.AreEqual(1, results.Count);
            AssertRingIsCyclicWalkOf(results[0], new List<(Vector3, Vector3)> { (a, b), (b, c), (c, d), (d, a) });
        }

        [Test]
        public void ExtractBoundaryRings_SquareAnnulus_ReturnsTwoRings()
        {
            // Outer square with a smaller inner square removed (a hole), triangulated as a ring
            // of 8 triangles connecting the two boundaries. Every outer edge and every inner
            // edge appears in exactly one triangle (boundary); every radial/diagonal edge
            // appears once in each direction across two adjacent triangles (interior).
            var o0 = new Vector3(0, 0, 0);
            var o1 = new Vector3(3, 0, 0);
            var o2 = new Vector3(3, 0, 3);
            var o3 = new Vector3(0, 0, 3);
            var i0 = new Vector3(1, 0, 1);
            var i1 = new Vector3(2, 0, 1);
            var i2 = new Vector3(2, 0, 2);
            var i3 = new Vector3(1, 0, 2);

            var mesh = CreateMesh(
                new[] { o0, o1, o2, o3, i0, i1, i2, i3 },
                new[]
                {
                    0, 1, 4, 1, 5, 4, // segment 0: (O0,O1,I0) + (O1,I1,I0)
                    1, 2, 5, 2, 6, 5, // segment 1: (O1,O2,I1) + (O2,I2,I1)
                    2, 3, 6, 3, 7, 6, // segment 2: (O2,O3,I2) + (O3,I3,I2)
                    3, 0, 7, 0, 4, 7, // segment 3: (O3,O0,I3) + (O0,I0,I3)
                });

            var results = new List<Vector3[]>();
            var count = MapboxGeometryUtilities.ExtractBoundaryRings(mesh, results);

            Assert.AreEqual(2, count);
            Assert.AreEqual(2, results.Count);

            Vector3[] outerRing = null;
            Vector3[] innerRing = null;
            foreach (var ring in results)
            {
                if (ring.Length == 4 && System.Array.Exists(ring, v => VectorsEqual(v, o0))) outerRing = ring;
                else if (ring.Length == 4 && System.Array.Exists(ring, v => VectorsEqual(v, i0))) innerRing = ring;
            }

            Assert.IsNotNull(outerRing, "Expected one ring to be the outer boundary.");
            Assert.IsNotNull(innerRing, "Expected one ring to be the inner (hole) boundary.");

            AssertRingIsCyclicWalkOf(outerRing, new List<(Vector3, Vector3)> { (o0, o1), (o1, o2), (o2, o3), (o3, o0) });
            // The hole boundary walks in the opposite rotational sense from the outer ring,
            // which is the expected result of this triangulation, not an arbitrary choice.
            AssertRingIsCyclicWalkOf(innerRing, new List<(Vector3, Vector3)> { (i1, i0), (i0, i3), (i3, i2), (i2, i1) });
        }

        [Test]
        public void ExtractBoundaryRings_ClosedMesh_FallsBackToConvexHullOfXZProjection()
        {
            // A tetrahedron is a closed 2-manifold: every edge is shared by exactly two
            // triangles, so there is no boundary edge at all. The apex is placed directly
            // above the centroid of the base triangle so it projects strictly inside the
            // base in the X-Z plane, making the base triangle the expected convex hull.
            var p0 = new Vector3(0, 0, 0);
            var p1 = new Vector3(4, 0, 0);
            var p2 = new Vector3(2, 0, 4);
            var p3 = new Vector3(2, 5, 4f / 3f); // XZ = centroid of (p0,p1,p2)

            var mesh = CreateMesh(
                new[] { p0, p1, p2, p3 },
                new[]
                {
                    1, 2, 3, // opposite P0
                    0, 3, 2, // opposite P1
                    0, 1, 3, // opposite P2
                    0, 2, 1, // opposite P3
                });

            var results = new List<Vector3[]>();
            var count = MapboxGeometryUtilities.ExtractBoundaryRings(mesh, results);

            Assert.AreEqual(1, count);
            Assert.AreEqual(1, results.Count);
            // Convex hull of the 4 projected points, with the apex excluded because it
            // projects to the (interior) centroid of the base triangle.
            CollectionAssert.AreEqual(new[] { p0, p1, p2 }, results[0]);
        }

        [Test]
        public void ExtractBoundaryRings_SplitVerticesWithinWeldTolerance_StillWeldIntoOneRing()
        {
            // Same quad as ExtractBoundaryRings_Quad_ReturnsSingleRingInBoundaryOrder, but the
            // diagonal's two vertices (A and C) are duplicated with a 1e-6 X offset and a
            // different Y - simulating an exporter splitting vertices for differing
            // normals/UVs across the diagonal. 1e-6 is well inside the default 1e-5 weld
            // tolerance, so this should still weld to the same single 4-vertex ring as the
            // unsplit quad. If welding did NOT happen, the two triangles would share no
            // vertices and this would instead produce two 3-vertex rings.
            var a = new Vector3(0, 0, 0);
            var b = new Vector3(1, 0, 0);
            var c = new Vector3(1, 0, 1);
            var d = new Vector3(0, 0, 1);
            var aSplit = new Vector3(0 + 1e-6f, 0.5f, 0);
            var cSplit = new Vector3(1 + 1e-6f, 0.5f, 1);

            var mesh = CreateMesh(
                new[] { a, b, c, aSplit, cSplit, d },
                new[] { 0, 1, 2, 3, 4, 5 });

            var results = new List<Vector3[]>();
            var count = MapboxGeometryUtilities.ExtractBoundaryRings(mesh, results);

            Assert.AreEqual(1, count, "Split vertices within the weld tolerance should still produce a single welded ring.");
            AssertRingIsCyclicWalkOf(results[0], new List<(Vector3, Vector3)> { (a, b), (b, c), (c, d), (d, a) });
        }

        [Test]
        public void PolygonsIntersect_OverlappingSquares_ReturnsTrue()
        {
            var squareA = new[]
            {
                new Vector3(0, 0, 0), new Vector3(4, 0, 0), new Vector3(4, 0, 4), new Vector3(0, 0, 4)
            };
            var squareB = new[]
            {
                new Vector3(2, 0, 2), new Vector3(6, 0, 2), new Vector3(6, 0, 6), new Vector3(2, 0, 6)
            };

            Assert.IsTrue(MapboxGeometryUtilities.PolygonsIntersect(squareA, squareB));
        }

        [Test]
        public void PolygonsIntersect_DisjointSquares_ReturnsFalse()
        {
            var squareA = new[]
            {
                new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1)
            };
            var squareB = new[]
            {
                new Vector3(5, 0, 5), new Vector3(6, 0, 5), new Vector3(6, 0, 6), new Vector3(5, 0, 6)
            };

            Assert.IsFalse(MapboxGeometryUtilities.PolygonsIntersect(squareA, squareB));
        }

        [Test]
        public void PolygonsIntersect_OneSquareFullyContainsAnother_ReturnsTrue()
        {
            var outer = new[]
            {
                new Vector3(0, 0, 0), new Vector3(10, 0, 0), new Vector3(10, 0, 10), new Vector3(0, 0, 10)
            };
            var inner = new[]
            {
                new Vector3(2, 0, 2), new Vector3(4, 0, 2), new Vector3(4, 0, 4), new Vector3(2, 0, 4)
            };

            Assert.IsTrue(MapboxGeometryUtilities.PolygonsIntersect(outer, inner));
        }

        [Test]
        public void PolygonsIntersect_SquaresAbuttingAlongOneEdge_ReturnsTrueViaCornerTouch()
        {
            // DOCUMENTS ACTUAL BEHAVIOUR, not an obviously-correct spec: two squares that only
            // touch along one shared edge (e.g. adjacent buildings) are reported as
            // intersecting. The shared edge segments themselves are exactly collinear/parallel,
            // so LineSegmentsIntersect's parallel check (d == 0) actually rejects that pair -
            // but a *different* edge pair meets exactly at the shared corner (1,0), and
            // LineSegmentsIntersect treats t==1/u==1 endpoint touches as an intersection
            // (t,u ranges are inclusive of 0 and 1). That corner touch is what makes this
            // return true, not the shared edge being detected as an overlap.
            var squareA = new[]
            {
                new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1)
            };
            var squareB = new[]
            {
                new Vector3(1, 0, 0), new Vector3(2, 0, 0), new Vector3(2, 0, 1), new Vector3(1, 0, 1)
            };

            Assert.IsTrue(MapboxGeometryUtilities.PolygonsIntersect(squareA, squareB));
        }

        [Test]
        public void IsPointInPolygon_PointInsideSquare_ReturnsTrue()
        {
            var square = new[]
            {
                new Vector3(0, 0, 0), new Vector3(2, 0, 0), new Vector3(2, 0, 2), new Vector3(0, 0, 2)
            };

            Assert.IsTrue(MapboxGeometryUtilities.IsPointInPolygon(new Vector3(1, 0, 1), square));
        }

        [Test]
        public void IsPointInPolygon_PointOutsideSquare_ReturnsFalse()
        {
            var square = new[]
            {
                new Vector3(0, 0, 0), new Vector3(2, 0, 0), new Vector3(2, 0, 2), new Vector3(0, 0, 2)
            };

            Assert.IsFalse(MapboxGeometryUtilities.IsPointInPolygon(new Vector3(3, 0, 3), square));
        }

        [Test]
        public void IsPointInPolygon_BoundaryPoints_TreatedByHalfOpenRayCastConvention()
        {
            // DOCUMENTS ACTUAL BEHAVIOUR: this ray-casting implementation is not symmetric on
            // the polygon boundary. A vertex, and the midpoint of the "lower" edge sharing
            // that vertex, both count as inside; the midpoint of the opposite ("upper") edge
            // counts as outside. This is the standard half-open ray-cast convention (it is
            // what keeps adjacent polygons from double-counting a shared edge), not a bug.
            var square = new[]
            {
                new Vector3(0, 0, 0), new Vector3(2, 0, 0), new Vector3(2, 0, 2), new Vector3(0, 0, 2)
            };

            Assert.IsTrue(MapboxGeometryUtilities.IsPointInPolygon(new Vector3(0, 0, 0), square), "Vertex (0,0) is treated as inside.");
            Assert.IsTrue(MapboxGeometryUtilities.IsPointInPolygon(new Vector3(1, 0, 0), square), "Midpoint of the bottom edge is treated as inside.");
            Assert.IsFalse(MapboxGeometryUtilities.IsPointInPolygon(new Vector3(2, 0, 1), square), "Midpoint of the right edge is treated as outside.");
        }

        [Test]
        public void BoundsOfVertices_Array_ProducesExpectedMinMax()
        {
            var vertices = new[]
            {
                new Vector3(1, 0, 2), new Vector3(5, 0, -3), new Vector3(-4, 0, 9)
            };

            var bounds = MapboxGeometryUtilities.BoundsOfVertices(vertices);

            Assert.AreEqual(new Vector3(0.5f, 0, 3f), bounds.center);
            Assert.AreEqual(new Vector3(9f, 1f, 12f), bounds.size);
            Assert.AreEqual(-4f, bounds.min.x);
            Assert.AreEqual(5f, bounds.max.x);
            Assert.AreEqual(-3f, bounds.min.z);
            Assert.AreEqual(9f, bounds.max.z);
        }

        [Test]
        public void BoundsOfVertices_SpanWithScaleOffset_MapsIntoTargetTileSpace()
        {
            Vector3[] verticesArray =
            {
                new Vector3(1, 0, 2), new Vector3(5, 0, -3), new Vector3(-4, 0, 9)
            };
            System.Span<Vector3> vertices = verticesArray;
            // scaleX=2, scaleZ=3, offsetX=10, offsetZ=-5.
            // transformedX = x * scaleX + offsetX; transformedZ = z * scaleZ - offsetZ.
            var scaleOffset = new Vector4(2f, 3f, 10f, -5f);

            var bounds = MapboxGeometryUtilities.BoundsOfVertices(vertices, scaleOffset);

            // Untransformed center/size are (0.5, 0, 3) and (9, 1, 12) (see the no-offset test
            // above); the transform is applied to those, not to per-vertex min/max.
            Assert.AreEqual(new Vector3(11f, 0, 14f), bounds.center);
            Assert.AreEqual(new Vector3(18f, 1f, 36f), bounds.size);
        }
    }
}
