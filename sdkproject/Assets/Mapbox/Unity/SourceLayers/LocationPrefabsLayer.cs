namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using System.Collections.Generic;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.MeshGeneration.Filters;
	using Mapbox.Unity.MeshGeneration.Modifiers;

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

		//Process UI Options
		private void SetFilterOptions(PrefabItemOptions item)
		{
			var _filterOptions = new VectorFilterOptions();

			string layerName = "";
			if (item.layerNameFromFindByTypeDictionary.TryGetValue(item.findByType, out layerName))
			{
				item.coreOptions.layerName = layerName;
			}

			string propertyName = "";
			item.propertyNameFromFindByTypeDictionary.TryGetValue(item.findByType, out propertyName);

			if (item.findByType == LocationPrefabFindBy.MapboxCategory)
			{
				List<LocationPrefabCategories> categoriesList = GetSelectedCategoriesList(item.categories);
				if (categoriesList.Contains(LocationPrefabCategories.None))
					return;

				List <string>stringsList = new List<string>();
				var concatenatedString = "";

				foreach (var category in categoriesList)
				{
					stringsList = LocationPrefabCategoryOptions.GetMakiListFromCategory(category);
					if(string.IsNullOrEmpty(concatenatedString))
						concatenatedString = string.Join(",", stringsList.ToArray());
					else
						concatenatedString += "," + string.Join(",", stringsList.ToArray());
				}

				LayerFilter filter = new LayerFilter(LayerFilterOperationType.Contains)
				{
					Key = propertyName,
					PropertyValue = concatenatedString
				};
				_filterOptions.filters.Add(filter);
			}
			else if (item.findByType == LocationPrefabFindBy.POIName)
			{

			}
			item.filterOptions = _filterOptions;
		}


		private List<LocationPrefabCategories> GetSelectedCategoriesList(LocationPrefabCategories cat)
		{
			List<LocationPrefabCategories> containingCategories = new List<LocationPrefabCategories>();

			var eligibleValues = Enum.GetValues(typeof(LocationPrefabCategories));
			if (cat == LocationPrefabCategories.None)
			{
				containingCategories.Add(LocationPrefabCategories.None);
				return containingCategories;
			}

			//For any other categories other than None and Any
			foreach(var value in eligibleValues)
			{
				var category = (LocationPrefabCategories)value;

				if (category == LocationPrefabCategories.AnyCategory || category==LocationPrefabCategories.None)
					continue;
				
				if((category & cat) != 0) //to check if category is contained in cat
				{
					containingCategories.Add(category);
				}
			}

			return containingCategories;
		}

		public void Initialize()
		{
			//loop through the list and set properties
			foreach(var item in _layerProperty.locationPrefabList)
			{
				//These are fixed properties
				item.coreOptions.geometryType = item.primitiveType;
				item.extrusionOptions = new GeometryExtrusionOptions
				{
					extrusionType = item.extrusionType
				};

				item.coreOptions.groupFeatures = item.groupFeatures;
				item.moveFeaturePositionTo = item._movePrefabFeaturePositionTo;

				//These properties are dependent on user choices
				if (item.findByType != LocationPrefabFindBy.AddressOrLatLon)
				{
					SetFilterOptions(item);
				}
			}
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