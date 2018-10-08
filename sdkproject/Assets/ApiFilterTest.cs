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
	}

	private VectorSubLayerProperties[] GetLayers()
	{
		VectorSubLayerProperties[] vectorSubLayers;
		if (!string.IsNullOrEmpty(layerToWorkWith))
		{
			vectorSubLayers = new VectorSubLayerProperties[] { _abstractMap.VectorData.FindFeatureSubLayerWithName(layerToWorkWith) };
		}
		else
		{
			vectorSubLayers = _abstractMap.VectorData.GetAllFeatureSubLayers().ToArray();
		}
		return vectorSubLayers;
	}

	private void DebugFilterInfo(ILayerFilter layerFilter)
	{
		Debug.Log("Key : " + layerFilter.GetKey);
		Debug.Log("Operator : " + layerFilter.GetFilterOperationType.ToString());
		Debug.Log("Property : " + layerFilter.GetPropertyValue);
		Debug.Log("Min/Max : " + layerFilter.GetMinValue + ", " + layerFilter.GetMaxValue);
	}

	[ContextMenu("Check Filter ALL")]
	public void CheckFilterAll()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			ILayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterKeyMatchesExact(Key) && x.FilterUsesOperationType(layerFilterOperationType) && x.FilterNumberValueEquals(min))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				ILayerFilter layerFilter = filters[j];
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
			ILayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterKeyMatchesExact(Key) || x.FilterUsesOperationType(layerFilterOperationType) || x.FilterNumberValueEquals(min))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				ILayerFilter layerFilter = filters[j];
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
			ILayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterKeyMatchesExact(Key))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				ILayerFilter layerFilter = filters[j];
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
			ILayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterKeyContains(Key))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				ILayerFilter layerFilter = filters[j];
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
			ILayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterUsesOperationType(layerFilterOperationType))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				ILayerFilter layerFilter = filters[j];
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
			ILayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterPropertyMatchesExact(property))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				ILayerFilter layerFilter = filters[j];
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
			ILayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterPropertyContains(property))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				ILayerFilter layerFilter = filters[j];
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
			ILayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterNumberValueEquals(min))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				ILayerFilter layerFilter = filters[j];
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
			ILayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterNumberValueIsLessThan(min))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				ILayerFilter layerFilter = filters[j];
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
			ILayerFilter[] filters = layers[i].Filtering.GetFiltersByQuery(x => (x.FilterNumberValueIsGreaterThan(min))).ToArray();
			if (filters.Length == 0)
			{
				continue;
			}
			Debug.Log(layers[i].Key);
			for (int j = 0; j < filters.Length; j++)
			{
				ILayerFilter layerFilter = filters[j];
				DebugFilterInfo(layerFilter);
			}
		}
	}

	[ContextMenu("Set String Contains")]
	public void SetStringContains()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			ILayerFilter[] filters = layers[i].Filtering.GetAllFilters().ToArray();
			if (filters.Length != 0)
			{
				ILayerFilter layerFilter = filters[i];
				layerFilter.SetStringContains(Key, property);
			}
		}
	}

	[ContextMenu("Set Number Is Equal")]
	public void SetNumberIsEqual()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			ILayerFilter[] filters = layers[i].Filtering.GetAllFilters().ToArray();
			if (filters.Length != 0)
			{
				ILayerFilter layerFilter = filters[i];
				layerFilter.SetNumberIsEqual(Key, min);
			}
		}
	}

	[ContextMenu("Set Number Is Less Than")]
	public void SetNumberIsLessThan()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			ILayerFilter[] filters = layers[i].Filtering.GetAllFilters().ToArray();
			if (filters.Length != 0)
			{
				ILayerFilter layerFilter = filters[i];
				layerFilter.SetNumberIsLessThan(Key, min);
			}
		}
	}

	[ContextMenu("Set Number Is Greater Than")]
	public void SetNumberIsGreaterThan()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			ILayerFilter[] filters = layers[i].Filtering.GetAllFilters().ToArray();
			if (filters.Length != 0)
			{
				ILayerFilter layerFilter = filters[i];
				layerFilter.SetNumberIsGreaterThan(Key, min);
			}
		}
	}

	[ContextMenu("Set Number Is In Range")]
	public void SetNumberIsInRange()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			ILayerFilter[] filters = layers[i].Filtering.GetAllFilters().ToArray();
			if (filters.Length != 0)
			{
				ILayerFilter layerFilter = filters[i];
				layerFilter.SetNumberIsInRange(Key, min, max);
			}
		}
	}

	[ContextMenu("Add String Filter Contains")]
	public void AddStringFilterContains()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			layers[i].Filtering.AddStringFilterContains(Key, property);
		}
	}

	[ContextMenu("Add Numeric Filter Equals")]
	public void AddNumericFilterEquals()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			layers[i].Filtering.AddNumericFilterEquals(Key, min);
		}
	}

	[ContextMenu("Add Numeric Filter Is Less Than")]
	public void AddNumericFilterLessThan()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			layers[i].Filtering.AddNumericFilterLessThan(Key, min);
		}
	}

	[ContextMenu("Add Numeric Filter Is Greater Than")]
	public void AddNumericFilterGreaterThan()
	{
		VectorSubLayerProperties[] layers = GetLayers();
		for (int i = 0; i < layers.Length; i++)
		{
			layers[i].Filtering.AddNumericFilterGreaterThan(Key, min);
		}
	}
}
