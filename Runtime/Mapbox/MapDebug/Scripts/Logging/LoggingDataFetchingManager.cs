using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Platform.TileJSON;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.MapDebug.Scripts.Benchmark;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mapbox.MapDebug.Scripts.Logging
{
    public class LoggingDataFetchingManager : DataFetchingManager, ILogWriter
    {
        public LoggingResilientWebRequestFileSource FileSource => (_cacheOnlySource != null ? _cacheOnlySource.Inner : _fileSource) as LoggingResilientWebRequestFileSource;
        public Queue<FetchInfo> GetFetchingQueue() => _fetchQueue;
        public int FetchQueueCount => _fetchQueue.Count;
        public int ActiveFetchCount => _globalActiveRequests.Count;
        public int AddedCount;
        public int InitializedCount;
        public int CancelledCount;

        public Dictionary<string, HashSet<FetchInfo>> _infosByTileset;
        public Dictionary<FetchInfo, InfoRecord> Records;

        private CacheOnlyFileSource _cacheOnlySource;
        public CacheOnlyFileSource CacheOnlySource => _cacheOnlySource;

        public LoggingDataFetchingManager(string getAccessToken, Func<string> getSkuToken) : base(getAccessToken, getSkuToken)
        {
            var loggingSource = new LoggingResilientWebRequestFileSource(getAccessToken, getSkuToken);
            _cacheOnlySource = new CacheOnlyFileSource(loggingSource);
            _fileSource = _cacheOnlySource;
            _infosByTileset = new Dictionary<string, HashSet<FetchInfo>>();
            Records = new Dictionary<FetchInfo, InfoRecord>();
			
            base.FetchInitialized += (f) =>
            {
                InitializedCount++;
                Records[f].StartTime = Time.realtimeSinceStartup;
            };

            base.FetchCancelled += info => { CancelledCount++; };
        }

        public override void EnqueueForFetching(FetchInfo info)
        {
            if (!_infosByTileset.ContainsKey(info.Tile.TilesetId))
            {
                _infosByTileset.Add(info.Tile.TilesetId, new HashSet<FetchInfo>());
            }
            _infosByTileset[info.Tile.TilesetId].Add(info);
            Records.Add(info, new InfoRecord(info) {QueueTime = Time.realtimeSinceStartup});

            info.Callback += (result) =>
            {
                Records[info].CompleteTime = Time.realtimeSinceStartup;
                Records[info].State = info.Tile.CurrentTileState;
            };

            AddedCount++;
            base.EnqueueForFetching(info);
        }

        public override TileJSON GetTileJSON(int timeout = 10)
        {
            return base.GetTileJSON(timeout);
        }

        public void SetDelayTime(float f) => _requestDelay = f;
        
        // public Queue<int> GetTileOrderQueue()
        // {
        //     return _tileOrder;
        // }
        //
        // public Dictionary<int, FetchInfo> GetFetchInfoQueue()
        // {
        //     return _tileFetchInfos;
        // }

        public int GetActiveRequestLimit()
        {
            return _activeRequestLimit;
        }

        // public Dictionary<int, Tile> GetActiveRequests()
        // {
        //     return _globalActiveRequests;
        // }

        public void ClearLogsAndStats()
        {
            InitializedCount = 0;
            CancelledCount = 0;
            _infosByTileset.Clear();
            Records.Clear();
        }

        public class InfoRecord
        {
            public FetchInfo Info;
            public TileState State;
            public float QueueTime;
            public float StartTime;
            public float CompleteTime;
            public bool IsCancelled = false;
            public float CancelTime;

            public InfoRecord(FetchInfo info)
            {
                Info = info;
            }
        }

        public void ResetStats()
        {
            ClearLogsAndStats();
        }

        public JObject DumpLogs()
        {
            var dataLog = new JObject();
            var jArray = new JArray();
            foreach (var infoRecord in Records)
            {
                var jo = CreateLogEntry(infoRecord);
                jArray.Add(jo);
            }

            dataLog["DataFetchingLogs"] = jArray;
            return dataLog;
        }

        public string PrintScreen()
        {
            return string.Format("Web Requests | Queue: {0}, Running: {1}, Added: {2}, Initialized: {3}, Cancelled: {4}", 0, _globalActiveRequests.Count, AddedCount, InitializedCount, CancelledCount);
        }

        private JObject CreateLogEntry(KeyValuePair<FetchInfo, LoggingDataFetchingManager.InfoRecord> pair)
        {
            var recordData = new JObject();
            recordData["TileId"] = pair.Key.Tile.Id.ToString();
            recordData["TilesetId"] = pair.Key.Tile.TilesetId;
            recordData["QueueTime"] = pair.Value.QueueTime;
            if (pair.Value.StartTime > 0)
            {
                recordData["TimeInQueue"] = pair.Value.StartTime - pair.Value.QueueTime;
                recordData["StartTime"] = pair.Value.StartTime;
            }

            if (pair.Value.CompleteTime > 0)
            {
                recordData["TimeInWork"] = pair.Value.CompleteTime - pair.Value.StartTime;
                recordData["FinishTime"] = pair.Value.CompleteTime;
            }

            recordData["TileState"] = pair.Key.Tile.CurrentTileState.ToString();
            recordData["StatusCode"] = pair.Key.Tile.StatusCode;
            //TileLogs = pair.Key.Tile.GetLogs.Select(x => x),
            //Errors = pair.Key.Tile.Exceptions?.Select(x => x)
            
            return recordData;
            // string jsonData = JsonConvert.SerializeObject(recordData);
            // return jsonData;
            
            // string message = string.Format("{0} - {1}", pair.Key.Tile.Id, pair.Key.Tile.TilesetId);
            // message += Environment.NewLine;
            // message += string.Format("Q: {0} |- {1} -| S: {2} |- {3} -| F:{4}", pair.Value.QueueTime, pair.Value.StartTime - pair.Value.QueueTime, pair.Value.StartTime, pair.Value.CompleteTime - pair.Value.StartTime, pair.Value.CompleteTime);
            // message += Environment.NewLine;
            // message += string.Format("State: {0} | Response: {1}", pair.Key.Tile.CurrentTileState, pair.Key.Tile.StatusCode);
            // if (pair.Key.Tile.HasError)
            // {
            //     message += Environment.NewLine;
            //     message += "-----------------------------------------------------------";
            //     foreach (var tileLog in pair.Key.Tile.GetLogs)
            //     {
            //         message += Environment.NewLine;
            //         message += string.Format("Log: {0} ", tileLog);
            //     }
            //
            //     message += Environment.NewLine;
            //     message += "-----------------------------------------------------------";
            //     foreach (var tileException in pair.Key.Tile.Exceptions)
            //     {
            //         message += Environment.NewLine;
            //         message += string.Format("Error: {0} ", tileException.Message);
            //         message += Environment.NewLine;
            //         message += "-----------------------------------------------------------";
            //     }
            // }
            // message += Environment.NewLine;
            // return message;
        }
    }
}