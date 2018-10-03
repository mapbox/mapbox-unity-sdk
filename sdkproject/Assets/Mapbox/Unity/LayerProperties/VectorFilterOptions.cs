namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Filters;
	using UnityEngine;
	using System.Linq;

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

		public virtual ILayerFilter AddStringFilterContains(string key, string property)
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

		public virtual ILayerFilter AddNumericFilterEquals(string key, float value)
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

		public virtual ILayerFilter AddNumericFilterLessThan(string key, float value)
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

		public virtual ILayerFilter AddNumericFilterGreaterThan(string key, float value)
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

		public virtual ILayerFilter AddNumericFilterInRange(string key, float min, float max)
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

		public virtual void RemoveAllFilters()
		{
			for (int i = 0; i < filters.Count; i++)
			{
				LayerFilter filter = filters[i];
				if (filter != null)
				{
					RemoveFilter(filter);
				}
			}
		}

		public virtual void RemoveFilter(LayerFilter layerFilter)
		{
			layerFilter.PropertyHasChanged -= OnLayerFilterChanged;
			if (filters.Contains(layerFilter))
			{
				filters.Remove(layerFilter);
				HasChanged = true;
			}
		}

		public virtual void RemoveFilter(ILayerFilter filter)
		{
			RemoveFilter((LayerFilter)filter);
		}

		public virtual void RemoveFilter(int index)
		{
			if (index < filters.Count && filters[index] != null)
			{
				RemoveFilter(filters[index]);
			}
		}

		public virtual ILayerFilter GetFilter(int index)
		{
			if (index < filters.Count && filters[index] != null)
			{
				return filters[index];
			}
			return null;
		}

		public virtual IEnumerable<ILayerFilter> GetAllFilters()
		{
			return (IEnumerable<ILayerFilter>)filters.AsEnumerable();
		}

		public virtual IEnumerable<ILayerFilter> GetFiltersByQuery(Func<ILayerFilter, bool> query)
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
