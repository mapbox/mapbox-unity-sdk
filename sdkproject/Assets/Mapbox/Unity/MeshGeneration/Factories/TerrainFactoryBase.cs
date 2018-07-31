using Mapbox.Unity.MeshGeneration.Factories;
using System.Collections;
using UnityEngine;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Map;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Enums;
using Mapbox.Unity.MeshGeneration.Factories.TerrainStrategies;
using System;

namespace Mapbox.Unity.MeshGeneration.Factories
{
	public class TerrainFactoryBase : AbstractTileFactory
	{
		public TerrainStrategy Strategy;
		[SerializeField]
		protected ElevationLayerProperties _elevationOptions = new ElevationLayerProperties();
		protected TerrainDataFetcher DataFetcher;

		public event Action OnMapIdUpdated = delegate { };

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

		public void Reinitialize()
		{
			Strategy.Initialize(_elevationOptions);
		}

		public override void SetOptions(LayerProperties options)
		{
			_elevationOptions = (ElevationLayerProperties)options;
		}

		protected override void OnRegistered(UnityTile tile)
		{
			Progress++;
			if (Strategy is IElevationBasedTerrainStrategy)
			{
				tile.HeightDataState = TilePropertyState.Loading;
				TerrainDataFetcherParameters parameters = new TerrainDataFetcherParameters()
				{
					canonicalTileId = tile.CanonicalTileId,
					mapid = _elevationOptions.sourceOptions.Id,
					tile = tile
				};
				DataFetcher.FetchData(parameters);
			}
			else
			{
				Strategy.RegisterTile(tile);
				Progress--;
			}

		}

		protected override void OnUnregistered(UnityTile tile)
		{
			Progress--;
			Strategy.UnregisterTile(tile);
		}
		#endregion

		#region DataFetcherEvents
		private void OnTerrainRecieved(UnityTile tile, RawPngRasterTile pngRasterTile)
		{
			if (tile != null)
			{
				Progress--;
				tile.SetHeightData(pngRasterTile.Data, _elevationOptions.requiredOptions.exaggerationFactor, _elevationOptions.modificationOptions.useRelativeHeight, _elevationOptions.requiredOptions.addCollider);
				Strategy.RegisterTile(tile);
			}
		}

		private void OnDataError(UnityTile tile, TileErrorEventArgs e)
		{
			if (tile != null)
			{
				Progress--;
				tile.HeightDataState = TilePropertyState.Error;
				//strategy might want to act on this , i.e. flattening tile mesh on data fetching failed?
				Strategy.DataErrorOccurred(tile, e);
			}
		}

		public void UpdateMapId(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			Debug.Log("UpdateMapId");
			if (OnMapIdUpdated != null)
			{
				OnMapIdUpdated();
			}
		}
		#endregion

	}
}
