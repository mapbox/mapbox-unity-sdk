using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache;
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
using Mapbox.UnityMapService.DataSources;
using Mapbox.UnityMapService.TileProviders;
using Mapbox.VectorModule;
using Mapbox.VectorModule.MeshGeneration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TerrainData = Mapbox.BaseModule.Data.DataFetchers.TerrainData;
using Mapbox.Tests;

public class MapTest : MonoBehaviour
{
    private string _helsinkiLatitudeLongitudeString = "60.1734031,24.9428875";
    private string _sanFranciscoLatitudeLongitudeString = "60.1734031,24.9428875";
    private LatitudeLongitude _helsinkiLatLng;
    private LatitudeLongitude _sfLatLng;
    
    private MapboxMap _map;
    private LoggingCacheManager _cacheManager;
    private MemoryCache _memoryCache;
    private UnitySource<RasterData> _imageSource;
    private UnitySource<TerrainData> _terrainSource;
    private UnitySource<VectorData> _vectorSource;
    private StaticApiLayerModule _imageLayer;
    private TerrainLayerModule _terrainLayer;
    private VectorLayerModule _vectorLayer;
    private LoggingDataFetchingManager _dataManager;

    // [UnitySetUp]
    // public IEnumerator OneTimeSetUp()
    // {
    //     // LoadMap(_helsinkiLatitudeLongitudeString);
    //     // yield return _map.Initialize();
    // }

    private IEnumerator LoadMap(string latlng, bool useSqlite = true, bool useFileCache = true)
    {
        var mapInfo = new MapInformation(latlng);
        mapInfo.SetInformation(null, 16, 45, null, 1000);
        mapInfo.Initialize();
        var mapboxContext = new MapboxContext();
        yield return mapboxContext.LoadConfigurationCoroutine(false);
        var unityContext = new UnityContext();
        yield return unityContext.Initialize();

        var taskManager = unityContext.TaskManager;
        _dataManager = new LoggingDataFetchingManager(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken);

        SqliteCache sqliteCache = null;
        if (useSqlite)
        {
            sqliteCache = new MockSqliteCache(taskManager);
            sqliteCache.ReadySqliteDatabase();
        }
        
        MockFileCache fileCache = null;
        if (useFileCache)
        {
            fileCache = new MockFileCache(taskManager);
        }

        _memoryCache = new MemoryCache();
        _cacheManager = new LoggingCacheManager(
            unityContext,
            _memoryCache,
            fileCache,
            sqliteCache);

        var mapService = new MapUnityService(
            unityContext,
            mapboxContext,
            new UnityFixedAreaTileProvider(),
            _cacheManager,
            _dataManager);

        _map = new MapboxMap(mapInfo, unityContext, mapService);
        var mapVisualizer = new MapboxMapVisualizer(mapInfo, unityContext,
            new TileCreator(unityContext));
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

        _imageSource = (UnitySource<RasterData>)mapService.GetStaticRasterSource(new ImageSourceSettings() { TilesetId = imageryTileset.Id });
        _terrainSource = (UnitySource<TerrainData>)mapService.GetTerrainRasterSource(terrainSettings.DataSettings);
        _vectorSource = (UnitySource<VectorData>)mapService.GetVectorSource(vectorSourceSettings);
        _imageLayer = new StaticApiLayerModule(_imageSource, new StaticLayerModuleSettings());
        _terrainLayer = new TerrainLayerModule(_terrainSource, terrainSettings);
        _vectorLayer = new VectorLayerModule(
            mapInfo,
            _vectorSource,
            unityContext, 
            new Dictionary<string, List<IVectorLayerVisualizer>>(),
            new VectorModuleSettings() { DataSettings = vectorSourceSettings });
        mapVisualizer.LayerModules.Add(_imageLayer);
        mapVisualizer.LayerModules.Add(_terrainLayer);
        mapVisualizer.LayerModules.Add(_vectorLayer);
    }

    [TearDown]
    public void TearDown()
    {
        _map.OnDestroy();
    }
    
