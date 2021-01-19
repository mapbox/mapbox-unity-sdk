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
using CustomImageLayerSample;

namespace Mapbox.Unity.MeshGeneration.Factories
{
	public class TerrainFactoryBase : AbstractTileFactory
	{
		public MapboxTerrainFactoryManager TerrainFactoryManager;
		public TerrainStrategy Strategy;
		[SerializeField]
		protected ElevationLayerProperties _elevationOptions = new ElevationLayerProperties();

		public string TilesetId
		{
			get
			{
				return _elevationOptions.sourceOptions.Id;
			}

			set
			{
				_elevationOptions.sourceOptions.Id = value;
			}
		}

		public ElevationLayerProperties Properties
		{
			get
			{
				return _elevationOptions;
			}
		}

		protected virtual void OnDestroy()
		{
			//unregister events
			if (TerrainFactoryManager != null)
			{
				TerrainFactoryManager.FetchingError -= OnFetchingError;
			}
		}

		protected override void OnInitialized()
		{
		}

		public override void SetOptions(LayerProperties options)
		{
			_elevationOptions = (ElevationLayerProperties)options;
			Strategy.Initialize(_elevationOptions);

			TerrainFactoryManager = new MapboxTerrainFactoryManager(
				Strategy,
				TilesetId,
				false,
				false,
				_elevationOptions.colliderOptions.addCollider,
				_elevationOptions.modificationOptions.useRelativeHeight,
				_elevationOptions.requiredOptions.exaggerationFactor);
			TerrainFactoryManager.FetchingError += OnFetchingError;
		}

		protected override void OnRegistered(UnityTile tile)
		{
			if (Properties.sourceType == ElevationSourceType.None)
			{
				tile.SetHeightData(null);
				tile.MeshFilter.sharedMesh.Clear();
				tile.ElevationType = TileTerrainType.None;
				return;
			}

			TerrainFactoryManager.RegisterTile(tile);
		}

		protected override void OnUnregistered(UnityTile tile)
		{
			TerrainFactoryManager.UnregisterTile(tile);
		}

		private void OnFetchingError(UnityTile tile, RasterTile rasterTile, TileErrorEventArgs errorEventArgs)
		{
			OnDataError(tile, rasterTile, errorEventArgs);
		}

		private void OnDataError(UnityTile tile, RasterTile rawTile, TileErrorEventArgs e)
		{
			base.OnErrorOccurred(tile, e);
			if (tile != null)
			{
				_tilesWaitingResponse.Remove(tile);
				Strategy.DataErrorOccurred(tile, e);
			}
		}

		protected override void OnPostProcess(UnityTile tile)
		{
		}

		protected override void OnUnbindEvents()
		{
		}
	}
}
