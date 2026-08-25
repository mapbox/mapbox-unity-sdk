using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mapbox.BaseModule.Utilities
{
    /// <summary>
    /// Utility class for 2D polygon geometry operations.
    /// Provides methods for polygon intersection detection, point-in-polygon tests,
    /// and other geometric calculations in the X-Z plane.
    /// </summary>
    public static class MapboxGeometryUtilities
    {
        /// <summary>
        /// Determines if two 2D polygons intersect using multiple collision detection strategies.
        /// Works in the X-Z plane (Y is ignored).
        ///
        /// OPTIMIZATION: This method assumes bounding box intersection has already been checked.
        /// Do NOT call this without checking bbox intersection first.
        /// </summary>
        /// <param name="polygonA">First polygon (array of vertices)</param>
        /// <param name="polygonB">Second polygon (array of vertices)</param>
        /// <returns>True if polygons intersect or overlap</returns>
        public static bool PolygonsIntersect(Vector3[] polygonA, Vector3[] polygonB)
        {
            // NOTE: Bbox check is skipped here because it's already done by the caller
            // This avoids redundant bounding box calculations

            // Strategy 1: Check if any vertex of one polygon is inside the other
            // This handles the case where one polygon is completely inside the other
            // OPTIMIZATION: Only check first vertex of each polygon initially
            // If polygons overlap significantly, this catches it immediately
            if (IsPointInPolygon(polygonA[0], polygonB) || IsPointInPolygon(polygonB[0], polygonA))
                return true;

            // Strategy 2: Check if any edges from the two polygons intersect
            // This handles the case where polygons overlap but neither contains the other's vertices
            // This is the most expensive check (O(n*m)), so we do it last
            if (DoEdgesIntersect(polygonA, polygonB))
                return true;

            // No intersection found
            return false;
        }

        /// <summary>
        /// Calculates the minimum separation distance between two bounding boxes.
        /// Returns 0 if boxes are overlapping, otherwise returns the gap distance.
        /// This is used as a fast heuristic to reject near-miss cases.
        /// </summary>
        /// <param name="a">First bounding box</param>
        /// <param name="b">Second bounding box</param>
        /// <returns>Separation distance (0 if overlapping)</returns>
        public static float GetBoundsSeparation(Bounds a, Bounds b)
        {
            // Calculate separation on X axis
            var dx = Mathf.Max(0, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x));

            // Calculate separation on Z axis
            var dz = Mathf.Max(0, Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z));

            // Return Euclidean distance (0 if boxes overlap on any axis)
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// Point-in-polygon test using ray casting algorithm.
        /// Casts a ray from the point to infinity and counts edge crossings.
        /// Works in 2D using X-Z coordinates.
        /// </summary>
        /// <param name="point">Point to test</param>
        /// <param name="polygon">Polygon vertices</param>
        /// <returns>True if point is inside polygon</returns>
        public static bool IsPointInPolygon(Vector3 point, Vector3[] polygon)
        {
            bool inside = false;
            int j = polygon.Length - 1; // Start with the last vertex

            // Cast a ray from the point in the +X direction
            // Count how many polygon edges it crosses
            for (int i = 0; i < polygon.Length; i++)
            {
                // Check if the edge from polygon[j] to polygon[i] crosses the ray
                // The edge must straddle the ray's Z coordinate
                if (((polygon[i].z > point.z) != (polygon[j].z > point.z)) &&
                    // And the crossing point must be to the right of the test point
                    (point.x < (polygon[j].x - polygon[i].x) * (point.z - polygon[i].z) / (polygon[j].z - polygon[i].z) + polygon[i].x))
                {
                    inside = !inside; // Toggle with each crossing
                }
                j = i; // Move to next edge
            }

            return inside;
        }

        /// <summary>
        /// Checks if any edge from polygonA intersects with any edge from polygonB.
        /// Uses line segment intersection test for each pair of edges.
        /// </summary>
        /// <param name="polygonA">First polygon</param>
        /// <param name="polygonB">Second polygon</param>
        /// <returns>True if any edges intersect</returns>
        public static bool DoEdgesIntersect(Vector3[] polygonA, Vector3[] polygonB)
        {
            // Test each edge of polygonA against each edge of polygonB
            for (int i = 0; i < polygonA.Length; i++)
            {
                int i2 = (i + 1) % polygonA.Length; // Wrap around to form closed polygon

                for (int j = 0; j < polygonB.Length; j++)
                {
                    int j2 = (j + 1) % polygonB.Length; // Wrap around to form closed polygon

                    if (LineSegmentsIntersect(polygonA[i], polygonA[i2], polygonB[j], polygonB[j2]))
                    {
                        return true; // Found an intersection
                    }
                }
            }

            return false; // No intersections found
        }

        /// <summary>
        /// Determines if two line segments intersect in 2D space (X-Z plane).
        /// Uses parametric line equations and checks if intersection point lies on both segments.
        /// </summary>
        /// <param name="p1">First point of first segment</param>
        /// <param name="p2">Second point of first segment</param>
        /// <param name="p3">First point of second segment</param>
        /// <param name="p4">Second point of second segment</param>
        /// <returns>True if segments intersect</returns>
        public static bool LineSegmentsIntersect(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            // Calculate the denominator for the parametric equations
            // If d is zero, the lines are parallel
            float d = (p2.x - p1.x) * (p4.z - p3.z) - (p2.z - p1.z) * (p4.x - p3.x);

            // Lines are parallel (or coincident) - no intersection
            if (Mathf.Abs(d) < 0.0001f)
                return false;

            // Calculate the parameters t and u for the intersection point
            // t is the position along the first segment (0 to 1)
            // u is the position along the second segment (0 to 1)
            float t = ((p3.x - p1.x) * (p4.z - p3.z) - (p3.z - p1.z) * (p4.x - p3.x)) / d;
            float u = ((p3.x - p1.x) * (p2.z - p1.z) - (p3.z - p1.z) * (p2.x - p1.x)) / d;

            // Segments intersect if both parameters are in the range [0, 1]
            return t >= 0 && t <= 1 && u >= 0 && u <= 1;
        }

        /// <summary>
        /// Calculates the 2D bounding box of a set of vertices with coordinate transformation.
        /// Works in the X-Z plane (Y is ignored for bounds calculation but preserved in output).
        ///
        /// The scaleOffset parameter is used to transform vertices from one tile coordinate space to another.
        /// This is essential when comparing geometry across different tile zoom levels (e.g., z15 buildings vs z14 landmarks).
        /// </summary>
        /// <param name="vertices">Vertices to calculate bounds for</param>
        /// <param name="scaleOffset">Transformation parameters as Vector4:
        /// - [0] = scaleX: Scale factor for X coordinates
        /// - [1] = scaleZ: Scale factor for Z coordinates
        /// - [2] = offsetX: Translation offset for X coordinates
        /// - [3] = offsetZ: Translation offset for Z coordinates
        ///
        /// Transformation applied:
        /// - transformedX = vertexX * scaleOffset[0] + scaleOffset[2]
        /// - transformedZ = vertexZ * scaleOffset[1] - scaleOffset[3]
        ///
        /// Example: To transform from z15 tile (200, 400) to parent z14 tile (100, 200),
        /// the scaleOffset would scale coordinates by 0.5 and apply appropriate offsets.
        /// </param>
        /// <returns>Bounding box in the transformed coordinate space</returns>
        /// <summary>
        /// Extracts the boundary outline(s) of a triangulated, roughly planar mesh as ordered
        /// rings in the X-Z plane, appending one array per ring.
        ///
        /// This exists because a mesh's vertex array is in storage order, not boundary order.
        /// Handing it straight to <see cref="PolygonsIntersect"/> describes a self-intersecting
        /// scribble rather than an outline, and the containment tests there require consecutive
        /// vertices to be adjacent along the boundary. A four-vertex quad happens to survive
        /// that; anything more detailed does not.
        ///
        /// A boundary edge is a directed triangle edge whose reverse is not also present, which
        /// is what distinguishes the outline from interior edges shared by two triangles.
        /// Positions are welded first: exporters split vertices for differing normals or UVs, so
        /// triangles that meet geometrically may not share indices, and without welding every
        /// edge would look like a boundary edge.
        ///
        /// Rings are returned as-is, so a footprint with a hole contributes the hole as its own
        /// ring. For landmark suppression that only means a building inside a courtyard is also
        /// hidden, which is the safer direction to err in.
        /// </summary>
        /// <param name="mesh">Triangulated source mesh. Only positions and triangles are read.</param>
        /// <param name="results">List that each extracted ring is appended to.</param>
        /// <param name="weldTolerance">Positions closer than this in X and Z are treated as one vertex.</param>
        /// <returns>Number of rings appended.</returns>
        public static int ExtractBoundaryRings(Mesh mesh, List<Vector3[]> results, float weldTolerance = 1e-5f)
        {
            if (mesh == null || results == null) return 0;

            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            if (vertices.Length < 3 || triangles.Length < 3) return 0;

            // Weld by quantized X-Z position. Y is left alone: footprints are planar in X-Z and
            // that is the plane the overlap tests work in.
            var scale = 1f / Mathf.Max(weldTolerance, 1e-7f);
            var canonical = new int[vertices.Length];
            var byPosition = new Dictionary<long, int>(vertices.Length);
            for (var i = 0; i < vertices.Length; i++)
            {
                var key = PositionKey(vertices[i], scale);
                if (byPosition.TryGetValue(key, out var existing))
                {
                    canonical[i] = existing;
                }
                else
                {
                    byPosition.Add(key, i);
                    canonical[i] = i;
                }
            }

            var directed = new HashSet<long>();
            var edges = new List<int>(triangles.Length * 2);
            for (var t = 0; t + 2 < triangles.Length; t += 3)
            {
                var a = canonical[triangles[t]];
                var b = canonical[triangles[t + 1]];
                var c = canonical[triangles[t + 2]];
                if (a == b || b == c || c == a) continue; // degenerate triangle

                AddEdge(edges, directed, a, b);
                AddEdge(edges, directed, b, c);
                AddEdge(edges, directed, c, a);
            }

            // Keep the edges with no opposite twin, indexed by their start vertex so the ring
            // can be walked.
            var next = new Dictionary<int, int>();
            for (var i = 0; i + 1 < edges.Count; i += 2)
            {
                var from = edges[i];
                var to = edges[i + 1];
                if (directed.Contains(EdgeKey(to, from))) continue; // interior edge
                if (!next.ContainsKey(from)) next.Add(from, to);
            }

            // A closed mesh (a solid prism used as a footprint volume, say) has a twin for every
            // edge and therefore no boundary at all. Fall back to the convex hull of the X-Z
            // projection, which is always a valid ring and encloses the true outline, so such a
            // footprint still suppresses rather than being skipped.
            if (next.Count < 3)
            {
                var hull = ConvexHullXZ(vertices, scale);
                if (hull == null) return 0;

                results.Add(hull);
                return 1;
            }

            var appended = 0;
            var visited = new HashSet<int>();
            var ring = new List<Vector3>();
            foreach (var start in next.Keys)
            {
                if (visited.Contains(start)) continue;

                ring.Clear();
                var current = start;
                // Add returns false once the walk closes back on a vertex already taken, which
                // both terminates the ring and guarantees termination overall.
                while (visited.Add(current))
                {
                    ring.Add(vertices[current]);
                    if (!next.TryGetValue(current, out current)) break;
                }

                if (ring.Count >= 3)
                {
                    results.Add(ring.ToArray());
                    appended++;
                }
            }

            return appended;
        }

        /// <summary>
        /// Convex hull of vertices projected onto the X-Z plane, by Andrew's monotone chain.
        /// Used as a fallback when a mesh has no boundary edges to walk. Winding is not
        /// normalised, because the polygon tests this feeds are orientation-independent.
        /// </summary>
        private static Vector3[] ConvexHullXZ(Vector3[] vertices, float scale)
        {
            var seen = new HashSet<long>();
            var points = new List<Vector3>(vertices.Length);
            foreach (var vertex in vertices)
            {
                if (seen.Add(PositionKey(vertex, scale))) points.Add(vertex);
            }

            if (points.Count < 3) return null;

            points.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.z.CompareTo(b.z));

            var chain = new Vector3[points.Count * 2];
            var count = 0;

            for (var i = 0; i < points.Count; i++)
            {
                while (count >= 2 && CrossXZ(chain[count - 2], chain[count - 1], points[i]) <= 0f) count--;
                chain[count++] = points[i];
            }

            var lowerCount = count + 1;
            for (var i = points.Count - 2; i >= 0; i--)
            {
                while (count >= lowerCount && CrossXZ(chain[count - 2], chain[count - 1], points[i]) <= 0f) count--;
                chain[count++] = points[i];
            }

            // The walk closes on the starting point, so the final entry is a duplicate.
            var length = count - 1;
            if (length < 3) return null;

            var hull = new Vector3[length];
            Array.Copy(chain, hull, length);
            return hull;
        }

        private static float CrossXZ(Vector3 origin, Vector3 a, Vector3 b)
            => (a.x - origin.x) * (b.z - origin.z) - (a.z - origin.z) * (b.x - origin.x);

        private static void AddEdge(List<int> edges, HashSet<long> directed, int from, int to)
        {
            edges.Add(from);
            edges.Add(to);
            directed.Add(EdgeKey(from, to));
        }

        private static long EdgeKey(int from, int to) => ((long)from << 32) | (uint)to;

        private static long PositionKey(Vector3 position, float scale)
        {
            var x = (long)Mathf.RoundToInt(position.x * scale);
            var z = (long)Mathf.RoundToInt(position.z * scale);
            return (x << 32) ^ (z & 0xFFFFFFFFL);
        }

        public static Bounds BoundsOfVertices(Span<Vector3> vertices, Vector4 scaleOffset)
        {
            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;

            // Find min/max in original coordinate space (using Z for Y)
            foreach (var vertex in vertices)
            {
                if (vertex.x < minX) minX = vertex.x;
                if (vertex.x > maxX) maxX = vertex.x;
                if (vertex.z < minY) minY = vertex.z;
                if (vertex.z > maxY) maxY = vertex.z;
            }

            // Calculate center and size in original space
            var centerX = (maxX + minX) / 2;
            var centerY = (maxY + minY) / 2;
            var sizeX = maxX - minX;
            var sizeY = maxY - minY;

            // Apply transformation to center and size
            var center = new Vector3(
                centerX * scaleOffset[0] + scaleOffset[2],
                0,
                centerY * scaleOffset[1] - scaleOffset[3]
            );
            var size = new Vector3(
                sizeX * scaleOffset[0],
                1,
                sizeY * scaleOffset[1]
            );

            return new Bounds(center, size);
        }

        /// <summary>
        /// Calculates the 2D bounding box of a set of vertices (Span).
        /// Works in the X-Z plane (Y is ignored for bounds calculation).
        /// No coordinate transformation is applied - vertices are used as-is.
        /// </summary>
        /// <param name="vertices">Vertices to calculate bounds for</param>
        /// <returns>Bounding box enclosing all vertices in the X-Z plane</returns>
        public static Bounds BoundsOfVertices(Span<Vector3> vertices)
        {
            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;

            foreach (var vertex in vertices)
            {
                if (vertex.x < minX) minX = vertex.x;
                if (vertex.x > maxX) maxX = vertex.x;
                if (vertex.z < minY) minY = vertex.z;
                if (vertex.z > maxY) maxY = vertex.z;
            }

            var center = new Vector3((maxX + minX) / 2, 0, (maxY + minY) / 2);
            var size = new Vector3(maxX - minX, 1, maxY - minY);
            return new Bounds(center, size);
        }

        /// <summary>
        /// Calculates the 2D bounding box of a set of vertices (Array).
        /// Works in the X-Z plane (Y is ignored for bounds calculation).
        /// No coordinate transformation is applied - vertices are used as-is.
        /// </summary>
        /// <param name="vertices">Vertices to calculate bounds for</param>
        /// <returns>Bounding box enclosing all vertices in the X-Z plane</returns>
        public static Bounds BoundsOfVertices(Vector3[] vertices)
        {
            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;

            foreach (var vertex in vertices)
            {
                if (vertex.x < minX) minX = vertex.x;
                if (vertex.x > maxX) maxX = vertex.x;
                if (vertex.z < minY) minY = vertex.z;
                if (vertex.z > maxY) maxY = vertex.z;
            }

            var center = new Vector3((maxX + minX) / 2, 0, (maxY + minY) / 2);
            var size = new Vector3(maxX - minX, 1, maxY - minY);
            return new Bounds(center, size);
        }
    }
}
