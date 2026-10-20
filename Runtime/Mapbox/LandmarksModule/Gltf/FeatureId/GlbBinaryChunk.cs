// SPDX-FileCopyrightText: 2026 Mapbox
// SPDX-License-Identifier: Apache-2.0

using System;

namespace Mapbox.LandmarksModule.FeatureId
{
    /// <summary>
    /// Locates the binary (BIN) chunk inside a GLB byte buffer.
    /// Landmark tiles arrive as self-contained GLB, so the buffer holding compressed
    /// payloads is always the BIN chunk. glTF assets with external buffer URIs are not
    /// supported here and are reported as "not found".
    /// </summary>
    internal static class GlbBinaryChunk
    {
        private const uint Magic = 0x46546C67; // "glTF"
        private const uint ChunkTypeBin = 0x004E4942; // "BIN\0"
        private const int HeaderLength = 12;
        private const int ChunkHeaderLength = 8;

        /// <summary>
        /// Finds the BIN chunk payload within a GLB buffer.
        /// </summary>
        /// <param name="glb">Raw GLB bytes.</param>
        /// <param name="offset">Byte offset of the BIN chunk payload within <paramref name="glb"/>.</param>
        /// <param name="length">Length of the BIN chunk payload in bytes.</param>
        /// <returns>True when a BIN chunk was found, false for non-GLB or truncated data.</returns>
        public static bool TryFind(byte[] glb, out int offset, out int length)
        {
            offset = 0;
            length = 0;

            if (glb == null || glb.Length < HeaderLength)
            {
                return false;
            }

            if (BitConverter.ToUInt32(glb, 0) != Magic)
            {
                return false;
            }

            // The header's total length is advisory; never read past the array we were handed.
            var total = (int) BitConverter.ToUInt32(glb, 8);
            if (total > glb.Length || total < HeaderLength)
            {
                total = glb.Length;
            }

            var cursor = HeaderLength;
            while (cursor + ChunkHeaderLength <= total)
            {
                var chunkLength = (int) BitConverter.ToUInt32(glb, cursor);
                var chunkType = BitConverter.ToUInt32(glb, cursor + 4);
                var payload = cursor + ChunkHeaderLength;

                if (chunkLength < 0 || payload + chunkLength > total)
                {
                    return false;
                }

                if (chunkType == ChunkTypeBin)
                {
                    offset = payload;
                    length = chunkLength;
                    return true;
                }

                cursor = payload + chunkLength;
            }

            return false;
        }
    }
}
