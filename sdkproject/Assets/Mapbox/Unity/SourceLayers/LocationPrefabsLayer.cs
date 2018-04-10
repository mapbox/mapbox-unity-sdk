namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using System.Collections.Generic;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Utilities;

	public class LocationPrefabsLayer : IVectorDataLayer
	{
		public LocationPrefabsLayer(VectorLayer layer)
		{
			_vectorLayer = layer;
		}

		[SerializeField]
		LocationPrefabsLayerProperties _prefabsLayerProperty = new LocationPrefabsLayerProperties();

		[NodeEditorElement(" Location Prefabs Layer ")]
		public LocationPrefabsLayerProperties LayerProperty
		{
			get
			{
				return _prefabsLayerProperty;
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
				return _prefabsLayerProperty.locationPrefabList.Count > 0;
			}
		}

		public string LayerSource
		{
			get
			{
				return MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreets).Id;
			}
		}

		//method used to set a common layer source for all the visualizers
		public void SetLayerSource(string vectorSource)
		{
			if (!string.IsNullOrEmpty(vectorSource))
			{
				_prefabsLayerProperty.sourceOptions.Id = vectorSource;
			}
			else
			{
				_prefabsLayerProperty.locationPrefabList.Clear();
				Debug.LogWarning("Empty source - turning off vector data. ");
			}
		}


		public void AddPrefabItem(PrefabItem item)
		{
			if (_prefabsLayerProperty.locationPrefabList == null)
			{
				_prefabsLayerProperty.locationPrefabList = new List<PrefabItem>();
			}
			_prefabsLayerProperty.locationPrefabList.Add(item);
		}

		public void RemovePrefabItem(int index)
		{
			if (_prefabsLayerProperty.locationPrefabList != null)
			{
				_prefabsLayerProperty.locationPrefabList.RemoveAt(index);
			}
		}

		public void Initialize(LayerProperties properties)
		{
			_prefabsLayerProperty = (LocationPrefabsLayerProperties)properties;
			Initialize();
		}

		public void Initialize()
		{
			//TODO Implement the addition of prefab modifier using a setOptions method on that modifier
			//_vectorTileFactory.SetOptions(_prefabsLayerProperty);
		}

		public void Remove()
		{
			_prefabsLayerProperty.locationPrefabList.Clear();
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