namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Filters;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using UnityEngine;
	using System.Linq;

	[Serializable]
	public class CoreVectorLayerProperties : MapboxDataProperty
	{
		[SerializeField]
		private string sourceId;
		[Tooltip("Is visualizer active.")]
		public bool isActive = true;
		[Tooltip("Name of the visualizer. ")]
		public string sublayerName = "untitled";
		[Tooltip("Primitive geometry type of the visualizer, allowed primitives - point, line, polygon")]
		public VectorPrimitiveType geometryType = VectorPrimitiveType.Polygon;
		[Tooltip("Name of the layer in the source tileset. This property is case sensitive.")]
		public string layerName = "layerName";
		[Tooltip("Snap features to the terrain elevation, use this option to draw features above terrain. ")]
		public bool snapToTerrain = true;
		[Tooltip("Groups features into one Unity GameObject.")]
		public bool combineMeshes = false;


		public override bool HasChanged
		{
			set
			{
				if (value == true)
				{
					OnPropertyHasChanged(new VectorLayerUpdateArgs { property = this });
				}
			}
		}
	}

	[Serializable]
	public class VectorFilterOptions : MapboxDataProperty, ISubLayerFiltering
	{
		[SerializeField]
		private string _selectedLayerName;
		public List<LayerFilter> filters = new List<LayerFilter>();
		[Tooltip("Operator to combine filters. ")]
		public LayerFilterCombinerOperationType combinerType = LayerFilterCombinerOperationType.All;

		public override bool HasChanged
		{
			set
			{
				if (value == true)
				{
					OnPropertyHasChanged(new VectorLayerUpdateArgs { property = this });
				}
			}
		}

		public void UnRegisterFilters()
		{
			for (int i = 0; i < filters.Count; i++)
			{
				filters[i].PropertyHasChanged -= OnLayerFilterChanged;
			}
		}

		public void RegisterFilters()
		{
			for (int i = 0; i < filters.Count; i++)
			{
				filters[i].PropertyHasChanged += OnLayerFilterChanged;
			}
		}

		private void OnLayerFilterChanged(object sender, System.EventArgs eventArgs)
		{
			HasChanged = true;
		}

		private void AddFilterToList(LayerFilter layerFilter)
		{
			filters.Add(layerFilter);
			HasChanged = true;
		}

		public virtual LayerFilter AddStringFilterContains(string key, string property)
		{
			LayerFilter layerFilter = new LayerFilter()
			{
				Key = key,
				filterOperator = LayerFilterOperationType.Contains,
				PropertyValue = property
			};
			AddFilterToList(layerFilter);
			return layerFilter;

		}

		public virtual LayerFilter AddNumericFilterEquals(string key, float value)
		{
			LayerFilter layerFilter = new LayerFilter()
			{
				Key = key,
				filterOperator = LayerFilterOperationType.IsEqual,
				Min = value
			};
			AddFilterToList(layerFilter);
			return layerFilter;
		}

		public virtual LayerFilter AddNumericFilterLessThan(string key, float value)
		{
			LayerFilter layerFilter = new LayerFilter()
			{
				Key = key,
				filterOperator = LayerFilterOperationType.IsLess,
				Min = value
			};
			AddFilterToList(layerFilter);
			return layerFilter;
		}

		public virtual LayerFilter AddNumericFilterGreaterThan(string key, float value)
		{
			LayerFilter layerFilter = new LayerFilter()
			{
				Key = key,
				filterOperator = LayerFilterOperationType.IsGreater,
				Min = value
			};
			AddFilterToList(layerFilter);
			return layerFilter;
		}

		public virtual LayerFilter AddNumericFilterInRange(string key, float min, float max)
		{
			LayerFilter layerFilter = new LayerFilter()
			{
				Key = key,
				filterOperator = LayerFilterOperationType.IsInRange,
				Min = min,
				Max = max
			};
			AddFilterToList(layerFilter);
			return layerFilter;
		}

		public void AddFilter()
		{
			AddFilterToList(new LayerFilter());
		}

		public virtual void DeleteFilter(LayerFilter layerFilter)
		{
			layerFilter.PropertyHasChanged -= OnLayerFilterChanged;
			if(filters.Contains(layerFilter))
			{
				filters.Remove(layerFilter);
				HasChanged = true;
			}
		}

		public virtual void DeleteFilter(int index)
		{
			if (index < filters.Count && filters[index] != null)
			{
				DeleteFilter(filters[index]);
			}
		}

		public virtual LayerFilter GetFilter(int index)
		{
			if(index < filters.Count && filters[index] != null)
			{
				return filters[index];
			}
			return null;
		}

		public virtual IEnumerable<LayerFilter> GetAllFilters()
		{
			return filters.AsEnumerable();
		}

		public virtual IEnumerable<LayerFilter> GetFiltersByQuery(Func<LayerFilter, bool> query)
		{
			foreach (var filter in filters)
			{
				if (query(filter))
				{
					yield return filter;
				}
			}
		}

		public virtual LayerFilterCombinerOperationType GetFilterCombinerType()
		{
			return combinerType;
		}

		public virtual void SetFilterCombinerType(LayerFilterCombinerOperationType layerFilterCombinerOperationType)
		{
			combinerType = layerFilterCombinerOperationType;
		}
	}
}
