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
using Mapbox.Platform;
using Mapbox.Unity.CustomLayer;
using Mapbox.Unity.DataContainers;

namespace Mapbox.Unity.MeshGeneration.Factories
{
	public class TerrainFactoryBase : AbstractTileFactory
	{
		public MapboxTerrainFactoryManager TerrainFactoryManager;
		public TerrainStrategy Strategy;
		protected ElevationLayerProperties _properties = new ElevationLayerProperties();

		public TerrainFactoryBase(ElevationLayerProperties layerProperty)
		{
			_properties = layerProperty;
			SetStrategy();
			Strategy.Initialize(_properties);
			TerrainFactoryManager = new MapboxTerrainFactoryManager(
				_properties,
				Strategy,
				false);
			TerrainFactoryManager.FetchingError += OnFetchingError;
		}

		public string TilesetId
		{
			get
			{
				return _properties.sourceOptions.Id;
			}

			set
			{
				_properties.sourceOptions.Id = value;
			}
		}

		public ElevationLayerProperties Properties
		{
			get
			{
				return _properties;
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

		public override void SetOptions(LayerProperties options)
		{
			_properties = (ElevationLayerProperties)options;
			Strategy.Initialize(_properties);
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

		public void PregenerateTileMesh(UnityTile tile)
		{
			TerrainFactoryManager.PregenerateTileMesh(tile);
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

		private void SetStrategy()
		{
			switch (_properties.elevationLayerType)
			{
				case ElevationLayerType.FlatTerrain:
				{
					if (!(Strategy is FlatTerrainStrategy))
						Strategy = new FlatTerrainStrategy();
					break;
				}
				case ElevationLayerType.LowPolygonTerrain:
				{
					if (!(Strategy is LowPolyTerrainStrategy))
						Strategy = new LowPolyTerrainStrategy();
					break;
				}
				case ElevationLayerType.TerrainWithElevation:
				{
					if (_properties.sideWallOptions.isActive)
					{
						if (!(Strategy is ElevatedTerrainWithSidesStrategy))
							Strategy = new ElevatedTerrainWithSidesStrategy();
					}
					else
					{
						if (!(Strategy is ElevatedTerrainStrategy))
							Strategy = new ElevatedTerrainStrategy();
					}

				}
					break;
				case ElevationLayerType.GlobeTerrain:
				{
					if (!(Strategy is FlatSphereTerrainStrategy))
						Strategy = new FlatSphereTerrainStrategy();
					break;
				}
				default:
					break;
			}
		}
	}
}
