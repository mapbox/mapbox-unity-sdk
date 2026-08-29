using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache;
using UnityEngine;

namespace Mapbox.BaseModule.Unity.ModuleBehaviours
{
    public class RuntimeCacheManagerBehaviour : MapboxCacheManagerBehaviour
    {
        public MapboxCacheManager CacheManager;
        public MemoryCache MemoryCache;

        public MapboxCacheManager GetCacheManager() => CacheManager;

        [Tooltip("Persist tiles to the SQLite database. Turn off only for a map that should never survive a session, such as a benchmark run; every other map wants this on.")]
        public bool CreateSqliteCache = true;

        [Tooltip("Persist tiles to the file cache, which backs the SQLite database. Leave on unless SQLite caching is also off.")]
        public bool CreateFileCache = true;

        [Tooltip("Give this map its own cache folders instead of the shared defaults. Turn on when two maps run in one project and must not evict each other's tiles.")]
        public bool UseCustomName = false;

        [Tooltip("Suffix appended to the default cache folder names, producing Mapbox/SqliteCache<suffix> and Mapbox/FileCache<suffix> under persistentDataPath. It is a suffix, not a replacement. Use a short identifier with no path separators, e.g. \"_city\" or \"_minimap\".")]
        public string CustomName;
        
        public override MapboxCacheManager GetCacheManager(UnityContext unityContext, DataFetchingManager dataFetchingManager)
        {
            if (CacheManager == null)
            {
                SqliteCache sqliteCache = null;
                FileCache fileCache = null;
                if (CreateSqliteCache)
                {
                    if (UseCustomName)
                    {
                        sqliteCache = new SqliteCache(unityContext.TaskManager, 1000, CustomName);
                    }
                    else
                    {
                        sqliteCache = new SqliteCache(unityContext.TaskManager, 1000);
                    }
                }

                if (CreateFileCache)
                {
                    if (UseCustomName)
                    {
                        fileCache = new FileCache(unityContext.TaskManager, CustomName);
                    }
                    else
                    {
                        fileCache = new FileCache(unityContext.TaskManager);
                    }
                }
                
                MemoryCache = new MemoryCache();
                CacheManager = new MapboxCacheManager(
                    unityContext,
                    MemoryCache,
                    fileCache,
                    sqliteCache);
            }

            return CacheManager;
        }
    }
}