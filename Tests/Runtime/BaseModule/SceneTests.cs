#if UNITY_RECORDER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.Example.Scripts.Map;
using Mapbox.Example.Scripts.MapInput;
using Mapbox.MapDebug.Scripts.Logging;
using Mapbox.UnityMapService;
using Mapbox.UnityMapService.TileProviders;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.Recorder;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Mapbox.Tests;

namespace Mapbox.BaseModuleTests.PlayModeTests
{
    internal class SceneTests
    {
        private const string TestSceneName = "WorldMapScene";
        private string TestScreenshotsFolder = "TestScreenshots";
        private int _sceneIndex = -1;

        private Dictionary<string, LatitudeLongitude> _latitudeLongitudes = new Dictionary<string, LatitudeLongitude>()
        {
            { "Chicago", new LatitudeLongitude(41.8336152, -87.896769) },
            { "Helsinki", new LatitudeLongitude(60.1711822, 24.9422687) },
            { "Berlin", new LatitudeLongitude(52.5165309, 13.3777779) },
            { "Paris", new LatitudeLongitude(48.8584642, 2.3097613) },
            { "SF", new LatitudeLongitude(37.7873314, -122.4080894) },
            { "Mountain", new LatitudeLongitude(46.85264466, -121.75710321) },
        };

        private static IEnumerable LatLngTestSource
        {
            get
            {
                yield return new TestCaseData(TestSceneName, 41.8336152,  -87.896769, 16, 90,  0, 1000).Returns(null);
                yield return new TestCaseData(TestSceneName, 60.1711822,  24.9422687, 18, 34, 13,    1).Returns(null);
                yield return new TestCaseData(TestSceneName, 52.5165309,  13.3777779, 16, 90,  0, 1000).Returns(null);
                yield return new TestCaseData(TestSceneName, 48.8584642,  2.30976131, 16, 90,  0, 1000).Returns(null);
                yield return new TestCaseData(TestSceneName, 37.7873314, -122.408089, 16, 90,  0, 1000).Returns(null);
                yield return new TestCaseData(TestSceneName, 46.8526446, -121.757103, 12, 90,  0, 1000).Returns(null);
            }
        }

        [UnityTest]
        [TestCaseSource(nameof(LatLngTestSource))]
        public IEnumerator GroupTest(string sceneName, double lat, double lon, int zoom, float pitch = 90, float bearing = 0, float scale = 1000)
        {
            yield return TestScene(sceneName, lat, lon, zoom, pitch, bearing, scale);
        }
        
        //[UnityTest]
        // public IEnumerator TestAllScenes()
        // {
        //     string[] files = Directory.GetFiles(Path.Combine(Application.dataPath + "/Mapbox/BaseModuleTests/PlayModeTests/Scenes/"), "*.unity");
        //     foreach(string file in files)
        //     {
        //         var name = Path.GetFileNameWithoutExtension(file);
        //         foreach (var location in _latitudeLongitudes)
        //         {
        //             yield return TestScene(name, location.Key);
        //         }
        //     }
        // }

        public IEnumerator TestScene(string sceneName, double lat, double lon, int zoom, float pitch = 0, float bearing = 0, float scale = 1000)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            yield return ProcessScene(sceneName, lat, lon, zoom, pitch, bearing, scale);
            yield return SceneManager.UnloadSceneAsync(sceneName);
        }
        
