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
        public Vector2 ReferenceTile;
        public RectD ReferenceMercatorRect;
        
        public List<Factory> Factories;

        public void Initialize(IFileSource fs, double lat, double lng, int zoom)
        {
            foreach (Factory fac in Factories.Where(x => x != null))
            {
                fac.Initialize(fs);
            }
            
            var v2 = Conversions.GeoToWorldPosition(lat, lng, new Vector2d(0, 0));
            ReferenceTile = Conversions.MetersToTile(v2, zoom);
            ReferenceMercatorRect = Conversions.TileBounds(ReferenceTile, zoom);

            OnInitialized();
        }
        
        public void ShowTile(UnityTile tile)
        {
            foreach (var fac in Factories.Where(x => x != null))
            {
                fac.Register(tile);
            }
        }

        public delegate void MapVisualizationEventArgs(MapVisualization sender, object param);
        public event MapVisualizationEventArgs Initialized;
        protected virtual void OnInitialized()
        {
            var handler = Initialized;
            if (handler != null) handler(this, null);
        }
    }
}
