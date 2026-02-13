using System.Collections.Generic;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Unity;

namespace Mapbox.BaseModule.Map
{
    public abstract class MapService
    {
        protected UnityContext _unityContext;
        protected MapboxContext _mapboxContext;
        protected List<Source> _dataSources = new List<Source>();
        private bool _allSourcesReady = false;

        public virtual IFileSource FileSource => null;

        public bool IsReady()
        {
            if (_allSourcesReady)
            {
                return true;
            }
            else
            {
                foreach (var source in _dataSources)
                {
                    if (!source.IsReady()) return false;
                }

                _allSourcesReady = true;
                return true;
            }
        }
        public abstract Source<RasterData> GetNewRasterSource(string name, string tilesetName, bool isRetina);
        public abstract Source<TerrainData> GetTerrainRasterSource(ImageSourceSettings settings);
        public abstract Source<RasterData> GetStaticRasterSource(ImageSourceSettings settings);
        public abstract Source<VectorData> GetVectorSource(VectorSourceSettings vectorSourceSettings);
        public abstract bool TileCover(IMapInformation mapInformation, TileCover tileCover);


        public abstract Source<BuildingData> GetBuildingSource(VectorSourceSettings settings);

        public bool GetTelemetryCollectionState() => _mapboxContext.GetTelemetryCollectiongState();
        public virtual bool SetTelemetryCollectionState(bool state) => _mapboxContext.SetTelemetryCollectionState(state);

        public virtual void ClearCachedData()
        {
            
        }
        
        public virtual void OnDestroy()
        {
            
        }
    }
}
