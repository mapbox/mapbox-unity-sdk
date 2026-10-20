using System.Collections;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Platform.TileJSON;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.ImageModule.Terrain;
using Mapbox.ImageModule.Terrain.TerrainStrategies;
using Mapbox.MapDebug.Scripts.Logging;
using Mapbox.UnityMapService;
using Mapbox.UnityMapService.TileProviders;
using NUnit.Framework;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.TestTools;
using Mapbox.Tests;

namespace Mapbox.BaseModuleTests.DataTests
{
    public class MapboxMapTests
    {
        private string _helsinkiLatitudeLongitudeString = "60.1734031,24.9428875";
        private string _sanFranciscoLatitudeLongitudeString = "60.1734031,24.9428875";
        private LatitudeLongitude _helsinkiLatLng;
        private LatitudeLongitude _sfLatLng;
        private MapboxMap _map;
        private bool _initialized;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            if (!_initialized)
            {
                _initialized = true;

                _helsinkiLatLng = Conversions.StringToLatLon(_helsinkiLatitudeLongitudeString);
                _sfLatLng = Conversions.StringToLatLon(_sanFranciscoLatitudeLongitudeString);

                var mapInfo = new MapInformation(_helsinkiLatitudeLongitudeString);
                mapInfo.SetInformation(null, 16, 45, null, 1000);
                mapInfo.Initialize();
                var mapboxContext = new MapboxContext();
                yield return mapboxContext.LoadConfigurationCoroutine(false);
                var unityContext = new UnityContext();
                unityContext.Initialize();

                var taskManager = new TaskManager();
                taskManager.Initialize();
                unityContext.TaskManager = taskManager;
                var dataManager = new DataFetchingManager(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken);

                var sqliteCache = new MockSqliteCache(taskManager);
                sqliteCache.ReadySqliteDatabase();

                var mapService = new MapUnityService(
                    unityContext,
                    mapboxContext,
                    new UnityFixedAreaTileProvider(),
                    new MapboxCacheManager(
                        unityContext,
                        new MemoryCache(),
                        new MockFileCache(taskManager),
                        sqliteCache),
                    dataManager);

                _map = new MapboxMap(mapInfo, unityContext, mapService);
                var mapVisualizer = new MapboxMapVisualizer(mapInfo, unityContext, new TileCreator(unityContext));
                mapVisualizer.LayerModules.Add(
                    new TerrainLayerModule(mapService.GetTerrainRasterSource(
                        new ImageSourceSettings()
                        {
                            TilesetId = MapboxDefaultElevation.GetParameters(ElevationSourceType.MapboxTerrain).Id
                        }), new TerrainLayerModuleSettings()));
                _map.MapVisualizer = mapVisualizer;
            }

            var initialization = Runnable.Instance.StartCoroutine(_map.Initialize());
            yield return initialization;

            Assert.IsNotNull(_map);
            Assert.IsNotNull(_map.MapVisualizer);
            Assert.IsTrue(_map.Status >= InitializationStatus.Initialized);
        }

        [Test]
        public void LatLngConversion()
        {
            var latlngToPosition = _map.MapInformation.ConvertLatLngToPosition(_helsinkiLatLng);
            var posToLatlng = _map.MapInformation.ConvertPositionToLatLng(Vector3.zero);
            
            Assert.AreEqual(latlngToPosition.x, 0);
            Assert.AreEqual(latlngToPosition.y, 0);
            Assert.AreEqual(latlngToPosition.z, 0);
            Assert.AreEqual(posToLatlng.Latitude, _helsinkiLatLng.Latitude, 0.001f);
            Assert.AreEqual(posToLatlng.Longitude, _helsinkiLatLng.Longitude, 0.001f);
            
            _map.UpdateTileCover();
            Assert.IsNotNull(_map.TileCover.Tiles);
        }

        [Test]
        public void CacheManager()
        {
            var mapService = (MapUnityService) _map.MapService;
            var cacheManager = mapService.GetCacheManager();
            Assert.IsNotNull(cacheManager);
        }

