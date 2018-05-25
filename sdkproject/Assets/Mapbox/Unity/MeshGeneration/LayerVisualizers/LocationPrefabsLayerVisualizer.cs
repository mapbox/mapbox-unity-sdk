namespace Mapbox.Unity.MeshGeneration.Interfaces
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.MeshGeneration.Filters;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Map;
	using Mapbox.VectorTile;

	public class LocationPrefabsLayerVisualizer : VectorLayerVisualizer
	{
		private int maxDensity = 30; //This value is same as the density's max range value in PrefabItemOptions

		public void SetProperties(PrefabItemOptions item, LayerPerformanceOptions performanceOptions)
		{
			SubLayerProperties = item;
			Active = item.isActive;
			_performanceOptions = performanceOptions;

			
			//Check to make sure that when Categories selection is none, the location prefab is disabled
			if (item.findByType == LocationPrefabFindBy.MapboxCategory && item.categories == LocationPrefabCategories.None)
			{
				return;
			}

			if(item.spawnPrefabOptions.prefab==null)
			{
				Debug.LogError("No prefab found. Please assign a prefab to spawn it on the map");
			}

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
				if(item.findByType == LocationPrefabFindBy.POIName)
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
						(_defaultStack as ModifierStack).moveFeaturePositionTo = item.moveFeaturePositionTo;

						if (_defaultStack.GoModifiers == null)
						{
							_defaultStack.GoModifiers = new List<GameObjectModifier>();
						}
						_defaultStack.GoModifiers.Add(prefabModifier);

						if( itemProperties.snapToTerrain == true )
						{
							_defaultStack.MeshModifiers.Add(ScriptableObject.CreateInstance<SnapTerrainModifier>());
						}
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
			string propertyName = "";
			item.categoryPropertyFromFindByTypeDictionary.TryGetValue(item.findByType, out propertyName);

			if (item.findByType == LocationPrefabFindBy.MapboxCategory)
			{
				List<LocationPrefabCategories> categoriesList = GetSelectedCategoriesList(item.categories);
				if (categoriesList == null || categoriesList.Count==0)
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
				AddFilterToItem(item, filter);
			}
		}

		/// <summary>
		/// Sets the density filters.
		/// </summary>
		/// <param name="item">Item.</param>
		private void SetDensityFilters(PrefabItemOptions item)
		{
			if (item.density >= maxDensity) // decided that the max value for density
				return;
			
			string propertyName = "";
			item.densityPropertyFromFindByTypeDictionary.TryGetValue(item.findByType, out propertyName);

			if (item.findByType == LocationPrefabFindBy.MapboxCategory || item.findByType == LocationPrefabFindBy.POIName)
			{
				LayerFilter filter = new LayerFilter(LayerFilterOperationType.IsLess)
				{
					Key = propertyName,
					Min = item.density
				};
				AddFilterToItem(item, filter);
			}
		}

		/// <summary>
		/// Sets the name filters.
		/// </summary>
		/// <param name="item">Item.</param>
		private void SetNameFilters(PrefabItemOptions item)
		{
			if (string.IsNullOrEmpty(item.nameString))
				return;
			
			string propertyName = "";
			item.namePropertyFromFindByTypeDictionary.TryGetValue(item.findByType, out propertyName);

			if (item.findByType == LocationPrefabFindBy.POIName)
			{
				LayerFilter filter = new LayerFilter(LayerFilterOperationType.Contains)
				{
					Key = propertyName,
					PropertyValue = item.nameString
				};
				AddFilterToItem(item, filter);
			}
		}

		/// <summary>
		/// Merges the filters with item filters.
		/// </summary>
		/// <param name="item">Item.</param>
		void AddFilterToItem(PrefabItemOptions item, LayerFilter filter)
		{
			if (item.filterOptions == null)
			{
				item.filterOptions = new VectorFilterOptions();
			}

			item.filterOptions.filters.Add(filter);
			item.filterOptions.combinerType = item._combinerType;

		}

		/// <summary>
		/// Gets the list of categories selected through the dropdown
		/// </summary>
		/// <returns>The selected categories list.</returns>
		/// <param name="selectedCategories">Cat.</param>
		private List<LocationPrefabCategories> GetSelectedCategoriesList(LocationPrefabCategories selectedCategories)
		{
			List<LocationPrefabCategories> containingCategories = new List<LocationPrefabCategories>();

			var eligibleValues = Enum.GetValues(typeof(LocationPrefabCategories));
			if (selectedCategories == LocationPrefabCategories.None || selectedCategories == LocationPrefabCategories.AnyCategory)
			{
				return containingCategories;
			}

			//For any other categories other than None and Any
			foreach(var value in eligibleValues)
			{
				var category = (LocationPrefabCategories)value;

				if (category == LocationPrefabCategories.AnyCategory || category==LocationPrefabCategories.None)
					continue;
				
				if((category & selectedCategories) != 0) //to check if category is contained in cat
				{
					containingCategories.Add(category);
				}
			}

			return containingCategories;
		}

		public override void Create(VectorTileLayer layer, UnityTile tile, Action callback)
		{
			//for layers using specific locations, ignore VectorTileLayer and
			//pass coordinates to the modifierstack using BuildFeatureFromLatLon.
			if( (SubLayerProperties as PrefabItemOptions).findByType 
			   == LocationPrefabFindBy.AddressOrLatLon)
			{
				BuildFeatureFromLatLon(layer, tile);
				if (callback != null)
				{
					callback();
				}
			}
			else
			{
				base.Create(layer, tile, callback);

			}

		}


		/// <summary>
		/// Creates a vector feature from lat lon and builds that feature using the modifier stack.
		/// </summary>
		/// <param name="layer">Layer.</param>
		/// <param name="tile">Tile.</param>
		private void BuildFeatureFromLatLon(VectorTileLayer layer, UnityTile tile)
		{
			var coordinates = (SubLayerProperties as PrefabItemOptions).coordinates;

			for (int i = 0; i < coordinates.Length; i++)
			{
				if(String.IsNullOrEmpty(coordinates[i]))
				{
					return;
				}

				//check if the coordinate is in the tile
				var coordinate = Conversions.StringToLatLon( coordinates[i] );
				var coordinateTileId = Conversions.LatitudeLongitudeToTileId(
					coordinate.x, coordinate.y, tile.InitialZoom);

				if( coordinateTileId.Canonical.Equals(tile.CanonicalTileId))
				{

					//create new vector feature
					var feature = new VectorFeatureUnity();
					feature.Properties = new Dictionary<string, object>();
					feature.Points = new List<List<Vector3>>();

					//create submesh for feature
					var latLonPoint = new List<Vector3>();

					//add point to submesh, and submesh to feature
					latLonPoint.Add(Conversions.LatitudeLongitudeToUnityTilePosition(coordinate, tile.InitialZoom, tile.TileScale,layer.Extent).ToVector3xz());
					feature.Points.Add(latLonPoint);

					//pass valid feature.Data to modifiers
					//this data has no relation to the features being drawn
					feature.Data = layer.GetFeature(0);

					//pass the feature to the mod stack
					base.Build(feature, tile, tile.gameObject);

				}

			}
		}

	}
}