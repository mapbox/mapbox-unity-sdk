using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.ImageModule;
using Mapbox.ImageModule.Terrain;
using Mapbox.ImageModule.Terrain.TerrainStrategies;
using Mapbox.MapDebug.Scripts.Logging;
using Mapbox.UnityMapService;
using Mapbox.UnityMapService.TileProviders;
using Mapbox.VectorModule;
using Mapbox.VectorModule.MeshGeneration;
using NUnit.Framework;
using UnityEngine.TestTools;
using Mapbox.Tests;

namespace Mapbox.BaseModuleTests.DataTests
{
    public class MapViewLoadTests
    {
        private string _helsinkiLatitudeLongitudeString = "60.1734031,24.9428875";
        private string _sanFranciscoLatitudeLongitudeString = "60.1734031,24.9428875";
        private LatitudeLongitude _helsinkiLatLng;
        private LatitudeLongitude _sfLatLng;
        private MapboxMap _map;
        private MapboxCacheManager _cacheManager;
        private MemoryCache _memoryCache;
        private Source<RasterData> _imageSource;
        private Source<TerrainData> _terrainSource;
        private Source<VectorData> _vectorSource;
        private StaticApiLayerModule _imageLayer;
        private TerrainLayerModule _terrainLayer;
        private VectorLayerModule _vectorLayer;

        [UnitySetUp]
        public IEnumerator OneTimeSetUp()
        {
            _helsinkiLatLng = Conversions.StringToLatLon(_helsinkiLatitudeLongitudeString);
            _sfLatLng = Conversions.StringToLatLon(_sanFranciscoLatitudeLongitudeString);

            var mapInfo = new MapInformation(_helsinkiLatitudeLongitudeString);
            mapInfo.SetInformation(null, 16, 45, null, 1000);
            mapInfo.Initialize();
            var mapboxContext = new MapboxContext();
            yield return mapboxContext.LoadConfigurationCoroutine(false);
            var unityContext = new UnityContext();
            yield return unityContext.Initialize();

            var taskManager = new TaskManager();
            taskManager.Initialize();
            unityContext.TaskManager = taskManager;
            var dataManager = new DataFetchingManager(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken);

            var sqliteCache = new MockSqliteCache(taskManager);
            sqliteCache.ReadySqliteDatabase();

            _memoryCache = new MemoryCache();
            _cacheManager = new MapboxCacheManager(
                unityContext,
                _memoryCache,
                new MockFileCache(taskManager),
                sqliteCache);
            
            var mapService = new MapUnityService(
                unityContext,
                mapboxContext,
                new UnityFixedAreaTileProvider(),
                _cacheManager,
                dataManager);
            
            _map = new MapboxMap(mapInfo, unityContext, mapService);
            var mapVisualizer = new MapboxMapVisualizer(mapInfo, unityContext, new TileCreator(unityContext));
            _map.MapVisualizer = mapVisualizer;

            var imageryTileset = MapboxDefaultImagery.GetParameters(ImagerySourceType.MapboxSatellite);
            var terrainTileset = MapboxDefaultElevation.GetParameters(ElevationSourceType.MapboxTerrain);
            var vectorTileset = MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreetsV8);

            var terrainSettings = new TerrainLayerModuleSettings();
            terrainSettings.DataSettings = new ImageSourceSettings()
            {
                TilesetId = terrainTileset.Id,
                ClampDataLevelToMax = 14
            };
            var vectorSourceSettings = new VectorSourceSettings()
            {
                TilesetId = vectorTileset.Id, 
                ClampDataLevelToMax = 15
            };
            
            _imageSource = mapService.GetStaticRasterSource(new ImageSourceSettings() { TilesetId = imageryTileset.Id});
            _terrainSource = mapService.GetTerrainRasterSource(terrainSettings.DataSettings);
            _vectorSource = mapService.GetVectorSource(vectorSourceSettings);
            _imageLayer = new StaticApiLayerModule(_imageSource, new StaticLayerModuleSettings());
            _terrainLayer = new TerrainLayerModule(_terrainSource, terrainSettings);
            _vectorLayer = new VectorLayerModule(
                mapInfo, 
                _vectorSource, 
                unityContext, 
                new Dictionary<string, List<IVectorLayerVisualizer>>(), 
                new VectorModuleSettings() { DataSettings = vectorSourceSettings});
            mapVisualizer.LayerModules.Add(_imageLayer);
            mapVisualizer.LayerModules.Add(_terrainLayer);
            mapVisualizer.LayerModules.Add(_vectorLayer);

            yield return _map.Initialize();
        }

        [UnityTest]
        public IEnumerator LoadMapView()
        {
            var mapLoaded = false;
            Runnable.EnableRunnableInEditor();
            var coroutine = Runnable.Instance.StartCoroutine(_map.LoadMapViewCoroutine(() =>
            {
                mapLoaded = true;
            }));
            yield return TestWait.While(() => mapLoaded == false, "LoadMapView: mapLoaded == false");

            var tiles = _map.TileCover.Tiles.Select(x => x.Canonical);


            Assert.IsTrue(_terrainLayer.GetDataId(tiles).All(x => _terrainSource.GetInstantData(x, out var td)));
            Assert.IsTrue(tiles.All(x => _imageSource.GetInstantData(x, out var td)));
            Assert.IsTrue(_vectorLayer.GetDataId(tiles).All(x => _vectorSource.GetInstantData(x, out var td)));
            
            Assert.IsTrue(mapLoaded);
        }
    }
}