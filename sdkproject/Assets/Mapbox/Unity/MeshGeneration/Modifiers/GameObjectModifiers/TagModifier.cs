namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Components;
    using Mapbox.Unity.MeshGeneration.Data;

    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Tag Modifier")]
    public class TagModifier : GameObjectModifier
    {
        [SerializeField]
        private string _tag;

        public override void Run(VectorEntity ve, UnityTile tile)
        {
            ve.GameObject.tag = _tag;
        }
    }
}