        private IEnumerator ProcessScene(string sceneName, double lat, double lon, int zoom, float pitch = 0, float bearing = 0, float scale = 1000)
        {
            var camera = Camera.main;
            var mapCore = GameObject.FindObjectOfType<MapBehaviourCore>();
            MapboxMap _map = null;
            if (mapCore.InitializationStatus == InitializationStatus.WaitingForInitialization)
            {
                mapCore.Initialized += map =>
                {
                    _map = map;
                };
                mapCore.MapInformation.SetInformation(new LatitudeLongitude(lat, lon), zoom, pitch, bearing, scale);
                mapCore.Initialize();
            }
            else
            {
                _map = mapCore.MapboxMap;
            }
        
            yield return TestWait.While(() => mapCore.InitializationStatus < InitializationStatus.ReadyForUpdates, "ProcessScene: mapCore.InitializationStatus < InitializationStatus.ReadyForUpdates");

            yield return new WaitForSeconds(2);
        
            Assert.IsNotEmpty(_map.TileCover.Tiles);
            Assert.NotZero(_map.TileCover.Tiles.Count);

            var path = TakeScreenshot(string.Format("{0}", string.Format("{0}_{1}_{2}", sceneName, lat.ToString("F"), lon.ToString("F"))), camera);
            Assert.IsNotEmpty(path);
        }

        static bool[] sqliteCache = new bool[] { true, false };
        static bool[] fileCache = new bool[] { true, false };
        
