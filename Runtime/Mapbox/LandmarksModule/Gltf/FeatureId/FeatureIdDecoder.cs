// SPDX-FileCopyrightText: 2024 Mapbox
// SPDX-License-Identifier: Apache-2.0

using Unity.Collections;
using UnityEngine;

namespace Mapbox.LandmarksModule.FeatureId
{
    /// <summary>
    /// Decodes packed _FEATURE_ID_RGBA4444 accessor data into Color values.
    /// The packed format: each vertex has a 4-byte (float-sized) value where
    /// lower 16 bits encode RGBA4444 color and upper 16 bits encode part ID.
    /// </summary>
    public static class FeatureIdDecoder
    {
        /// <summary>
        /// Unpacks raw accessor bytes straight into a destination color buffer.
        /// Writing into the caller's final buffer avoids an intermediate array per primitive.
        /// </summary>
        /// <param name="data">Raw accessor byte data (SCALAR float, 4 bytes per element).</param>
        /// <param name="destination">Buffer to write into.</param>
        /// <param name="destinationOffset">First index in <paramref name="destination"/> to write.</param>
        /// <param name="vertexCount">Number of vertices to decode.</param>
        public static void Decode(
            NativeSlice<byte> data,
            NativeArray<Color> destination,
            int destinationOffset,
            int vertexCount)
        {
            for (var i = 0; i < vertexCount; i++)
            {
                var offset = i * 4;
                var intValue = (data[offset]) |
                               (data[offset + 1] << 8) |
                               (data[offset + 2] << 16) |
                               (data[offset + 3] << 24);

                var color = intValue & 0xffff;

                float r = ((color & 0xF000) | (color & 0xF000) >> 4) >> 8;
                float g = ((color & 0x0F00) | (color & 0x0F00) >> 4) >> 4;
                float b = (color & 0x00F0) | (color & 0x00F0) >> 4;

                var id = (intValue >> 16) & 0xffff;
                var pid = id & 0xf;

                destination[destinationOffset + i] = new Color(r / 255f, g / 255f, b / 255f, pid);
            }
        }
    }
}
