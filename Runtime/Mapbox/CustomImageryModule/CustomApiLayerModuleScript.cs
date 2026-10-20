using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.ImageModule;
using Mapbox.UnityMapService;
using UnityEngine;

namespace Mapbox.CustomImageryModule
{
    public class CustomApiLayerModuleScript : ModuleConstructorScript
    {
        public CustomSourceSettings CustomSourceSettings;
        public StaticLayerModuleSettings Settings = new StaticLayerModuleSettings()
        {
            RejectTilesOutsideZoom = new Vector2(2, 25),
            DataSettings = new ImageSourceSettings()
            {
                ClampDataLevelToMax = 25
            }
        };
        [Tooltip("How this layer takes part in the map.\n\n" +
                 "Display: draws on the map tiles, and a tile waits for this imagery before it counts " +
                 "as loaded. Right for a single imagery layer.\n\n" +
                 "Display Non Blocking: draws, but never holds a tile back - imagery appears when it " +
                 "arrives instead of the tile waiting for it. Use it if this source is slow or " +
                 "sometimes has no data, so it cannot stall the map.\n\n" +
                 "Data Only: fetches, caches and keeps imagery without drawing it. REQUIRED if the map " +
                 "already has another imagery layer, because a tile has one imagery slot and two " +
                 "drawing layers overwrite each other. Read it from a minimap or your own code via " +
                 "the module's RasterSource.")]
        public CustomImageryMode Mode = CustomImageryMode.Display;

        [Tooltip("Zoom levels coarser to fetch at. 0 fetches a tile per map tile. -2 fetches one tile " +
                 "per 16 map tiles, cutting requests and cache entries by the same factor at a " +
                 "quarter of the linear detail - worth it for a minimap that draws the imagery small " +
                 "anyway. Placement is handled automatically: consumers work the sub-rect out from " +
                 "the fetched tile's own id, so a coarser tile still shows the right area. " +
                 "Clamped so it never asks for a tile finer than the map tile, past Clamp Data Level " +
                 "To Max, or below this layer's minimum zoom.")]
        public int DataZoomOffset = 0;

        public override ILayerModule ModuleImplementation { get; protected set; }

        private void Start()
        {
			
        }

        public override ILayerModule ConstructModule(MapService service, IMapInformation mapInformation,
            UnityContext unityContext)
        {
            if (Settings.SourceType == ImagerySourceType.None)
            {

            }
            else if (Settings.SourceType == ImagerySourceType.Custom)
            {
                Settings.DataSettings.TilesetId = Settings.CustomSourceId;
            }
            else
            {
                var imageryTileset = MapboxDefaultImagery.GetParameters(Settings.SourceType);
                Settings.DataSettings.TilesetId = imageryTileset.Id;
            }

            var unityService = service as MapUnityService;

            // DataSettings is passed through, not replaced by a fresh instance. The SourceType /
            // CustomSourceId resolution above writes the tileset id into it, and the source needs
            // that for two things: building URLs when no UrlFormat is given, and keying its cache.
            // Handing it a throwaway with a hardcoded id discarded both - which left the Source Type
            // dropdown doing nothing at all, and put every custom imagery layer in one shared
            // "CustomImagery" cache namespace where two different URLs would collide. The rest of
            // DataSettings (retina, cache size, data level clamp) was being dropped with it.
            if (string.IsNullOrEmpty(Settings.DataSettings.TilesetId))
            {
                // Nothing resolved a tileset id - SourceType None, or Custom with an empty id. The
                // URL then comes from UrlFormat, but the cache still needs a name to key on.
                Settings.DataSettings.TilesetId = "CustomImagery";
            }

            var source = new CustomSource(CustomSourceSettings, unityService.FetchingManager,
                unityService.CacheManager, Settings.DataSettings);
            ModuleImplementation = new CustomImageryLayerModule(source, Settings, Mode, DataZoomOffset);
            return ModuleImplementation;
        }
    }
}