using System.Collections.Generic;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule;
using Mapbox.VectorModule.ComponentSystem;
using UnityEngine;

namespace Mapbox.Samples.LandmarkTour
{
    /// <summary>
    /// The Components module with its tile zoom range opened up, so the tour has a
    /// basemap at the continental view as well as at street level.
    ///
    /// The base class hardcodes <c>RejectTilesOutsideZoom</c> to (12, 16), which is right
    /// for a city-scale scene but means nothing at all is drawn at zoom 4. Overriding the
    /// settings is the whole reason this subclass exists; the visualizer list, tileset and
    /// everything else come from the base class unchanged.
    ///
    /// What the wider range actually buys depends on the vector tiles: in Mapbox Streets
    /// v8, water starts at zoom 0, landuse and roads around 5, and buildings at 13. So the
    /// overview is ground plus coastlines, and the rest fills in during the descent.
    /// </summary>
    public class LandmarkTourComponentsModuleScript : MapboxComponentsModuleScript
    {
        [Tooltip("Tiles outside this zoom range are skipped. Lower the minimum to draw a basemap at continental zooms; the module's own default is (12, 16).")]
        public Vector2 RejectTilesOutsideZoom = new Vector2(2, 16);

        [Tooltip("Vector data is never fetched above this zoom; higher tiles reuse their ancestor's data. Matches the module default of 15.")]
        public int ClampDataLevelToMax = 15;

        [Tooltip("Number of vector tiles held in the source cache. Matches the module default of 100.")]
        public int CacheSize = 100;

        protected override VectorLayerModule GetVectorLayerModule(
            IMapInformation mapInformation,
            UnityContext unityContext,
            MapService service,
            Dictionary<string, List<IVectorLayerVisualizer>> dictionary)
        {
            var settings = new VectorModuleSettings
            {
                DataSettings = new VectorSourceSettings
                {
                    TilesetId = MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreetsV8).Id,
                    CacheSize = CacheSize,
                    ClampDataLevelToMax = ClampDataLevelToMax
                },
                RejectTilesOutsideZoom = RejectTilesOutsideZoom
            };

            return new MapboxComponentsModule(mapInformation, service.GetVectorSource(settings.DataSettings), unityContext, dictionary, settings);
        }
    }
}
