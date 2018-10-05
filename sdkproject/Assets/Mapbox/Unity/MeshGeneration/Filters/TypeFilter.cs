namespace Mapbox.Unity.MeshGeneration.Filters
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using Mapbox.Unity.Map;

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
	public class LayerFilter : MapboxDataProperty, ILayerFilter
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

		public LayerFilter(LayerFilterOperationType filterOperation = LayerFilterOperationType.Contains)
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

		/// <summary>
		/// Sets the string contains.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="property">Property.</param>
		public virtual void SetStringContains(string key, string property)
		{
			filterOperator = LayerFilterOperationType.Contains;
			Key = key;
			PropertyValue = property;
			HasChanged = true;
		}

		/// <summary>
		/// Sets the number is equal.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
		public virtual void SetNumberIsEqual(string key, float value)
		{
			filterOperator = LayerFilterOperationType.IsEqual;
			Key = key;
			Min = value;
			HasChanged = true;
		}

		/// <summary>
		/// Sets the number is less than.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
		public virtual void SetNumberIsLessThan(string key, float value)
		{
			filterOperator = LayerFilterOperationType.IsLess;
			Key = key;
			Min = value;
			HasChanged = true;
		}

		/// <summary>
		/// Sets the number is greater than.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
		public virtual void SetNumberIsGreaterThan(string key, float value)
		{
			filterOperator = LayerFilterOperationType.IsGreater;
			Key = key;
			Min = value;
			HasChanged = true;
		}

		/// <summary>
		/// Sets the number is in range.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="min">Minimum.</param>
		/// <param name="max">Max.</param>
		public virtual void SetNumberIsInRange(string key, float min, float max)
		{
			filterOperator = LayerFilterOperationType.IsInRange;
			Key = key;
			Min = min;
			Max = max;
			HasChanged = true;
		}

		/// <summary>
		/// Gets the key.
		/// </summary>
		/// <returns>The key.</returns>
		public virtual string GetKey
		{
			get
			{
				return Key;
			}
		}

		/// <summary>
		/// Gets the type of the filter operation.
		/// </summary>
		/// <returns>The filter operation type.</returns>
		public virtual LayerFilterOperationType GetFilterOperationType
		{
			get
			{
				return filterOperator;
			}
		}

		/// <summary>
		/// Gets the property value.
		/// </summary>
		/// <returns>The property value.</returns>
		public virtual string GetPropertyValue
		{
			get
			{
				return PropertyValue;
			}
		}

		/// <summary>
		/// Gets the minimum value.
		/// </summary>
		/// <returns>The minimum value.</returns>
		public virtual float GetNumberValue
		{
			get
			{
				return Min;
			}
		}

		/// <summary>
		/// Gets the minimum value.
		/// </summary>
		/// <returns>The minimum value.</returns>
		public virtual float GetMinValue
		{
			get
			{
				return Min;
			}
		}

		/// <summary>
		/// Gets the max value.
		/// </summary>
		/// <returns>The max value.</returns>
		public virtual float GetMaxValue
		{
			get
			{
				return Max;
			}
		}

		/// <summary>
		/// Returns true if filter key contains a given string.
		/// </summary>
		/// <returns><c>true</c>, if key contains was filtered, <c>false</c> otherwise.</returns>
		/// <param name="key">Key.</param>
		public virtual bool FilterKeyContains(string key)
		{
			return Key.Contains(key);
		}

		/// <summary>
		/// Returns true if filter key matches a given string exactly.
		/// </summary>
		/// <returns><c>true</c>, if key matches exact was filtered, <c>false</c> otherwise.</returns>
		/// <param name="key">Key.</param>
		public virtual bool FilterKeyMatchesExact(string key)
		{
			return Key == key;
		}

		/// <summary>
		/// Returns true if filter uses a given operation type.
		/// </summary>
		/// <returns><c>true</c>, if uses operation type was filtered, <c>false</c> otherwise.</returns>
		/// <param name="layerFilterOperationType">Layer filter operation type.</param>
		public virtual bool FilterUsesOperationType(LayerFilterOperationType layerFilterOperationType)
		{
			return filterOperator == layerFilterOperationType;
		}

		/// <summary>
		/// Returns true if filter property contains a given string.
		/// </summary>
		/// <returns><c>true</c>, if property contains was filtered, <c>false</c> otherwise.</returns>
		/// <param name="property">Property.</param>
		public virtual bool FilterPropertyContains(string property)
		{
			return PropertyValue.Contains(property);
		}

		/// <summary>
		/// Returns true if filter property matches a given string exactly.
		/// </summary>
		/// <returns><c>true</c>, if property matches exact was filtered, <c>false</c> otherwise.</returns>
		/// <param name="property">Property.</param>
		public virtual bool FilterPropertyMatchesExact(string property)
		{
			return PropertyValue == property;
		}

		/// <summary>
		/// Returns true if filter number value is equal to a given number.
		/// </summary>
		/// <returns><c>true</c>, if number value equals was filtered, <c>false</c> otherwise.</returns>
		/// <param name="value">Value.</param>
		public virtual bool FilterNumberValueEquals(float value)
		{
			return Mathf.Approximately(Min, value);
		}

		/// <summary>
		/// Returns true if filter number value is greater than a given number.
		/// </summary>
		/// <returns><c>true</c>, if number value is greater than was filtered, <c>false</c> otherwise.</returns>
		/// <param name="value">Value.</param>
		public virtual bool FilterNumberValueIsGreaterThan(float value)
		{
			return Min > value;
		}

		/// <summary>
		/// Returns true if filter number value is less than a given number.
		/// </summary>
		/// <returns><c>true</c>, if number value is less than was filtered, <c>false</c> otherwise.</returns>
		/// <param name="value">Value.</param>
		public virtual bool FilterNumberValueIsLessThan(float value)
		{
			return Min < value;	
		}

		/// <summary>
		/// Returns true if filter range values contain a given number.
		/// </summary>
		/// <returns><c>true</c>, if is in range value contains was filtered, <c>false</c> otherwise.</returns>
		/// <param name="value">Value.</param>
		public virtual bool FilterIsInRangeValueContains(float value)
		{
			return Min < value && value < Max;
		}
	}
}
