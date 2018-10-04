using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Interfaces;
using Mapbox.Unity.MeshGeneration.Filters;
using UnityEngine;

public class ApiTest : MonoBehaviour
{
	private AbstractMap _abstractMap;
	public ImagerySourceType imagerySource = ImagerySourceType.MapboxStreets;

	public LocationPrefabCategories LocationPrefabCategories;
	public LayerFilterOperationType layerFilterOperationType;
	public GameObject PoiPrefab;


	public LayerFilterCombinerOperationType layerFilterCombinerOperationType;
	public string filterKey;

	public float min;
	public float max;
	public string contains;

	readonly StyleTypes[] testStyles = new StyleTypes[3] { StyleTypes.Fantasy, StyleTypes.Realistic, StyleTypes.Simple };
	int styleId = -1;
	void Start()
	{
		_abstractMap = FindObjectOfType<AbstractMap>();
	}

	[ContextMenu("ChangeExtentType")]
	public void ChangeExtentType()
	{
		_abstractMap.SetExtent(MapExtentType.CameraBounds);
	}

	[ContextMenu("ChangeExtentOptions")]
	public void ChangeExtentOptions()
	{
		_abstractMap.SetExtentOptions(new RangeTileProviderOptions { east = 2, west = 3, north = 0, south = 1 });
	}


	[ContextMenu("EnableTerrainColliders")]
	public void EnableTerrainColliders()
	{
		_abstractMap.Terrain.EnableCollider(true);
	}

	[ContextMenu("DisableTerrainColliders")]
	public void DisableTerrainColliders()
	{
		_abstractMap.Terrain.EnableCollider(false);
	}

	[ContextMenu("IncreaseTerrainExagguration")]
	public void IncreaseTerrainExagguration()
	{
		_abstractMap.Terrain.SetExaggerationFactor(_abstractMap.Terrain.ExaggerationFactor + 0.5f);
	}

	[ContextMenu("SetTerrainLayer")]
	public void SetTerrainLayer()
	{
		_abstractMap.Terrain.AddToUnityLayer(LayerMask.NameToLayer("Water"));
	}

	[ContextMenu("SetTerrainDataSource")]
	public void SetTerrainDataSource()
	{
		if (_abstractMap.Terrain.LayerSource == ElevationSourceType.MapboxTerrain)
		{
			_abstractMap.Terrain.SetLayerSource(ElevationSourceType.None);
		}
		else
		{
			_abstractMap.Terrain.SetLayerSource(ElevationSourceType.MapboxTerrain);
		}
	}

	[ContextMenu("EnableVectorColliders")]
	public void EnableVectorColliders()
	{
		var layer = _abstractMap.VectorData.FindFeatureLayerWithName("ExtrudedBuildings");
		layer.colliderOptions.colliderType = ColliderType.MeshCollider;
		layer.colliderOptions.HasChanged = true;
	}

	[ContextMenu("DisableVectorColliders")]
	public void DisableVectorColliders()
	{
		var layer = _abstractMap.VectorData.FindFeatureLayerWithName("ExtrudedBuildings");
		layer.colliderOptions.colliderType = ColliderType.None;
		layer.colliderOptions.HasChanged = true;
	}

	[ContextMenu("ChangeImagery")]
	public void ChangeImagery()
	{
		imagerySource = (imagerySource == ImagerySourceType.MapboxSatelliteStreet) ? ImagerySourceType.MapboxStreets : imagerySource + 1;
		_abstractMap.ImageLayer.SetLayerSource(imagerySource);
	}
	[ContextMenu("DisableLayer")]
	public void DisableLayer()
	{
		var layer = _abstractMap.VectorData.FindFeatureLayerWithName("ExtrudedBuildings");
		if (layer != null)
		{
			layer.SetActive(false);
		}
		else
		{
			Debug.Log("Layer not found");
		}
	}

	[ContextMenu("ChangeBuildingMaterial")]
	public void ChangeBuildingMaterial()
	{
		styleId = (styleId == 2) ? 0 : styleId + 1;
		var layer = _abstractMap.VectorData.FindFeatureLayerWithName("ExtrudedBuildings");
		if (layer != null)
		{
			layer.SetStyleType(testStyles[styleId]);
		}
		else
		{
			Debug.Log("Layer not found");
		}
	}

	[ContextMenu("AddLayer")]
	public void AddLayer()
	{
		VectorSubLayerProperties subLayerProperties = new VectorSubLayerProperties();
		subLayerProperties.coreOptions.geometryType = VectorPrimitiveType.Polygon;
		subLayerProperties.coreOptions.layerName = "building";

		_abstractMap.VectorData.AddFeatureLayer(subLayerProperties);
	}

