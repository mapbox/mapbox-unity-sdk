namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.MeshGeneration.Factories.TerrainStrategies;

	// Layer Concrete Implementation.
	[Serializable]
	public class TerrainLayer : AbstractLayer, ITerrainLayer
	{
		[SerializeField]
		[NodeEditorElement("Terrain Layer")]
		ElevationLayerProperties _layerProperty = new ElevationLayerProperties();
		[NodeEditorElement("Terrain Layer")]
		public ElevationLayerProperties LayerProperty
		{
			get
			{
				return _layerProperty;
			}
		}
		public MapLayerType LayerType
		{
			get
			{
				return MapLayerType.Elevation;
			}
		}

		public bool IsLayerActive
		{
			get
			{
				return (_layerProperty.sourceType != ElevationSourceType.None);
			}
		}

		public string LayerSource
		{
			get
			{
				return _layerProperty.sourceOptions.Id;
			}
		}

		public TerrainLayer()
		{
		}

		public TerrainLayer(ElevationLayerProperties properties)
		{
			_layerProperty = properties;
		}

		public void SetLayerSource(ElevationSourceType terrainSource)
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

		public void SetLayerSource(string terrainSource)
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

		public void SetTerrainOptions(ElevationLayerType type, ElevationRequiredOptions requiredOptions = null, ElevationModificationOptions modificationOptions = null)
		{
			_layerProperty.elevationLayerType = type;
			Debug.Log("Terrain Type : " + _layerProperty.elevationLayerType);
			if (requiredOptions != null)
			{
				_layerProperty.requiredOptions = requiredOptions;
			}

			if (modificationOptions != null)
			{
				_layerProperty.modificationOptions = modificationOptions;
			}
			_layerProperty.HasChanged = true;
		}

		public void ShowSideWalls(float wallHeight, Material wallMaterial)
		{
			_layerProperty.sideWallOptions.isActive = true;
			_layerProperty.sideWallOptions.wallHeight = wallHeight;
			_layerProperty.sideWallOptions.wallMaterial = wallMaterial;
			_layerProperty.HasChanged = true;
		}

		public void AddToUnityLayer(int layerId)
		{
			_layerProperty.unityLayerOptions.addToLayer = true;
			_layerProperty.unityLayerOptions.layerId = layerId;
			_layerProperty.HasChanged = true;
		}

		public void Initialize(LayerProperties properties)
		{
			_layerProperty = (ElevationLayerProperties)properties;

			Initialize();
		}

		public void Initialize()
		{
			_elevationFactory = ScriptableObject.CreateInstance<TerrainFactoryBase>();
			SetFactoryOptions();

			_layerProperty.colliderOptions.PropertyHasChanged += (property, e) =>
			{
				NotifyUpdateLayer(_elevationFactory, property as MapboxDataProperty, false);
			};
			_layerProperty.requiredOptions.PropertyHasChanged += (property, e) =>
			{
				NotifyUpdateLayer(_elevationFactory, property as MapboxDataProperty, false);
			};
			_layerProperty.unityLayerOptions.PropertyHasChanged += (property, e) =>
			{
				NotifyUpdateLayer(_elevationFactory, property as MapboxDataProperty, false);
			};
			_layerProperty.PropertyHasChanged += (property, e) =>
			{
				//terrain factory uses strategy objects and they are controlled by layer
				//so we have to refresh that first
				//pushing new settings to factory directly
				Debug.Log("here");
				SetFactoryOptions();
				//notifying map to reload existing tiles
				NotifyUpdateLayer(_elevationFactory, property as MapboxDataProperty, false);
			};
		}
		// public void RedrawLayer(object sender, System.EventArgs e)
		// {
		// 	SetFactoryOptions();
		// 	//notifying map to reload existing tiles
		// 	NotifyUpdateLayer(_elevationFactory, property as MapboxDataProperty, false);
		// }

		private void SetFactoryOptions()
		{
			//terrain factory uses strategy objects and they are controlled by layer
			//so we have to refresh that first
			SetStrategy();
			//pushing new settings to factory directly
			Factory.SetOptions(_layerProperty);
		}

		private void SetStrategy()
		{
			switch (_layerProperty.elevationLayerType)
			{
				case ElevationLayerType.FlatTerrain:
					_elevationFactory.Strategy = new FlatTerrainStrategy();
					break;
				case ElevationLayerType.LowPolygonTerrain:
					_elevationFactory.Strategy = new LowPolyTerrainStrategy();
					break;
				case ElevationLayerType.TerrainWithElevation:
					if (_layerProperty.sideWallOptions.isActive)
					{
						_elevationFactory.Strategy = new ElevatedTerrainWithSidesStrategy();
					}
					else
					{
						_elevationFactory.Strategy = new ElevatedTerrainStrategy();
					}
					break;
				case ElevationLayerType.GlobeTerrain:
					_elevationFactory.Strategy = new FlatSphereTerrainStrategy();
					break;
				default:
					break;
			}
		}

		public void Remove()
		{
			_layerProperty = new ElevationLayerProperties
			{
				sourceType = ElevationSourceType.None
			};
		}

		public void Update(LayerProperties properties)
		{
			Initialize(properties);
		}
		public AbstractTileFactory Factory
		{
			get
			{
				return _elevationFactory;
			}
		}
		private TerrainFactoryBase _elevationFactory;

		#region API Methods

		/// <summary>
		/// Set the data source for the terrain layer. Can be used to disable terrain layer by setting it "None".
		/// </summary>
		/// <param name="dataSource"></param>
		public void SetDataSource(ElevationSourceType dataSource)
		{
			if (_layerProperty.sourceType != dataSource)
			{
				_layerProperty.sourceType = dataSource;
				_layerProperty.HasChanged = true;
			}
		}

		/// <summary>
		/// Sets the main strategy for terrain mesh generation.
		/// Flat terrain doesn't pull data from servers and just uses a quad as terrain.
		/// </summary>
		/// <param name="elecationType">Type of the elevation strategy</param>
		public void SetElevationType(ElevationLayerType elecationType)
		{
			if (LayerProperty.elevationLayerType != elecationType)
			{
				LayerProperty.elevationLayerType = elecationType;
				LayerProperty.HasChanged = true;
			}
		}

		/// <summary>
		/// Add/Remove terrain collider. Terrain uses mesh collider.
		/// </summary>
		/// <param name="enable">Boolean for enabling/disabling mesh collider</param>
		public void EnableCollider(bool enable)
		{
			if (LayerProperty.colliderOptions.addCollider != enable)
			{
				LayerProperty.colliderOptions.addCollider = enable;
				LayerProperty.colliderOptions.HasChanged = true;
			}
		}

		/// <summary>
		/// Sets the elevation multiplier for terrain. It'll regenerate terrain mesh, multiplying each point elevation by provided value.
		/// </summary>
		/// <param name="factor">Elevation multiplier</param>
		public void SetExaggerationFactor(float factor)
		{
			if (LayerProperty.requiredOptions.exaggerationFactor != factor)
			{
				LayerProperty.requiredOptions.exaggerationFactor = factor;
				LayerProperty.requiredOptions.HasChanged = true;
			}
		}

		public void SetLayer(int layerId)
		{
			if (LayerProperty.unityLayerOptions.layerId != layerId)
			{
				LayerProperty.unityLayerOptions.layerId = layerId;
				LayerProperty.unityLayerOptions.HasChanged = true;
			}
		}

		#endregion


	}
}
