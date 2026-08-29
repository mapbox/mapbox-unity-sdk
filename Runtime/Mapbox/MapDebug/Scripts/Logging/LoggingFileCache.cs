using System;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Tiles;
using Newtonsoft.Json.Linq;

namespace Mapbox.MapDebug.Scripts.Logging
{
    public class MockFileCache : LoggingFileCache
    {
        public MockFileCache(TaskManager taskManager, string folderNamePostFix = "_MOCK") : base(taskManager, folderNamePostFix)
        {
        }

    }
    
    public class LoggingFileCache : FileCache, ILogWriter
    {
        private int _savedCount = 0;
        private int _readCount = 0;

        public int SavedCount => _savedCount;
        public int ReadCount => _readCount;
		
        public LoggingFileCache(TaskManager taskManager, string folderNamePostFix = "") : base(taskManager, folderNamePostFix)
        {
        }

        protected override void OnFileSaved(MapboxTileData infoTextureCacheItem, string path)
        {
            _savedCount++;
            base.OnFileSaved(infoTextureCacheItem, path);
        }

        public override bool GetAsync<T>(CanonicalTileId tileId, string tilesetId, bool isTextureNonreadable, Action<T> callback)
        {
            var returnValue = base.GetAsync(tileId, tilesetId, isTextureNonreadable, callback);
            if (returnValue)
            {
                _readCount++;
            }
            return returnValue;
        }

        public void ResetStats()
        {
            _savedCount = 0;
            _readCount = 0;
        }

        public JObject DumpLogs()
        {
            return null;
        }

        public string PrintScreen()
        {
            return string.Format("File Cache | Read: {0}, Write:{1}", _readCount, _savedCount);
        }
    }
}