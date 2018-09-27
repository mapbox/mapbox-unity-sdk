using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Interfaces;
using UnityEngine;

public class ApiTest : MonoBehaviour
{
	private AbstractMap _abstractMap;
	public ImagerySourceType imagerySource = ImagerySourceType.MapboxStreets;

	public LocationPrefabCategories LocationPrefabCategories;
	public GameObject PoiPrefab;

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
			layer.SetTexturingType(testStyles[styleId]);
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
		Debug.Log("ChangePoiCategory ---> " + pois.GetType().ToString()); //PrefabItemOptions
		pois.HasChanged = true;
	}

	[ContextMenu("ChangePoiPrefab")]
	public void ChangePoiPrefab()
	{
		var pois = _abstractMap.VectorData.FindPoiLayerWithName("loc");
		pois.spawnPrefabOptions.prefab = PoiPrefab;
        Debug.Log("ChangePoiPrefab ---> " + pois.spawnPrefabOptions.GetType().ToString());//SpawnPrefabOptions
		pois.spawnPrefabOptions.HasChanged = true;
	}

	[ContextMenu("ChangeToPoiByName")]
	public void ChangeToPoiByName()
	{
		var pois = _abstractMap.VectorData.FindPoiLayerWithName("loc");
		pois.findByType = LocationPrefabFindBy.POIName;
		pois.nameString = "yerba";
		Debug.Log("ChangeToPoiByName ---> " + pois.GetType().ToString());//PrefabItemOptions
		pois.HasChanged = true;
	}

	[ContextMenu("ChangeToCategory")]
	public void ChangeToCategory()
	{
		var pois = _abstractMap.VectorData.FindPoiLayerWithName("loc");
		pois.findByType = LocationPrefabFindBy.MapboxCategory;
		Debug.Log("ChangeToCategory ---> " + pois.GetType().ToString());//PrefabItemOptions
		pois.HasChanged = true;
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