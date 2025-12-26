using System;
using System.Linq;
using Mapbox.BaseModule;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.Example.Scripts.ModuleBehaviours;
using Mapbox.Example.Scripts.TileProviderBehaviours;
using Mapbox.ImageModule.Terrain.TerrainStrategies;
using Mapbox.UnityMapService;
using Mapbox.UnityMapService.TileProviders;
using UnityEngine;

namespace Mapbox.Example.Scripts.Map
{
    public class MapboxMapBehaviour : MapBehaviourCore
    {
        [Tooltip("Unity tools for map to use")]
        public UnityContext UnityContext;

        [SerializeField] protected TileCreatorBehaviour _tileCreatorBehaviour;
        [SerializeField] protected TileProviderBehaviour TileProvider;
        [SerializeField] protected DataFetchingManagerBehaviour DataFetcher;
        [SerializeField] protected MapboxCacheManagerBehaviour CacheManager;
        private MapService _mapService;
        
        public bool InitializeOnStart = true;
        public Action<MapService> MapServiceReady = (v) => { };

        public virtual void Start()
        {
            if (InitializeOnStart)
                Initialize();
        }
        
        [ContextMenu("Initialize")]
        public override void Initialize()
        {
            if (InitializationStatus != InitializationStatus.WaitingForInitialization)
                return;

            MapInformation.Initialize();
            UnityContext.Initialize();
            
            var moduleScripts = GetComponents<ModuleConstructorScript>();
            if (moduleScripts == null || moduleScripts.Length == 0)
            {
                Debug.LogError("MapboxMapBehaviour: No ModuleConstructorScript components found. Add a map layer module (e.g., VectorLayerModuleScript or StaticApiLayerModuleScript) to render tiles.");
            }
            else
            {
                int enabledCount = moduleScripts.Count(m => m != null && m.enabled);
                Debug.Log($"MapboxMapBehaviour: Found {moduleScripts.Length} module script(s), enabled {enabledCount}.");
            }

            var mapboxContext = new MapboxContext();
            _mapService = GetMapService(mapboxContext, UnityContext);
            MapServiceReady(_mapService);

            MapboxMap = CreateMapObject();
            MapboxMap.Initialized += InitializationCompleted;
            StartCoroutine(MapboxMap.Initialize());
        }

        private void InitializationCompleted()
        {
            Initialized(MapboxMap);
            MapboxMap.LoadMapView();
        }

        private void Update()
        {
            if (InitializationStatus == InitializationStatus.ReadyForUpdates && _mapService.IsReady())
            {
                MapboxMap.MapUpdated();
            }
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
            //passing map info to visualizer for root object, default tile material/texture
            var mapVisualizer = CreateMapVisualizer(MapInformation, UnityContext);
            foreach (var moduleBaseScript in GetComponents<ModuleConstructorScript>())
            {
                if (!moduleBaseScript.enabled) continue;
                mapVisualizer.LayerModules.Add(moduleBaseScript.ConstructModule(_mapService, MapInformation, UnityContext));
            }
            MapboxMap.MapVisualizer = mapVisualizer;
            return MapboxMap;
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
                var defaultMapboxTerrainMaterial = new Material(Shader.Find(Constants.Map.DefaultTerrainShaderName));
                tileCreator = new TileCreator(unityContext, new[] { defaultMapboxTerrainMaterial });
            }
            return new MapboxMapVisualizer(mapInfo, unityContext, tileCreator);
        }

        protected virtual MapService GetMapService(MapboxContext mapboxContext, UnityContext unityContext)
        {
            var mapCamera = FindCamera();
            var tileProvider = TileProvider != null ? TileProvider.Core : new UnityTileProvider(new UnityTileProviderSettings(mapCamera));
            var dataFetchingManager = CreateDataFetchingManager(mapboxContext);
            var cacheManager = GetCacheManager(unityContext, dataFetchingManager);

            return new MapUnityService(
                unityContext,
                mapboxContext,
                tileProvider,
                cacheManager,
                dataFetchingManager);
        }

        protected virtual MapboxCacheManager GetCacheManager(UnityContext unityContext, DataFetchingManager dataFetchingManager)
        {
            if (CacheManager != null)
                return CacheManager.GetCacheManager(unityContext, dataFetchingManager);
            
            SqliteCache sqliteCache = null;
            FileCache fileCache = null;
            sqliteCache = new SqliteCache(unityContext.TaskManager, 1000);
            fileCache = new FileCache(unityContext.TaskManager);

            var cacheManager = new MapboxCacheManager(
                unityContext,
                new MemoryCache(),
                fileCache,
                sqliteCache);
            return cacheManager;
        }
        
        protected virtual DataFetchingManager CreateDataFetchingManager(MapboxContext mapboxContext)
        {
            return DataFetcher != null
                ? DataFetcher.GetDataFetchingManager(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken)
                : new DataFetchingManager(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken);
        }
        
        private Camera FindCamera()
        {
            var mapCamera = Camera.main;
            if (mapCamera == null)
            {
                Debug.Log("No camera is tagged as Main Camera. Using the first one found in the scene.");
            }

            return mapCamera;
        }
    }
}
