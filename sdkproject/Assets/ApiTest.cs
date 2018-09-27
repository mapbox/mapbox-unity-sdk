﻿using System;
using System.Collections;
using System.Collections.Generic;
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
		_abstractMap.Terrain.SetExaggerationFactor(_abstractMap.Terrain.LayerProperty.requiredOptions.exaggerationFactor + 0.5f);
	}

	[ContextMenu("SetTerrainLayer")]
	public void SetTerrainLayer()
	{
		_abstractMap.Terrain.SetLayer(LayerMask.NameToLayer("Water"));
	}
	
	[ContextMenu("SetTerrainDataSource")]
	public void SetTerrainDataSource()
	{
		if (_abstractMap.Terrain.LayerProperty.sourceType == ElevationSourceType.MapboxTerrain)
		{
			_abstractMap.Terrain.SetDataSource(ElevationSourceType.None);
		}
		else
		{
			_abstractMap.Terrain.SetDataSource(ElevationSourceType.MapboxTerrain);
		}
	}
	
	[ContextMenu("EnableVectorColliders")]
	public void EnableVectorColliders()
	{
		var layer = _abstractMap.VectorData.LayerProperty.FindFeatureLayerWithName("ExtrudedBuildings");
		layer.colliderOptions.colliderType = ColliderType.MeshCollider;
		layer.colliderOptions.HasChanged = true;
	}

	[ContextMenu("DisableVectorColliders")]
	public void DisableVectorColliders()
	{
		var layer = _abstractMap.VectorData.LayerProperty.FindFeatureLayerWithName("ExtrudedBuildings");
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
		var layer = _abstractMap.VectorData.LayerProperty.FindFeatureLayerWithName("ExtrudedBuildings");
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
		var layer = _abstractMap.VectorData.LayerProperty.FindFeatureLayerWithName("ExtrudedBuildings");
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

		_abstractMap.VectorData.LayerProperty.AddVectorLayer(subLayerProperties);
	}
	
	[ContextMenu("AddPoiLayer")]
	public void AddPoiLayer()
	{
		var prefabItemOptions = new PrefabItemOptions();
		prefabItemOptions.categories = LocationPrefabCategories;
		prefabItemOptions.spawnPrefabOptions = new SpawnPrefabOptions();
		prefabItemOptions.spawnPrefabOptions.prefab = PoiPrefab;

		_abstractMap.VectorData.LayerProperty.AddPoiLayer(prefabItemOptions);
	}

	[ContextMenu("RemoveLayer")]
	public void RemoveLayer()
	{
		_abstractMap.VectorData.LayerProperty.RemoveFeatureLayerWithName("ExtrudedBuildings");
	}
	
	[ContextMenu("RemovePoiLayer")]
	public void RemovePoiLayer()
	{
		_abstractMap.VectorData.LayerProperty.RemovePoiLayerWithName("loc");
	}

	[ContextMenu("IncreaseRoadHeight")]
	public void IncreaseRoadHeight()
	{
		var roads = _abstractMap.VectorData.LayerProperty.FindFeatureLayerWithName("Roads");
		roads.extrusionOptions.maximumHeight = roads.extrusionOptions.maximumHeight + 2;
		roads.extrusionOptions.HasChanged = true;
	}

	[ContextMenu("IncreaseRoadWidth")]
	public void IncreaseRoadWidth()
	{
		var roads = _abstractMap.VectorData.LayerProperty.FindFeatureLayerWithName("Roads");
		roads.lineGeometryOptions.Width = roads.lineGeometryOptions.Width + 2;
		roads.lineGeometryOptions.HasChanged = true;
	}

	[ContextMenu("ChangePoiCategory")]
	public void ChangePoiCategory()
	{
		var pois = _abstractMap.VectorData.LayerProperty.FindPoiLayerWithName("loc");
		pois.categories = LocationPrefabCategories;
		pois.HasChanged = true;
	}

	[ContextMenu("ChangePoiPrefab")]
	public void ChangePoiPrefab()
	{
		var pois = _abstractMap.VectorData.LayerProperty.FindPoiLayerWithName("loc");
		pois.spawnPrefabOptions.prefab = PoiPrefab;
		pois.spawnPrefabOptions.HasChanged = true;
	}

	[ContextMenu("ChangeToPoiByName")]
	public void ChangeToPoiByName()
	{
		var pois = _abstractMap.VectorData.LayerProperty.FindPoiLayerWithName("loc");
		pois.findByType = LocationPrefabFindBy.POIName;
		pois.nameString = "yerba";
		pois.HasChanged = true;
	}

	[ContextMenu("ChangeToCategory")]
	public void ChangeToCategory()
	{
		var pois = _abstractMap.VectorData.LayerProperty.FindPoiLayerWithName("loc");
		pois.findByType = LocationPrefabFindBy.MapboxCategory;
		pois.HasChanged = true;
	}

	[ContextMenu("Vector - Add New Filter")]
	public void AddNewFilter()
	{
		var vectorLayer = _abstractMap.VectorData.LayerProperty.FindFeatureLayerWithName("loc");
		LayerFilter layerFilter = new LayerFilter(LayerFilterOperationType.Contains);
		vectorLayer.filterOptions.filters.Add(layerFilter);
		vectorLayer.filterOptions.HasChanged = true;
	}

	[ContextMenu("Vector - Remove Filter")]
	public void RemoveFilter()
	{
		int index = 0;
		var vectorLayer = _abstractMap.VectorData.LayerProperty.FindFeatureLayerWithName("loc");
		if(index < vectorLayer.filterOptions.filters.Count)
		{
			vectorLayer.filterOptions.filters.RemoveAt(index);
		}
		vectorLayer.filterOptions.HasChanged = true;
	}

	[ContextMenu("Vector - Set Filter Combiner Type")]
	public void SetFilterCombinerType()
	{
		var vectorLayer = _abstractMap.VectorData.LayerProperty.FindFeatureLayerWithName("loc");

		vectorLayer.filterOptions.combinerType = layerFilterCombinerOperationType;

		vectorLayer.filterOptions.HasChanged = true;
	}

	[ContextMenu("Vector - Set Filter Key")]
	public void SetFilterKey()
	{
		int index = 0;
		var vectorLayer = _abstractMap.VectorData.LayerProperty.FindFeatureLayerWithName("loc");
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
		var vectorLayer = _abstractMap.VectorData.LayerProperty.FindFeatureLayerWithName("loc");
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
		var vectorLayer = _abstractMap.VectorData.LayerProperty.FindFeatureLayerWithName("loc");
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
		var vectorLayer = _abstractMap.VectorData.LayerProperty.FindFeatureLayerWithName("loc");
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
		var vectorLayer = _abstractMap.VectorData.LayerProperty.FindFeatureLayerWithName("loc");
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
}