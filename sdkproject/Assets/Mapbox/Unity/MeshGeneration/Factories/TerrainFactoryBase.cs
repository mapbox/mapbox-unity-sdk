using Mapbox.Unity.MeshGeneration.Factories;
using System.Collections;
using UnityEngine;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Map;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Enums;
using Mapbox.Unity.MeshGeneration.Factories.TerrainStrategies;
using System;
using System.Collections.Generic;

namespace Mapbox.Unity.MeshGeneration.Factories
{
	public class TerrainFactoryBase : AbstractTileFactory
	{
		public TerrainStrategy Strategy;
		[SerializeField]
		protected ElevationLayerProperties _elevationOptions = new ElevationLayerProperties();
		protected TerrainDataFetcher DataFetcher;

		public TerrainDataFetcher GetFetcher()
		{
			return DataFetcher;
		}

		public ElevationLayerProperties Properties
		{
			get
			{
				return _elevationOptions;
			}
		}

		#region UnityMethods
		private void OnDestroy()
		{
			if (DataFetcher != null)
			{
				DataFetcher.DataRecieved -= OnTerrainRecieved;
				DataFetcher.FetchingError -= OnDataError;
			}
		}
		#endregion

		#region AbstractFactoryOverrides
		protected override void OnInitialized()
		{
			Strategy.Initialize(_elevationOptions);
			DataFetcher = ScriptableObject.CreateInstance<TerrainDataFetcher>();
			DataFetcher.DataRecieved += OnTerrainRecieved;
			DataFetcher.FetchingError += OnDataError;
		}

		public override void SetOptions(LayerProperties options)
		{
			_elevationOptions = (ElevationLayerProperties)options;
			Strategy.Initialize(_elevationOptions);
		}

		protected override void OnRegistered(UnityTile tile)
		{
			if (Properties.sourceType == ElevationSourceType.None)
			{
				tile.SetHeightData(null);
				tile.MeshFilter.sharedMesh.Clear();
				tile.ElevationType = TileTerrainType.None;
				tile.HeightDataState = TilePropertyState.None;
				return;
			}

			if (Strategy is IElevationBasedTerrainStrategy)
			{
				tile.HeightDataState = TilePropertyState.Loading;
				TerrainDataFetcherParameters parameters = new TerrainDataFetcherParameters()
				{
					canonicalTileId = tile.CanonicalTileId,
					tilesetId = _elevationOptions.sourceOptions.Id,
					tile = tile
				};
				DataFetcher.FetchData(parameters);
			}
			else
			{
				//reseting height data
				tile.SetHeightData(null);
				Strategy.RegisterTile(tile);
				tile.HeightDataState = TilePropertyState.Loaded;
			}
		}

		protected override void OnUnregistered(UnityTile tile)
		{
			if (_tilesWaitingResponse != null && _tilesWaitingResponse.Contains(tile))
			{
				_tilesWaitingResponse.Remove(tile);
			}
			Strategy.UnregisterTile(tile);
		}

		public override void Clear()
		{
			DestroyImmediate(DataFetcher);
		}

		protected override void OnPostProcess(UnityTile tile)
		{
			Strategy.PostProcessTile(tile);
		}

		public override void UnbindEvents()
		{
			base.UnbindEvents();
		}

		protected override void OnUnbindEvents()
		{
		}
		#endregion

		#region DataFetcherEvents
		private void OnTerrainRecieved(UnityTile tile, RawPngRasterTile pngRasterTile)
		{
			if (tile != null)
			{
				_tilesWaitingResponse.Remove(tile);

				if (tile.HeightDataState != TilePropertyState.Unregistered)
				{
					tile.SetHeightData(pngRasterTile.Data, _elevationOptions.requiredOptions.exaggerationFactor, _elevationOptions.modificationOptions.useRelativeHeight, _elevationOptions.colliderOptions.addCollider);
					Strategy.RegisterTile(tile);
				}


			}
		}

		private void OnDataError(UnityTile tile, RawPngRasterTile rawTile, TileErrorEventArgs e)
		{
			base.OnErrorOccurred(tile, e);
			if (tile != null)
			{
				_tilesWaitingResponse.Remove(tile);
				if (tile.HeightDataState != TilePropertyState.Unregistered)
				{
					Strategy.DataErrorOccurred(tile, e);
					tile.HeightDataState = TilePropertyState.Error;
				}
			}
		}
		#endregion

	}
}
