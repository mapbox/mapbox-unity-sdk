using System;
using System.Runtime.CompilerServices;
using Mapbox.BaseModule.Map;
using Mapbox.VectorModule.ComponentSystem.BuildingComponentVisualizer;
using Mapbox.VectorModule.ComponentSystem.Data;
using Mapbox.VectorModule.MeshGeneration.MeshModifiers;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem.Modifiers
{
    public interface IPerformanceExtrusion
    {
        int CalculateTriCountFor(int totalPointCount);

        int Run(Span<Vector3> vertices, Span<Vector3> normals, 
            int vertexAnchorIndex, 
            int[] triList, 
            int triIndex,
            float height,
            float minHeight,
            FeatureVertexData vertexData,
            float tileSizeX, 
            float scale);
    }
    
    public class ArrayChamferHeight : IPerformanceExtrusion
    {
        private ChamferSettings _settings;

        private static readonly Vector3 Up = Vector3.up;
        
        public ArrayChamferHeight(ChamferSettings settings)
        {
            _settings = settings;
        }
        
        public int Run(Span<Vector3> vertices, 
            Span<Vector3> normals, 
            int vertexAnchorIndex, 
            int[] triList, 
            int triIndex, 
            float height,
            float minHeight,
            FeatureVertexData vertexData,
            float tileSizeX, 
            float  scale)
        {
            if (vertexData == null || vertexData.Submeshes.Count < 1)
                return triIndex;

            var scaledOffset = (_settings.OffsetInMeters / tileSizeX) / scale;

            height = (height / scale) / tileSizeX;
            minHeight   = (minHeight > 0)
                ? (minHeight / scale) / tileSizeX
                : 0;

            triIndex = Chamfer(vertices, normals, vertexData, triList, triIndex, minHeight, height, scaledOffset, vertexAnchorIndex);
            return triIndex;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 NormalizeXZ(Vector3 v)
        {
            float invLen = 1.0f / Mathf.Sqrt(v.x * v.x + v.z * v.z + 1e-12f);
            v.x *= invLen;
            v.z *= invLen;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 PerpXZ(Vector3 v) => new Vector3(-v.z, 0f, v.x);

        /// <summary>
        /// |-x1-|-y1-|-z1-|-x2-|-y2-|-z2-|
        /// x1: original polygon vertices (will get changed)
        /// y1, z1: original submesh/hole vertices (will get changed)
        /// x2,y2,z2: chamfer and side wall vertices
        /// originalPolygonPointIndexer: index of the point we are currently processing. whole loop works on the original
        /// polygon points so this is the anchor point. it should move in (x1,z1) range.
        /// addedPointCount: pointer for x2|y2|z2 section start. pointer for where the second ring of submesh starts.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="normals"></param>
        /// <param name="feature"></param>
        /// <param name="trilist"></param>
        /// <param name="triIndex"></param>
        /// <param name="height"></param>
        private int Chamfer(
            Span<Vector3> vertices,
            Span<Vector3> normals,
            FeatureVertexData vertexData,
            int[] trilist,
            int triIndex,
            float minHeight,
            float height,
            float scaledOffset,
            int startIndex)
        {
            int polyPointCount = vertexData.VertexCount;
            int originalPolygonPointIndexer = 0;
            int addedPointCount = 0;
            const int vertPerStep = 4;

            // Local caches
            Vector3 v1, v2, n1, n2, pij1, pij2, pjk1, pjk2, poi;
            Vector3 prev, curr, next;
            var verts = vertexData.Vertices;
            var submeshes = vertexData.Submeshes;

            for (int i = 1; i < submeshes.Count; i++)
            {
                int start = submeshes[i - 1];
                int end = submeshes[i];
                int count = end - start;
                var subSpan = verts.AsSpan(start, count);

                int secondRingStartIndex = polyPointCount + addedPointCount;

                for (int j = 0; j < count; j++)
                {
                    int jPrev = (j == 0) ? count - 1 : j - 1;
                    int jNext = (j == count - 1) ? 0 : j + 1;

                    prev = subSpan[jPrev];
                    curr = subSpan[j];
                    next = subSpan[jNext];

                    // ---- precompute normalized edge directions ----
                    v1.x = curr.x - prev.x;
                    v1.y = 0f;
                    v1.z = curr.z - prev.z;
                    v1 = NormalizeXZ(v1);
                    n1 = PerpXZ(v1);
                    v1.x *= scaledOffset;
                    v1.z *= scaledOffset;

                    pij1.x = prev.x - n1.x * scaledOffset;
                    pij1.y = 0f;
                    pij1.z = prev.z - n1.z * scaledOffset;

                    pij2.x = curr.x - n1.x * scaledOffset;
                    pij2.y = 0f;
                    pij2.z = curr.z - n1.z * scaledOffset;

                    v2.x = next.x - curr.x;
                    v2.y = 0f;
                    v2.z = next.z - curr.z;
                    v2 = NormalizeXZ(v2);
                    n2 = PerpXZ(v2);
                    v2.x *= scaledOffset;
                    v2.z *= scaledOffset;

                    pjk1.x = curr.x - n2.x * scaledOffset;
                    pjk1.y = 0f;
                    pjk1.z = curr.z - n2.z * scaledOffset;

                    pjk2.x = next.x - n2.x * scaledOffset;
                    pjk2.y = 0f;
                    pjk2.z = next.z - n2.z * scaledOffset;

                    // ---- intersection ----
                    // Parallel offset edges have no single crossing point, and that is not an error:
                    // it is what a collinear corner looks like, which redundant points in a footprint
                    // make common. The corner's own offset is the right chamfer vertex there - the
                    // same answer the exactly-coincident case gives.
                    if (!TryFindIntersection(pij1, pij2, pjk1, pjk2, out poi))
                        poi = pij2;

                    int vertBase = secondRingStartIndex + vertPerStep * j;
                    int curIdx = originalPolygonPointIndexer + j;

                    Vector3 vertCurr = vertices[curIdx];
                    float yTop = vertCurr.y + height;
                    float yMin = vertCurr.y + minHeight;

                    // write vertices (inline, minimal constructors)
                    vertices[vertBase] = new Vector3(vertCurr.x - v1.x, yTop, vertCurr.z - v1.z);
                    vertices[vertBase + 1] = new Vector3(vertCurr.x + v2.x, yTop, vertCurr.z + v2.z);
                    vertices[vertBase + 2] = new Vector3(vertCurr.x - v1.x, yMin, vertCurr.z - v1.z);
                    vertices[vertBase + 3] = new Vector3(vertCurr.x + v2.x, yMin, vertCurr.z + v2.z);

                    // move original vertex
                    vertices[curIdx] = new Vector3(poi.x, yTop + scaledOffset, poi.z);

                    // normals
                    normals[curIdx] = Up;
                    normals[vertBase] = n1;
                    normals[vertBase + 1] = n2;
                    normals[vertBase + 2] = n1;
                    normals[vertBase + 3] = n2;

                    // ---- triangles ----
                    int si = startIndex;
                    int baseA = si + curIdx;
                    int baseB = si + vertBase;

                    trilist[triIndex++] = baseA;
                    trilist[triIndex++] = baseB;
                    trilist[triIndex++] = baseB + 1;

                    bool notLast = j < count - 1;
                    if (notLast)
                    {
                        int nextTri = j + 1;
                        int baseNext = si + originalPolygonPointIndexer + nextTri;
                        int baseBnext = si + secondRingStartIndex + vertPerStep * nextTri;

                        trilist[triIndex++] = baseNext;
                        trilist[triIndex++] = baseA;
                        trilist[triIndex++] = baseBnext;

                        trilist[triIndex++] = baseA;
                        trilist[triIndex++] = baseB + 1;
                        trilist[triIndex++] = baseBnext;

                        trilist[triIndex++] = baseB + 1;
                        trilist[triIndex++] = baseB + 3;
                        trilist[triIndex++] = baseBnext;

                        trilist[triIndex++] = baseBnext;
                        trilist[triIndex++] = baseB + 3;
                        trilist[triIndex++] = baseBnext + 2;
                    }
                    else
                    {
                        int baseFirst = si + originalPolygonPointIndexer;
                        int baseBfirst = si + secondRingStartIndex;

                        trilist[triIndex++] = baseFirst;
                        trilist[triIndex++] = baseA;
                        trilist[triIndex++] = baseBfirst;

                        trilist[triIndex++] = baseBfirst;
                        trilist[triIndex++] = baseA;
                        trilist[triIndex++] = baseB + 1;

                        trilist[triIndex++] = baseB + 1;
                        trilist[triIndex++] = baseB + 3;
                        trilist[triIndex++] = baseBfirst + 2;

                        trilist[triIndex++] = baseBfirst;
                        trilist[triIndex++] = baseB + 1;
                        trilist[triIndex++] = baseBfirst + 2;
                    }

                    trilist[triIndex++] = si + vertBase;
                    trilist[triIndex++] = si + vertBase + 2;
                    trilist[triIndex++] = si + vertBase + 3;

                    trilist[triIndex++] = si + vertBase + 1;
                    trilist[triIndex++] = si + vertBase;
                    trilist[triIndex++] = si + vertBase + 3;
                }

                originalPolygonPointIndexer += subSpan.Length;
                addedPointCount += vertPerStep * subSpan.Length;
            }

            return triIndex;
        }
        
        /// <summary>
        /// Crossing point of two XZ lines, or false when they are parallel or nearly so.
        ///
        /// This used to answer a NaN-filled vector for the parallel case, and every caller wrote the
        /// result straight into the vertex buffer. A collinear corner - two consecutive footprint
        /// edges pointing the same way, which redundant OSM points produce constantly - therefore
        /// put NaN into a building's X and Z while its Y stayed finite, and Unity reported it later
        /// as "abnormal mesh bounds" against an anonymous mesh, far from here. Returning false makes
        /// the caller choose a fallback instead of silently propagating a non-number.
        /// </summary>
        private static bool TryFindIntersection(
            Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4,
            out Vector3 intersection)
        {
            intersection = default;

            float dx12 = p2.x - p1.x;
            float dz12 = p2.z - p1.z;
            float dx34 = p4.x - p3.x;
            float dz34 = p4.z - p3.z;

            float denominator = dz12 * dx34 - dx12 * dz34;

            // Parallel or nearly parallel: no single intersection exists.
            if (Mathf.Abs(denominator) < 1e-8f)
            {
                return false;
            }

            float dx13 = p1.x - p3.x;
            float dz13 = p1.z - p3.z;

            float t1 = (dx13 * dz34 + (p3.z - p1.z) * dx34) / denominator;

            intersection = new Vector3(
                p1.x + dx12 * t1,
                0f,
                p1.z + dz12 * t1
            );

            return true;
        }
        
        public int CalculateTriCountFor(int totalPointCount)
        {
            return totalPointCount * 21; //7 tris , 3 int per
        }
    }
}