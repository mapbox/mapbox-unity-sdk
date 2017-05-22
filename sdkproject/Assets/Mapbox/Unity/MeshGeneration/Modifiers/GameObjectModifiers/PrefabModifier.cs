namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Components;
    using Mapbox.Unity.MeshGeneration.Interfaces;

    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Prefab Modifier")]
    public class PrefabModifier : GameObjectModifier
    {
        [SerializeField]
        private GameObject _prefab;

        [SerializeField]
        private bool _scaleDownWithWorld = false;

        public override void Run(FeatureBehaviour fb)
        {
            int selpos = fb.Data.Points[0].Count / 2;
            var met = fb.Data.Points[0][selpos];
            var go = Instantiate(_prefab);
            go.name = fb.Data.Data.Id.ToString();
            go.transform.position = met;
            go.transform.SetParent(fb.transform, false);

            var bd = go.AddComponent<FeatureBehaviour>();
            bd.Init(fb.Data);

            var tm = go.GetComponent<IFeaturePropertySettable>();
            if (tm != null)
            {
                tm.Set(fb.Data.Properties);
            }

            if (!_scaleDownWithWorld)
            {
                go.transform.localScale = Vector3.one / go.transform.lossyScale.x;
            }
        }
    }
}
