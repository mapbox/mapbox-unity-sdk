namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
    using Mapbox.Unity.MeshGeneration.Components;
    using Mapbox.Unity.MeshGeneration.Interfaces;

    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Prefab Modifier")]
    public class PrefabModifier : GameObjectModifier
    {
        [SerializeField]
        private GameObject _prefab;

        [SerializeField]
        private bool _scaleDownWithWorld = false;

        public override void Run(VectorEntity ve, UnityTile tile)
        {
            int selpos = ve.Feature.Points[0].Count / 2;
            var met = ve.Feature.Points[0][selpos];
            var go = Instantiate(_prefab);
            go.name = ve.Feature.Data.Id.ToString();
            go.transform.position = met;
            go.transform.SetParent(ve.GameObject.transform, false);

            var bd = go.AddComponent<FeatureBehaviour>();
            bd.Init(ve.Feature);

            var tm = go.GetComponent<IFeaturePropertySettable>();
            if (tm != null)
            {
                tm.Set(ve.Feature.Properties);
            }

            if (!_scaleDownWithWorld)
            {
				go.transform.localScale = Vector3.one / tile.TileScale;
            }
        }
    }
}
