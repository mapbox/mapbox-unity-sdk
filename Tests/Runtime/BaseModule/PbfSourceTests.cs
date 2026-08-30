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
using Mapbox.Tests;

namespace Mapbox.BaseModuleTests
{
    [TestFixture]
    internal class PbfSourceTests
    {
        private MockSqliteCache _sqliteCache;
        private MapboxCacheManager _cacheManager;
        private LoggingDataFetchingManager _fetchingManager;
        private VectorSource _vectorSource;

        private string _testTilesetName = "mapbox.mapbox-streets-v8";
        private CanonicalTileId _testTileId = new CanonicalTileId(16, 5, 7);
        private Texture2D _testTexture;
        private VectorData _testVectorData;
        private bool _initialized;

        [UnitySetUp]
        public IEnumerator OneTimeSetUp()
        {
            if (_initialized) yield break;
            _initialized = true;

            _testTexture = Texture2D.whiteTexture;
            _testVectorData = new VectorData()
            {
                TilesetId = _testTilesetName,
                TileId = _testTileId,
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
            _sqliteCache = new MockSqliteCache(taskManager);
            _sqliteCache.ReadySqliteDatabase();
            _cacheManager = new MapboxCacheManager(unityContext, new MemoryCache(), null, _sqliteCache);
            _fetchingManager =
                new LoggingDataFetchingManager(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken);
            _vectorSource = new VectorSource(_fetchingManager, _cacheManager,
                new VectorSourceSettings() { TilesetId = _testTilesetName });
            _vectorSource.Initialize();
        }

        [SetUp]
        public void Setup()
        {
            _vectorSource.ClearMemoryCache();
            _sqliteCache.ClearDatabase();
            _sqliteCache.ReadySqliteDatabase();
        }

        [UnityTest]
        public IEnumerator SaveBlobTest()
        {
            _vectorSource.SaveBlob(_testVectorData, false);

            VectorData resultData = null;
            bool isDone = false;
            Runnable.Instance.StartCoroutine(_vectorSource.LoadTileCoroutine(_testTileId, data =>
            {
                isDone = true;
                resultData = data;
            }));
            yield return TestWait.While(() => !isDone, "SaveBlobTest: !isDone");
            
            Assert.NotNull(resultData);
            Assert.AreEqual(_testTileId, resultData.TileId);
            Assert.AreEqual(_testTilesetName, resultData.TilesetId);
            Assert.AreEqual(_testVectorData.Data, resultData.Data);
        }
        
        [UnityTest]
        public IEnumerator UpdateTileNoTagNoExpiration()
        {
            _vectorSource.ClearMemoryCache();
            bool isWorking = true; 
            Runnable.Instance.StartCoroutine(_vectorSource.LoadTileCoroutine(_testTileId, data =>
            {
                isWorking = false;
            }));
            yield return TestWait.While(() => isWorking, "UpdateTileNoTagNoExpiration: isWorking");

            //update all tiles to no etag and expiration so web request response will be 200 due to no etag
            var allTiles = _sqliteCache.GetAllTiles();
            foreach (var tile in allTiles)
            {
                tile.etag = "";
                tile.expirationDate = 0;
                _sqliteCache.UpdateTile(tile);
            }
            
            //check if everything expired without etag
            allTiles = _sqliteCache.GetAllTiles();
            foreach (var tile in allTiles)
            {
                Assert.IsEmpty(tile.etag);
                Assert.AreEqual(tile.expirationDate, 0);
            }
            
            //remove from memory cache for clean reload
            _vectorSource.ClearMemoryCache();
            
            
            //waiting for tile update finish
            var eventFired = false;
            float timeout = 5f;
            float t = 0f;
            _vectorSource.TileUpdated += (s, e) =>
            {
                if (e == _testTileId)
                    eventFired = true;
            };
            
            
            //load tile again, it's not in memory cache so it'll read file again and check expiration
            isWorking = true; 
            Runnable.Instance.StartCoroutine(_vectorSource.LoadTileCoroutine(_testTileId, data =>
            {
                isWorking = false;
            }));
            yield return TestWait.While(() => isWorking, "UpdateTileNoTagNoExpiration: isWorking");
            
            while (!eventFired && t < timeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
            if (!eventFired && t >= timeout)
            {
                Assert.Fail("Image expiration test failed due to timeout");
            }
            
            Assert.IsTrue(_fetchingManager.FileSource.ResponseCodeCounts.ContainsKey(200));
            Assert.AreNotEqual(_fetchingManager.FileSource.ResponseCodeCounts[200], 0);
                        
            allTiles = _sqliteCache.GetAllTiles();
            foreach (var tile in allTiles)
            {
                Assert.IsNotEmpty(tile.etag);
                Assert.AreNotEqual(tile.expirationDate, 0);
            }
        }
        
        [UnityTest]
        public IEnumerator UpdateTileWithTagNoExpiration()
        {
            bool isWorking = true; 
            Runnable.Instance.StartCoroutine(_vectorSource.LoadTileCoroutine(_testTileId, data =>
            {
                isWorking = false;
            }));
            yield return TestWait.While(() => isWorking, "UpdateTileWithTagNoExpiration: isWorking");

            //update all tiles to no exp. we keep etags so web request response will be 304
            var allTiles = _sqliteCache.GetAllTiles();
            foreach (var tile in allTiles)
            {
                tile.expirationDate = 0;
                _sqliteCache.UpdateTile(tile);
            }
            
            //check if everything expired without etag
            allTiles = _sqliteCache.GetAllTiles();
            foreach (var tile in allTiles)
            {
                Assert.AreEqual(tile.expirationDate, 0);
            }
            
            //remove from memory cache for clean reload
            _vectorSource.ClearMemoryCache();
            
            
            
            //waiting for tile update finish
            var eventFired = false;
            float timeout = 5f;
            float t = 0f;
            _vectorSource.TileUpdated += (s, e) =>
            {
                if (e == _testTileId)
                    eventFired = true;
            };
            
            
            
            //load tile again, it's not in memory cache so it'll read file again and check expiration
            isWorking = true; 
            Runnable.Instance.StartCoroutine(_vectorSource.LoadTileCoroutine(_testTileId, data =>
            {
                isWorking = false;
            }));
            yield return TestWait.While(() => isWorking, "UpdateTileWithTagNoExpiration: isWorking");
            
            while (!eventFired && t < timeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
            if (!eventFired && t >= timeout)
            {
                Assert.Fail("Image expiration test failed due to timeout");
            }

            Assert.IsTrue(_fetchingManager.FileSource.ResponseCodeCounts.ContainsKey(304));
            Assert.AreNotEqual(_fetchingManager.FileSource.ResponseCodeCounts[304], 0);
                        
            allTiles = _sqliteCache.GetAllTiles();
            foreach (var tile in allTiles)
            {
                Assert.IsNotEmpty(tile.etag);
                Assert.AreNotEqual(tile.expirationDate, 0);
            }
        }
    }
}