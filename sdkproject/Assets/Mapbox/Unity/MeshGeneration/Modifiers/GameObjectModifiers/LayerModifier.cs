namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Components;
	using Mapbox.Unity.MeshGeneration.Data;

    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Layer Modifier")]
    public class LayerModifier : GameObjectModifier
    {
        [SerializeField]
        private int _layerId;

		public override void Run(VectorEntity ve, UnityTile tile)
        {
            ve.GameObject.layer = _layerId;
        }
    }
}