        [UnityTest]
        public IEnumerator ZoomOutTest([ValueSource("fileCache")] bool fileCache, [ValueSource("sqliteCache")] bool sqliteCache)
        {
            var sceneName = TestSceneName;
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            
            var cacheManager = GameObject.FindObjectOfType<LoggingCacheManagerBehaviour>();
            cacheManager.CreateFileCache = fileCache;
            cacheManager.CreateSqliteCache = sqliteCache;
            
            var mapCore = GameObject.FindObjectOfType<MapBehaviourCore>();
            MapboxMap _map = null;
            var cameraController = GameObject.FindObjectOfType<Moving3dCameraBehaviour>();
            if (mapCore.InitializationStatus == InitializationStatus.WaitingForInitialization)
            {
                mapCore.Initialized += map => { _map = map; };
                mapCore.MapInformation.SetInformation(null, 16);
                mapCore.Initialize();
            }
            else
            {
                _map = mapCore.MapboxMap;
            }
            yield return TestWait.While(() => mapCore.InitializationStatus < InitializationStatus.ReadyForUpdates, "ZoomOutTest: mapCore.InitializationStatus < InitializationStatus.ReadyForUpdates");

            yield return new WaitForSeconds(1);

            RecorderController _testRecorderController;
            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            _testRecorderController = new RecorderController(controllerSettings);
            var videoRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            videoRecorder.name = "MapboxDebugVideoRecorder";
            videoRecorder.Enabled = true;
            videoRecorder.VideoBitRateMode = VideoBitrateMode.High;
            videoRecorder.AudioInputSettings.PreserveAudio = false;
            videoRecorder.OutputFile = Path.Combine(GetFolderName(TestContext.CurrentContext.Test.MethodName), "zoomVideo_" + DateTime.Now.ToString("ddMMyyy_hhmm"));
            
            controllerSettings.AddRecorderSettings(videoRecorder);
            controllerSettings.FrameRate = 60;

            RecorderOptions.VerboseMode = false;
            _testRecorderController.PrepareRecording();
            _testRecorderController.StartRecording();
            
            var zoomOutDeadline = Time.realtimeSinceStartup + 60f;
            while (mapCore.MapInformation.Zoom > 6)
            {
                // The loop drives the zoom itself, so it cannot use TestWait; it still needs an end,
                // since a map that clamps or stalls above 6 would otherwise spin forever.
                Assert.Less(Time.realtimeSinceStartup, zoomOutDeadline,
                    $"Timed out zooming out to 6; stuck at {mapCore.MapInformation.Zoom}.");
                mapCore.MapboxMap.ChangeView(null, mapCore.MapInformation.Zoom - 1 * Time.deltaTime);
                yield return null;
            }
            
            _testRecorderController?.StopRecording();
            yield return SceneManager.UnloadSceneAsync(sceneName);
        }
        
        
        
        
        [UnityTest]
        public IEnumerator ZoomInTest([ValueSource("fileCache")] bool fileCache, [ValueSource("sqliteCache")] bool sqliteCache)
        {
            var sceneName = TestSceneName;
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            var cacheManager = GameObject.FindObjectOfType<LoggingCacheManagerBehaviour>();
            cacheManager.CreateFileCache = fileCache;
            cacheManager.CreateSqliteCache = sqliteCache;
            
            var mapCore = GameObject.FindObjectOfType<MapBehaviourCore>();
            MapboxMap _map = null;
            var cameraController = GameObject.FindObjectOfType<Moving3dCameraBehaviour>();
            if (mapCore.InitializationStatus == InitializationStatus.WaitingForInitialization)
            {
                mapCore.Initialized += map => { _map = map; };
                mapCore.MapInformation.SetInformation(null, 8);
                yield return mapCore.Initialize();
            }
            else
            {
                _map = mapCore.MapboxMap;
            }
            yield return TestWait.While(() => mapCore.InitializationStatus < InitializationStatus.ReadyForUpdates, "ZoomInTest: mapCore.InitializationStatus < InitializationStatus.ReadyForUpdates");

            yield return new WaitForSeconds(1);

            RecorderController _testRecorderController;
            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            _testRecorderController = new RecorderController(controllerSettings);
            var videoRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            videoRecorder.name = "MapboxDebugVideoRecorder";
            videoRecorder.Enabled = true;
            videoRecorder.VideoBitRateMode = VideoBitrateMode.High;
            videoRecorder.AudioInputSettings.PreserveAudio = false;
            videoRecorder.OutputFile = Path.Combine(GetFolderName(TestContext.CurrentContext.Test.MethodName), "zoomVideo_" + DateTime.Now.ToString("ddMMyyy_hhmm"));
            
            controllerSettings.AddRecorderSettings(videoRecorder);
            controllerSettings.FrameRate = 60;

            RecorderOptions.VerboseMode = false;
            _testRecorderController.PrepareRecording();
            _testRecorderController.StartRecording();
            
            // while (mapCore.MapInformation.Zoom < 16.9)
            // {
            //     cameraController.Zoom(mapCore.MapInformation, 1 * Time.deltaTime);
            //     yield return null;
            // }
            
            var zoomInDeadline = Time.realtimeSinceStartup + 60f;
            while (mapCore.MapInformation.Zoom < 16.9)
            {
                Assert.Less(Time.realtimeSinceStartup, zoomInDeadline,
                    $"Timed out zooming in to 16.9; stuck at {mapCore.MapInformation.Zoom}.");
                mapCore.MapboxMap.ChangeView(null, mapCore.MapInformation.Zoom + 1 * Time.deltaTime);
                yield return null;
            }

            yield return new WaitForSeconds(1);
            
            _testRecorderController?.StopRecording();
            yield return SceneManager.UnloadSceneAsync(sceneName);
        }
        
        private string TakeScreenshot(string sceneName, Camera camera)
        {
            var rt = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = rt;
            var screenShot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            camera.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            camera.targetTexture = null;
            RenderTexture.active = null;
            GameObject.Destroy(rt);
            byte[] bytes = screenShot.EncodeToJPG();
            
            var path = SaveFile(bytes, sceneName);
            return path;
        }

        private string GetFolderName(string currentMethodName) => Path.Combine(Application.persistentDataPath, TestScreenshotsFolder, currentMethodName);
        
        private string SaveFile(byte[] bytes, string sceneName)
        {
            var currentMethodName = TestContext.CurrentContext.Test.MethodName;
            var dirPath = GetFolderName(currentMethodName);
            if(!Directory.Exists(dirPath)) {
                Directory.CreateDirectory(dirPath);
            }

            var fileName = string.Format("{0}.jpg", sceneName);
            var fullPath = Path.Combine(dirPath, fileName);
            File.WriteAllBytes(fullPath, bytes);
            return fullPath;
        }
    }
}
#endif

