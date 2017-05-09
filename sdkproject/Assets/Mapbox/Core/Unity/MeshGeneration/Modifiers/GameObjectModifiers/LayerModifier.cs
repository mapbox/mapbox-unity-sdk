namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Components;
    


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
