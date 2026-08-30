using System;
using System.Collections;
using System.Linq;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.MapDebug.Scripts.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Mapbox.Tests;

namespace Mapbox.BaseModuleTests
{
    [TestFixture]
    internal class CacheManagerTests
    {
        private MapboxCacheManager _cacheManager;
        private MockFileCache _fileCache;
        private MockSqliteCache _sqliteCache;
        
        private string _testTilesetName = "test_tilesetId";
        private CanonicalTileId _testTileId = new CanonicalTileId(16, 5, 7);
        private Texture2D _testTexture;
        private RasterData _testRasterData;
        private VectorData _testVectorData;
        private DateTime _testExpirationDate;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var unityContext = new UnityContext();
            var taskManager = new MockTaskManager();
            taskManager.Initialize();
            unityContext.TaskManager = taskManager;
            _fileCache = new MockFileCache(taskManager);
            _sqliteCache = new MockSqliteCache(taskManager);
            _sqliteCache.ReadySqliteDatabase();
            _cacheManager = new MapboxCacheManager(unityContext, new MemoryCache(), _fileCache, _sqliteCache);
            
            _testExpirationDate = DateTime.Now.AddDays(5);
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
            _testVectorData = new VectorData()
            {
                TilesetId = _testTilesetName,
                TileId = _testTileId,
                Data = new byte[10] { 0, 1, 0, 1, 0, 2, 0, 3, 0, 4 },
                ExpirationDate = DateTime.Now.AddHours(1),
                ETag = "testETAG"
            };
        }

        [SetUp]
        public void Setup()
        {
            _fileCache.ClearAll();
            _sqliteCache.ClearDatabase();
            _sqliteCache.ReadySqliteDatabase();
        }

