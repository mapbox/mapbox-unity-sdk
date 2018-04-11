namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using System.Collections.Generic;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Utilities;

	[Serializable]
	public class LocationPrefabsLayer : IVectorDataLayer
	{
		public LocationPrefabsLayer(VectorLayer layer)
		{
			_vectorLayer = layer;
		}


		//Fixed source
		private LayerSourceOptions defautlLayerSource = new LayerSourceOptions
		{
			layerSource = MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreets)	
		};

		[SerializeField]
		LocationPrefabsLayerProperties _layerProperty = new LocationPrefabsLayerProperties();

		[NodeEditorElement(" Location Prefabs Layer ")]
		public LocationPrefabsLayerProperties LayerProperty
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
				return _layerProperty.locationPrefabList.Count > 0;
			}
		}

		public string LayerSource
		{
			get
			{
				return _vectorLayer.LayerSource;
			}
		}

		//method used to set a common layer source for all the visualizers
		public void SetLayerSource(string vectorSource)
		{
			_vectorLayer.AddLayerSource(vectorSource);
		}


		public void AddPrefabItem(PrefabItemOptions item)
		{
			if (_layerProperty.locationPrefabList == null)
			{
				_layerProperty.locationPrefabList = new List<PrefabItemOptions>();
			}
			_layerProperty.locationPrefabList.Add(item);

			_vectorLayer.AddLayerSource(defautlLayerSource.Id);
		}

		public void RemovePrefabItem(int index)
		{
			if (_layerProperty.locationPrefabList != null)
			{
				_layerProperty.locationPrefabList.RemoveAt(index);
			}
		}

		public void Initialize(LayerProperties properties)
		{
			_layerProperty = (LocationPrefabsLayerProperties)properties;
			Initialize();
		}

		public void Initialize()
		{
			//set fixed properties
			//TODO Implement the addition of prefab modifier using a setOptions method on that modifier
			//_vectorTileFactory.SetOptions(_prefabsLayerProperty);
		}

		public void Remove()
		{
			_layerProperty.locationPrefabList.Clear();
		}

		public void Update(LayerProperties properties)
		{
			Initialize(properties);
		}

		public VectorLayer vectorLayer
		{
			get
			{
				return _vectorLayer;
			}
		}
		private VectorLayer _vectorLayer;
	}
}