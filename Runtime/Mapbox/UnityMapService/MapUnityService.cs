using System;
using System.Collections.Generic;
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

		/// <summary>
		/// Returns the one shared vector source, creating it on the first call.
		///
		/// One source per map is deliberate: the Vector Tiles API composes tilesets, so
		/// "{a},{b}/{z}/{x}/{y}.vector.pbf" fetches the union of their layers in a single request,
		/// and a second source would mean a second request and a second cache entry per tile.
		///
		/// The consequence is that whoever calls first defines the tileset for everyone after them.
		/// A later caller asking for a tileset the existing source does not carry used to receive it
		/// silently and then find none of its layers in any tile; it is now told, naming both ids.
		/// Composing them for the caller is not something this method can do - the source is already
		/// fetching - so the fix on the caller's side is to compose the ids up front, which
		/// VectorModuleSettingsDrawer's "Add Mapbox Streets V8" button exists to help with.
		/// </summary>
		public override Source<VectorData> GetVectorSource(VectorSourceSettings settings)
		{
			var existing = _dataSources.FirstOrDefault(x => x is VectorSource) as VectorSource;
			if (existing != null)
			{
				WarnIfTilesetIsMissing(existing.TilesetId, settings.TilesetId);
				return existing;
			}

			var vectorSource = new VectorSource(_fetchingManager, _cacheManager, settings);
			_dataSources.Add(vectorSource);
			return vectorSource;
		}

		/// <summary>
		/// Reports every requested tileset the shared source does not carry. Both ids can be
		/// composed, comma-separated lists, so this is a set comparison rather than string equality.
		/// </summary>
		private static void WarnIfTilesetIsMissing(string existingTilesetId, string requestedTilesetId)
		{
			if (string.IsNullOrEmpty(requestedTilesetId) || existingTilesetId == requestedTilesetId)
			{
				return;
			}

			var carried = new HashSet<string>((existingTilesetId ?? string.Empty)
				.Split(',')
				.Select(id => id.Trim())
				.Where(id => id.Length > 0));

			var missing = requestedTilesetId
				.Split(',')
				.Select(id => id.Trim())
				.Where(id => id.Length > 0 && !carried.Contains(id))
				.ToList();

			if (missing.Count == 0)
			{
				return;
			}

			Debug.LogError(string.Format(
				"A vector source for '{0}' already exists and is the only one this map will have, so " +
				"the request for '{1}' returns it unchanged. None of the tiles it produces will carry " +
				"a layer from {2}. Compose the tileset ids into one comma-separated id " +
				"(\"{0},{2}\") on whichever module constructs the source first.",
				existingTilesetId, requestedTilesetId, string.Join(",", missing)));
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

		public override void OnDestroy()
		{
			base.OnDestroy();
			FetchingManager.OnDestroy();
			CacheManager.OnDestroy();
		}
	}
}
