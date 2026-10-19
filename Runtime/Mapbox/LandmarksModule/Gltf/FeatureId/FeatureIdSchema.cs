// SPDX-FileCopyrightText: 2024 Mapbox
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Runtime.Serialization;
using GLTFast.Newtonsoft.Schema;
using GLTFast.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Scripting;
using MeshExtras = GLTFast.Newtonsoft.Schema.MeshExtras;
using MeshPrimitiveExtensions = GLTFast.Newtonsoft.Schema.MeshPrimitiveExtensions;

namespace Mapbox.LandmarksModule.FeatureId.Schema
{
    /// <summary>
    /// Extended Attributes class that captures custom/non-standard glTF vertex attributes
    /// (e.g. _FEATURE_ID_RGBA4444) via JsonExtensionData, without modifying the core schema.
    /// </summary>
    public class FeatureIdAttributes : Attributes
    {
        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalAttributes;
    }

    /// <summary>
    /// Custom MeshPrimitive that uses FeatureIdAttributes to capture extension vertex attributes.
    /// Shadows the base 'attributes' field so Newtonsoft deserializes into the extended type.
    /// </summary>
    public class FeatureIdMeshPrimitive : MeshPrimitiveBase<MeshPrimitiveExtensions>, IJsonObject
    {
        public new FeatureIdAttributes attributes;

        public UnclassifiedData extras;

        [JsonExtensionData]
        IDictionary<string, JToken> m_JsonExtensionData;

        [Preserve]
        public FeatureIdMeshPrimitive() {}

        /// <summary>
        /// After Newtonsoft deserializes into the shadowed 'attributes' field,
        /// sync it back to the base class field so glTFast's internal code
        /// (which accesses MeshPrimitiveBase.attributes) sees the data.
        /// </summary>
        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            ((MeshPrimitiveBase)this).attributes = attributes;
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            if (m_JsonExtensionData != null
                && m_JsonExtensionData.TryGetValue(key, out var token))
            {
                value = token.ToObject<T>();
                return true;
            }

            value = default;
            return false;
        }
    }

    /// <summary>
    /// Custom Mesh parameterized with FeatureIdMeshPrimitive.
    /// </summary>
    public class FeatureIdMesh : MeshBase<MeshExtras, FeatureIdMeshPrimitive>, IJsonObject
    {
        public UnclassifiedData extensions;

        [JsonExtensionData]
        IDictionary<string, JToken> m_JsonExtensionData;

        [Preserve]
        public FeatureIdMesh() {}

        public bool TryGetValue<T>(string key, out T value)
        {
            if (m_JsonExtensionData != null
                && m_JsonExtensionData.TryGetValue(key, out var token))
            {
                value = token.ToObject<T>();
                return true;
            }

            value = default;
            return false;
        }
    }

    /// <summary>
    /// Custom Root that swaps in FeatureIdMesh, enabling the full custom schema chain
    /// for feature ID support without any core library schema modifications.
    /// </summary>
    public class FeatureIdRoot : RootBase<
        GLTFast.Newtonsoft.Schema.Accessor,
        GLTFast.Newtonsoft.Schema.Animation,
        GLTFast.Newtonsoft.Schema.Asset,
        GLTFast.Newtonsoft.Schema.Buffer,
        GLTFast.Newtonsoft.Schema.BufferView,
        GLTFast.Newtonsoft.Schema.Camera,
        GLTFast.Newtonsoft.Schema.RootExtensions,
        GLTFast.Newtonsoft.Schema.Image,
        GLTFast.Newtonsoft.Schema.Material,
        FeatureIdMesh,
        GLTFast.Newtonsoft.Schema.Node,
        GLTFast.Newtonsoft.Schema.Sampler,
        GLTFast.Newtonsoft.Schema.Scene,
        GLTFast.Newtonsoft.Schema.Skin,
        GLTFast.Newtonsoft.Schema.Texture
    >, IJsonObject
    {
        public UnclassifiedData extras;

        [JsonExtensionData]
        IDictionary<string, JToken> m_JsonExtensionData;

        [Preserve]
        public FeatureIdRoot() {}

        public bool TryGetValue<T>(string key, out T value)
        {
            if (m_JsonExtensionData != null
                && m_JsonExtensionData.TryGetValue(key, out var token))
            {
                value = token.ToObject<T>();
                return true;
            }

            value = default;
            return false;
        }
    }
}
