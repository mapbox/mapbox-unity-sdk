namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using Mapbox.Unity.MeshGeneration.Data;
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Components;
    
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Collider Modifier")]
    public class ColliderModifier : GameObjectModifier
    {
        [SerializeField]
        private ColliderType _colliderType;

		public override void Run(VectorEntity ve, UnityTile tile)
        {
            switch (_colliderType)
            {
                case ColliderType.BoxCollider:
                    ve.GameObject.AddComponent<BoxCollider>();
                    break;
                case ColliderType.MeshCollider:
                    ve.GameObject.AddComponent<MeshCollider>();
                    break;
                case ColliderType.SphereCollider:
                    ve.GameObject.AddComponent<SphereCollider>();
                    break;
                default:
                    break;
            }
        }


        public enum ColliderType
        {
            None,
            BoxCollider,
            MeshCollider,
            SphereCollider
        }

    }
}
