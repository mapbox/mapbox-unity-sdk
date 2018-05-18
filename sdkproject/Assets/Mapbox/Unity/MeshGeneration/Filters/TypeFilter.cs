namespace Mapbox.Unity.MeshGeneration.Filters
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;
	using System.Linq;
	using System.Collections.Generic;

	public class TypeFilter : FilterBase
	{
		public override string Key { get { return "type"; } }
		[SerializeField]
		private string[] _types;
		[SerializeField]
		private TypeFilterType _behaviour;

		public override bool Try(VectorFeatureUnity feature)
		{
			var check = false;
			for (int i = 0; i < _types.Length; i++)
			{
				if (_types[i].ToLowerInvariant() == feature.Properties["type"].ToString().ToLowerInvariant())
				{
					check = true;
				}
			}
			return _behaviour == TypeFilterType.Include ? check : !check;
		}

		public enum TypeFilterType
		{
			Include,
			Exclude
		}
	}

	public enum LayerFilterOperationType
	{
		Contains,
		IsEqual,
		IsGreater,
		IsLess,
		IsInRange,
	}

	public enum LayerFilterCombinerOperationType
	{
		Any,
		All,
		None,
	}

	[Serializable]
	public class LayerFilterCombiner : ILayerFeatureFilterComparer
	{
		public List<ILayerFeatureFilterComparer> Filters;

		public LayerFilterCombinerOperationType Type;

		public bool Try(VectorFeatureUnity feature)
		{
			switch (Type)
			{
				case LayerFilterCombinerOperationType.Any:
					return Filters.Any(m => m.Try(feature));
				case LayerFilterCombinerOperationType.All:
					return Filters.All(m => m.Try(feature));
				case LayerFilterCombinerOperationType.None:
					return !Filters.Any(m => m.Try(feature));
				default:
					return false;
			}
		}
	}

	public class LayerFilterComparer : ILayerFeatureFilterComparer
	{
		public virtual bool Try(VectorFeatureUnity feature)
		{
			return true;
		}

		public static ILayerFeatureFilterComparer AnyOf(params ILayerFeatureFilterComparer[] filters)
		{
			return new LayerFilterCombiner
			{
				Type = LayerFilterCombinerOperationType.Any,
				Filters = filters.ToList(),
			};
		}

		public static ILayerFeatureFilterComparer AllOf(params ILayerFeatureFilterComparer[] filters)
		{
			return new LayerFilterCombiner
			{
				Type = LayerFilterCombinerOperationType.All,
				Filters = filters.ToList(),
			};
		}

		public static ILayerFeatureFilterComparer NoneOf(params ILayerFeatureFilterComparer[] filters)
		{
			return new LayerFilterCombiner
			{
				Type = LayerFilterCombinerOperationType.None,
				Filters = filters.ToList(),
			};
		}
		public static ILayerFeatureFilterComparer HasProperty(string property)
		{
			return new LayerHasPropertyFilterComparer
			{
				Key = property
			};
		}

		public static ILayerFeatureFilterComparer HasPropertyInRange(string property, double min, double max)
		{
			return new LayerPropertyInRangeFilterComparer
			{
				Key = property,
				Min = min,
				Max = max
			};
		}

		public static ILayerFeatureFilterComparer HasPropertyGreaterThan(string property, double min)
		{
			return new LayerPropertyIsGreaterFilterComparer
			{
				Key = property,
				Min = min,
			};
		}

		public static ILayerFeatureFilterComparer HasPropertyLessThan(string property, double min)
		{
			return new LayerPropertyIsLessFilterComparer
			{
				Key = property,
				Min = min,
			};
		}

		public static ILayerFeatureFilterComparer HasPropertyIsEqual(string property, double min)
		{
			return new LayerPropertyIsEqualFilterComparer
			{
				Key = property,
				Min = min,
			};
		}


		public static ILayerFeatureFilterComparer PropertyContainsValue(string property, params object[] values)
		{
			return new LayerPropertyContainsFilterComparer
			{
				Key = property,
				ValueSet = values.ToList()
			};
		}
	}

	[Serializable]
	public class LayerHasPropertyFilterComparer : ILayerFeatureFilterComparer
	{
		public string Key;

		public bool Try(VectorFeatureUnity feature)
		{
			object property;
			if (feature.Properties.TryGetValue(Key, out property))
			{
				return PropertyComparer(property);
			}
			return false;
		}

		protected virtual bool PropertyComparer(object property)
		{
			return true;
		}
	}

	[Serializable]
	public class LayerPropertyInRangeFilterComparer : LayerHasPropertyFilterComparer
	{
		public double Min;
		public double Max;

		protected override bool PropertyComparer(object property)
		{
			if (property == null)
			{
				return false;
			}
			var propertyValue = Convert.ToDouble(property);
			if (propertyValue < Min)
			{
				return false;
			}
			if (propertyValue >= Max)
			{
				return false;
			}
			return true;
		}
	}

	[Serializable]
	public class LayerPropertyIsGreaterFilterComparer : LayerHasPropertyFilterComparer
	{
		public double Min;

		protected override bool PropertyComparer(object property)
		{
			var propertyValue = Convert.ToDouble(property);
			if (property == null)
			{
				return false;
			}
			if (propertyValue > Min)
			{
				return true;
			}
			return false;
		}
	}

	[Serializable]
	public class LayerPropertyIsLessFilterComparer : LayerHasPropertyFilterComparer
	{
		public double Min;

		protected override bool PropertyComparer(object property)
		{

			if (property == null)
			{
				return false;
			}
			var propertyValue = Convert.ToDouble(property);

			if (propertyValue < Min)
			{
				return true;
			}
			return false;
		}
	}

	[Serializable]
	public class LayerPropertyIsEqualFilterComparer : LayerHasPropertyFilterComparer
	{
		public double Min;

		protected override bool PropertyComparer(object property)
		{
			if (property == null)
			{
				return false;
			}

			var propertyValue = Convert.ToDouble(property);
			if (Math.Abs(propertyValue - Min) < Mapbox.Utils.Constants.EpsilonFloatingPoint)
			{
				return true;
			}
			return false;
		}
	}

	[Serializable]
	public class LayerPropertyContainsFilterComparer : LayerHasPropertyFilterComparer
	{
		public List<object> ValueSet;

		protected override bool PropertyComparer(object property)
		{
			foreach (var value in ValueSet)
			{
				if (property.ToString().ToLower().Contains(value.ToString()))
				{
					return true;
				}
			}
			return false;
		}
	}

	[Serializable]
	public class LayerFilter
	{
		[Tooltip("Name of the property to use as key. This property is case sensitive.")]
		public string Key;
		[SerializeField]
		[Tooltip("Description of the property defined as key.")]
		private string KeyDescription;
		[Tooltip("Value to match using the operator. ")]
		public string PropertyValue = string.Empty;
		[Tooltip("Value to match using the operator. ")]
		public float Min, Max;

		[Tooltip("Filter operator to apply. ")]
		public LayerFilterOperationType filterOperator;
		private char[] _delimiters = new char[] { ',' };
		public LayerFilter(LayerFilterOperationType filterOperation)
		{
			filterOperator = filterOperation;
		}

		public ILayerFeatureFilterComparer GetFilterComparer()
		{
			if (_delimiters == null)
			{
				_delimiters = new char[] { ',' };
			}
			ILayerFeatureFilterComparer filterComparer = new LayerFilterComparer();

			switch (filterOperator)
			{
				case LayerFilterOperationType.IsEqual:
					filterComparer = LayerFilterComparer.HasPropertyIsEqual(Key, Min);
					break;
				case LayerFilterOperationType.IsGreater:
					filterComparer = LayerFilterComparer.HasPropertyGreaterThan(Key, Min);
					break;
				case LayerFilterOperationType.IsLess:
					filterComparer = LayerFilterComparer.HasPropertyLessThan(Key, Min);
					break;
				case LayerFilterOperationType.Contains:
					var matchList = PropertyValue.ToLower()
						.Split(_delimiters, StringSplitOptions.RemoveEmptyEntries)
						.Select(p => p.Trim())
						.Where(p => !string.IsNullOrEmpty(p))
						.ToArray();
					filterComparer = LayerFilterComparer.PropertyContainsValue(Key, matchList);
					break;
				case LayerFilterOperationType.IsInRange:
					filterComparer = LayerFilterComparer.HasPropertyInRange(Key, Min, Max);
					break;
				default:
					break;
			}
			return filterComparer;
		}
	}
}
