using System.Collections.Generic;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Newtonsoft.Json.Linq;

namespace Mapbox.MapDebug.Scripts.Logging
{
    public class LoggingMapVisualizer : MapboxMapVisualizer, ILogWriter
    {
        private List<string> _logs;
        private TileCover _tileCover;
        public LoggingMapVisualizer(IMapInformation mapInformation, UnityContext unityContext, ITileCreator tileCreator) : base(mapInformation, unityContext, tileCreator)
        {
            _logs = new List<string>();
        }
        
        public JObject DumpLogs()
        {
            var array = new JArray();
            foreach (var log in _logs)
            {
                array.Add(log);
            }
            JObject wrapper = new JObject();
            wrapper["InitializationLogs"] = array;
            return wrapper;
        }

        public override void Load(TileCover tileCover)
        {
            _tileCover = tileCover;
            base.Load(tileCover);
        }

        public void ResetStats()
        {
            
        }

        public string PrintScreen()
        {
            if (_tileCover != null)
            {
                return string.Format("Tiles | Required: {0}, Loaded: {1}", _tileCover.Tiles.Count, ActiveTiles.Count);
            }

            return string.Empty;
        }
    }
}