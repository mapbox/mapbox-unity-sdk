// SPDX-FileCopyrightText: 2026 Mapbox
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mapbox.LandmarksModule.FeatureId
{
    /// <summary>
    /// Writes decoded feature ID colors onto meshes produced by glTFast.
    /// glTFast builds meshes with the low-level mesh API (SetVertexBufferParams /
    /// SetVertexBufferData), so Color has to be added as an additional vertex stream rather
    /// than through <see cref="Mesh.colors"/>.
    /// </summary>
    public static class FeatureIdMeshWriter
    {
        private static bool _warned;

        private const MeshUpdateFlags UpdateFlags =
            MeshUpdateFlags.DontRecalculateBounds |
            MeshUpdateFlags.DontValidateIndices |
            MeshUpdateFlags.DontNotifyMeshUsers |
            MeshUpdateFlags.DontResetBoneBounds;

        /// <summary>
        /// Applies the feature ID colors for one glTF mesh to its converted Unity mesh.
        /// One Unity mesh may merge several glTF primitives into sub-meshes, so each primitive is
        /// unpacked directly into its own range of a single buffer, in sub-mesh order.
        /// </summary>
        /// <param name="mesh">The converted Unity mesh.</param>
        /// <param name="meshIndex">Original glTF mesh index.</param>
        /// <param name="primitiveIndices">Original glTF primitive index per sub-mesh.</param>
        /// <param name="table">Feature ID data for the tile.</param>
        /// <returns>True when colors were written.</returns>
        public static bool Apply(Mesh mesh, int meshIndex, int[] primitiveIndices, FeatureIdColorTable table)
        {
            if (mesh == null || table == null || primitiveIndices == null || primitiveIndices.Length == 0)
            {
                return false;
            }

            var hasAnyFeatureId = false;
            for (var i = 0; i < primitiveIndices.Length; i++)
            {
                if (table.TryGetCount(meshIndex, primitiveIndices[i], out _))
                {
                    hasAnyFeatureId = true;
                    break;
                }
            }

            if (!hasAnyFeatureId)
            {
                return false;
            }

            var totalVertices = mesh.vertexCount;

            // Cleared, so primitives without feature IDs keep the default color. This is the only
            // Color-sized allocation per mesh: primitives are decoded straight into their range.
            var colors = new NativeArray<Color>(totalVertices, Allocator.Temp, NativeArrayOptions.ClearMemory);
            try
            {
                var vertexOffset = 0;
                for (var i = 0; i < primitiveIndices.Length; i++)
                {
                    if (table.TryGetCount(meshIndex, primitiveIndices[i], out var count))
                    {
                        // Assumes glTFast kept one converted vertex per accessor element for this
                        // sub-mesh. If it ever de-duplicates or splits, colors would shift; warn
                        // rather than silently mis-tint.
                        if (i < mesh.subMeshCount && count != mesh.GetSubMesh(i).vertexCount)
                        {
                            WarnOnce(
                                $"Landmark feature ID vertex count mismatch on glTF mesh {meshIndex} " +
                                $"primitive {primitiveIndices[i]}: {count} decoded vs " +
                                $"{mesh.GetSubMesh(i).vertexCount} in the sub-mesh. Colors may be misaligned.");
                        }

                        var writable = Math.Min(count, totalVertices - vertexOffset);
                        if (writable > 0)
                        {
                            table.Write(meshIndex, primitiveIndices[i], colors, vertexOffset, writable);
                        }

                        vertexOffset += writable;
                    }
                    else if (i < mesh.subMeshCount)
                    {
                        // No feature ID for this primitive; leave its vertices at the default color.
                        vertexOffset += mesh.GetSubMesh(i).vertexCount;
                    }
                }

                WriteColorStream(mesh, colors);
            }
            finally
            {
                colors.Dispose();
            }

            return true;
        }

        /// <summary>
        /// Logs a diagnostic once per session, so a systematic mismatch is visible without
        /// flooding the console from a per-tile code path.
        /// </summary>
        private static void WarnOnce(string message)
        {
            if (_warned)
            {
                return;
            }

            _warned = true;
            Debug.LogWarning(message);
        }

        /// <summary>
        /// Adds or overwrites the Color vertex stream on a mesh built with the low-level mesh API.
        /// When Color is absent the existing streams are read back, the layout is extended with a
        /// new Color stream, and all data is re-uploaded.
        /// </summary>
        private static void WriteColorStream(Mesh mesh, NativeArray<Color> colors)
        {
            var existingAttributes = mesh.GetVertexAttributes();

            var maxStream = 0;
            var existingColorStream = -1;

            for (var i = 0; i < existingAttributes.Length; i++)
            {
                if (existingAttributes[i].stream > maxStream)
                {
                    maxStream = existingAttributes[i].stream;
                }

                if (existingAttributes[i].attribute == VertexAttribute.Color)
                {
                    existingColorStream = existingAttributes[i].stream;
                }
            }

            if (existingColorStream >= 0)
            {
                mesh.SetVertexBufferData(
                    colors, 0, 0, colors.Length, existingColorStream, UpdateFlags);
                return;
            }

            // Re-setting the vertex layout clears vertex data, so cache every stream first.
            var streamDataCache = new NativeArray<byte>[maxStream + 1];
            using (var readMeshData = Mesh.AcquireReadOnlyMeshData(mesh))
            {
                var readData = readMeshData[0];
                for (var stream = 0; stream <= maxStream; stream++)
                {
                    var streamData = readData.GetVertexData<byte>(stream);
                    streamDataCache[stream] =
                        new NativeArray<byte>(streamData.Length, Allocator.Temp);
                    NativeArray<byte>.Copy(streamData, streamDataCache[stream]);
                }
            }

            var colorStream = maxStream + 1;
            var newAttributes = new VertexAttributeDescriptor[existingAttributes.Length + 1];
            Array.Copy(existingAttributes, newAttributes, existingAttributes.Length);
            newAttributes[existingAttributes.Length] = new VertexAttributeDescriptor(
                VertexAttribute.Color, VertexAttributeFormat.Float32, 4, colorStream);

            mesh.SetVertexBufferParams(mesh.vertexCount, newAttributes);

            for (var stream = 0; stream <= maxStream; stream++)
            {
                mesh.SetVertexBufferData(
                    streamDataCache[stream], 0, 0, streamDataCache[stream].Length, stream, UpdateFlags);
                streamDataCache[stream].Dispose();
            }

            mesh.SetVertexBufferData(colors, 0, 0, colors.Length, colorStream, UpdateFlags);
        }
    }
}
