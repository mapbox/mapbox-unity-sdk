using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.ComponentSystem.Data;
using Mapbox.VectorModule.MeshGeneration;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem.Modifiers
{
    public class ArrayPolygon
    {
        /// <summary>
        /// Buffer capacity that can hold any triangulation of a single feature, for callers
        /// sizing the <c>triList</c> they pass to <see cref="Polygonize"/>.
        ///
        /// Ear clipping yields at most (V + 2H - 2) triangles for V vertices across H holes,
        /// so that many triangles is an upper bound. One spare triangle is added on top,
        /// because <see cref="Polygonize"/> treats a buffer as full at
        /// <c>count + offset &gt;= length</c> and so needs a slot it never writes to.
        ///
        /// The result is counted in triangles and multiplied by three, so the capacity is
        /// always a multiple of three. Callers hand whole buffers to <c>Mesh.SetTriangles</c>
        /// with the unused tail left as zeroes (degenerate triangles that draw nothing), and
        /// that call rejects any length that is not a multiple of three. It is also why the
        /// visualizers' default pool size is 10002 rather than 10000.
        /// </summary>
        public static int WorstCaseIndexCount(FeatureVertexData vertexData)
        {
            // Submeshes holds ring boundaries, so there are Count - 1 rings and at most
            // Count - 2 of them are holes.
            var holeCount = Mathf.Max(0, vertexData.Submeshes.Count - 2);
            var triangleCount = Mathf.Max(0, vertexData.VertexCount + 2 * holeCount - 2);
            return (triangleCount + 1) * 3;
        }

        public int Polygonize(
            Span<Vector3> vertices,
            Span<Vector3> normals,
            int vertexAnchorIndex,
            int[] triList,
            int triIndex,
            FeatureVertexData vertexData,
            float pushUp = 0)
        {
            int arrayMaxLength = triList.Length;
            var submeshes = vertexData.Submeshes;
            var allVerts = vertexData.Vertices;
            int submeshCount = submeshes.Count;

            if (submeshCount < 2)
                return triIndex;

            // Save initial state for rollback on overflow
            int initialTriIndex = triIndex;

            var holes = new List<int>(8);
            int vertWriteIndex = 0;
            bool nextIsHole = false;

            int subStart = submeshes[0];
            int subEnd = submeshes[0];

            for (int i = 1; i < submeshCount; i++)
            {
                int prevStart = submeshes[i - 1];
                int prevEnd = submeshes[i];
                int prevLength = prevEnd - prevStart;

                var subVert = allVerts.AsSpan(prevStart, prevLength);

                // Detect the start of a new polygon (outer boundary)
                if (IsClockwise(subVert) && vertWriteIndex > 0)
                {
                    var span = allVerts.AsSpan(subStart, subEnd - subStart);
                    var flatData = EarcutLibrary.Flatten(span);
                    var result = EarcutLibrary.Earcut(flatData.Vertices, holes, 2);

                    if (result.Count + triIndex >= arrayMaxLength)
                    {
                        // Roll back partial triangles written by this feature
                        for (int j = initialTriIndex; j < triIndex; j++)
                            triList[j] = 0;
                        return -1;
                    }

                    for (int j = 0; j < result.Count; j++)
                        triList[triIndex++] = vertexAnchorIndex + result[j];

                    vertexAnchorIndex += flatData.Vertices.Length / 2;
                    holes.Clear();
                    nextIsHole = false;
                    subStart = prevStart; // new polygon begins here
                }

                subEnd = prevEnd;

                if (nextIsHole)
                    holes.Add(prevStart - subStart);

                nextIsHole = true;

                for (int j = 0; j < prevLength; j++)
                {
                    subVert[j].y += pushUp;
                    vertices[vertWriteIndex] = subVert[j];
                    normals[vertWriteIndex++] = Constants.Math.Vector3Up;
                }
            }

            // Final polygon flush
            var finalSpan = allVerts.AsSpan(subStart, subEnd - subStart);
            var finalFlat = EarcutLibrary.Flatten(finalSpan);
            var finalResult = EarcutLibrary.Earcut(finalFlat.Vertices, holes, 2);

            if (finalResult.Count + triIndex >= arrayMaxLength)
            {
                // Roll back partial triangles written by this feature
                for (int j = initialTriIndex; j < triIndex; j++)
                    triList[j] = 0;
                return -1;
            }

            for (int i = 0; i < finalResult.Count; i++)
                triList[triIndex++] = vertexAnchorIndex + finalResult[i];

            return triIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsClockwise(ReadOnlySpan<Vector3> verts)
        {
            double sum = 0.0;
            int count = verts.Length;

            for (int i = 0; i < count; i++)
            {
                var v1 = verts[i];
                var v2 = verts[(i + 1) % count];
                sum += (v2.x - v1.x) * (v2.z + v1.z);
            }

            return sum > 0.0;
        }


        // public int Polygonize(Span<Vector3> vertices, Span<Vector3> normals, int vertexAnchorIndex, int[] triList, int triIndex, PerfVectorFeatureUnity feature)
        // {
        //     var arrayMaxLength = triList.Length;
        //     var counter = feature.VertexData.Submeshes.Count;
        //     var holes = new List<int>();
        //     int vertStartIndex = 0;
        //     Data flatData;
        //     List<int> result;
        //     var subStart = 0;
        //     var subEnd = 0;
        //     var nextIsHole = false;
        //
        //     for (int i = 1; i < counter; i++)
        //     {
        //         var subVert = feature.VertexData.Vertices.AsSpan(feature.VertexData.Submeshes[i - 1],
        //             feature.VertexData.Submeshes[i] - feature.VertexData.Submeshes[i - 1]);
        //
        //         if (IsClockwise(subVert) && vertStartIndex > 0)
        //         {
        //             flatData = EarcutLibrary.Flatten(feature.VertexData.Vertices.AsSpan(subStart, subEnd - subStart));
        //             result = EarcutLibrary.Earcut(flatData.Vertices, holes, 2);
        //             
        //             if (result.Count + triIndex >= arrayMaxLength) return -1;
        //             
        //             for (int j = 0; j < result.Count; j++)
        //             {
        //                 triList[triIndex++] = (vertexAnchorIndex + result[j]);
        //             }
        //
        //             vertexAnchorIndex += flatData.Vertices.Length / 2;
        //             holes.Clear();
        //             nextIsHole = false;
        //             subStart = feature.VertexData.Submeshes[i - 1];
        //         }
        //
        //         subEnd = feature.VertexData.Submeshes[i];
        //         if(nextIsHole)
        //         {
        //             holes.Add(feature.VertexData.Submeshes[i - 1] - subStart);
        //         }
        //         nextIsHole = true;
        //
        //         for (int j = 0; j < subVert.Length; j++)
        //         {
        //             vertices[vertStartIndex + j] = subVert[j];
        //             normals[vertStartIndex + j] = Constants.Math.Vector3Up;
        //         }
        //
        //         vertStartIndex += subVert.Length;
        //     }
        //
        //     flatData = EarcutLibrary.Flatten(feature.VertexData.Vertices.AsSpan(subStart, subEnd - subStart));
        //     result = EarcutLibrary.Earcut(flatData.Vertices, holes, 2);
        //
        //     if (result.Count + triIndex >= arrayMaxLength) return -1;
        //     for (int i = 0; i < result.Count; i++)
        //     {
        //         triList[triIndex++] = (vertexAnchorIndex + result[i]);
        //     }
        //
        //     return triIndex;
        // }
        //
        // private bool IsClockwise(Span<Vector3> subVert)
        // {
        //     double sum = 0.0;
        //     var counter = subVert.Length;
        //     for (int i = 0; i < counter; i++)
        //     {
        //         var v1 = subVert[i];
        //         var v2 = subVert[(i + 1) % counter];
        //         sum += (v2.x - v1.x) * (v2.z + v1.z);
        //     }
        //
        //     return sum > 0.0;
        // }
    }
}