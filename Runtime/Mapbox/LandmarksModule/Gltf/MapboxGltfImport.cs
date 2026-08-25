// SPDX-FileCopyrightText: 2024 Mapbox
// SPDX-License-Identifier: Apache-2.0

using GLTFast;
using GLTFast.Loading;
using GLTFast.Logging;
using GLTFast.Materials;
using GLTFast.Schema;
using Mapbox.LandmarksModule.FeatureId.Schema;
using Newtonsoft.Json;

namespace Mapbox.LandmarksModule
{
    /// <summary>
    /// Custom GltfImport subclass that adds feature ID support.
    /// Uses a custom schema chain (FeatureIdRoot -> FeatureIdMesh -> FeatureIdMeshPrimitive)
    /// to capture custom vertex attributes like _FEATURE_ID_RGBA4444 via JsonExtensionData,
    /// without modifying any core library schema files.
    /// The attribute data itself is decoded from the tile's own GLB bytes; see
    /// <see cref="FeatureId.FeatureIdColorTable"/>.
    /// </summary>
    public class MapboxGltfImport : GltfImportBase<FeatureIdRoot>
    {
        /// <inheritdoc cref="GltfImportBase(IDownloadProvider,IDeferAgent,IMaterialGenerator,ICodeLogger)"/>
        public MapboxGltfImport(
            IDownloadProvider downloadProvider = null,
            IDeferAgent deferAgent = null,
            IMaterialGenerator materialGenerator = null,
            ICodeLogger logger = null
        ) : base(downloadProvider, deferAgent, materialGenerator, logger)
        {
        }

        /// <inheritdoc />
        protected override RootBase ParseJson(string json)
        {
            return JsonConvert.DeserializeObject<FeatureIdRoot>(json);
        }
    }
}
