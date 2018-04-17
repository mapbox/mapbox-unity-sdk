namespace Mapbox.Unity.MeshGeneration.Interfaces
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.MeshGeneration.Filters;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.Map;

	public class LocationPrefabsLayerVisualizer : VectorLayerVisualizer
	{
		public void SetProperties(PrefabItemOptions item)
		{
			SubLayerProperties = item;
			//These are fixed properties
			item.coreOptions.geometryType = item.primitiveType;
			item.extrusionOptions = new GeometryExtrusionOptions
			{
				extrusionType = item.extrusionType
			};

			item.coreOptions.groupFeatures = item.groupFeatures;
			item.moveFeaturePositionTo = item._movePrefabFeaturePositionTo;


			string layerName = "";
			if (item.layerNameFromFindByTypeDictionary.TryGetValue(item.findByType, out layerName))
			{
				item.coreOptions.layerName = layerName;
				base.Key = layerName;
			}

			//These properties are dependent on user choices
			if (item.findByType != LocationPrefabFindBy.AddressOrLatLon)
			{
				SetFilterOptions(item);
			}

			switch (item.coreOptions.geometryType)
			{
				case VectorPrimitiveType.Point:
					if (typeof(PrefabItemOptions).IsAssignableFrom(item.GetType())) //to check that the instance is of type PrefabItemOptions
					{
						var itemProperties = (PrefabItemOptions)item;
						var prefabModifier = ScriptableObject.CreateInstance<PrefabModifier>();
						prefabModifier.SetProperties(itemProperties.spawnPrefabOptions);
						_defaultStack = ScriptableObject.CreateInstance<ModifierStack>();
						if (_defaultStack.GoModifiers == null)
						{
							_defaultStack.GoModifiers = new List<GameObjectModifier>();
						}
						_defaultStack.GoModifiers.Add(prefabModifier);
					}
					break;
				default:
					break;
			}
		}

		//Process UI Options
		private void SetFilterOptions(PrefabItemOptions item)
		{
			var _filterOptions = new VectorFilterOptions();

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
	}
}