using System;
using System.Collections.Generic;
using System.ComponentModel;
using Mapbox.BaseModule.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mapbox.VectorModule.Filters
{
	[CreateAssetMenu(menuName = "Mapbox/Filters/Type Filter")]
	[DisplayName("String Property Filter")]
	public class FeatureStringPropertyFilterObject : FilterBaseObject
	{
		[NonSerialized] private FeatureStringPropertyFilter _filter;
		public FeatureStringPropertyFilterSettings PropertyFilterSettings;

		public override ILayerFeatureFilterComparer Filter
		{
			get
			{
				if (_filter == null)
					_filter = new FeatureStringPropertyFilter(PropertyFilterSettings);
				return _filter;
			}
		}
	}
	
	[Serializable]
	public class FeatureStringPropertyFilter : FilterBase
	{
		public FeatureStringPropertyFilterSettings PropertyFilterSettings;

		// Case-insensitive by comparer, so no lowered copy is allocated per feature.
		private HashSet<string> _types;

		public FeatureStringPropertyFilter(FeatureStringPropertyFilterSettings propertyFilterSettings)
		{
			PropertyFilterSettings = propertyFilterSettings;
		}

		public override void Initialize()
		{
			base.Initialize();
			if (PropertyFilterSettings.CheckOperation == StringCheckOperation.Contains)
			{
				_types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				if (!string.IsNullOrEmpty(PropertyFilterSettings.FilterString))
				{
					foreach (var s in PropertyFilterSettings.FilterString.Split(','))
					{
						_types.Add(s.Trim());
					}
				}
			}
		}

		/// <summary>
		/// Runs per feature, so no allocation: the property is read with TryGetValue and compared
		/// case-insensitively in place.
		///
		/// This used to index Properties directly, which threw KeyNotFoundException on any feature
		/// lacking the property - and plenty do, whole layers of them - and it lowercased both the
		/// feature's value and the filter's own string on every single feature, three allocations to
		/// answer one comparison. A feature without the property is now simply not a match.
		/// </summary>
		public override bool Try(VectorFeatureUnity feature)
		{
			//this is a slightly cheesy way to negate by a flag
			//so right side is simply negated by the first part and equality check

			var result = false;
			if (feature.Properties != null &&
			    feature.Properties.TryGetValue(PropertyFilterSettings.PropertyName, out var value) &&
			    value != null)
			{
				// Only strings avoid a ToString allocation here; a boxed number formats itself, which
				// is the caller asking a string filter to do a number's job.
				var featureValue = value as string ?? value.ToString();

				if (PropertyFilterSettings.CheckOperation == StringCheckOperation.Equals)
				{
					result = string.Equals(PropertyFilterSettings.FilterString, featureValue,
						StringComparison.OrdinalIgnoreCase);
				}
				else if (PropertyFilterSettings.CheckOperation == StringCheckOperation.Contains)
				{
					// Initialize builds this; if it never ran, nothing can match.
					result = _types != null && _types.Contains(featureValue);
				}
			}

			return (!PropertyFilterSettings.Invert) ? result : !result;
		}
	}

	[Serializable]
	public class FeatureStringPropertyFilterSettings
	{
		public string PropertyName;
		public StringCheckOperation CheckOperation = StringCheckOperation.Equals;
		public string FilterString;
		public bool Invert = false;
	}

	public enum StringCheckOperation
	{
		Equals,
		Contains
	}
}
