using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.MapDebug.Scripts.Logging;
using Mapbox.UnityMapService;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Mapbox.Tests;

namespace Mapbox.BaseModuleTests.DataTests
{
    [TestFixture]
    internal class RasterDataTests
    {
        private MockTaskManager _taskManager;
        private MapboxContext _mapboxContext;
        private DataFetchingManager _dataFetchingManager;
        private UnityContext _unityContext;
        private FileCache _fileCache;
        private ISqliteCache _sqliteCache;
        private Style _imageryTileset;
        private UnwrappedTileId _tileId;
        private HashSet<CanonicalTileId> _testTileHashset;
        private bool _initialized;

        [UnitySetUp]
        public IEnumerator OneTimeSetup()
        {
            if (_initialized) yield break;
            _initialized = true;

            _taskManager = new MockTaskManager();
            _mapboxContext = new MapboxContext();
            yield return _mapboxContext.LoadConfigurationCoroutine(false);
            _dataFetchingManager = new DataFetchingManager(_mapboxContext.GetAccessToken(), _mapboxContext.GetSkuToken);
            _unityContext = new UnityContext();
            _unityContext.Initialize(_taskManager);
            _fileCache = new MockFileCache(_taskManager);
            _fileCache.ClearAll();
            _sqliteCache = new MockSqliteCache(_taskManager);
            _sqliteCache.ClearDatabase();
            _sqliteCache.ReadySqliteDatabase();
            _imageryTileset = MapboxDefaultImagery.GetParameters(ImagerySourceType.MapboxStreets);
            Runnable.EnableRunnableInEditor();
            
            var lonLat = new LatitudeLongitude(24.937700, 60.163200);
            _tileId = Conversions.LatitudeLongitudeToTileId(lonLat, 16);
            _testTileHashset = new HashSet<CanonicalTileId>()
            {
                _tileId.Canonical,
                new CanonicalTileId(_tileId.Z, _tileId.X + 1, _tileId.Y),
                new CanonicalTileId(_tileId.Z, _tileId.X + 2, _tileId.Y),
                new CanonicalTileId(_tileId.Z, _tileId.X + 3, _tileId.Y),
            };
        }
    
        [SetUp]
        public void SetUp()
        {
        
        }

        [UnityTest, Order(5)]
        public IEnumerator RequestTileListNoCache()
        {
            var mapService = new MapUnityService(_unityContext, _mapboxContext, null,
                new MapboxCacheManager(_unityContext, new MemoryCache(), null, null));

            var rasterSource = mapService.GetStaticRasterSource(new ImageSourceSettings() { TilesetId = _imageryTileset.Id });

            Runnable.EnableRunnableInEditor();
            List<RasterData> loadedTiles = null;
            var coroutine = Runnable.Instance.StartCoroutine(rasterSource.LoadTilesCoroutine(_testTileHashset, (data) =>
            {
                loadedTiles = data;
            }));
            yield return coroutine;

            Assert.NotNull(loadedTiles);
            Assert.IsNotEmpty(loadedTiles);
        }

        [UnityTest, Order(4)]
        public IEnumerator RequestTileListCheckWithFileSqlite()
        {
            var mapService = new MapUnityService(_unityContext, _mapboxContext, null,
                new MapboxCacheManager(_unityContext, new MemoryCache(), _fileCache, _sqliteCache));
            
            var rasterSource = mapService.GetStaticRasterSource(new ImageSourceSettings() { TilesetId = _imageryTileset.Id });
        
            Runnable.EnableRunnableInEditor();
            List<RasterData> loadedTiles = null;
            var coroutine = Runnable.Instance.StartCoroutine(rasterSource.LoadTilesCoroutine(_testTileHashset, (data) =>
            {
                loadedTiles = data;
            }));
            yield return coroutine;
        
            Assert.NotNull(loadedTiles);
            Assert.IsNotEmpty(loadedTiles);
            foreach (var rasterData in loadedTiles)
            {
                var dataFromSqlite = _sqliteCache.Get<RasterData>(_imageryTileset.Id, rasterData.TileId);
                Assert.NotNull(dataFromSqlite);
                
                RasterData dataFromFile = null;
                coroutine = Runnable.Instance.StartCoroutine(_fileCache.GetCoroutine<RasterData>(rasterData.TileId, _imageryTileset.Id, true, data =>
                {
                    dataFromFile = data;
                }));
                yield return coroutine;
                
                Assert.NotNull(dataFromFile);
            }
        }
        
