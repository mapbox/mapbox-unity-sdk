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
				if(item.findByType == LocationPrefabFindBy.MapboxCategory)
				{
					SetCategoryFilterOptions(item);
				}
				if(item.findByType == LocationPrefabFindBy.AddressOrLatLon)
				{
					SetNameFilters(item);
				}

				SetDensityFilters(item);

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

		/// <summary>
		/// Sets the category filter options.
		/// </summary>
		/// <param name="item">Item.</param>
		private void SetCategoryFilterOptions(PrefabItemOptions item)
		{
			var _filterOptions = new VectorFilterOptions();

			string propertyName = "";
			item.categoryPropertyFromFindByTypeDictionary.TryGetValue(item.findByType, out propertyName);

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

			MergeFiltersWithItem(item, _filterOptions);
		}

		/// <summary>
		/// Sets the density filters.
		/// </summary>
		/// <param name="item">Item.</param>
		private void SetDensityFilters(PrefabItemOptions item)
		{
			var _filterOptions = new VectorFilterOptions();

			string propertyName = "";
			item.densityPropertyFromFindByTypeDictionary.TryGetValue(item.findByType, out propertyName);

			if (item.findByType == LocationPrefabFindBy.MapboxCategory || item.findByType == LocationPrefabFindBy.POIName)
			{
				LayerFilter filter = new LayerFilter(LayerFilterOperationType.IsLess)
				{
					Key = propertyName,
					PropertyValue = item.density.ToString()
				};
				_filterOptions.filters.Add(filter);
			}

			MergeFiltersWithItem(item, _filterOptions);
		}

		/// <summary>
		/// Sets the name filters.
		/// </summary>
		/// <param name="item">Item.</param>
		private void SetNameFilters(PrefabItemOptions item)
		{
			if (string.IsNullOrEmpty(item.poiName))
				return;
			
			var _filterOptions = new VectorFilterOptions();

			string propertyName = "";
			item.namePropertyFromFindByTypeDictionary.TryGetValue(item.findByType, out propertyName);

			if (item.findByType == LocationPrefabFindBy.POIName)
			{
				LayerFilter filter = new LayerFilter(LayerFilterOperationType.Contains)
				{
					Key = propertyName,
					PropertyValue = item.poiName
				};
				_filterOptions.filters.Add(filter);
			}

			MergeFiltersWithItem(item, _filterOptions);
		}

		/// <summary>
		/// Merges the filters with item filters.
		/// </summary>
		/// <param name="item">Item.</param>
		void MergeFiltersWithItem(PrefabItemOptions item, VectorFilterOptions filterOptions)
		{
			if (item.filterOptions == null || item.filterOptions.filters.Count == 0)
			{
				item.filterOptions = filterOptions;
			}
			else
			{
				item.filterOptions.filters.AddRange(filterOptions.filters);
			}
		}

		/// <summary>
		/// Gets the list of categories selected through the dropdown
		/// </summary>
		/// <returns>The selected categories list.</returns>
		/// <param name="cat">Cat.</param>
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