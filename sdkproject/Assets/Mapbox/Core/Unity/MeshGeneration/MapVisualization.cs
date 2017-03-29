namespace Mapbox.Unity.MeshGeneration
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Data;
    using Mapbox.Unity.MeshGeneration.Factories;
    using Mapbox.Platform;

    [CreateAssetMenu(menuName = "Mapbox/MapVisualization")]
    public class MapVisualization : ScriptableObject
    {
        public List<Factory> Factories;

        public void Initialize(MonoBehaviour runner, IFileSource fs)
        {
            foreach (Factory fac in Factories.Where(x => x != null))
            {
                fac.Initialize(runner, fs);
            }
        }

        public void ShowTile(UnityTile tile)
        {
            foreach (var fac in Factories.Where(x => x != null))
            {
                fac.Register(tile);
            }
        }
    }
}
