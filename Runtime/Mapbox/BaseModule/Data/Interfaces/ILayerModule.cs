using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;

namespace Mapbox.BaseModule.Data.Interfaces
{
	public interface ILayerModule
	{
		bool LoadInstant(UnityMapTile unityTile);
		IEnumerator LoadTiles(IEnumerable<CanonicalTileId> tiles);
		
		IEnumerable<IEnumerator> GetTileCoverCoroutines(IEnumerable<CanonicalTileId> tiles);
		
		bool RetainTiles(HashSet<CanonicalTileId> retainedTiles);
		/// <summary>
		/// Prepares the module for use.
		///
		/// <paramref name="tileLifecycle"/> is the visualizer's narrow observer surface - tile load
		/// and unload events, the map information, and the active tile set. A module that wants to
		/// keep its own bookkeeping in step with tiles subscribes here; one that does not simply
		/// ignores the parameter. It is passed in because this is the first and only point a module
		/// is handed a reference to whatever is driving it: neither the constructor nor
		/// ConstructModule receives one.
		///
		/// Deliberately the narrow ITileLifecycleSource rather than the visualizer itself, so a
		/// module cannot reach into pooling or temp-tile orchestration.
		/// </summary>
		IEnumerator Initialize(ITileLifecycleSource tileLifecycle);
		void OnDestroy();
		void UpdatePositioning(IMapInformation mapInfo);
		void LoadTempTile(UnityMapTile tile);
	}

	public interface ILayerModuleScript : ILayerModule
	{
		bool enabled { get; }
		ILayerModule ConstructModule(MapService service, MapInformation mapInformation, UnityContext unityContext);
	}
}