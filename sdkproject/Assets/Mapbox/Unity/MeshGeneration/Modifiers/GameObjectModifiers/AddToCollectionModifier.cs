namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Components;
    using Mapbox.Unity.MeshGeneration.Data;

    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Add To Cache Modifier")]
    public class AddToCollectionModifier : GameObjectModifier
    {
        [SerializeField]
        private FeatureCollectionBase _collection;
		
        public override void Run(VectorEntity ve, UnityTile tile)
        {
			var ra = ve.GameObject.AddComponent<RangeAnimator>();
			ra.Initialize(ve.MeshRenderer);
			ve.RangeAnimator = ra;
			ve.TileScale = tile.TileScale;
			_collection.AddFeature(ve);
		}
    }
}