    [UnityTest]
    public IEnumerator LoadMapView()
    {
        yield return LoadMap(_helsinkiLatitudeLongitudeString);
        yield return _map.Initialize();
        
        var tileCover = new TileCover();
        _map.MapService.TileCover(_map.MapInformation, tileCover);
        yield return _map.LoadTileCoverToMemory(tileCover);

        var tiles = tileCover.Tiles.Select(x => x.Canonical);
        Assert.IsTrue(_terrainLayer.GetDataId(tiles).All(x => _terrainSource.GetInstantData(x, out var td)));
        Assert.IsTrue(tiles.All(x => _imageSource.GetInstantData(x, out var td)));
        Assert.IsTrue(_vectorLayer.GetDataId(tiles).All(x => _vectorSource.GetInstantData(x, out var td)));
    }
    
    [UnityTest]
    public IEnumerator LoadMapOfExpiredData()
    {
        //this wrongly assumes data is already available in the long term cache so it's incomplete
        
        yield return LoadMap(_helsinkiLatitudeLongitudeString);

        //expire all tiles in db
        var sqliteCache = _cacheManager.SqLiteCache as SqliteCache;
        foreach (var tile in sqliteCache.GetAllTiles())
        {
            tile.expirationDate = 0;
            sqliteCache.UpdateTile(tile);
        }
        
        yield return _map.Initialize();
        yield return _map.LoadMapViewCoroutine();
        yield return new WaitForSeconds(2);
        yield return TestWait.While(() => _dataManager.ActiveFetchCount > 0 || _dataManager.FetchQueueCount > 0, "LoadMapOfExpiredData: _dataManager.ActiveFetchCount > 0 || _dataManager.FetchQueueCount > 0");
        
        var updatedTileCount = 0;
        foreach (var record in _dataManager.Records)
        {
            if (record.Key.IsUpdate)
                updatedTileCount++;
        }
        
        Assert.IsTrue(_map.Status >= InitializationStatus.Initialized);
        Assert.AreNotEqual(_map.TileCover.Tiles.Count, 0);
        Assert.AreNotEqual(updatedTileCount, 0);
    }
    
    [UnityTest]
    public IEnumerator LoadMapWithoutSql()
    {
        yield return LoadMap(_helsinkiLatitudeLongitudeString, false);
        yield return _map.Initialize();
        
        yield return _map.LoadMapViewCoroutine();
        _map.MapVisualizer.LoadSnapshot(_map.TileCover);
        yield return new WaitForSeconds(2);
        
        Assert.IsTrue(_map.Status == InitializationStatus.ReadyForUpdates);
        Assert.AreNotEqual(_map.TileCover.Tiles.Count, 0);
        Assert.AreNotEqual(_map.MapVisualizer.ActiveTiles.Count, 0);
    }
    
    [UnityTest]
    public IEnumerator LoadMapWithoutFile()
    {
        yield return LoadMap(_helsinkiLatitudeLongitudeString, true, false);
        yield return _map.Initialize();
        
        yield return _map.LoadMapViewCoroutine();
        _map.MapVisualizer.LoadSnapshot(_map.TileCover);
        yield return new WaitForSeconds(2);
        
        Assert.IsTrue(_map.Status == InitializationStatus.ReadyForUpdates);
        Assert.AreNotEqual(_map.TileCover.Tiles.Count, 0);
        Assert.AreNotEqual(_map.MapVisualizer.ActiveTiles.Count, 0);
    }

    [UnityTest]
    public IEnumerator LoadThenCancelAllRequests()
    {
        MapboxCacheManager.DeleteAllCache();
        yield return LoadMap(_helsinkiLatitudeLongitudeString);
        yield return _map.Initialize();
        
        _dataManager.SetDelayTime(5);
        Assert.IsTrue(_map.Status >= InitializationStatus.Initialized);
        
        Runnable.EnableRunnableInEditor();
        Runnable.Run(_map.LoadMapViewCoroutine());
        yield return new WaitForSeconds(1);

        var finishedCount = 0;
        var cancelledCount = 0;
        _dataManager.FetchFinished += info => finishedCount++;
        _dataManager.FetchCancelled += info => cancelledCount++;
        
        foreach (var info in _dataManager.GetFetchingQueue())
        {
            info.Tile.Cancel();
        }

        yield return TestWait.While(() => _map.Status < InitializationStatus.ReadyForUpdates, "LoadThenCancelAllRequests: _map.Status < InitializationStatus.ReadyForUpdates");
        
        Assert.IsTrue(_map.Status >= InitializationStatus.Initialized);
        Assert.AreEqual(finishedCount, 0);
        Assert.AreNotEqual(cancelledCount, 0);
    }
}