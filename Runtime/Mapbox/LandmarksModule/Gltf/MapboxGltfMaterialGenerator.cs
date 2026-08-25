using GLTFast;
using GLTFast.Logging;
using GLTFast.Materials;
using GLTFast.Schema;
using UnityEngine;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;

namespace Mapbox.LandmarkModule
{
    public class MapboxGltfMaterialGenerator : ShaderGraphMaterialGenerator
    {
        private Material _buildingMaterial;
        private static readonly int OcclusionMap = Shader.PropertyToID("_OcclusionMap");
        private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");

        public MapboxGltfMaterialGenerator(Material unityContextBuildingMaterial)
        {
            _buildingMaterial = unityContextBuildingMaterial != null
                ? unityContextBuildingMaterial
                : new Material(Shader.Find("Standard"));
        }

        protected override Material GenerateDefaultMaterial(bool pointsSupport = false)
        {
            //Debug.Log("GenerateDefaultMaterial ran");
            return _buildingMaterial;
        }

        public override Material GenerateMaterial(MaterialBase gltfMaterial, IGltfReadable gltf,
            bool pointsSupport = false)
        {
            
            MaterialType? materialType;
            var shaderMode = ShaderMode.Opaque;

            bool isUnlit = gltfMaterial.Extensions?.KHR_materials_unlit != null;
            bool isSpecularGlossiness = gltfMaterial.Extensions?.KHR_materials_pbrSpecularGlossiness != null;
            
            var metallicShaderFeatures = GetMetallicShaderFeatures(gltfMaterial);
            var material = GameObject.Instantiate(_buildingMaterial);
            shaderMode = (ShaderMode)(metallicShaderFeatures & MetallicShaderFeatures.ModeMask);
            
            
            if(material==null) return null;

            material.name = gltfMaterial.name;

            Color baseColorLinear = Color.white;
            RenderQueue? renderQueue = null;

            //added support for KHR_materials_pbrSpecularGlossiness
            
            if (gltfMaterial.PbrMetallicRoughness!=null
                // If there's a specular-glossiness extension, ignore metallic-roughness
                // (according to extension specification)
                && gltfMaterial.Extensions?.KHR_materials_pbrSpecularGlossiness == null)
            {
                //baseColorLinear = gltfMaterial.PbrMetallicRoughness.BaseColor;
                
                material.SetFloat(MaterialProperty.Metallic, gltfMaterial.PbrMetallicRoughness.metallicFactor );
                material.SetFloat(MaterialProperty.RoughnessFactor, gltfMaterial.PbrMetallicRoughness.roughnessFactor );

                if(TrySetTexture(
                    gltfMaterial.PbrMetallicRoughness.MetallicRoughnessTexture,
                    material,
                    gltf,
                    MaterialProperty.MetallicRoughnessMap,
                    MaterialProperty.MetallicRoughnessMapScaleTransform,
                    MaterialProperty.MetallicRoughnessMapRotation,
                    MaterialProperty.MetallicRoughnessMapTexCoord
                    )) {
                    // material.EnableKeyword(KW_METALLIC_ROUGHNESS_MAP);
                }

                // TODO: When the occlusionTexture equals the metallicRoughnessTexture, we could sample just once instead of twice.
                // if (!DifferentIndex(gltfMaterial.occlusionTexture,gltfMaterial.pbrMetallicRoughness.metallicRoughnessTexture)) {
                //    ...
                // }
            }

            if(TrySetTexture(
                gltfMaterial.NormalTexture,
                material,
                gltf,
                MaterialProperty.NormalTexture,
                MaterialProperty.NormalTextureScaleTransform,
                MaterialProperty.NormalTextureRotation,
                MaterialProperty.NormalTextureTexCoord
                )) {
                // material.EnableKeyword(ShaderKeyword.normalMap);
                material.SetFloat(MaterialProperty.NormalTextureScale,gltfMaterial.NormalTexture.scale);
            }

            if(TrySetTexture(
                gltfMaterial.OcclusionTexture,
                material,
                gltf,
                MaterialProperty.OcclusionTexture,
                MaterialProperty.OcclusionTextureScaleTransform,
                MaterialProperty.OcclusionTextureRotation,
                MaterialProperty.OcclusionTextureTexCoord
                )) {
                material.EnableKeyword(k_OcclusionKeyword);
                material.SetFloat(MaterialProperty.OcclusionTextureStrength,gltfMaterial.OcclusionTexture.strength);
            }

            if(TrySetTexture(
                gltfMaterial.EmissiveTexture,
                material,
                gltf,
                MaterialProperty.EmissiveTexture,
                MaterialProperty.EmissiveTextureScaleTransform,
                MaterialProperty.EmissiveTextureRotation,
                MaterialProperty.EmissiveTextureTexCoord
                )) {
                material.EnableKeyword(k_EmissiveKeyword);
            }

            if (gltfMaterial.Extensions != null) {

                // Transmission - Approximation
                var transmission = gltfMaterial.Extensions.KHR_materials_transmission;
                if (transmission != null) {
                    renderQueue = ApplyTransmission(ref baseColorLinear, gltf, transmission, material, null);
                }
            }

            if (gltfMaterial.GetAlphaMode() == MaterialBase.AlphaMode.Mask) {
                SetAlphaModeMask(gltfMaterial, material);
#if USING_HDRP
                if (gltfMaterial.Extensions?.KHR_materials_unlit != null) {
                    renderQueue = RenderQueue.Transparent;
                } else
#endif
                renderQueue = RenderQueue.AlphaTest;
            } else {
                material.SetFloat(MaterialProperty.AlphaCutoff, 0);
                // double sided opaque would make errors in HDRP 7.3 otherwise
                material.SetOverrideTag(MotionVectorTag,MotionVectorUser);
                material.SetShaderPassEnabled(MotionVectorsPass,false);
            }
            if (!renderQueue.HasValue) {
                if(shaderMode == ShaderMode.Opaque) {
                    renderQueue = gltfMaterial.GetAlphaMode() == MaterialBase.AlphaMode.Mask
                        ? RenderQueue.AlphaTest
                        : RenderQueue.Geometry;
                } else {
                    renderQueue = RenderQueue.Transparent;
                }
            }

            material.renderQueue = (int) renderQueue.Value;

            if (gltfMaterial.doubleSided) {
                SetDoubleSided(gltfMaterial, material);
            }

            switch (shaderMode) {
                case ShaderMode.Opaque:
                    SetShaderModeOpaque(gltfMaterial, material);
                    break;
                case ShaderMode.Blend:
                    SetShaderModeBlend(gltfMaterial, material);
                    break;
                case ShaderMode.Premultiply:
                    SetShaderModePremultiply(gltfMaterial, material);
                    break;
            }

            material.SetVector(MaterialProperty.BaseColor, baseColorLinear.gamma);

            if(gltfMaterial.Emissive != Color.black) {
                material.SetColor(MaterialProperty.EmissiveFactor, gltfMaterial.Emissive);
                material.EnableKeyword(k_EmissiveKeyword);
            }

            if (gltfMaterial.Extensions?.KHR_materials_clearcoat?.clearcoatFactor > 0)
            {
                var clearcoat = gltfMaterial.Extensions.KHR_materials_clearcoat;
                material.SetFloat(ClearcoatProperty, clearcoat.clearcoatFactor);
                TrySetTexture(clearcoat.clearcoatTexture,
                    material,
                    gltf,
                    ClearcoatTextureProperty,
                    ClearcoatTextureScaleTransformProperty,
                    ClearcoatTextureRotationProperty,
                    ClearcoatTextureTexCoordProperty);
                material.SetFloat(ClearcoatRoughnessProperty, clearcoat.clearcoatRoughnessFactor);
                material.EnableKeyword(k_ClearcoatKeyword);
                TrySetTexture(clearcoat.clearcoatRoughnessTexture,
                    material,
                    gltf,
                    ClearcoatRoughnessTextureProperty,
                    ClearcoatRoughnessTextureScaleTransformProperty,
                    ClearcoatRoughnessTextureRotationProperty,
                    ClearcoatRoughnessTextureTexCoordProperty);
                // The scale only exists when the normal texture does, so it has to stay inside the
                // TrySetTexture guard, matching upstream ShaderGraphMaterialGenerator.
                if (TrySetTexture(clearcoat.clearcoatNormalTexture,
                        material,
                        gltf,
                        ClearcoatNormalTextureProperty,
                        ClearcoatNormalTextureScaleTransformProperty,
                        ClearcoatNormalTextureRotationProperty,
                        ClearcoatNormalTextureTexCoordProperty))
                {
                    material.SetFloat(ClearcoatNormalTextureScaleProperty, clearcoat.clearcoatNormalTexture.scale);
                }
            }

            return material;
        }
        
        readonly int k_BaseMapPropId = Shader.PropertyToID("baseColorTexture");
        readonly int k_BaseMapScaleTransformPropId = Shader.PropertyToID("baseColorTexture_ST"); //TODO: support in shader!
        readonly int k_BaseMapRotationPropId = Shader.PropertyToID("baseColorTexture_Rotation"); //TODO; support in shader!
        readonly int k_BaseMapUVChannelPropId = Shader.PropertyToID("baseColorTexture_texCoord"); //TODO; support in shader!
        const string k_OcclusionKeyword = "_OCCLUSION";
        const string k_EmissiveKeyword = "_EMISSIVE";
        const string k_ClearcoatKeyword = "_CLEARCOAT";
    }
}