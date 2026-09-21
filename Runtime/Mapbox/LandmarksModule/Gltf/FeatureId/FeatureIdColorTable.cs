// SPDX-FileCopyrightText: 2026 Mapbox
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using GLTFast.Schema;
using Mapbox.LandmarksModule.FeatureId.Schema;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using UnityEngine;

namespace Mapbox.LandmarksModule.FeatureId
{
    /// <summary>
    /// Feature ID data for one landmark tile, built from the tile's own GLB bytes rather than
    /// from glTFast's buffers, so it does not depend on glTFast keeping volatile load data alive
    /// past the end of <c>Load</c>.
    /// <para>
    /// The decompressed bytes are held as-is and unpacked to <see cref="Color"/> only once, in
    /// <see cref="Write"/>, straight into the caller's final vertex buffer. Holding ~430 KB of
    /// bytes per tile is far cheaper than materializing a <c>Color</c> array per primitive
    /// (16 bytes per vertex, then copied again to group by mesh).
    /// </para>
    /// Owns native memory: dispose once scene instantiation for the tile has finished.
    /// </summary>
    internal class FeatureIdColorTable : IDisposable
    {
        private const string AttributeName = "_FEATURE_ID_RGBA4444";

        private readonly struct Entry
        {
            public readonly int BufferView;
            public readonly int ByteOffset;
            public readonly int Count;

            public Entry(int bufferView, int byteOffset, int count)
            {
                BufferView = bufferView;
                ByteOffset = byteOffset;
                Count = count;
            }
        }

        private readonly Dictionary<(int meshIndex, int primitiveIndex), Entry> _entries =
            new Dictionary<(int meshIndex, int primitiveIndex), Entry>();

        private Dictionary<int, NativeArray<byte>> _decodedViews;

        /// <summary>Number of primitives carrying feature ID data.</summary>
        public int Count => _entries.Count;

        /// <summary>
        /// Gets the number of vertices of feature ID data available for one glTF primitive.
        /// </summary>
        public bool TryGetCount(int meshIndex, int primitiveIndex, out int count)
        {
            if (_entries.TryGetValue((meshIndex, primitiveIndex), out var entry))
            {
                count = entry.Count;
                return true;
            }

            count = 0;
            return false;
        }

        /// <summary>
        /// Unpacks one primitive's feature IDs directly into <paramref name="destination"/>.
        /// </summary>
        /// <param name="meshIndex">Original glTF mesh index.</param>
        /// <param name="primitiveIndex">Original glTF primitive index.</param>
        /// <param name="destination">Final color buffer for the whole Unity mesh.</param>
        /// <param name="destinationOffset">First index to write.</param>
        /// <param name="count">Number of vertices to write; must not exceed the stored count.</param>
        /// <returns>True when data was written.</returns>
        public bool Write(
            int meshIndex,
            int primitiveIndex,
            NativeArray<Color> destination,
            int destinationOffset,
            int count)
        {
            if (count <= 0
                || _decodedViews == null
                || !_entries.TryGetValue((meshIndex, primitiveIndex), out var entry)
                || !_decodedViews.TryGetValue(entry.BufferView, out var view))
            {
                return false;
            }

            var byteCount = count * 4;
            if (count > entry.Count
                || entry.ByteOffset < 0
                || entry.ByteOffset + byteCount > view.Length
                || destinationOffset < 0
                || destinationOffset + count > destination.Length)
            {
                return false;
            }

            FeatureIdDecoder.Decode(
                new NativeSlice<byte>(view, entry.ByteOffset, byteCount),
                destination,
                destinationOffset,
                count);
            return true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_decodedViews == null)
            {
                return;
            }

            foreach (var view in _decodedViews.Values)
            {
                if (view.IsCreated)
                {
                    view.Dispose();
                }
            }

            _decodedViews = null;
            _entries.Clear();
        }

