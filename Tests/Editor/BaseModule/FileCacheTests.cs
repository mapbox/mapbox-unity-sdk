using System.Collections;
using System.IO;
using System.Linq;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Utilities;
using Mapbox.MapDebug.Scripts.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Mapbox.Tests;

namespace Mapbox.BaseModuleTests
{
    
    
    [TestFixture]
    internal class FileCacheTests
    {
        private FileCache _fileCache;
        private MockTaskManager _taskManager;
        private string _testTilesetName = "test_tilesetId";
        private CanonicalTileId _testTileId = new CanonicalTileId(16, 5, 7);
        private Texture2D _testTexture;
        private RasterData _testRasterData;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _taskManager = new MockTaskManager();
            _taskManager.Initialize();
            _fileCache = new MockFileCache(_taskManager);
            _testTexture = new Texture2D(100, 100, TextureFormat.ARGB32, false);
            _testRasterData = new RasterData()
            {
                TilesetId = _testTilesetName,
                TileId = _testTileId,
                Texture = _testTexture,
                Data = _testTexture.GetRawTextureData()
            };

        }

        [SetUp]
        public void Setup()
        {
            _fileCache.ClearAll();
        }

        [Test]
        public void TestAvailability()
        {
            Assert.IsTrue(_fileCache.TestAvailability());
        }
        
        [UnityTest]
        public IEnumerator AddTileTest()
        {
            var resultFilePath = "";
            yield return _fileCache.AddCoroutine(_testRasterData, (s) =>
            {
                resultFilePath = s;
            });
            
            Assert.IsNotEmpty(resultFilePath);
            Assert.True(File.Exists(Path.Combine(_fileCache.PersistantCacheRootFolderPath, resultFilePath)));
        }

        [UnityTest]
        public IEnumerator ExistsTest()
        {
            yield return AddTileTest();
            Assert.IsTrue(_fileCache.Exists(_testTileId, _testTilesetName));
        }

        // [UnityTest]
        // public IEnumerator ReadTileTest()
        // {
        //     AddTileTest();
        //     RasterData resultData = null;
        //     var isDone = false;
        //     _fileCache.GetAsync<RasterData>(_testTileId, _testTilesetName, true, (data) =>
        //     {
        //         resultData = data;
        //         isDone = true;
        //     });
        //     while (!isDone) yield return null;
        //     
        //     Assert.IsNotNull(resultData);
        //     Assert.AreEqual(_testTileId, resultData.TileId);
        //     Assert.AreEqual(_testTilesetName, resultData.TilesetId);
        // }
        
        [UnityTest]
        public IEnumerator ReadTileCoroutineTest()
        {
            yield return AddTileTest();
            RasterData resultData = null;
            bool isWorking = true;
            Runnable.EnableRunnableInEditor();
            Runnable.Instance.StartCoroutine(_fileCache.GetCoroutine<RasterData>(_testTileId, _testTilesetName, true, (data) =>
            {
                isWorking = false;
                resultData = data;
            }));
            yield return TestWait.While(() => isWorking, "ReadTileCoroutineTest: isWorking");
            
            Assert.IsNotNull(resultData);
            Assert.AreEqual(_testTileId, resultData.TileId);
            Assert.AreEqual(_testTilesetName, resultData.TilesetId);
        }

        [UnityTest]
        public IEnumerator DeleteTileTest()
        {
            yield return AddTileTest();
            _fileCache.DeleteTileFile(_testRasterData);
            
            RasterData resultData = null;
            bool isWorking = true;
            Runnable.EnableRunnableInEditor();
            Runnable.Instance.StartCoroutine(_fileCache.GetCoroutine<RasterData>(_testTileId, _testTilesetName, true, (data) =>
            {
                isWorking = false;
                resultData = data;
            }));
            yield return TestWait.While(() => isWorking, "DeleteTileTest: isWorking");
            
            Assert.IsNull(resultData);
            
        }

        [Test]
        public void ClearAllTest()
        {
            AddTileTest();
            _fileCache.ClearAll();
            Assert.IsEmpty(_fileCache.GetFileList());
        }

        [UnityTest]
        public IEnumerator GetTileListTest()
        {
            yield return AddTileTest();
            var list = _fileCache.GetFileList();
            Assert.IsNotNull(list);
            Assert.IsTrue(list.Count == 1);
        }
        
    }
}