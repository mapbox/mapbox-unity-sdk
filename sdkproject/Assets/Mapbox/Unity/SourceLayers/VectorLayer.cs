namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.Utilities;

	[Serializable]
	public class VectorLayer : IVectorDataLayer
	{
		[SerializeField]
		LocationPrefabsLayerProperties _locationPrefabsLayerProperties = new LocationPrefabsLayerProperties();	


		[SerializeField]
		VectorLayerProperties _layerProperty = new VectorLayerProperties();

		[NodeEditorElement(" Vector Layer ")]
		public VectorLayerProperties LayerProperty
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
				return MapLayerType.Vector;
			}
		}

		public bool IsLayerActive
		{
			get
			{
				return (_layerProperty.sourceType != VectorSourceType.None);
			}
		}

		public string LayerSource
		{
			get
			{
				return _layerProperty.sourceOptions.Id;
			}
		}

		public void SetLayerSource(VectorSourceType vectorSource)
		{
			if (vectorSource != VectorSourceType.Custom && vectorSource != VectorSourceType.None)
			{
				_layerProperty.sourceType = vectorSource;
				_layerProperty.sourceOptions.layerSource = MapboxDefaultVector.GetParameters(vectorSource);
			}
			else
			{
				Debug.LogWarning("Invalid style - trying to set " + vectorSource.ToString() + " as default style!");
			}
		}

		public void SetLayerSource(string vectorSource)
		{
			if (!string.IsNullOrEmpty(vectorSource))
			{
				_layerProperty.sourceType = VectorSourceType.Custom;
				_layerProperty.sourceOptions.Id = vectorSource;
			}
			else
			{
				_layerProperty.sourceType = VectorSourceType.None;
				Debug.LogWarning("Empty source - turning off vector data. ");
			}
		}

		public void AddLayerSource(string vectorSource)
		{
			if (!string.IsNullOrEmpty(vectorSource))
			{
				if (!_layerProperty.sourceOptions.Id.Contains(vectorSource))
				{
					if (string.IsNullOrEmpty(_layerProperty.sourceOptions.Id))
					{
						SetLayerSource(vectorSource);
						return;
					}
					var newLayerSource = _layerProperty.sourceOptions.Id + "," + vectorSource;
					SetLayerSource(newLayerSource);
				}
			}
			else
			{
				Debug.LogError("Empty source. Nothing was added to the list of data sources");
			}
		}

		public void AddVectorLayer(VectorSubLayerProperties subLayerProperties)
		{
			if (_layerProperty.vectorSubLayers == null)
			{
				_layerProperty.vectorSubLayers = new List<VectorSubLayerProperties>();
			}
			_layerProperty.vectorSubLayers.Add(subLayerProperties);
		}

		public void AddLocationPrefabItem(PrefabItemOptions prefabItem)
		{
			//ensure that there is a list of prefabitems
			if (LocationPrefabsLayerProperties.locationPrefabList == null)
			{
				LocationPrefabsLayerProperties.locationPrefabList = new List<PrefabItemOptions>();
			}

			if(_layerProperty.locationPrefabList == null)
			{
				_layerProperty.locationPrefabList = new List<PrefabItemOptions>();
			}

			//add the prefab item if it doesn't already exist
			if (!LocationPrefabsLayerProperties.locationPrefabList.Contains(prefabItem))
			{
				LocationPrefabsLayerProperties.locationPrefabList.Add(prefabItem);
			}

			//add the prefab item if it doesn't already exist
			if (!_layerProperty.locationPrefabList.Contains(prefabItem))
			{
				_layerProperty.locationPrefabList.Add(prefabItem);
			}
		}

		public void RemoveVectorLayer(int index)
		{
			if (_layerProperty.vectorSubLayers != null)
			{
				_layerProperty.vectorSubLayers.RemoveAt(index);
			}
		}

		public void RemovePrefabItem(int index)
		{
			if (LocationPrefabsLayerProperties.locationPrefabList != null)
			{
				LocationPrefabsLayerProperties.locationPrefabList.RemoveAt(index);
			}
		}


		public void Initialize(LayerProperties properties)
		{
			_layerProperty = (VectorLayerProperties)properties;
			Initialize();
		}

		public void Initialize()
		{
			_vectorTileFactory = ScriptableObject.CreateInstance<VectorTileFactory>();
			//if (_layerProperty.sourceType != VectorSourceType.None || _layerProperty.sourceOptions.Id.Contains(MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreets).Id))
			//{
			//	foreach (var item in _locationPrefabsLayerProperties.locationPrefabList)
			//	{
			//		//Add PrefabItemOptions items as a VectorSubLayerProperties
			//		if (!_layerProperty.vectorSubLayers.Contains(item))
			//		{
			//			AddVectorLayer(item);
			//		}
			//	}
			//}
			_layerProperty.locationPrefabList = LocationPrefabsLayerProperties.locationPrefabList;
			_vectorTileFactory.SetOptions(_layerProperty);
		}

		public void Remove()
		{
			_layerProperty = new VectorLayerProperties
			{
				sourceType = VectorSourceType.None
			};
		}

		public void Update(LayerProperties properties)
		{
			Initialize(properties);
		}

		public VectorTileFactory Factory
		{
			get
			{
				return _vectorTileFactory;
			}
		}
		private VectorTileFactory _vectorTileFactory;

		public LocationPrefabsLayerProperties LocationPrefabsLayerProperties
		{
			get
			{
				return _locationPrefabsLayerProperties;
			}
		}
	}
}