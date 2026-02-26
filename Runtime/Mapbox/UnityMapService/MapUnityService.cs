using System;
using System.Linq;
using Mapbox.BaseModule;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Telemetry;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.UnityMapService.DataSources;
using Mapbox.UnityMapService.TileProviders;
using UnityEngine;
using TerrainData = Mapbox.BaseModule.Data.DataFetchers.TerrainData;

namespace Mapbox.UnityMapService
{
	public sealed class MapUnityService : MapService, IUnityMapService
	{
		private ITelemetryLibrary _telemetryLibrary;
		private TileProvider _tileProvider;
		private MapboxCacheManager _cacheManager;
		private DataFetchingManager _fetchingManager;

		public override IFileSource FileSource => FetchingManager;

		public MapboxCacheManager CacheManager => _cacheManager;
		public DataFetchingManager FetchingManager => _fetchingManager;

		public MapUnityService(
			UnityContext unityContext,
			MapboxContext mapboxContext,
			TileProvider tileProvider,
			MapboxCacheManager cacheManager = null,
			DataFetchingManager fetchingManager = null)
		{
			_unityContext = unityContext;
			_mapboxContext = mapboxContext;
			_tileProvider = tileProvider;
			_fetchingManager = fetchingManager ?? new DataFetchingManager(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken);
			_cacheManager = cacheManager ?? new MapboxCacheManager(unityContext, new MemoryCache(), new FileCache(_unityContext.TaskManager), new SqliteCache(_unityContext.TaskManager, 1000));
		}

		private T GetObject<T>()
		{
			var classType = typeof(T);
			var factoryType = AppDomain.CurrentDomain
				.GetAssemblies()
				.SelectMany(s => s.GetTypes()).FirstOrDefault(p => classType.IsAssignableFrom(p) && p != classType);

			return (T) Activator.CreateInstance(factoryType);
		}
		
		public override bool TileCover(IMapInformation mapInformation, TileCover tileCover)
		{
			return _tileProvider.GetTileCover(mapInformation, tileCover);
		}

		public override Source<RasterData> GetNewRasterSource(string name, string tilesetName, bool isRetina)
		{
			throw new NotImplementedException();
		}

		public override Source<TerrainData> GetTerrainRasterSource(ImageSourceSettings settings)
		{
			
			var terrainSource = new TerrainSource(FetchingManager, CacheManager, settings);
			_dataSources.Add(terrainSource);
			return terrainSource;
		}

		public override Source<RasterData> GetStaticRasterSource(ImageSourceSettings settings)
		{
			var staticRasterSource = new StaticSource(FetchingManager, CacheManager, settings);
			_dataSources.Add(staticRasterSource);
			return staticRasterSource;
		}

		public override Source<VectorData> GetVectorSource(VectorSourceSettings settings)
		{
			var vectorSource = new VectorSource(FetchingManager, CacheManager, settings);
			_dataSources.Add(vectorSource);
			return vectorSource;
		}
		
		public override Source<BuildingData> GetBuildingSource(VectorSourceSettings settings)
		{
			var vectorSource = new BuildingSource(FetchingManager, CacheManager, settings);
			_dataSources.Add(vectorSource);
			return vectorSource;
		}
		
		public MapboxCacheManager GetCacheManager() => CacheManager;
		public DataFetchingManager GetFetchingManager() => FetchingManager;

		public override void ClearCachedData()
		{
			_cacheManager.ClearCachedData();
		}

		public override void ClearCachedData()
		{
			_cacheManager.ClearCachedData();
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			FetchingManager.OnDestroy();
			CacheManager.OnDestroy();
		}
	}
}
