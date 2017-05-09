namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Components;

    /// <summary>
    /// Texture Modifier is a basic modifier which simply adds a TextureSelector script to the features.
    /// Logic is all pushed into this TextureSelector mono behaviour to make it's easier to change it in runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Layer Modifier")]
    public class LayerModifier : GameObjectModifier
    {
        [SerializeField]
        private int _layerId;

        public override void Run(FeatureBehaviour fb)
        {
            fb.gameObject.layer = _layerId;
        }
    }
}