        [UnityTest]
        public IEnumerator TileJson()
        {
            var mapService = (MapUnityService) _map.MapService;
            var dataFetcher = mapService.GetFetchingManager();
            Assert.IsNotNull(dataFetcher);
            var tileJson = dataFetcher.GetTileJSON();
            var vectorTileset = MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreetsV8);
            TileJSONResponse response = null;
            var request = tileJson.Get(vectorTileset.Id, r =>
            {
                response = r;
            });
            yield return TestWait.While(() => request.IsCompleted == false, "TileJson: request.IsCompleted == false");
            Assert.IsNotNull(response);
        }

        [UnityTest]
        public IEnumerator LoadMapIntoMemory()
        {
            var mapLoaded = false;
            var coroutine = Runnable.Instance.StartCoroutine(_map.LoadMapViewCoroutine(() =>
            {
                mapLoaded = true;
            }));
            yield return TestWait.While(() => mapLoaded == false, "LoadMapIntoMemory: mapLoaded == false");
            
            Assert.IsTrue(mapLoaded);
        }

        [Test]
        public void ChangeViewToSF()
        {
            _map.ChangeView(_sfLatLng);
            var latlngToPosition = _map.MapInformation.ConvertLatLngToPosition(_sfLatLng);
            var posToLatlng = _map.MapInformation.ConvertPositionToLatLng(Vector3.zero);
            
            Assert.AreEqual(latlngToPosition.x, 0);
            Assert.AreEqual(latlngToPosition.y, 0);
            Assert.AreEqual(latlngToPosition.z, 0);
            Assert.AreEqual(posToLatlng.Latitude, _sfLatLng.Latitude, 0.001f);
            Assert.AreEqual(posToLatlng.Longitude, _sfLatLng.Longitude, 0.001f);

            _map.ChangeView(_sfLatLng, 12, 45, 30);
            Assert.AreEqual(_map.MapInformation.Zoom, 12);
            Assert.AreEqual(_map.MapInformation.Pitch, 45);
            Assert.AreEqual(_map.MapInformation.Bearing, 30);
        }
        
        
        private static IEnumerable LatLngElevationsource
        {
            get
            {
                yield return new TestCaseData(new LatitudeLongitude(27.9878, 86.9250), 8848f).Returns(null);   // Mount Everest summit, Nepal/China
                yield return new TestCaseData(new LatitudeLongitude(35.3606, 138.7274), 3776f).Returns(null); // Mount Fuji, Japan
                yield return new TestCaseData(new LatitudeLongitude(36.5786, -118.2923), 4421f).Returns(null); // Mount Whitney, USA
                yield return new TestCaseData(new LatitudeLongitude(51.1789, -1.8262), 102f).Returns(null);   // Stonehenge, UK

                yield return new TestCaseData(new LatitudeLongitude(46.8523, -121.7603), 4372f).Returns(null); // Mount Rainier, USA
                yield return new TestCaseData(new LatitudeLongitude(-13.1631, -72.5450), 2430f).Returns(null); // Machu Picchu, Peru
                yield return new TestCaseData(new LatitudeLongitude(19.8207, -155.4681), 4207f).Returns(null); // Mauna Kea, USA

                yield return new TestCaseData(new LatitudeLongitude(-25.3444, 131.0369), 863f).Returns(null); // Uluru, Australia
                yield return new TestCaseData(new LatitudeLongitude(43.6532, -79.3832), 92f).Returns(null);   // Toronto, Canada
                yield return new TestCaseData(new LatitudeLongitude(55.7558, 37.6176), 156f).Returns(null);   // Moscow, Russia
                yield return new TestCaseData(new LatitudeLongitude(35.6895, 139.6917), 38f).Returns(null);   // Tokyo, Japan
                yield return new TestCaseData(new LatitudeLongitude(-34.6037, -58.3816), 25f).Returns(null);  // Buenos Aires, Argentina

                yield return new TestCaseData(new LatitudeLongitude(51.5074, -0.1278), 18f).Returns(null);    // London, UK
                yield return new TestCaseData(new LatitudeLongitude(37.7749, -122.4194), 16f).Returns(null);  // San Francisco, USA
                yield return new TestCaseData(new LatitudeLongitude(28.6139, 77.2090), 216f).Returns(null);   // New Delhi, India
                yield return new TestCaseData(new LatitudeLongitude(41.9028, 12.4964), 52f).Returns(null);    // Rome, Italy
                yield return new TestCaseData(new LatitudeLongitude(19.4326, -99.1332), 2240f).Returns(null); // Mexico City, Mexico
            }
        }

        [UnityTest]
        [TestCaseSource(nameof(LatLngElevationsource))]
        public IEnumerator TestElevations(LatitudeLongitude latLng, float expectedElevation)
        {
            if (_map.MapVisualizer.TryGetLayerModule<TerrainLayerModule>(out var terrainModule))
            {
                yield return terrainModule.GetElevationData(latLng, (elevation) =>
                {
                    Assert.AreEqual(elevation, expectedElevation, expectedElevation * 0.1f, $"{latLng} : {elevation} - {expectedElevation}");
                });
            }
        }   
    }

    public class DataFetcherTests
    {
        private LoggingDataFetchingManager _datafetcher;
        private CanonicalTileId _tileId;
        private string _tilesetId;
        private bool _initialized;

        [UnitySetUp]
        public IEnumerator OneTimeSetup()
        {
            if (_initialized) yield break;
            _initialized = true;

            var mapboxContext = new MapboxContext();
            yield return mapboxContext.LoadConfigurationCoroutine(false);
            _datafetcher = new LoggingDataFetchingManager(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken);
            var vectorTileset = MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreetsV8);
            _tilesetId = vectorTileset.Id;
            _tileId = Conversions.LatitudeLongitudeToTileId(Conversions.StringToLatLon("60.1734031,24.9428875"), 16).Canonical;
        }

        [UnityTest]
        public IEnumerator VectorFetching()
        {
            var tile = new BaseModule.Data.Tiles.VectorTile(_tileId, _tilesetId);
            bool isDone = false;
            _datafetcher.EnqueueForFetching(new FetchInfo(tile, (result) =>
            {
                isDone = true;
            }));
            //Assert.AreEqual(_datafetcher.TotalRequestCount, 1);
            yield return TestWait.While(() => isDone == false, "VectorFetching: isDone == false");
            
            Assert.NotNull(tile);
            Assert.NotNull(tile.ByteData);
            Assert.IsNotEmpty(tile.ByteData);
        }
        
        
        [UnityTest]
        public IEnumerator VectorFetchingCancelled()
        {
            var tile = new BaseModule.Data.Tiles.VectorTile(_tileId, _tilesetId);
            bool isDone = false;
            _datafetcher.EnqueueForFetching(new FetchInfo(tile, (result) =>
            {
                isDone = true;
            }));
            tile.Cancel();
            yield return TestWait.While(() => isDone == false, "VectorFetchingCancelled: isDone == false");
            
            Assert.NotNull(tile);
            Assert.AreEqual(tile.CurrentTileState, TileState.Canceled);
        }
    }
}