	[ContextMenu("AddPoiLayer")]
	public void AddPoiLayer()
	{
		var prefabItemOptions = new PrefabItemOptions();
		prefabItemOptions.categories = LocationPrefabCategories;
		prefabItemOptions.spawnPrefabOptions = new SpawnPrefabOptions();
		prefabItemOptions.spawnPrefabOptions.prefab = PoiPrefab;

		_abstractMap.VectorData.AddPoiLayer(prefabItemOptions);
	}

	[ContextMenu("RemoveLayer")]
	public void RemoveLayer()
	{
		_abstractMap.VectorData.RemoveFeatureLayerWithName("ExtrudedBuildings");
	}

	[ContextMenu("RemovePoiLayer")]
	public void RemovePoiLayer()
	{
		_abstractMap.VectorData.RemovePoiLayerWithName("loc");
	}

	[ContextMenu("IncreaseRoadHeight")]
	public void IncreaseRoadHeight()
	{
		var roads = _abstractMap.VectorData.FindFeatureLayerWithName("Roads");
		roads.extrusionOptions.maximumHeight = roads.extrusionOptions.maximumHeight + 2;
		roads.extrusionOptions.HasChanged = true;
	}

	[ContextMenu("IncreaseRoadWidth")]
	public void IncreaseRoadWidth()
	{
		var roads = _abstractMap.VectorData.FindFeatureLayerWithName("Roads");
		roads.lineGeometryOptions.Width = roads.lineGeometryOptions.Width + 2;
		roads.lineGeometryOptions.HasChanged = true;
	}

	[ContextMenu("ChangePoiCategory")]
	public void ChangePoiCategory()
	{
		var pois = _abstractMap.VectorData.FindPoiLayerWithName("loc");
		pois.categories = LocationPrefabCategories;
		pois.HasChanged = true;
	}

	[ContextMenu("ChangePoiPrefab")]
	public void ChangePoiPrefab()
	{
		var pois = _abstractMap.VectorData.FindPoiLayerWithName("loc");
		pois.spawnPrefabOptions.prefab = PoiPrefab;
		pois.spawnPrefabOptions.HasChanged = true;
	}

	[ContextMenu("ChangeToPoiByName")]
	public void ChangeToPoiByName()
	{
		var pois = _abstractMap.VectorData.FindPoiLayerWithName("loc");
		pois.findByType = LocationPrefabFindBy.POIName;
		pois.nameString = "yerba";
		pois.HasChanged = true;
	}

	[ContextMenu("ChangeToCategory")]
	public void ChangeToCategory()
	{
		var pois = _abstractMap.VectorData.FindPoiLayerWithName("loc");
		pois.findByType = LocationPrefabFindBy.MapboxCategory;
		pois.HasChanged = true;
	}

	[ContextMenu("Vector - Add New Filter")]
	public void AddNewFilter()
	{
		var vectorLayer = _abstractMap.VectorData.FindFeatureLayerWithName("loc");
		LayerFilter layerFilter = new LayerFilter(LayerFilterOperationType.Contains);
		vectorLayer.filterOptions.filters.Add(layerFilter);
		vectorLayer.filterOptions.HasChanged = true;
	}

	[ContextMenu("Vector - Remove Filter")]
	public void RemoveFilter()
	{
		int index = 0;
		var vectorLayer = _abstractMap.VectorData.FindFeatureLayerWithName("loc");
		if (index < vectorLayer.filterOptions.filters.Count)
		{
			vectorLayer.filterOptions.filters.RemoveAt(index);
		}
		vectorLayer.filterOptions.HasChanged = true;
	}

	[ContextMenu("Vector - Set Filter Combiner Type")]
	public void SetFilterCombinerType()
	{
		var vectorLayer = _abstractMap.VectorData.FindFeatureLayerWithName("loc");

		vectorLayer.filterOptions.combinerType = layerFilterCombinerOperationType;

		vectorLayer.filterOptions.HasChanged = true;
	}

	[ContextMenu("Vector - Set Filter Key")]
	public void SetFilterKey()
	{
		int index = 0;
		var vectorLayer = _abstractMap.VectorData.FindFeatureLayerWithName("loc");
		if (index < vectorLayer.filterOptions.filters.Count)
		{
			vectorLayer.filterOptions.filters[index].Key = filterKey;
		}
		vectorLayer.filterOptions.HasChanged = true;
	}

	[ContextMenu("Vector - Set Filter Operator")]
	public void SetFilterOperator()
	{
		int index = 0;
		var vectorLayer = _abstractMap.VectorData.FindFeatureLayerWithName("loc");
		if (index < vectorLayer.filterOptions.filters.Count)
		{
			vectorLayer.filterOptions.filters[index].filterOperator = layerFilterOperationType;
		}
		vectorLayer.filterOptions.HasChanged = true;
	}

