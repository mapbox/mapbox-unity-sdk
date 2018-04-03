namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.MeshGeneration.Factories.TerrainStrategies;

	// Layer Concrete Implementation. 
	[Serializable]
	public class TerrainLayer : ITerrainLayer
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
		}

		public void SetTerrainMaterial(Material baseMaterial)
		{
			_layerProperty.requiredOptions.baseMaterial = baseMaterial;
		}

		public void ShowSideWalls(float wallHeight, Material wallMaterial)
		{
			_layerProperty.sideWallOptions.isActive = true;
			_layerProperty.sideWallOptions.wallHeight = wallHeight;
			_layerProperty.sideWallOptions.wallMaterial = wallMaterial;
		}

		public void AddToUnityLayer(int layerId)
		{
			_layerProperty.unityLayerOptions.addToLayer = true;
			_layerProperty.unityLayerOptions.layerId = layerId;
		}

		public void Initialize(LayerProperties properties)
		{
			_layerProperty = (ElevationLayerProperties)properties;

			Initialize();
		}

		public void Initialize()
		{
			_elevationFactory = ScriptableObject.CreateInstance<TerrainFactoryBase>();
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

			_elevationFactory.SetOptions(_layerProperty);
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

	}
}