        [UnityTest]
        public IEnumerator SaveImageTest()
        {
            _cacheManager.SaveImage(_testRasterData, false);
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
        public IEnumerator SaveImageCoroutineTest()
        {
            yield return _cacheManager.SaveImageCoroutine(_testRasterData, false);
            Assert.AreEqual(_fileCache.GetFileList().Count, 1);
            Assert.AreEqual(_sqliteCache.GetAllTiles().Count(), 1);
            var metaData = _sqliteCache.Get<RasterData>(_testTilesetName, _testTileId);
            Assert.AreEqual(metaData.ETag, _testRasterData.ETag); 
            
            RasterData resultData = null;
            bool isDone = false;
            Runnable.Instance.StartCoroutine(_fileCache.GetCoroutine<RasterData>(_testTileId, _testTilesetName, false,
                data =>
                {
                    isDone = true;
                    resultData = data;
                }));
            yield return TestWait.While(() => !isDone, "SaveImageCoroutineTest: !isDone");
            Assert.AreEqual(_testTexture.GetPixels32(), resultData.Texture.GetPixels32());
        }
        
        [UnityTest]
        public IEnumerator SaveBlobTest()
        {
            yield return _cacheManager.SaveBlobCoroutine(_testVectorData, false);
            Assert.AreEqual(_sqliteCache.GetAllTiles().Count(), 1);
            VectorData vectorData = null;
            yield return Runnable.Instance.StartCoroutine(_cacheManager.GetBlobCoroutine<VectorData>(_testTileId,
                _testTilesetName, 1, null,
                data =>
                {
                    vectorData = data;
                }));
            Assert.AreEqual(vectorData.ETag, _testVectorData.ETag);
            Assert.AreEqual(vectorData.Data, _testVectorData.Data);
        }
        
        [UnityTest]
        public IEnumerator GetBlobCoroutine()
        {
            yield return _cacheManager.SaveBlobCoroutine(_testVectorData, false);
            VectorData result = null;
            yield return Runnable.Instance.StartCoroutine(_cacheManager.GetBlobCoroutine<VectorData>(_testTileId,
                _testTilesetName, 1, null,
                (data) =>
                {
                    result = data;
                }));

            Assert.NotNull(result);
            Assert.AreEqual(result.Data, _testVectorData.Data);
        }

        [UnityTest]
        public IEnumerator GetImageTest()
        {
            yield return SaveImageTest();
            RasterData resultData = null;
            bool isDone = false;
            _cacheManager.GetImageAsync<RasterData>(_testTileId, _testTilesetName, false, data =>
            {
                resultData = data;
                isDone = true;
            });
            yield return TestWait.While(() => !isDone, "GetImageTest: !isDone");
            
            Assert.AreEqual(_testRasterData.TilesetId, resultData.TilesetId);
            Assert.AreEqual(_testRasterData.TileId, resultData.TileId);
            Assert.AreEqual(_testTexture.GetPixels32(), resultData.Texture.GetPixels32());
        }
        
        [UnityTest]
        public IEnumerator GetImageCoroutineTest()
        {
            yield return SaveImageTest();
            RasterData resultData = null;
            bool isDone = false;
            Runnable.EnableRunnableInEditor();
            Runnable.Instance.StartCoroutine(_cacheManager.GetImageCoroutine<RasterData>(_testTileId, _testTilesetName, false, data =>
            {
                resultData = data;
                isDone = true;
            }));
            yield return TestWait.While(() => !isDone, "GetImageCoroutineTest: !isDone");
            
            Assert.AreEqual(_testRasterData.TilesetId, resultData.TilesetId);
            Assert.AreEqual(_testRasterData.TileId, resultData.TileId);
            Assert.AreEqual(_testTexture.GetPixels32(), resultData.Texture.GetPixels32());
        }
        
        [UnityTest]
        public IEnumerator GetTileInfoTest()
        {
            yield return SaveBlobTest();

            // GetTileInfoTask submits the task itself and hands back the wrapper, so the result is
            // not there yet on the line after the call - this used to read DataResult immediately
            // and assert against the null it got.
            var wrapper = _cacheManager.GetTileInfoTask<VectorData>(_testTileId, _testTilesetName);
            yield return TestWait.Until(() => wrapper.IsCompleted, "GetTileInfoTest: the tile-info read to complete");
            var resultData = wrapper.DataResult;

            Assert.AreEqual(DataTaskOutcome.Completed, wrapper.Outcome);
            Assert.NotNull(resultData, "Cache returned nothing for a tile SaveBlobTest just wrote.");
            Assert.AreEqual(_testVectorData.TilesetId, resultData.TilesetId);
            Assert.AreEqual(_testVectorData.TileId, resultData.TileId);
            Assert.AreEqual(_testVectorData.Data, resultData.Data);
        }

        [UnityTest]
        public IEnumerator ReadEtagTest()
        {
            yield return SaveImageTest();
            
            RasterData resultData = null;
            bool isDone = false;
            _cacheManager.GetImageAsync<RasterData>(_testTileId, _testTilesetName, false, data =>
            {
                resultData = data;
                isDone = true;
            });
            yield return TestWait.While(() => !isDone, "ReadEtagTest: !isDone");

            Assert.IsNull(resultData.ETag);
            Assert.IsNull(resultData.ExpirationDate);
            // The task fills the instance passed to it, so resultData gains its ETag and expiry once
            // it completes. Without the wait, the two assertions below contradicted the two above.
            var wrapper = _cacheManager.GetTileInfoTask(resultData.TileId, resultData.TilesetId, 1, resultData);
            yield return TestWait.Until(() => wrapper.IsCompleted, "ReadEtagTest: the tile-info read to complete");

            Assert.AreEqual(_testRasterData.ETag, resultData.ETag);
            Assert.Less(_testRasterData.ExpirationDate.Value.Subtract(resultData.ExpirationDate.Value).TotalMinutes, 5);
        }

        [UnityTest]
        public IEnumerator UpdateExpirationTest()
        {
            yield return SaveImageTest();
            
            var wrapper = _cacheManager.GetTileInfoTask<RasterData>(_testTileId, _testTilesetName, 1);
            yield return TestWait.Until(() => wrapper.IsCompleted, "UpdateExpirationTest: the first tile-info read to complete");
            var resultData = wrapper.DataResult;

            Assert.NotNull(resultData);
            Assert.Less(_testRasterData.ExpirationDate.Value.Subtract(resultData.ExpirationDate.Value).TotalMinutes, 5);
            
            _cacheManager.UpdateExpiration(_testTileId, _testTilesetName, _testExpirationDate);
            
            var wrapper2 = _cacheManager.GetTileInfoTask<RasterData>(_testTileId, _testTilesetName, 1);
            yield return TestWait.Until(() => wrapper2.IsCompleted, "UpdateExpirationTest: the re-read after UpdateExpiration to complete");
            resultData = wrapper2.DataResult;

            Assert.NotNull(resultData);
            Assert.Less(_testExpirationDate.Subtract(resultData.ExpirationDate.Value).TotalMinutes, 5);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _fileCache.ClearAll();
            _sqliteCache.ClearDatabase();
            _sqliteCache.DeleteSqliteFile();
            _sqliteCache.Dispose();
        }
    }
}