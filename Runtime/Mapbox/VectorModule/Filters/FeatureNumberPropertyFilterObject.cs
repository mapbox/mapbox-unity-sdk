using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Mapbox.BaseModule.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mapbox.VectorModule.Filters
{
	[CreateAssetMenu(menuName = "Mapbox/Filters/Number Filter")]
	[DisplayName("Number Property Filter")]
	public class FeatureNumberPropertyFilterObject : FilterBaseObject
	{
		[NonSerialized] private FeatureNumberPropertyFilter _filter;
		public FeatureNumberPropertyFilterSettings NumberFilterSettings;

		public override ILayerFeatureFilterComparer Filter
		{
			get
			{
				if (_filter == null)
					_filter = new FeatureNumberPropertyFilter(NumberFilterSettings);
				return _filter;
			}
		}
	}

	[Serializable]
	public class FeatureNumberPropertyFilter : FilterBase
	{
		public FeatureNumberPropertyFilterSettings NumberFilterSettings;

		public FeatureNumberPropertyFilter(FeatureNumberPropertyFilterSettings numberFilterSettings)
		{
			NumberFilterSettings = numberFilterSettings;
		}

		/// <summary>
		/// Runs per feature, so the boxed property is converted numerically rather than through its
		/// string form. It used to be <c>float.Parse(properties[name].ToString())</c>: an allocation
		/// and a culture-dependent parse per feature, and a KeyNotFoundException on any feature
		/// lacking the property. A feature without a readable number is not a match.
		/// </summary>
		public override bool Try(VectorFeatureUnity feature)
		{
			if (feature.Properties == null ||
			    !feature.Properties.TryGetValue(NumberFilterSettings.PropertyName, out var boxed) ||
			    !TryReadNumber(boxed, out var value))
			{
				return false;
			}

			switch (NumberFilterSettings.checkOperation)
			{
				case FilterCheckOperation.Equals: return value == NumberFilterSettings.FilterValue;
				case FilterCheckOperation.NotEquals: return value != NumberFilterSettings.FilterValue;
				case FilterCheckOperation.LessThan: return value < NumberFilterSettings.FilterValue;
				case FilterCheckOperation.LessThanOrEquals: return value <= NumberFilterSettings.FilterValue;
				case FilterCheckOperation.MoreThan: return value > NumberFilterSettings.FilterValue;
				case FilterCheckOperation.MoreThanOrEquals: return value >= NumberFilterSettings.FilterValue;
			}

			return false;
		}

		/// <summary>
		/// Unboxes the numeric types an MVT property can arrive as, without going through a string.
		/// The decoder boxes integers as <c>long</c> (see MvtProperties.NormalizeIntegers), reals as
		/// <c>float</c> or <c>double</c>; the rest are listed because a custom decode path may box
		/// differently. A string value falls back to an invariant parse - never a culture-dependent
		/// one, which is what the previous ToString round trip effectively did.
		/// </summary>
		private static bool TryReadNumber(object boxed, out float value)
		{
			switch (boxed)
			{
				case long l: value = l; return true;
				case double d: value = (float) d; return true;
				case float f: value = f; return true;
				case int i: value = i; return true;
				case ulong u: value = u; return true;
				case uint u: value = u; return true;
				case short s: value = s; return true;
				case ushort s: value = s; return true;
				case sbyte s: value = s; return true;
				case byte b: value = b; return true;
				case bool bo: value = bo ? 1 : 0; return true;
				case string str:
					return float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
				default:
					value = 0;
					return false;
			}
		}
	}

	[Serializable]
	public class FeatureNumberPropertyFilterSettings
	{
		public FilterCheckOperation checkOperation = FilterCheckOperation.Equals;
		public string PropertyName;
		public float FilterValue;
	}
	
	public enum FilterCheckOperation
	{
		NotEquals,
		Equals,
		LessThan,
		LessThanOrEquals,
		MoreThan,
		MoreThanOrEquals
	}
}