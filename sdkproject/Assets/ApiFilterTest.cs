using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Interfaces;
using Mapbox.Unity.MeshGeneration.Filters;
using UnityEngine;

public class ApiFilterTest : MonoBehaviour
{
	private AbstractMap _abstractMap;

	private VectorSubLayerProperties[] _layers; 

	public string layerToWorkWith;

	public string Key;
	public LayerFilterOperationType layerFilterOperationType;
	public float min;
	public float max;
	public string property;

	public LayerFilterCombinerOperationType layerFilterCombinerOperationType;

	void Start()
	{
		_abstractMap = FindObjectOfType<AbstractMap>();
		//_layers = _abstractMap.VectorData.GetAllFeatureLayers().ToArray();
	}

	private VectorSubLayerProperties[] GetLayers()
	{
		VectorSubLayerProperties[] vectorSubLayers;
		if(!string.IsNullOrEmpty(layerToWorkWith))
		{
			vectorSubLayers = new VectorSubLayerProperties[] { _abstractMap.VectorData.FindFeatureLayerWithName(layerToWorkWith) };
		}
		else
		{
			vectorSubLayers = _abstractMap.VectorData.GetAllFeatureLayers().ToArray();
		}
		return vectorSubLayers;
	}

	private void DebugFilterInfo(LayerFilter layerFilter)
	{
		Debug.Log("Key : " + layerFilter.Key);
		Debug.Log("Operator : " + layerFilter.filterOperator.ToString());
		Debug.Log("Property : " + layerFilter.PropertyValue);
		Debug.Log("Min/Max : " + layerFilter.Min + ", " + layerFilter.Max);
	}

	[ContextMenu("Add Filter")]
	public void AddFilters()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			VectorSubLayerProperties vectorSubLayerProperties = layers[i];
			vectorSubLayerProperties.Filtering.AddFilter();
		}
	}

	[ContextMenu("Add Filter With Everything")]
	public void AddFiltersWithEverything()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			VectorSubLayerProperties vectorSubLayerProperties = layers[i];
			LayerFilterBundle layerFilterBundle = new LayerFilterBundle()
			{
				key = Key,
				layerFilterOperationType = layerFilterOperationType,
				propertyValue = property,
				min = min,
				max = max
			};
			vectorSubLayerProperties.Filtering.AddFilter(layerFilterBundle);
		}
	}

	[ContextMenu("Check Filter ALL")]
	public void CheckFilterAll()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			LayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterKeyMatchesExact(Key) && x.FilterUsesOperationType(layerFilterOperationType) && x.FilterNumberValueEquals(min))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				LayerFilter layerFilter = filters[j];
				DebugFilterInfo(layerFilter);
			}
		}
	}

	[ContextMenu("Check Filter ANY")]
	public void CheckFilterAny()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			LayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterKeyMatchesExact(Key) || x.FilterUsesOperationType(layerFilterOperationType) || x.FilterNumberValueEquals(min))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				LayerFilter layerFilter = filters[j];
				DebugFilterInfo(layerFilter);
			}
		}
	}

	[ContextMenu("Check Filter Key Exact")]
	public void CheckFilterKeyExact()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			LayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterKeyMatchesExact(Key))).ToArray();
			if(filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				LayerFilter layerFilter = filters[j];
				DebugFilterInfo(layerFilter);
			}
		}
	}

	[ContextMenu("Check Filter Key Contains")]
	public void CheckFilterKeyContains()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			LayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterKeyContains(Key))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				LayerFilter layerFilter = filters[j];
				DebugFilterInfo(layerFilter);
			}
		}
	}

	[ContextMenu("Check Filter Uses Operation Type")]
	public void CheckFilterUsesOperationType()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			LayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterUsesOperationType(layerFilterOperationType))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				LayerFilter layerFilter = filters[j];
				DebugFilterInfo(layerFilter);
			}
		}
	}

	[ContextMenu("Check Filter Has Exact Property")]
	public void CheckFilterHasExactProperty()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			LayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterPropertyMatchesExact(property))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				LayerFilter layerFilter = filters[j];
				DebugFilterInfo(layerFilter);
			}
		}
	}

	[ContextMenu("Check Filter Contains Property")]
	public void CheckFilterContainsProperty()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			LayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterPropertyContains(property))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				LayerFilter layerFilter = filters[j];
				DebugFilterInfo(layerFilter);
			}
		}
	}

	[ContextMenu("Check Filter Num Is Equal")]
	public void CheckFilterNumValueIsEqual()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			LayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterNumberValueEquals(min))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				LayerFilter layerFilter = filters[j];
				DebugFilterInfo(layerFilter);
			}
		}
	}

	[ContextMenu("Check Filter Num Is Less")]
	public void CheckFilterNumValueIsLess()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			LayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterNumberValueIsLessThan(min))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				LayerFilter layerFilter = filters[j];
				DebugFilterInfo(layerFilter);
			}
		}
	}

	[ContextMenu("Check Filter Num Is Greater")]
	public void CheckFilterNumValueIsGreater()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			LayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterNumberValueIsGreaterThan(min))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				LayerFilter layerFilter = filters[j];
				DebugFilterInfo(layerFilter);
			}
		}
	}

	[ContextMenu("Set Filter Keys")]
	public void SetFilterKeys()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			LayerFilter[] filters = layers[i].Filtering.GetAllFilters().ToArray();
			if (filters.Length != 0)
			{
				LayerFilter layerFilter = filters[i];
				layerFilter.SetKey(Key);
			}
		}
	}

	[ContextMenu("Set Filter Operators")]
	public void SetFilterOperators()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			LayerFilter[] filters = layers[i].Filtering.GetAllFilters().ToArray();
			if (filters.Length != 0)
			{
				LayerFilter layerFilter = filters[i]; 
				layerFilter.SetFilterOperationType(layerFilterOperationType);
			}
		}
	}

	[ContextMenu("Set Filter Equals")]
	public void SetFilterEquals()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			LayerFilter[] filters = layers[i].Filtering.GetAllFilters().ToArray();
			if (filters.Length != 0)
			{
				LayerFilter layerFilter = filters[i];
				layerFilter.SetIsEqual(min);
			}
		}
	}

	[ContextMenu("Set Filter Less Than")]
	public void SetFilterLessThan()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			LayerFilter[] filters = layers[i].Filtering.GetAllFilters().ToArray();
			if (filters.Length != 0)
			{
				LayerFilter layerFilter = filters[i];
				layerFilter.SetIsLessThan(min);
			}
		}
	}

	[ContextMenu("Set Filter Greater Than")]
	public void SetFilterGreaterThan()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			LayerFilter[] filters = layers[i].Filtering.GetAllFilters().ToArray();
			if (filters.Length != 0)
			{
				LayerFilter layerFilter = filters[i];
				layerFilter.SetIsGreaterThan(min);
			}
		}
	}

	[ContextMenu("Set Filter In Range")]
	public void SetFilterInRange()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			LayerFilter[] filters = layers[i].Filtering.GetAllFilters().ToArray();
			if (filters.Length != 0)
			{
				LayerFilter layerFilter = filters[i];
				layerFilter.SetIsInRange(min, max);
			}
		}
	}
}