	[ContextMenu("Vector - Set Filter Min Value")]
	public void SetFilterCompareValue()
	{
		int index = 0;
		var vectorLayer = _abstractMap.VectorData.FindFeatureLayerWithName("loc");
		if (index < vectorLayer.filterOptions.filters.Count)
		{
			vectorLayer.filterOptions.filters[index].Min = min;
		}
		vectorLayer.filterOptions.HasChanged = true;
	}

	[ContextMenu("Vector - Set Filter Compare MinMaxValue")]
	public void SetFilterCompareMinMaxValue()
	{
		int index = 0;
		var vectorLayer = _abstractMap.VectorData.FindFeatureLayerWithName("loc");
		if (index < vectorLayer.filterOptions.filters.Count)
		{
			vectorLayer.filterOptions.filters[index].Min = min;
			vectorLayer.filterOptions.filters[index].Max = max;
		}
		vectorLayer.filterOptions.HasChanged = true;
	}

	[ContextMenu("Vector - Set Filter Contains Value")]
	public void SetFilterContainsValue()
	{
		int index = 0;
		var vectorLayer = _abstractMap.VectorData.FindFeatureLayerWithName("loc");
		if (index < vectorLayer.filterOptions.filters.Count)
		{
			vectorLayer.filterOptions.filters[index].PropertyValue = contains;
		}
		vectorLayer.filterOptions.HasChanged = true;
	}

	[ContextMenu("TestPoiCategoryApi")]
	public void TestPoiCategoryApi()
	{
		_abstractMap.VectorData.SpawnPrefabByCategory(PoiPrefab, LocationPrefabCategories);
	}

	[ContextMenu("LogAllFeatureLayerNames")]
	public void LogAllFeatureLayerNames()
	{
		var str = "";
		str += "All Feature Layers: ";
		str += string.Join(",", _abstractMap.VectorData.GetAllFeatureLayers().Select(x => x.Key).ToArray());
		str += "\r\n";
		str += "Polygon Layers: ";
		str += string.Join(",", _abstractMap.VectorData.GetAllPolygonFeatureLayers().Select(x => x.Key).ToArray());
		str += "\r\n";
		str += "Line Layers: ";
		str += string.Join(",", _abstractMap.VectorData.GetAllLineFeatureLayers().Select(x => x.Key).ToArray());
		str += "\r\n";
		str += "Point Layers: ";
		str += string.Join(",", _abstractMap.VectorData.GetAllPointFeatureLayers().Select(x => x.Key).ToArray());
		str += "\r\n";
		str += "Feature Layer at index 0: ";
		str += _abstractMap.VectorData.GetFeatureLayerAtIndex(0).Key;
		str += "\r\n";
		str += "Feature Layers with \"B\" in the name ";
		str += string.Join(",", _abstractMap.VectorData.GetFeatureLayerByQuery(x => x.coreOptions.sublayerName.Contains("B")).Select(x => x.coreOptions.sublayerName).ToArray());
		str += "\r\n";
		str += "All Poi Layers: ";
		str += string.Join(",", _abstractMap.VectorData.GetAllPoiLayers().Select(x => x.Key).ToArray());
		str += "\r\n";
		str += "Poi Layer at index 0: ";
		str += _abstractMap.VectorData.GetPoiLayerAtIndex(0).Key;
		str += "\r\n";
		str += "Poi Layers with \"L\" in the name ";
		str += string.Join(",", _abstractMap.VectorData.GetPoiLayerByQuery(x => x.coreOptions.sublayerName.Contains("L")).Select(x => x.coreOptions.sublayerName).ToArray());

		Debug.Log(str);
	}

	[ContextMenu("RemoveFirstFeatureLayer")]
	public void RemoveFirstFeatureLayer()
	{
		_abstractMap.VectorData.RemoveFeatureLayer(_abstractMap.VectorData.GetFeatureLayerAtIndex(0));
	}

	[ContextMenu("RemoveFirstPoiLayer")]
	public void RemoveFirstPoiLayer()
	{
		_abstractMap.VectorData.RemovePoiLayer(_abstractMap.VectorData.GetPoiLayerAtIndex(0));
	}

	[ContextMenu("ToggleCoroutines")]
	public void ToggleCoroutines()
	{
		_abstractMap.VectorData.EnableCoroutines(!_abstractMap.VectorData.LayerProperty.performanceOptions.isEnabled);
	}

	[ContextMenu("ToggleStyleOptimization")]
	public void ToggleStyleOptimization()
	{
		_abstractMap.VectorData.EnableOptimizedStyle(!_abstractMap.VectorData.LayerProperty.useOptimizedStyle);
	}
}