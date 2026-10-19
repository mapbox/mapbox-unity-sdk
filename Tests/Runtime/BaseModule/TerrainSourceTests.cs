using System;
using System.Collections;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.MapDebug.Scripts.Logging;
using Mapbox.UnityMapService.DataSources;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TerrainData = Mapbox.BaseModule.Data.DataFetchers.TerrainData;
using Mapbox.Tests;

namespace Mapbox.BaseModuleTests
{
    [TestFixture]
    internal class TerrainSourceTests
    {
        private MockFileCache _fileCache;
        private MockSqliteCache _sqliteCache;
        private MapboxCacheManager _cacheManager;
        private DataFetchingManager _fetchingManager;
        private TerrainSource _terrainSource;

        private string _testTilesetName = "mapbox.terrain-rgb";
        private CanonicalTileId _testTileId = new CanonicalTileId(16, 37310, 18968);
        private Texture2D _testTexture;
        private RasterData _testRasterData;
        private bool _initialized;

        [UnitySetUp]
        public IEnumerator OneTimeSetUp()
        {
            if (_initialized) yield break;
            _initialized = true;

            _testTexture = Texture2D.whiteTexture;
            _testRasterData = new TerrainData()
            {
                TilesetId = _testTilesetName,
                TileId = _testTileId,
                Texture = _testTexture,
                Data = ImageConversion.EncodeToJPG(_testTexture),
                ExpirationDate = DateTime.Now.AddHours(1),
                ETag = "testETAG"
            };

            var mapboxContext = new MapboxContext();
            yield return mapboxContext.LoadConfigurationCoroutine(false);
            var unityContext = new UnityContext();
            var taskManager = new MockTaskManager();
            taskManager.Initialize();
            unityContext.TaskManager = taskManager;
            _fileCache = new MockFileCache(taskManager);
            _sqliteCache = new MockSqliteCache(taskManager);
            _sqliteCache.ReadySqliteDatabase();
            _cacheManager = new MapboxCacheManager(unityContext, new MemoryCache(), _fileCache, _sqliteCache);
            _fetchingManager =
                new LoggingDataFetchingManager(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken);
            _terrainSource = new TerrainSource(_fetchingManager, _cacheManager,
                new ImageSourceSettings()
                    { TilesetId = _testTilesetName, UseNonReadableTextures = false });
            _terrainSource.Initialize();
        }

        [SetUp]
        public void Setup()
        {
            _fileCache.ClearAll();
            _sqliteCache.ClearDatabase();
            _sqliteCache.ReadySqliteDatabase();
        }
        
        [UnityTest]
        public IEnumerator LoadTileCoroutineTest()
        {
            TerrainData resultData = null;
            bool isDone = false;
            Runnable.EnableRunnableInEditor();
            Runnable.Instance.StartCoroutine(_terrainSource.LoadTileCoroutine(_testTileId, data =>
            {
                resultData = data;
                isDone = true;
            }));
            yield return TestWait.While(() => !isDone, "LoadTileCoroutineTest: !isDone");
            
            Assert.AreEqual(_testRasterData.TilesetId, resultData.TilesetId);
            Assert.AreEqual(_testRasterData.TileId, resultData.TileId);
            Assert.AreNotEqual(resultData.Texture.GetPixels32().Length, 0);
            Assert.AreEqual(resultData.CacheType, CacheType.NoCache);
            Assert.IsNotNull(resultData.ElevationValues);
            Assert.AreNotEqual(resultData.ElevationValues.Length, 0);
        }
    }
}