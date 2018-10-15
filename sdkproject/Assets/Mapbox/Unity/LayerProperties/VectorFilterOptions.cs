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

		/// <summary>
		/// Adds a string filter that uses a contains operator.
		/// </summary>
		/// <returns>The filter.</returns>
		/// <param name="key">Key.</param>
		/// <param name="property">Property.</param>
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

		/// <summary>
		/// Adds a number filter that uses an equals operator.
		/// </summary>
		/// <returns>The filter.</returns>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
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

		/// <summary>
		/// Adds a number filter that uses a less than operator.
		/// </summary>
		/// <returns>The filter.</returns>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
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

		/// <summary>
		/// Adds a number filter that uses a greater than operator.
		/// </summary>
		/// <returns>The filter.</returns>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
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

		/// <summary>
		/// Adds a number filter that uses an in range operator.
		/// </summary>
		/// <returns>The filter.</returns>
		/// <param name="key">Key.</param>
		/// <param name="min">Minimum.</param>
		/// <param name="max">Max.</param>
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

		/// <summary>
		/// Removes all filters.
		/// </summary>
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

		/// <summary>
		/// Removes a filter.
		/// </summary>
		/// <param name="layerFilter">Layer filter.</param>
		public virtual void RemoveFilter(LayerFilter layerFilter)
		{
			layerFilter.PropertyHasChanged -= OnLayerFilterChanged;
			if (filters.Contains(layerFilter))
			{
				filters.Remove(layerFilter);
				HasChanged = true;
			}
		}

		/// <summary>
		/// Removes a filter.
		/// </summary>
		/// <param name="filter">Filter.</param>
		public virtual void RemoveFilter(ILayerFilter filter)
		{
			RemoveFilter((LayerFilter)filter);
		}

		/// <summary>
		/// Removes the filter using an index lookup.
		/// </summary>
		/// <param name="index">Index.</param>
		public virtual void RemoveFilter(int index)
		{
			if (index < filters.Count && filters[index] != null)
			{
				RemoveFilter(filters[index]);
			}
		}

		/// <summary>
		/// Gets a filter using an index lookup.
		/// </summary>
		/// <returns>The filter.</returns>
		/// <param name="index">Index.</param>
		public virtual ILayerFilter GetFilter(int index)
		{
			if (index < filters.Count && filters[index] != null)
			{
				return filters[index];
			}
			return null;
		}

		/// <summary>
		/// Gets all filters.
		/// </summary>
		/// <returns>All filters.</returns>
		public virtual IEnumerable<ILayerFilter> GetAllFilters()
		{
			return (IEnumerable<ILayerFilter>)filters.AsEnumerable();
		}

		/// <summary>
		/// Gets the filters by query.
		/// </summary>
		/// <returns>Filters by query.</returns>
		/// <param name="query">Query.</param>
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

		/// <summary>
		/// Gets the type of the filter combiner.
		/// </summary>
		/// <returns>The filter combiner type.</returns>
		public virtual LayerFilterCombinerOperationType GetFilterCombinerType()
		{
			return combinerType;
		}

		/// <summary>
		/// Sets the type of the filter combiner.
		/// </summary>
		/// <param name="layerFilterCombinerOperationType">Layer filter combiner operation type.</param>
		public virtual void SetFilterCombinerType(LayerFilterCombinerOperationType layerFilterCombinerOperationType)
		{
			combinerType = layerFilterCombinerOperationType;
		}
	}
}
