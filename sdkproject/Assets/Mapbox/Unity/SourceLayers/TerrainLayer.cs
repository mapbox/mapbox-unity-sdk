using Mapbox.Map;
using Mapbox.Platform;
using Mapbox.Unity.MeshGeneration.Data;

namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.MeshGeneration.Factories.TerrainStrategies;

	// Layer Concrete Implementation.
	[Serializable]
	public class TerrainLayer : AbstractLayer, ITerrainLayer, IGlobeTerrainLayer
	{
		[SerializeField] ElevationLayerProperties _layerProperty = new ElevationLayerProperties();
		public ElevationLayerProperties LayerProperty => _layerProperty;
		public MapLayerType LayerType => MapLayerType.Elevation;
		public bool IsLayerActive => (_layerProperty.sourceType != ElevationSourceType.None);
		public string LayerSourceId => _layerProperty.sourceOptions.Id;
		public ElevationSourceType LayerSource => _layerProperty.sourceType;
		public ElevationLayerType ElevationType
		{
			get => _layerProperty.elevationLayerType;
			set
			{
				if (_layerProperty.elevationLayerType != value)
				{
					_layerProperty.elevationLayerType = value;
					_layerProperty.HasChanged = true;
				}
			}
		}
		public float ExaggerationFactor
		{
			get => _layerProperty.requiredOptions.exaggerationFactor;
			set
			{
				_layerProperty.requiredOptions.exaggerationFactor = value;
				_layerProperty.requiredOptions.HasChanged = true;
			}
		}
		public TerrainFactoryBase Factory => _elevationFactory;
		public float EarthRadius
		{
			get => _layerProperty.modificationOptions.earthRadius;

			set => _layerProperty.modificationOptions.earthRadius = value;
		}

		private TerrainFactoryBase _elevationFactory;

		public Action<AbstractTileFactory, TileErrorEventArgs> FactoryError = (factory, args) => { };

		public void Initialize(IFileSource fileSource)
		{
			_elevationFactory = new TerrainFactoryBase(fileSource, _layerProperty);
			//terrain factory uses strategy objects and they are controlled by layer
			//so we have to refresh that first
			_elevationFactory.OnTileError += delegate(object sender, TileErrorEventArgs args) { FactoryError(_elevationFactory, args); };

			_layerProperty.colliderOptions.PropertyHasChanged += SetFactoryOptions;
			_layerProperty.requiredOptions.PropertyHasChanged += SetFactoryOptions;
			_layerProperty.unityLayerOptions.PropertyHasChanged += SetFactoryOptions;
			_layerProperty.PropertyHasChanged += SetFactoryOptions;
		}

		private void SetFactoryOptions(object sender, System.EventArgs e)
		{
			//terrain factory uses strategy objects and they are controlled by layer
			//so we have to refresh that first
			Factory.SetOptions(_layerProperty);
			NotifyUpdateLayer(_elevationFactory, sender as MapboxDataProperty, true);
		}



		#region API Methods

		/// <summary>
		/// Sets the data source for Terrain Layer.
		/// Defaults to MapboxTerrain.
		/// Use <paramref name="terrainSource"/> = None, to disable the Terrain Layer.
		/// </summary>
		/// <param name="terrainSource">Terrain source.</param>
		public virtual void SetLayerSource(ElevationSourceType terrainSource = ElevationSourceType.MapboxTerrain)
		{
			if (terrainSource != ElevationSourceType.Custom && terrainSource != ElevationSourceType.None)
			{
				_layerProperty.sourceType = terrainSource;
				_layerProperty.sourceOptions.layerSource = MapboxDefaultElevation.GetParameters(terrainSource);
				_layerProperty.HasChanged = true;
			}
			else
			{
				Debug.LogWarning("Invalid style - trying to set " + terrainSource.ToString() + " as default style!");
			}
		}

		/// <summary>
		/// Sets the data source to a custom source for Terrain Layer.
		/// </summary>
		/// <param name="terrainSource">Terrain source.</param>
		public virtual void SetLayerSource(string terrainSource)
		{
			if (!string.IsNullOrEmpty(terrainSource))
			{
				_layerProperty.sourceType = ElevationSourceType.Custom;
				_layerProperty.sourceOptions.Id = terrainSource;
			}
			else
			{
				_layerProperty.sourceType = ElevationSourceType.None;
				_layerProperty.elevationLayerType = ElevationLayerType.FlatTerrain;
				Debug.LogWarning("Empty source - turning off terrain. ");
			}
			_layerProperty.HasChanged = true;
		}
		/// <summary>
		/// Sets the main strategy for terrain mesh generation.
		/// Flat terrain doesn't pull data from servers and just uses a quad as terrain.
		/// </summary>
		/// <param name="elevationType">Type of the elevation strategy</param>
		public virtual void SetElevationType(ElevationLayerType elevationType)
		{
			if (_layerProperty.elevationLayerType != elevationType)
			{
				_layerProperty.elevationLayerType = elevationType;
				_layerProperty.HasChanged = true;
			}
		}

		/// <summary>
		/// Add/Remove terrain collider. Terrain uses mesh collider.
		/// </summary>
		/// <param name="enable">Boolean for enabling/disabling mesh collider</param>
		public virtual void EnableCollider(bool enable)
		{
			if (_layerProperty.colliderOptions.addCollider != enable)
			{
				_layerProperty.colliderOptions.addCollider = enable;
				_layerProperty.colliderOptions.HasChanged = true;
			}
		}

		/// <summary>
		/// Sets the elevation multiplier for terrain. It'll regenerate terrain mesh, multiplying each point elevation by provided value.
		/// </summary>
		/// <param name="factor">Elevation multiplier</param>
		public virtual void SetExaggerationFactor(float factor)
		{
			if (_layerProperty.requiredOptions.exaggerationFactor != factor)
			{
				_layerProperty.requiredOptions.exaggerationFactor = factor;
				_layerProperty.requiredOptions.HasChanged = true;
			}
		}

		/// <summary>
		/// Turn on terrain side walls.
		/// </summary>
		/// <param name="wallHeight">Wall height.</param>
		/// <param name="wallMaterial">Wall material.</param>
		public virtual void EnableSideWalls(float wallHeight, Material wallMaterial)
		{
			_layerProperty.sideWallOptions.isActive = true;
			_layerProperty.sideWallOptions.wallHeight = wallHeight;
			_layerProperty.sideWallOptions.wallMaterial = wallMaterial;
			_layerProperty.HasChanged = true;
		}
		public void DisableSideWalls()
		{
			_layerProperty.sideWallOptions.isActive = false;
			_layerProperty.HasChanged = true;
		}

		public void RemoveFromUnityLayer(int layerId)
		{
			if (_layerProperty.unityLayerOptions.layerId == layerId)
			{
				_layerProperty.unityLayerOptions.addToLayer = false;
				_layerProperty.HasChanged = true;
			}
		}

		/// <summary>
		/// Adds Terrain GameObject to Unity layer.
		/// </summary>
		/// <param name="layerId">Layer identifier.</param>
		public virtual void AddToUnityLayer(int layerId)
		{
			if (_layerProperty.unityLayerOptions.layerId != layerId)
			{
				_layerProperty.unityLayerOptions.addToLayer = true;
				_layerProperty.unityLayerOptions.layerId = layerId;
				_layerProperty.HasChanged = true;
			}
		}

		/// <summary>
		/// Change terrain layer settings.
		/// </summary>
		/// <param name="dataSource">The data source for the terrain height map.</param>
		/// <param name="elevationType">Mesh generation strategy for the tile/height.</param>
		/// <param name="enableCollider">Enable/Disable collider component for the tile game object.</param>
		/// <param name="factor">Multiplier for the height data.</param>
		/// <param name="layerId">Unity Layer for the tile game object.</param>
		public virtual void SetProperties(ElevationSourceType dataSource = ElevationSourceType.MapboxTerrain,
			ElevationLayerType elevationType = ElevationLayerType.TerrainWithElevation,
			bool enableCollider = false,
			float factor = 1,
			int layerId = 0)
		{
			if (_layerProperty.sourceType != dataSource ||
				_layerProperty.elevationLayerType != elevationType)
			{
				_layerProperty.sourceType = dataSource;
				_layerProperty.elevationLayerType = elevationType;
				_layerProperty.HasChanged = true;
			}

			if (_layerProperty.colliderOptions.addCollider != enableCollider)
			{
				_layerProperty.colliderOptions.addCollider = enableCollider;
				_layerProperty.colliderOptions.HasChanged = true;
			}

			if (_layerProperty.requiredOptions.exaggerationFactor != factor)
			{
				_layerProperty.requiredOptions.exaggerationFactor = factor;
				_layerProperty.requiredOptions.HasChanged = true;
			}

			if (_layerProperty.unityLayerOptions.layerId != layerId)
			{
				_layerProperty.unityLayerOptions.layerId = layerId;
				_layerProperty.unityLayerOptions.HasChanged = true;
			}
		}


		#endregion
	}
}
