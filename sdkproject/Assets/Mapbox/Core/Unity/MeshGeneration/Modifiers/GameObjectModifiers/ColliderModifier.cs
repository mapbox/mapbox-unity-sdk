namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Components;
    
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Collider Modifier")]
    public class ColliderModifier : GameObjectModifier
    {
        [SerializeField]
        private ColliderType _colliderType;

        public override void Run(FeatureBehaviour fb)
        {
            switch (_colliderType)
            {
                case ColliderType.BoxCollider:
                    fb.gameObject.AddComponent<BoxCollider>();
                    break;
                case ColliderType.MeshCollider:
                    fb.gameObject.AddComponent<MeshCollider>();
                    break;
                case ColliderType.SphereCollider:
                    fb.gameObject.AddComponent<SphereCollider>();
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
