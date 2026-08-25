using System.Collections.Generic;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.Unity;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem
{
    public class MapboxComponentsModuleScript : ModuleConstructorScript
    {
        [SerializeField] private List<LayerVisualizerConstructor> _layerVisualizers;

        [Tooltip("Tiles outside this zoom range are skipped entirely. The default (12, 16) suits a city-scale scene but draws nothing at continental zooms, so lower the minimum if the map is ever viewed zoomed out. What a wider range actually shows depends on the tileset: in Mapbox Streets v8 water starts at zoom 0, landuse and roads around 5, and buildings at 13.")]
        public Vector2 RejectTilesOutsideZoom = new Vector2(12, 16);

        [Tooltip("Vector data is never fetched above this zoom; tiles beyond it reuse their ancestor's data. Raise it only if features need to keep gaining detail past zoom 15 - each extra level multiplies the tiles fetched by four.")]
        public int ClampDataLevelToMax = 15;

        [Tooltip("Vector tiles held in the source's memory cache. 100 covers a typical screenful with room for a pan; raise it for wide views or fast flights, lower it on memory-constrained devices.")]
        public int CacheSize = 100;

        public override ILayerModule ModuleImplementation { get; protected set; }

        public void Start()
        {
			
        }
        
        /// <summary>
        /// Builds the module and its settings from the fields above.
        ///
        /// Still virtual, but only for things the Inspector cannot express - a different tileset, or
        /// settings derived at runtime. The zoom range, data clamp and cache size were the reason
        /// subclassing was needed at all, and they are serialized fields now: a scene that wants a
        /// continental basemap sets a lower minimum here instead of shipping a subclass whose entire
        /// body is a copy of this method with three numbers changed.
        /// </summary>
        protected virtual VectorLayerModule GetVectorLayerModule(IMapInformation mapInformation, UnityContext unityContext, MapService service, Dictionary<string, List<IVectorLayerVisualizer>> dictionary)
        {
            var tilesetId = MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreetsV8).Id;
            var settings = new VectorModuleSettings()
            {
                DataSettings = new VectorSourceSettings()
                {
                    TilesetId = tilesetId,
                    CacheSize = CacheSize,
                    ClampDataLevelToMax = ClampDataLevelToMax
                },
                RejectTilesOutsideZoom = RejectTilesOutsideZoom
            };
            return new MapboxComponentsModule(mapInformation, service.GetVectorSource(settings.DataSettings), unityContext, dictionary, settings);
        }

        public override ILayerModule ConstructModule(MapService service, IMapInformation mapInformation, UnityContext unityContext)
        {
            var dictionary = new Dictionary<string, List<IVectorLayerVisualizer>>();
            foreach (var visualizerObject in _layerVisualizers)
            {
                if(visualizerObject == null) continue;
                var visualizer = visualizerObject.ConstructLayerVisualizer(mapInformation, unityContext);
                if(!dictionary.ContainsKey(visualizer.VectorLayerName))
                    dictionary.Add(visualizer.VectorLayerName, new List<IVectorLayerVisualizer>());
                dictionary[visualizer.VectorLayerName].Add(visualizer);
            }
            ModuleImplementation = GetVectorLayerModule(mapInformation, unityContext, service, dictionary);
            return ModuleImplementation;
        }
    }
}