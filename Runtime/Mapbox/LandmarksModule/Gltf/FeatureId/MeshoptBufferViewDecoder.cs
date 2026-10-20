// SPDX-FileCopyrightText: 2026 Mapbox
// SPDX-License-Identifier: Apache-2.0

#if MESHOPT_IS_RECENT
using GLTFast.Schema;
using Meshoptimizer;
using Unity.Collections;

namespace Mapbox.LandmarksModule.FeatureId
{
    /// <summary>
    /// Decodes a single EXT_meshopt_compression buffer view straight out of the tile's GLB bytes,
    /// using the same public meshoptimizer API that glTFast itself calls.
    /// This keeps feature ID extraction independent of glTFast's internal buffer lifetime, which
    /// ends when loading completes.
    /// </summary>
    internal static class MeshoptBufferViewDecoder
    {
        /// <summary>
        /// Decompresses one meshopt buffer view.
        /// </summary>
        /// <param name="glb">Raw GLB bytes of the tile.</param>
        /// <param name="binOffset">Offset of the GLB BIN chunk payload, from <see cref="GlbBinaryChunk"/>.</param>
        /// <param name="binLength">Length of the GLB BIN chunk payload.</param>
        /// <param name="meshopt">The buffer view's EXT_meshopt_compression parameters.</param>
        /// <param name="allocator">Allocator for the decoded buffer. Must be job-compatible.</param>
        /// <param name="decoded">The decompressed bytes. Caller owns and must dispose on success.</param>
        /// <returns>True on success. False when the view is unsupported or decoding failed.</returns>
        public static bool TryDecode(
            byte[] glb,
            int binOffset,
            int binLength,
            BufferViewMeshoptExtension meshopt,
            Allocator allocator,
            out NativeArray<byte> decoded)
        {
            decoded = default;

            if (meshopt == null)
            {
                return false;
            }

            // The compressed payload always lives in the GLB's own binary chunk. The buffer the
            // view nominally points at is the meshopt fallback buffer, which carries no data.
            if (meshopt.Buffer != 0)
            {
                return false;
            }

            var decodedLength = meshopt.count * meshopt.ByteStride;
            var sourceLength = meshopt.ByteLength;
            if (decodedLength <= 0 || sourceLength <= 0)
            {
                return false;
            }

            if (meshopt.ByteOffset < 0 || meshopt.ByteOffset + sourceLength > binLength)
            {
                return false;
            }

            var source = new NativeArray<byte>(sourceLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var destination = new NativeArray<byte>(decodedLength, allocator, NativeArrayOptions.UninitializedMemory);
            var decodeSucceeded = false;
            try
            {
                NativeArray<byte>.Copy(glb, binOffset + meshopt.ByteOffset, source, 0, sourceLength);

                var returnCode = Decode.DecodeGltfBufferSync(
                    destination,
                    meshopt.count,
                    meshopt.ByteStride,
                    source.AsReadOnly(),
                    meshopt.GetMode(),
                    meshopt.GetFilter());

                decodeSucceeded = returnCode == 0;
            }
            finally
            {
                source.Dispose();

                // Also covers the decode throwing, which would otherwise leak the destination.
                if (!decodeSucceeded)
                {
                    destination.Dispose();
                }
            }

            if (!decodeSucceeded)
            {
                return false;
            }

            decoded = destination;
            return true;
        }
    }
}
#endif
