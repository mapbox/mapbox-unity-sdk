using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache;
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
    internal class ImageSourceTests
    {
        private MockFileCache _fileCache;
        private MockSqliteCache _sqliteCache;
        private MapboxCacheManager _cacheManager;
        private LoggingDataFetchingManager _fetchingManager;
        private ImageSource<RasterData> _imageSource;

        private string _testTilesetName = "mapbox.satellite";
        private CanonicalTileId _testTileId = new CanonicalTileId(16, 5, 7);
        private Texture2D _testTexture;
        private RasterData _testRasterData;
        private bool _initialized;

        [UnitySetUp]
        public IEnumerator OneTimeSetUp()
        {
            if (_initialized) yield break;
            _initialized = true;

            _testTexture = Texture2D.whiteTexture;
            _testRasterData = new RasterData()
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
            _fetchingManager = new LoggingDataFetchingManager(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken);
            _imageSource = new StaticSource(_fetchingManager, _cacheManager,
                new ImageSourceSettings() { TilesetId = _testTilesetName, UseNonReadableTextures = false});
            _imageSource.Initialize();
        }

        [SetUp]
        public void Setup()
        {
            _imageSource.ClearMemoryCache();
            _fileCache.ClearAll();
            _sqliteCache.ClearDatabase();
            _sqliteCache.ReadySqliteDatabase();
        }
        
        [UnityTest]
        public IEnumerator SaveImageTest()
        {
            _imageSource.SaveImage(_testRasterData, false);
            
            Assert.AreEqual(_fileCache.GetFileList().Count, 1);
            Assert.AreEqual(_sqliteCache.GetAllTiles().Count(), 1);
            var metaData = _sqliteCache.Get<RasterData>(_testTilesetName, _testTileId);
            Assert.AreEqual(metaData.ETag, _testRasterData.ETag); 
             
            RasterData resultData = null;
            bool isDone = false;
            _fileCache.GetAsync<RasterData>(_testTileId, _testTilesetName, false, 
                data =>
                {
                    isDone = true;
                    resultData = data;
                });
            yield return TestWait.While(() => !isDone, "SaveImageTest: !isDone");
            Assert.AreEqual(_testTexture.GetPixels32(), resultData.Texture.GetPixels32());
        }
        
        [UnityTest]
        public IEnumerator GetImageCallback()
        {
            yield return SaveImageTest();
            
            RasterData resultData = null;
            bool isDone = false;
            _imageSource.GetImageAsync<RasterData>(_testTileId, _testTilesetName, false, data =>
            {
                resultData = data;
                isDone = true;
            });
            yield return TestWait.While(() => !isDone, "GetImageCallback: !isDone");
            
            Assert.AreEqual(_testRasterData.TilesetId, resultData.TilesetId);
            Assert.AreEqual(_testRasterData.TileId, resultData.TileId);
            Assert.AreEqual(_testTexture.GetPixels32(), resultData.Texture.GetPixels32());
        }
        
        [UnityTest]
        public IEnumerator GetImageCoroutineTest()
        {
            yield return SaveImageTest();
            
            RasterData resultData = null;
            
            //doing this inWorking trick because editor tests only supports yield return null
            bool isWorking = true;
            Runnable.Instance.StartCoroutine(_imageSource.GetImageCoroutine<RasterData>(_testTileId, _testTilesetName,
                false, data =>
                {
                    isWorking = false;
                    resultData = data;
                }));
            yield return TestWait.While(() => isWorking, "GetImageCoroutineTest: isWorking");
            
            Assert.AreEqual(_testRasterData.TilesetId, resultData.TilesetId);
            Assert.AreEqual(_testRasterData.TileId, resultData.TileId);
            Assert.AreEqual(_testTexture.GetPixels32(), resultData.Texture.GetPixels32());
        }
        
        [UnityTest]
        public IEnumerator LoadTileCoroutineTestNoPreSaveShouldWeb()
        {
            RasterData resultData = null;
            //doing this inWorking trick because editor tests only supports yield return null
            bool isWorking = true; 
            Runnable.Instance.StartCoroutine(_imageSource.LoadTileCoroutine(_testTileId, data =>
            {
                isWorking = false;
                resultData = data;
            }));
            yield return TestWait.While(() => isWorking, "LoadTileCoroutineTestNoPreSaveShouldWeb: isWorking");
            
            Assert.AreEqual(_testRasterData.TilesetId, resultData.TilesetId);
            Assert.AreEqual(_testRasterData.TileId, resultData.TileId);
            Assert.AreNotEqual(resultData.Texture.GetPixels32().Length, 0);
            Assert.AreEqual(resultData.CacheType, CacheType.NoCache);
        }
        
        [UnityTest]
        public IEnumerator LoadTileCoroutineTestShouldFileCache()
        {
            yield return SaveImageTest();
            
            RasterData resultData = null;
            
            //doing this inWorking trick because editor tests only supports yield return null
            bool isWorking = true; 
            Runnable.Instance.StartCoroutine(_imageSource.LoadTileCoroutine(_testTileId, data =>
            {
                isWorking = false;
                resultData = data;
            }));
            yield return TestWait.While(() => isWorking, "LoadTileCoroutineTestShouldFileCache: isWorking");
            
            Assert.AreEqual(_testRasterData.TilesetId, resultData.TilesetId);
            Assert.AreEqual(_testRasterData.TileId, resultData.TileId);
            Assert.AreNotEqual(resultData.Texture.GetPixels32().Length, 0);
            Assert.AreEqual(CacheType.FileCache, resultData.CacheType);
        }

        [UnityTest]
        public IEnumerator UpdateTileNoTagNoExpiration()
        {
            _imageSource.ClearMemoryCache();
            bool isWorking = true; 
            Runnable.Instance.StartCoroutine(_imageSource.LoadTileCoroutine(_testTileId, data =>
            {
                isWorking = false;
            }));
            yield return TestWait.While(() => isWorking, "UpdateTileNoTagNoExpiration: isWorking");

            //update all tiles to no etag and expiration so web request response will be 200 due to no etag
                        var allTiles = _sqliteCache.GetAllTiles();
                        Assert.IsNotEmpty(allTiles, "Cache held no tiles, so the per-tile assertions below would not run.");
                        foreach (var tile in allTiles)
                        {
                            tile.etag = "";
                            tile.expirationDate = 0;
                            _sqliteCache.UpdateTile(tile);
                        }
            
            //check if everything expired without etag
            allTiles = _sqliteCache.GetAllTiles();
            Assert.IsNotEmpty(allTiles, "Cache held no tiles, so the per-tile assertions below would not run.");
            foreach (var tile in allTiles)
            {
                Assert.IsEmpty(tile.etag);
                Assert.AreEqual(tile.expirationDate, 0);
            }
            
            //remove from memory cache for clean reload
            _imageSource.ClearMemoryCache();
            
            //load tile again, it's not in memory cache so it'll read file again and check expiration
            isWorking = true; 
            Runnable.Instance.StartCoroutine(_imageSource.LoadTileCoroutine(_testTileId, data =>
            {
                isWorking = false;
            }));
            yield return TestWait.While(() => isWorking, "UpdateTileNoTagNoExpiration: isWorking");
            
            //waiting for tile update finish
                        var eventFired = false;
                        float timeout = 5f;
                        float t = 0f;
                        _imageSource.TileUpdated += (s, e) =>
                        {
                            if (e == _testTileId)
                                eventFired = true;
                        };
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
            Assert.IsNotEmpty(allTiles, "Cache held no tiles, so the per-tile assertions below would not run.");
            foreach (var tile in allTiles)
            {
                Assert.IsNotEmpty(tile.etag);
                Assert.AreNotEqual(tile.expirationDate, 0);
            }
        }
        
        [UnityTest]
        public IEnumerator UpdateTileWithTagNoExpiration()
        {
            _imageSource.ClearMemoryCache();
            bool isWorking = true; 
            Runnable.Instance.StartCoroutine(_imageSource.LoadTileCoroutine(_testTileId, data =>
            {
                isWorking = false;
            }));
            yield return TestWait.While(() => isWorking, "UpdateTileWithTagNoExpiration: isWorking");

            //update all tiles to no exp. we keep etags so web request response will be 304
                        var allTiles = _sqliteCache.GetAllTiles();
                        Assert.IsNotEmpty(allTiles, "Cache held no tiles, so the per-tile assertions below would not run.");
                        foreach (var tile in allTiles)
                        {
                            tile.expirationDate = 0;
                            _sqliteCache.UpdateTile(tile);
                        }
            
            //check if everything expired without etag
            allTiles = _sqliteCache.GetAllTiles();
            Assert.IsNotEmpty(allTiles, "Cache held no tiles, so the per-tile assertions below would not run.");
            foreach (var tile in allTiles)
            {
                Assert.AreEqual(tile.expirationDate, 0);
            }
            
            //remove from memory cache for clean reload
            _imageSource.ClearMemoryCache();
            
            //load tile again, it's not in memory cache so it'll read file again and check expiration
            isWorking = true; 
            Runnable.Instance.StartCoroutine(_imageSource.LoadTileCoroutine(_testTileId, data =>
            {
                isWorking = false;
            }));
            yield return TestWait.While(() => isWorking, "UpdateTileWithTagNoExpiration: isWorking");
            
            //waiting for tile update finish
                        var eventFired = false;
                        float timeout = 5f;
                        float t = 0f;
                        _imageSource.TileUpdated += (s, e) =>
                        {
                            if (e == _testTileId)
                                eventFired = true;
                        };
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
            Assert.IsNotEmpty(allTiles, "Cache held no tiles, so the per-tile assertions below would not run.");
            foreach (var tile in allTiles)
            {
                Assert.IsNotEmpty(tile.etag);
                Assert.AreNotEqual(tile.expirationDate, 0);
            }
        }
    }
}