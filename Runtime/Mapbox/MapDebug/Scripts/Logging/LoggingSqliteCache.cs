using System;
using Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Tiles;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mapbox.MapDebug.Scripts.Logging
{
    public class MockSqliteCache : LoggingSqliteCache
    {
        public MockSqliteCache(TaskManager taskManager, uint? maxTileCount = null) : base(taskManager, maxTileCount, "_MOCK")
        {
        }

    }
    
    public class LoggingSqliteCache : SqliteCache, ILogWriter
    {
        private int _addCount = 0;
        private int _getCount = 0;
        private int _updateCount = 0;
        private int _removeCount = 0;

        public int AddCount => _addCount;
        public int GetCount => _getCount;
        public int UpdateCount => _updateCount;
        public int RemoveCount => _removeCount;
        
        public LoggingSqliteCache(TaskManager taskManager, uint? maxTileCount = null, string folderNamePostFix = "") : base(taskManager, maxTileCount, folderNamePostFix)
        {
        }


        protected override void Add(string tilesetName, CanonicalTileId tileId, byte[] data, string path, string etag, DateTime? expirationDate, bool forceInsert = false, Action<bool> callback = null)
        {
            _addCount++;
            base.Add(tilesetName, tileId, data, path, etag, expirationDate, forceInsert, callback);
        }

        public override T Get<T>(string tilesetName, CanonicalTileId tileId, T data = null)
        {
            _getCount++;
            return base.Get<T>(tilesetName, tileId, data);
        }

        public override void UpdateExpiration(string tilesetName, CanonicalTileId tileId, DateTime expirationDate)
        {
            _updateCount++;
            base.UpdateExpiration(tilesetName, tileId, expirationDate);
        }

        public override int RemoveData(string tilesetName, int zoom, int x, int y)
        {
            _removeCount++;
            return base.RemoveData(tilesetName, zoom, x, y);
        }

        public void ResetStats()
        {
            _addCount = 0;
            _getCount = 0;
            _updateCount = 0;
            _removeCount = 0;
        }

        public JObject DumpLogs()
        {
            return null;
        }

        public string PrintScreen()
        {
            return string.Format("Sqlite Cache | C:{0} R:{1}, U:{2}, D:{3}", _addCount, _getCount, _updateCount, _removeCount);
        }
    }
}