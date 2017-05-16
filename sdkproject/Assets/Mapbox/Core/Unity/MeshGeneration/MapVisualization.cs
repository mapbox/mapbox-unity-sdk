namespace Mapbox.Unity.MeshGeneration
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Data;
    using Mapbox.Unity.MeshGeneration.Factories;
    using Mapbox.Platform;
    using Utils;
    using Utilities;

    [CreateAssetMenu(menuName = "Mapbox/MapVisualization")]
    public class MapVisualization : ScriptableObject
    {
        public WorldParameters WorldParameters;
        public List<Factory> Factories;

        /// <summary>
        /// Initializes the factories by passing the file source down, which's necessary for data (web/file) calls
        /// </summary>
        /// <param name="fs"></param>
        public void Initialize(IFileSource fs, WorldParameters parameters)
        {
            WorldParameters = parameters;
            foreach (Factory fac in Factories.Where(x => x != null))
            {
                fac.Initialize(fs, parameters);
            }
        }

        
        /// <summary>
        /// Registers requested tiles to the factories
        /// </summary>
        /// <param name="tile"></param>
        public void ShowTile(UnityTile tile)
        {
            foreach (var fac in Factories.Where(x => x != null))
            {
                fac.Register(tile);
            }
        }
    }
}
