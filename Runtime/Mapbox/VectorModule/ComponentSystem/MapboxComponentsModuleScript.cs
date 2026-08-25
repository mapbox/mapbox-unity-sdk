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
        public override ILayerModule ModuleImplementation { get; protected set; }

        public void Start()
        {
			
        }
        
        /// <summary>
        /// Builds the module and its settings. Virtual so a project can supply its own
        /// <see cref="VectorModuleSettings"/> — most usefully a wider
        /// <c>RejectTilesOutsideZoom</c>, since the default (12, 16) means the module
        /// draws nothing at continental zoom levels.
        /// </summary>
        protected virtual VectorLayerModule GetVectorLayerModule(IMapInformation mapInformation, UnityContext unityContext, MapService service, Dictionary<string, List<IVectorLayerVisualizer>> dictionary)
        {
            var tilesetId = MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreetsV8).Id;
            var settings = new VectorModuleSettings()
            {
                DataSettings = new VectorSourceSettings()
                {
                    TilesetId = tilesetId,
                    CacheSize = 100,
                    ClampDataLevelToMax = 15
                }
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