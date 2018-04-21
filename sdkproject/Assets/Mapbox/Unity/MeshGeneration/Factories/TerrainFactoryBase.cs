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

		public override void SetOptions(LayerProperties options)
		{
			_elevationOptions = (ElevationLayerProperties)options;
		}

		internal override void OnInitialized()
		{
			Strategy.Initialize(_elevationOptions);
			DataFetcher = ScriptableObject.CreateInstance<TerrainDataFetcher>();
			DataFetcher.DataRecieved += OnTerrainRecieved;
			DataFetcher.FetchingError += OnDataError;
		}

		private void OnDestroy()
		{
			DataFetcher.DataRecieved -= OnTerrainRecieved;
			DataFetcher.FetchingError -= OnDataError;
		}

		internal override void OnRegistered(UnityTile tile)
		{
			Progress++;
			if (Strategy is IElevationBasedTerrainStrategy)
			{
				tile.HeightDataState = TilePropertyState.Loading;
				DataFetcher.FetchTerrain(tile.CanonicalTileId, _elevationOptions.sourceOptions.Id, tile);
			}
			else
			{
				Strategy.RegisterTile(tile);
				Progress--;
			}

		}

		private void OnTerrainRecieved(UnityTile tile, RawPngRasterTile pngRasterTile)
		{
			if (tile != null)
			{
				Progress--;
				tile.SetHeightData(pngRasterTile.Data, _elevationOptions.requiredOptions.exaggerationFactor, _elevationOptions.modificationOptions.useRelativeHeight);
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

		internal override void OnUnregistered(UnityTile tile)
		{
			Progress--;
			Strategy.UnregisterTile(tile);
		}
	}
}