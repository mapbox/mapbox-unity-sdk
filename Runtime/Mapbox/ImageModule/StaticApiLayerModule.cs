using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using UnityEngine;

namespace Mapbox.ImageModule
{
	public class StaticApiLayerModule : ILayerModule
	{
		protected StaticLayerModuleSettings _settings;
		protected Source<RasterData> _rasterSource;
		public Source<RasterData> RasterSource => _rasterSource;
		private HashSet<CanonicalTileId> _retainedTiles;

		public StaticApiLayerModule(Source<RasterData> source, StaticLayerModuleSettings settings) : base()
		{
			_settings = settings;
			_rasterSource = source;
		}

		public virtual IEnumerator Initialize(ITileLifecycleSource tileLifecycle)
		{
			yield return _rasterSource.Initialize();
			if (_settings.LoadBackgroundTextures)
			{
				_rasterSource.DownloadAndCacheBaseTiles();
			}
		}
		
		public virtual void LoadTempTile(UnityMapTile unityTile)
		{
			if (IsZinSupportedRange(unityTile.CanonicalTileId.Z) == false)
			{
				//unityTile.ImageContainer.DisableImagery();
				return;
			}
			
			var parentTileId = unityTile.CanonicalTileId;
			for (int i = unityTile.CanonicalTileId.Z; i >= 2; i--)
			{
				parentTileId.MoveToParent();
				if (_rasterSource.GetInstantData(parentTileId, out var instantData))
				{
					unityTile.ImageContainer.SetImageData(instantData, TileContainerState.Temporary);
					return;
				}
			}
		}

		public virtual bool LoadInstant(UnityMapTile unityTile)
		{
			if (IsZinSupportedRange(unityTile.CanonicalTileId.Z) == false)
			{
				//unityTile.ImageContainer.DisableImagery();
				return true;
			}
			
			if (_rasterSource.GetInstantData(unityTile.CanonicalTileId, out var instantData))
			{
				unityTile.ImageContainer.SetImageData(instantData);
				return true;
			}
			return false;
		}
		
		public virtual bool RetainTiles(HashSet<CanonicalTileId> retainedTiles)
		{
			_retainedTiles = retainedTiles;
			var isReady = _rasterSource.RetainTiles(_retainedTiles);
			return isReady;
		}

		public void UpdatePositioning(IMapInformation mapInfo)
		{
            
		}

		public IEnumerator ChangeTilesetId(string tilesetId)
		{
			yield return _rasterSource.ChangeTilesetId(tilesetId);
		}
		
		public virtual void OnDestroy()
		{
			_rasterSource.OnDestroy();
		}


		//COROUTINE METHODS only used in initialization so far
		#region coroutines
		public virtual IEnumerator LoadTileData(CanonicalTileId tileId, Action<MapboxTileData> callback = null) => _rasterSource.LoadTileCoroutine(tileId, callback);		
		public virtual IEnumerator LoadTiles(IEnumerable<CanonicalTileId> tiles)
		{
			yield return _rasterSource.LoadTilesCoroutine(tiles);
		}
		
		public IEnumerable<IEnumerator> GetTileCoverCoroutines(IEnumerable<CanonicalTileId> tiles)
		{
			return tiles.Where(x => IsZinSupportedRange(x.Z)).Select(x => LoadTileData(x));
		}
		#endregion
		
		private bool IsZinSupportedRange(int targetZ)
		{
			return _settings.RejectTilesOutsideZoom.x <= targetZ && _settings.RejectTilesOutsideZoom.y >= targetZ;
		}
	}
}