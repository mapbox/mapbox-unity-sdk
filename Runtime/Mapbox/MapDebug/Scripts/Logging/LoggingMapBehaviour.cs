using System;
using System.Collections;
using Mapbox.BaseModule;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Unity.ModuleBehaviours;
using Mapbox.BaseModule.Utilities;
using Mapbox.Example.Scripts.TileProviderBehaviours;
using Mapbox.LocationModule;
using Mapbox.UnityMapService;
using Mapbox.UnityMapService.TileProviders;
using UnityEngine;

namespace Mapbox.MapDebug.Scripts.Logging
{
    /// <summary>
    /// Instrumented version of MapboxMapBehaviour that wraps core components
    /// with logging/stats variants (LoggingDataFetchingManager, LoggingMapVisualizer, etc.).
    /// Mirrors MapboxMapBehaviour's initialization flow exactly.
    /// </summary>
    public class LoggingMapBehaviour : MapBehaviourCore
    {
        [Tooltip("Unity tools for map to use")]
        public UnityContext UnityContext;

        [SerializeField] protected TileCreatorBehaviour _tileCreatorBehaviour;
        [SerializeField] protected TileProviderBehaviour TileProvider;
        [SerializeField] protected MapboxCacheManagerBehaviour CacheManagerBehaviour;
        [SerializeField] protected LocationProviderFactory LocationFactory;

        public bool InitializeOnStart = true;
        public Action<MapService> MapServiceReady = (v) => { };

        private MapLogger _mapLogger;
        private MapService _mapService;
        private DataFetchingManager _dataFetchingManager;
        private MapboxCacheManager _cacheManager;
        private LoggingFileCache _loggingFileCache;
        private LoggingSqliteCache _loggingSqliteCache;

        public DataFetchingManager DataFetchingManager => _dataFetchingManager;
        public MapboxCacheManager MapboxCacheManager => _cacheManager;
        public LoggingFileCache LoggingFileCache => _loggingFileCache;
        public LoggingSqliteCache LoggingSqliteCache => _loggingSqliteCache;


        public virtual void Start()
        {
            if (InitializeOnStart)
                StartCoroutine(Initialize());
        }

        [ContextMenu("Initialize Map")]
        public override IEnumerator Initialize()
        {
            if (InitializationStatus != InitializationStatus.WaitingForInitialization)
                yield break;

            _mapLogger = FindObjectOfType<MapLogger>();

            MapInformation.Initialize();

            var taskManager = new LoggingTaskManager();
            _mapLogger?.AddLogger(taskManager);
            yield return UnityContext.Initialize(taskManager);

            yield return UnityContext.HandlePermission();

            if (Application.isEditor || UnityContext.LocationPermissionState == LocationPermissionState.Granted)
            {
                if (LocationFactory != null)
                {
                    yield return LocationFactory.Initialize();
                    var locationProvider = LocationFactory.DefaultLocationProvider;
                    MapInformation.SetLatitudeLongitude(locationProvider.CurrentLocation.LatitudeLongitude);
                }
            }
            else
            {
                Debug.Log("Location permission is " + UnityContext.LocationPermissionState);
            }

            var mapboxContext = new MapboxContext();
            yield return mapboxContext.Initialize();
            _mapService = GetMapService(mapboxContext, UnityContext);
            MapServiceReady(_mapService);

            MapboxMap = CreateMapObject();
            MapboxMap.Initialized += InitializationCompleted;
            yield return MapboxMap.Initialize();
        }

        private void InitializationCompleted()
        {
            Initialized(MapboxMap);
            MapboxMap.LoadMapView();
        }

        private void OnValidate()
        {
            if (UnityContext == null)
                UnityContext = new UnityContext();
            if (UnityContext.MapRoot == null)
                UnityContext.MapRoot = transform;
            if (UnityContext.CoroutineStarter == null)
                UnityContext.CoroutineStarter = this;
        }

        private void OnDestroy()
        {
            MapboxMap?.OnDestroy();
            UnityContext.OnDestroy();
        }

        protected virtual MapboxMap CreateMapObject()
        {
            MapboxMap = new MapboxMap(MapInformation, UnityContext, _mapService);
            var mapVisualizer = CreateMapVisualizer(MapInformation, UnityContext);
            foreach (var moduleBaseScript in GetComponents<ModuleConstructorScript>())
            {
                if (!moduleBaseScript.enabled) continue;
                mapVisualizer.LayerModules.Add(
                    moduleBaseScript.ConstructModule(_mapService, MapInformation, UnityContext));
            }
            MapboxMap.MapVisualizer = mapVisualizer;
            return MapboxMap;
        }

        protected virtual MapService GetMapService(MapboxContext mapboxContext, UnityContext unityContext)
        {
            var mapCamera = FindCamera();
            var tileProvider = TileProvider != null
                ? TileProvider.Core
                : new UnityTileProvider(new UnityTileProviderSettings(mapCamera));
            _dataFetchingManager = CreateDataFetchingManager(mapboxContext);
            _cacheManager = GetCacheManager(unityContext, _dataFetchingManager);

            return new MapUnityService(
                unityContext,
                mapboxContext,
                tileProvider,
                _cacheManager,
                _dataFetchingManager);
        }

        protected virtual MapboxCacheManager GetCacheManager(UnityContext unityContext, DataFetchingManager dataFetchingManager)
        {
            if (CacheManagerBehaviour != null)
                return CacheManagerBehaviour.GetCacheManager(unityContext, dataFetchingManager);

            _loggingSqliteCache = new MockSqliteCache(unityContext.TaskManager, 1000);
            _loggingFileCache = new MockFileCache(unityContext.TaskManager);

            _mapLogger?.AddLogger(_loggingFileCache);
            _mapLogger?.AddLogger(_loggingSqliteCache);

            var cacheManager = new LoggingCacheManager(
                unityContext,
                new MemoryCache(),
                _loggingFileCache,
                _loggingSqliteCache);
            _mapLogger?.AddLogger(cacheManager);
            return cacheManager;
        }

        protected virtual MapboxMapVisualizer CreateMapVisualizer(IMapInformation mapInfo, UnityContext unityContext)
        {
            ITileCreator tileCreator;
            if (_tileCreatorBehaviour != null)
            {
                tileCreator = _tileCreatorBehaviour.GetTileCreator(unityContext);
            }
            else
            {
                var defaultMaterial = new Material(Shader.Find(Constants.Map.DefaultTerrainShaderName));
                tileCreator = new TileCreator(unityContext, new[] { defaultMaterial });
            }

            var mapVis = new LoggingMapVisualizer(mapInfo, unityContext, tileCreator);
            _mapLogger?.AddLogger(mapVis);
            return mapVis;
        }

        protected virtual DataFetchingManager CreateDataFetchingManager(MapboxContext mapboxContext)
        {
            var fetcher = new LoggingDataFetchingManager(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken);
            _mapLogger?.AddLogger(fetcher);
            return fetcher;
        }

        private Camera FindCamera()
        {
            var mapCamera = Camera.main;
            if (mapCamera == null)
                Debug.Log("No camera is tagged as Main Camera. Using the first one found in the scene.");
            return mapCamera;
        }
    }
}