        /// <summary>
        /// Decompresses the buffer views holding feature IDs out of the tile's GLB bytes and
        /// records where each primitive's data sits inside them. Feature IDs occupy their own
        /// meshopt buffer view, so this duplicates none of glTFast's geometry decoding.
        /// </summary>
        /// <param name="root">The tile's deserialized glTF root, from the custom feature ID schema.</param>
        /// <param name="glb">Raw GLB bytes of the tile.</param>
        /// <returns>A populated table the caller must dispose, or null when the tile has no feature IDs.</returns>
        public static FeatureIdColorTable Build(RootBase root, byte[] glb)
        {
#if MESHOPT_IS_RECENT
            if (root?.Meshes == null || root.Accessors == null || root.BufferViews == null)
            {
                return null;
            }

            if (!GlbBinaryChunk.TryFind(glb, out var binOffset, out var binLength))
            {
                return null;
            }

            FeatureIdColorTable table = null;

            for (var meshIndex = 0; meshIndex < root.Meshes.Count; meshIndex++)
            {
                var primitives = root.Meshes[meshIndex]?.Primitives;
                if (primitives == null)
                {
                    continue;
                }

                for (var primitiveIndex = 0; primitiveIndex < primitives.Count; primitiveIndex++)
                {
                    if (primitives[primitiveIndex] is not FeatureIdMeshPrimitive primitive)
                    {
                        continue;
                    }

                    var additionalAttributes = primitive.attributes?.AdditionalAttributes;
                    if (additionalAttributes == null
                        || !additionalAttributes.TryGetValue(AttributeName, out var token))
                    {
                        continue;
                    }

                    var accessorIndex = token.Value<int>();
                    if (accessorIndex < 0 || accessorIndex >= root.Accessors.Count)
                    {
                        continue;
                    }

                    var accessor = root.Accessors[accessorIndex];
                    if (accessor == null
                        || accessor.bufferView < 0
                        || accessor.bufferView >= root.BufferViews.Count)
                    {
                        continue;
                    }

                    var meshopt = root.BufferViews[accessor.bufferView]?.Extensions?.EXT_meshopt_compression;
                    if (meshopt == null)
                    {
                        // The landmarks tileset always meshopt-compresses feature IDs. An
                        // uncompressed view would need a plain buffer read instead.
                        continue;
                    }

                    table ??= new FeatureIdColorTable();
                    if (!table.CertifyViewDecoded(accessor.bufferView, meshopt, glb, binOffset, binLength))
                    {
                        continue;
                    }

                    var byteSize = accessor.ByteSize;
                    if (accessor.byteOffset < 0 || byteSize <= 0)
                    {
                        continue;
                    }

                    table._entries[(meshIndex, primitiveIndex)] =
                        new Entry(accessor.bufferView, accessor.byteOffset, accessor.count);
                }
            }

            if (table != null && table.Count == 0)
            {
                table.Dispose();
                return null;
            }

            return table;
#else
            WarnAboutMissingMeshopt();
            return null;
#endif
        }

#if MESHOPT_IS_RECENT
        /// <summary>
        /// Decompresses a buffer view once and caches it for the lifetime of this table.
        /// Persistent allocation, because the buffer is held across the async instantiation
        /// that follows and is released in <see cref="Dispose"/>.
        /// </summary>
        private bool CertifyViewDecoded(
            int bufferViewIndex,
            BufferViewMeshoptExtension meshopt,
            byte[] glb,
            int binOffset,
            int binLength)
        {
            _decodedViews ??= new Dictionary<int, NativeArray<byte>>();
            if (_decodedViews.ContainsKey(bufferViewIndex))
            {
                return true;
            }

            if (!MeshoptBufferViewDecoder.TryDecode(
                    glb, binOffset, binLength, meshopt, Allocator.Persistent, out var decoded))
            {
                return false;
            }

            _decodedViews[bufferViewIndex] = decoded;
            return true;
        }
#else
        private static bool _warnedAboutMissingMeshopt;

        /// <summary>
        /// Covers an outdated meshoptimizer package: the assembly reference still resolves, but
        /// glTFast compiles out <c>BufferViewMeshoptExtension</c> below its own
        /// <c>MESHOPT_IS_RECENT</c> threshold, so the decode path cannot be compiled either.
        /// glTFast separately fails the tile load, since the tiles require the extension.
        /// </summary>
        private static void WarnAboutMissingMeshopt()
        {
            if (_warnedAboutMissingMeshopt)
            {
                return;
            }

            _warnedAboutMissingMeshopt = true;
            Debug.LogWarning(
                "Landmark feature IDs are unavailable: the com.unity.meshopt.decompress package " +
                "is older than 0.2.0-exp.1 and does not expose the API this decode path needs. " +
                "Landmarks still draw, untinted by feature id. The package cannot be absent - " +
                "MapboxLandmarksModule requires it to compile at all.");
        }
#endif
    }
}
