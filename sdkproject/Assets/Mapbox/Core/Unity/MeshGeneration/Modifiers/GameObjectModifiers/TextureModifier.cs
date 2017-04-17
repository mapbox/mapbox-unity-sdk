namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Components;

    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Texture Modifier")]
    public class TextureModifier : GameObjectModifier
    {
        [SerializeField]
        private bool _textureTop;
        [SerializeField]
        private bool _useSatelliteTexture;
        [SerializeField]
        private Material[] _topMaterials;

        [SerializeField]
        private bool _textureSides;
        [SerializeField]
        private Material[] _sideMaterials;

        public override void Run(FeatureBehaviour fb)
        {
            var ts = fb.gameObject.AddComponent<TextureSelector>();
            ts.Initialize(fb, _textureTop, _useSatelliteTexture, _topMaterials, _textureSides, _sideMaterials);
        }
    }
}
