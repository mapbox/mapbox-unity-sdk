using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Interfaces;
using Mapbox.Unity.MeshGeneration.Filters;
using Mapbox.Unity.MeshGeneration.Modifiers;
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

		_abstractMap.OnTilesStarting += (s) => { Debug.Log("Starting " + string.Join(",", s.Select(x => x.ToString()).ToArray())); };
		_abstractMap.OnTileFinished += (s) => { Debug.Log("Finished " + s.CanonicalTileId); };
		_abstractMap.OnTilesDisposing += (s) => { Debug.Log("Disposing " + string.Join(",", s.Select(x => x.ToString()).ToArray())); };

		_abstractMap.MapVisualizer.OnTileHeightProcessingFinished += (s) => { Debug.Log("Terrain finished " + s.CanonicalTileId); };
		_abstractMap.MapVisualizer.OnTileImageProcessingFinished += (s) => { Debug.Log("Image finished " + s.CanonicalTileId); };
		_abstractMap.MapVisualizer.OnTileVectorProcessingFinished += (s) => { Debug.Log("Vector finished " + s.CanonicalTileId); };

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
		var layer = _abstractMap.VectorData.FindFeatureSubLayerWithName("ExtrudedBuildings");
		layer.colliderOptions.colliderType = ColliderType.MeshCollider;
		layer.colliderOptions.HasChanged = true;
	}

	[ContextMenu("DisableVectorColliders")]
	public void DisableVectorColliders()
	{
		var layer = _abstractMap.VectorData.FindFeatureSubLayerWithName("ExtrudedBuildings");
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
		var layer = _abstractMap.VectorData.FindFeatureSubLayerWithName("ExtrudedBuildings");
		if (layer != null)
		{
			layer.SetActive(false);
		}
		else
		{
			Debug.Log("Layer not found");
		}
	}

	[ContextMenu("EnableLayer")]
	public void EnableLayer()
	{
		var layer = _abstractMap.VectorData.FindFeatureSubLayerWithName("ExtrudedBuildings");
		if (layer != null)
		{
			layer.SetActive(true);
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
		var layer = _abstractMap.VectorData.FindFeatureSubLayerWithName("ExtrudedBuildings");
		if (layer != null)
		{
			layer.Texturing.SetStyleType(testStyles[styleId]);
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

		_abstractMap.VectorData.AddFeatureSubLayer(subLayerProperties);
	}

	[ContextMenu("AddPoiLayer")]
	public void AddPoiLayer()
	{
		var prefabItemOptions = new PrefabItemOptions();
		prefabItemOptions.categories = LocationPrefabCategories;
		prefabItemOptions.spawnPrefabOptions = new SpawnPrefabOptions();
		prefabItemOptions.spawnPrefabOptions.prefab = PoiPrefab;

		_abstractMap.VectorData.AddPointsOfInterestSubLayer(prefabItemOptions);
	}

	[ContextMenu("RemoveLayer")]
	public void RemoveLayer()
	{
		_abstractMap.VectorData.RemoveFeatureSubLayerWithName("ExtrudedBuildings");
	}

	[ContextMenu("RemovePoiLayer")]
	public void RemovePoiLayer()
	{
		_abstractMap.VectorData.RemovePointsOfInterestSubLayerWithName("loc");
	}

	[ContextMenu("IncreaseRoadHeight")]
	public void IncreaseRoadHeight()
	{
		var roads = _abstractMap.VectorData.FindFeatureSubLayerWithName("Roads");
		roads.extrusionOptions.maximumHeight = roads.extrusionOptions.maximumHeight + 2;
		roads.extrusionOptions.HasChanged = true;
	}

	[ContextMenu("IncreaseRoadWidth")]
	public void IncreaseRoadWidth()
	{
		var roads = _abstractMap.VectorData.FindFeatureSubLayerWithName("Roads");
		roads.lineGeometryOptions.Width = roads.lineGeometryOptions.Width + 2;
		roads.lineGeometryOptions.HasChanged = true;
	}

	[ContextMenu("ChangePoiCategory")]
	public void ChangePoiCategory()
	{
		var pois = _abstractMap.VectorData.FindPointsofInterestSubLayerWithName("loc");
		pois.categories = LocationPrefabCategories;
		pois.HasChanged = true;
	}

	[ContextMenu("ChangePoiPrefab")]
	public void ChangePoiPrefab()
	{
		var pois = _abstractMap.VectorData.FindPointsofInterestSubLayerWithName("loc");
		pois.spawnPrefabOptions.prefab = PoiPrefab;
		pois.spawnPrefabOptions.HasChanged = true;
	}

	[ContextMenu("ChangeToPoiByName")]
	public void ChangeToPoiByName()
	{
		var pois = _abstractMap.VectorData.FindPointsofInterestSubLayerWithName("loc");
		pois.findByType = LocationPrefabFindBy.POIName;
		pois.nameString = "yerba";
		pois.HasChanged = true;
	}

	[ContextMenu("ChangeToCategory")]
	public void ChangeToCategory()
	{
		var pois = _abstractMap.VectorData.FindPointsofInterestSubLayerWithName("loc");
		pois.findByType = LocationPrefabFindBy.MapboxCategory;
		pois.HasChanged = true;
	}

	[ContextMenu("Vector - Add New Filter")]
	public void AddNewFilter()
	{
		var vectorLayer = _abstractMap.VectorData.FindFeatureSubLayerWithName("loc");
		LayerFilter layerFilter = new LayerFilter(LayerFilterOperationType.Contains);
		vectorLayer.filterOptions.filters.Add(layerFilter);
		vectorLayer.filterOptions.HasChanged = true;
	}

	[ContextMenu("Vector - Remove Filter")]
	public void RemoveFilter()
	{
		int index = 0;
		var vectorLayer = _abstractMap.VectorData.FindFeatureSubLayerWithName("loc");
		if (index < vectorLayer.filterOptions.filters.Count)
		{
			vectorLayer.filterOptions.filters.RemoveAt(index);
		}

		vectorLayer.filterOptions.HasChanged = true;
	}

	[ContextMenu("Vector - Set Filter Combiner Type")]
	public void SetFilterCombinerType()
	{
		var vectorLayer = _abstractMap.VectorData.FindFeatureSubLayerWithName("loc");

		vectorLayer.filterOptions.combinerType = layerFilterCombinerOperationType;

		vectorLayer.filterOptions.HasChanged = true;
	}

	[ContextMenu("Vector - Set Filter Key")]
	public void SetFilterKey()
	{
		int index = 0;
		var vectorLayer = _abstractMap.VectorData.FindFeatureSubLayerWithName("loc");
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
		var vectorLayer = _abstractMap.VectorData.FindFeatureSubLayerWithName("loc");
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
		var vectorLayer = _abstractMap.VectorData.FindFeatureSubLayerWithName("loc");
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
		var vectorLayer = _abstractMap.VectorData.FindFeatureSubLayerWithName("loc");
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
		var vectorLayer = _abstractMap.VectorData.FindFeatureSubLayerWithName("loc");
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
		str += string.Join(",", _abstractMap.VectorData.GetAllFeatureSubLayers().Select(x => x.Key).ToArray());
		str += "\r\n";
		str += "Polygon Layers: ";
		str += string.Join(",", _abstractMap.VectorData.GetAllPolygonFeatureSubLayers().Select(x => x.Key).ToArray());
		str += "\r\n";
		str += "Line Layers: ";
		str += string.Join(",", _abstractMap.VectorData.GetAllLineFeatureSubLayers().Select(x => x.Key).ToArray());
		str += "\r\n";
		str += "Point Layers: ";
		str += string.Join(",", _abstractMap.VectorData.GetAllPointFeatureSubLayers().Select(x => x.Key).ToArray());
		str += "\r\n";
		str += "Feature Layer at index 0: ";
		str += _abstractMap.VectorData.GetFeatureSubLayerAtIndex(0).Key;
		str += "\r\n";
		str += "Feature Layers with \"B\" in the name ";
		str += string.Join(",", _abstractMap.VectorData.GetFeatureSubLayerByQuery(x => x.coreOptions.sublayerName.Contains("B")).Select(x => x.coreOptions.sublayerName).ToArray());
		str += "\r\n";
		str += "All Poi Layers: ";
		str += string.Join(",", _abstractMap.VectorData.GetAllPointsOfInterestSubLayers().Select(x => x.Key).ToArray());
		str += "\r\n";
		str += "Poi Layer at index 0: ";
		str += _abstractMap.VectorData.GetPointsOfInterestSubLayerAtIndex(0).Key;
		str += "\r\n";
		str += "Poi Layers with \"L\" in the name ";
		str += string.Join(",", _abstractMap.VectorData.GetPointsOfInterestSubLayerByQuery(x => x.coreOptions.sublayerName.Contains("L")).Select(x => x.coreOptions.sublayerName).ToArray());

		Debug.Log(str);
	}

	[ContextMenu("RemoveFirstFeatureLayer")]
	public void RemoveFirstFeatureLayer()
	{
		_abstractMap.VectorData.RemoveFeatureSubLayer(_abstractMap.VectorData.GetFeatureSubLayerAtIndex(0));
	}

	[ContextMenu("RemoveFirstPoiLayer")]
	public void RemoveFirstPoiLayer()
	{
		_abstractMap.VectorData.RemovePointsOfInterestSubLayer(_abstractMap.VectorData.GetPointsOfInterestSubLayerAtIndex(0));
	}

	[ContextMenu("ToggleCoroutines")]
	public void ToggleCoroutines()
	{
		_abstractMap.VectorData.EnableVectorFeatureProcessingWithCoroutines(10);
	}

	[ContextMenu("ToggleStyleOptimization")]
	public void ToggleStyleOptimization()
	{
		_abstractMap.VectorData.SetLayerSourceWithOptimizedStyle(VectorSourceType.MapboxStreets, "styleId", "date", "name");
	}

	[ContextMenu("Switch First Layer To Custom Style")]
	public void SwitchFirstToCustom()
	{
		var layer = _abstractMap.VectorData.GetFeatureSubLayerAtIndex(0);
		layer.CreateCustomStyle(
			new List<MeshModifier>()
			{
				ScriptableObject.CreateInstance<PolygonMeshModifier>(),
				ScriptableObject.CreateInstance<HeightModifier>()
			},
			new List<GameObjectModifier>()
			{

			});
	}

	[ContextMenu("Add Polygon Mesh Modifier")]
	public void AddPolygonMeshModifier()
	{
		var layer = _abstractMap.VectorData.GetFeatureSubLayerAtIndex(0);
		layer.BehaviorModifiers.AddMeshModifier(ScriptableObject.CreateInstance<PolygonMeshModifier>());
	}

	[ContextMenu("Add GameObject Modifier")]
	public void AddGameObjectModifier()
	{
		var layer = _abstractMap.VectorData.GetFeatureSubLayerAtIndex(0);
		layer.BehaviorModifiers.AddGameObjectModifier(ScriptableObject.CreateInstance<ColliderModifier>());
	}

	[ContextMenu("Debug Height Mesh Modifier Names")]
	public void GetHeightMeshModifier()
	{
		var layer = _abstractMap.VectorData.GetFeatureSubLayerAtIndex(0);
		foreach (var mm in layer.BehaviorModifiers.GetMeshModifier(x => x is HeightModifier || x is TextureSideWallModifier))
		{
			Debug.Log(mm.GetType().Name);
		}
	}

	[ContextMenu("Debug Material Modifier Names")]
	public void GetShortNameMeshModifier()
	{
		var layer = _abstractMap.VectorData.GetFeatureSubLayerAtIndex(0);
		foreach (var mm in layer.BehaviorModifiers.GetGameObjectModifier(x => x is MaterialModifier))
		{
			Debug.Log(mm.GetType().Name);
		}
	}

	[ContextMenu("Remove Material Modifier")]
	public void RemoveMaterialModifier()
	{
		var layer = _abstractMap.VectorData.GetFeatureSubLayerAtIndex(0);
		var mod = layer.BehaviorModifiers.GetGameObjectModifier(x => x is MaterialModifier);
		layer.BehaviorModifiers.RemoveGameObjectModifier(mod[0]);
	}

	[ContextMenu("Remove Height Modifier")]
	public void RemoveHeightModifier()
	{
		var layer = _abstractMap.VectorData.GetFeatureSubLayerAtIndex(0);
		var mod = layer.BehaviorModifiers.GetMeshModifier(x => x is HeightModifier || x is TextureSideWallModifier);
		layer.BehaviorModifiers.RemoveMeshModifier(mod[0]);
	}
}
