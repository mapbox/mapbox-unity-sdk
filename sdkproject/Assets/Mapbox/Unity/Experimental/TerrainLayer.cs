namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Factories;
	// Layer Concrete Implementation. 
	[Serializable]
	public class TerrainLayer : ITerrainLayer
	{
		public MapLayerType LayerType
		{
			get
			{
				return MapLayerType.Elevation;
			}
		}

		[SerializeField]
		bool _isLayerActive;
		public bool IsLayerActive
		{
			get
			{
				return _isLayerActive;
			}
			set
			{
				_isLayerActive = value;
			}
		}

		[SerializeField]
		string _layerSource;
		public string LayerSource
		{
			get
			{
				return _layerSource;
			}
			set
			{
				_layerSource = value;
			}
		}
		[SerializeField]
		ElevationLayerProperties _layerProperty;
		public LayerProperties LayerProperty
		{
			get
			{
				return _layerProperty;
			}
			set
			{
				_layerProperty = (ElevationLayerProperties)value;
			}
		}

		public void Initialize(LayerProperties properties)
		{
			var elevationLayerProperties = (ElevationLayerProperties)properties;

			switch (elevationLayerProperties.elevationLayerType)
			{
				case ElevationLayerType.FlatTerrain:
					_elevationFactory = ScriptableObject.CreateInstance<FlatTerrainFactory>();
					break;
				case ElevationLayerType.LowPolygonTerrain:
					_elevationFactory = ScriptableObject.CreateInstance<LowPolyTerrainFactory>();
					break;
				case ElevationLayerType.TerrainWithElevation:
					if (elevationLayerProperties.sideWallOptions.isActive)
					{
						_elevationFactory = ScriptableObject.CreateInstance<TerrainWithSideWallsFactory>();
					}
					else
					{
						Debug.Log("Setting Terrain Factory");
						_elevationFactory = ScriptableObject.CreateInstance<TerrainFactory>();
					}

					break;
				case ElevationLayerType.GlobeTerrain:
					_elevationFactory = ScriptableObject.CreateInstance<FlatSphereTerrainFactory>();
					break;
				default:
					break;
			}
			_elevationFactory.SetOptions(elevationLayerProperties);
		}

		public void Remove()
		{
			throw new System.NotImplementedException();
		}

		public void Update(LayerProperties properties)
		{
			throw new System.NotImplementedException();
		}
		public AbstractTileFactory ElevationFactory
		{
			get
			{
				return _elevationFactory;
			}
		}
		private AbstractTileFactory _elevationFactory;

	}
}