        [UnityTest, Order(3)]
        public IEnumerator RequestWithAllCaches()
        {
            var mapService = new MapUnityService(_unityContext, _mapboxContext, null,
                new MapboxCacheManager(_unityContext, new MemoryCache(), _fileCache, _sqliteCache));

            var rasterSource = mapService.GetStaticRasterSource(new ImageSourceSettings() { TilesetId = _imageryTileset.Id });

            RasterData resultData = null;
            var coroutineId = Runnable.Run(rasterSource.LoadTileCoroutine(_tileId.Canonical, (data) =>
            {
                resultData = data;
            }));

            yield return TestWait.While(() => Runnable.IsRunning(coroutineId), "RequestWithAllCaches: Runnable.IsRunning(coroutineId)");

            Assert.NotNull(resultData.Texture);
            Assert.AreEqual(resultData.CacheType, CacheType.NoCache);
            var fileCount = _fileCache.GetFileList().Count;
            Assert.AreEqual(1, fileCount, "File Cache should have one tile, instead it has " + fileCount);

            //adding same tile as second test so sqlite tile count should still be one
            var sqliteTileCount = _sqliteCache.GetAllTiles().Count;
            Assert.AreEqual(1, sqliteTileCount, "Sqlite should have one tile, instead it has " + sqliteTileCount);
        }

        [UnityTest, Order(2)]
        public IEnumerator RequestNoFileYesSqliteCache()
        {
            var mapService = new MapUnityService(_unityContext, _mapboxContext, null,
                new MapboxCacheManager(_unityContext, new MemoryCache(), null, _sqliteCache));
        
            var rasterSource = mapService.GetStaticRasterSource(new ImageSourceSettings() { TilesetId = _imageryTileset.Id });
        
            RasterData resultData = null;
            var coroutineId = Runnable.Run(rasterSource.LoadTileCoroutine(_tileId.Canonical, (data) =>
            {
                resultData = data;
            }));

            yield return TestWait.While(() => Runnable.IsRunning(coroutineId), "RequestNoFileYesSqliteCache: Runnable.IsRunning(coroutineId)");
            
            Assert.NotNull(resultData.Texture);
            Assert.AreEqual(resultData.CacheType, CacheType.NoCache);
            Assert.IsEmpty(_sqliteCache.GetAllTiles());
        }
    
        [UnityTest, Order(1)]
        public IEnumerator RequestNoFileNoSqliteCaches()
        {
            var cacheManager = new MapboxCacheManager(_unityContext, new MemoryCache(), null, null);
            var mapService = new MapUnityService(_unityContext, _mapboxContext, null, cacheManager);
        
            var rasterSource = mapService.GetStaticRasterSource(new ImageSourceSettings() { TilesetId = _imageryTileset.Id });
            
            RasterData resultData = null;
            var coroutineId = Runnable.Run(rasterSource.LoadTileCoroutine(_tileId.Canonical, (data) =>
            {
                resultData = data;
            }));

            yield return TestWait.While(() => Runnable.IsRunning(coroutineId), "RequestNoFileNoSqliteCaches: Runnable.IsRunning(coroutineId)");
            
            Assert.NotNull(resultData.Texture);
            Assert.AreEqual(resultData.CacheType, CacheType.NoCache);
        }
    }
}