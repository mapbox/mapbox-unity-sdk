using Mapbox.Unity.SourceLayers;

namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Data;
	using UnityEngine;

	[Serializable]
	public class GeometryExtrusionOptions : ModifierProperties, ISubLayerExtrusionOptions
	{
		public override Type ModifierType
		{
			get
			{
				return typeof(HeightModifier);
			}
		}

		[SerializeField]
		private string _selectedLayerName;
		public ExtrusionType extrusionType = ExtrusionType.None;
		public ExtrusionGeometryType extrusionGeometryType = ExtrusionGeometryType.RoofAndSide;
		[Tooltip("Property name in feature layer to use for extrusion.")]
		public string propertyName = "height";
		public string propertyDescription = "";
		public float minimumHeight = 0f;
		public float maximumHeight = 0f;
		[Tooltip("Scale factor to multiply the extrusion value of the feature.")]
		public float extrusionScaleFactor = 1f;

		public GeometryExtrusionWithAtlasOptions ToGeometryExtrusionWithAtlasOptions()
		{
			return new GeometryExtrusionWithAtlasOptions(this);
		}


		/// <summary>
		/// Disable mesh extrusion for the features in this layer.
		/// </summary>
		public virtual void DisableExtrusion()
		{
			if (extrusionType != ExtrusionType.None)
			{
				extrusionType = ExtrusionType.None;
				HasChanged = true;
			}
		}

		/// <summary>
		/// Sets the height value to be used for Absolute Height extrusion type.
		/// Same field is used for the maximum height of Range Extrusion type so beware
		/// of possible side effects.
		/// </summary>
		/// <param name="absoluteHeight">Fixed height value for all features in the layer.</param>
		public virtual void SetAbsoluteHeight(float absoluteHeight)
		{
			if (maximumHeight != absoluteHeight)
			{
				maximumHeight = absoluteHeight;
				HasChanged = true;
			}
		}

		/// <summary>
		/// Change the minimum and maximum height values used for Range Height option.
		/// Maximum height is also used for Absolute Height option so beware of possible side
		/// effects.
		/// </summary>
		/// <param name="minHeight">Lower bound to be used for extrusion</param>
		/// <param name="maxHeight">Top bound to be used for extrusion</param>
		public virtual void SetHeightRange(float minHeight, float maxHeight)
		{
			if (minimumHeight != minHeight ||
				maximumHeight != maxHeight)
			{
				minimumHeight = minHeight;
				maximumHeight = maxHeight;
				HasChanged = true;
			}
		}

		/// <summary>
		/// Sets the extrusion multiplier which will be used only in the Y axis (height).
		/// </summary>
		/// <param name="multiplier">Multiplier value.</param>
		public virtual void SetExtrusionMultiplier(float multiplier)
		{
			if (extrusionScaleFactor != multiplier)
			{
				extrusionScaleFactor = multiplier;
				HasChanged = true;
			}
		}

		/// <summary>
		/// Changes extrusion type to "Absolute height" and extrudes all features by
		/// the given fixed value.
		/// </summary>
		/// <param name="geometryType">Option to create top and side polygons after extrusion.</param>
		/// <param name="height">Extrusion value</param>
		/// <param name="scaleFactor">Height multiplier</param>
		public virtual void EnableAbsoluteExtrusion(ExtrusionGeometryType geometryType, float height, float scaleFactor = 1)
		{
			if (extrusionType != ExtrusionType.AbsoluteHeight ||
				extrusionGeometryType != geometryType ||
				!Mathf.Approximately(maximumHeight, height) ||
				!Mathf.Approximately(extrusionScaleFactor, scaleFactor))
			{
				extrusionType = ExtrusionType.AbsoluteHeight;
				extrusionGeometryType = geometryType;
				maximumHeight = height;
				extrusionScaleFactor = scaleFactor;
				HasChanged = true;
			}
		}

		/// <summary>
		/// Changes extrusion type to "Property" and extrudes all features by
		/// the choosen property's value.
		/// </summary>
		/// <param name="geometryType">Option to create top and side polygons after extrusion.</param>
		/// <param name="propertyAttribute">Name of the property to use for extrusion</param>
		/// <param name="scaleFactor">Height multiplier</param>
		public virtual void EnablePropertyExtrusion(ExtrusionGeometryType geometryType, string propertyAttribute = "height", float scaleFactor = 1)
		{
			if (extrusionType != ExtrusionType.PropertyHeight ||
				extrusionGeometryType != geometryType ||
				propertyName != propertyAttribute ||
				!Mathf.Approximately(extrusionScaleFactor, scaleFactor))
			{
				extrusionType = ExtrusionType.PropertyHeight;
				extrusionGeometryType = geometryType;
				propertyName = propertyAttribute;
				extrusionScaleFactor = scaleFactor;
				HasChanged = true;
			}
		}

		/// <summary>
		/// Changes extrusion type to "Minimum Height" and extrudes all features by
		/// the choosen property's value such that all vertices (roof) will be
		/// flat at the lowest vertex elevation (after terrain elevation taken into account).
		/// </summary>
		/// <param name="extrusionGeometryType">Option to create top and side polygons after extrusion.</param>
		/// <param name="propertyName">Name of the property to use for extrusion</param>
		/// <param name="extrusionScaleFactor">Height multiplier</param>
		public virtual void EnableMinExtrusion(ExtrusionGeometryType extrusionGeometryType, string propertyName = "height", float extrusionScaleFactor = 1)
		{
			if (extrusionType != ExtrusionType.MinHeight ||
				this.extrusionGeometryType != extrusionGeometryType ||
				this.propertyName != propertyName ||
				!Mathf.Approximately(this.extrusionScaleFactor, extrusionScaleFactor))
			{
				extrusionType = ExtrusionType.MinHeight;
				this.extrusionGeometryType = extrusionGeometryType;
				this.propertyName = propertyName;
				this.extrusionScaleFactor = extrusionScaleFactor;
				HasChanged = true;
			}
		}

		/// <summary>
		/// Changes extrusion type to "Range Height" and extrudes all features by
		/// the choosen property's value such that all vertices (roof) will be
		/// flat at the highest vertex elevation (after terrain elevation taken into account).
		/// </summary>
		/// <param name="extrusionGeometryType">Option to create top and side polygons after extrusion.</param>
		/// <param name="propertyName">Name of the property to use for extrusion</param>
		/// <param name="extrusionScaleFactor">Height multiplier</param>
		void ISubLayerExtrusionOptions.EnableMaxExtrusion(ExtrusionGeometryType extrusionGeometryType, string propertyName, float extrusionScaleFactor)
		{
			if (extrusionType != ExtrusionType.MaxHeight ||
				this.extrusionGeometryType != extrusionGeometryType ||
				this.propertyName != propertyName ||
				!Mathf.Approximately(this.extrusionScaleFactor, extrusionScaleFactor))
			{
				this.extrusionType = ExtrusionType.MaxHeight;
				this.extrusionGeometryType = extrusionGeometryType;
				this.propertyName = propertyName;
				this.extrusionScaleFactor = extrusionScaleFactor;
				HasChanged = true;
			}
		}

		/// /// <summary>
		/// Changes extrusion type to "Minimum Height" and extrudes all features by
		/// the choosen property's value such that they'll be in provided range.
		/// Lower values will be increase to Minimum Height and higher values will
		/// be lowered to Maximum height.
		/// </summary>
		/// <param name="extrusionGeometryType">Option to create top and side polygons after extrusion.</param>
		/// <param name="minHeight">Lower bound to be used for extrusion</param>
		/// <param name="maxHeight">Top bound to be used for extrusion</param>
		/// <param name="extrusionScaleFactor">Height multiplier</param>
		public virtual void EnableRangeExtrusion(ExtrusionGeometryType extrusionGeometryType, float minHeight, float maxHeight, float extrusionScaleFactor = 1)
		{
			if (extrusionType != ExtrusionType.RangeHeight ||
				this.extrusionGeometryType != extrusionGeometryType ||
				!Mathf.Approximately(minimumHeight, minHeight) ||
				!Mathf.Approximately(maximumHeight, maxHeight) ||
				!Mathf.Approximately(this.extrusionScaleFactor, extrusionScaleFactor))
			{
				extrusionType = ExtrusionType.RangeHeight;
				this.extrusionGeometryType = extrusionGeometryType;
				minimumHeight = minHeight;
				maximumHeight = maxHeight;
				this.extrusionScaleFactor = extrusionScaleFactor;
				HasChanged = true;
			}
		}
	}

	[Serializable]
	public class GeometryExtrusionWithAtlasOptions : ModifierProperties
	{
		public override Type ModifierType
		{
			get
			{
				return typeof(TextureSideWallModifier);
			}
		}
		public UvMapType texturingType = UvMapType.Tiled;
		public AtlasInfo atlasInfo;
		public ExtrusionType extrusionType = ExtrusionType.None;
		public ExtrusionGeometryType extrusionGeometryType = ExtrusionGeometryType.RoofAndSide;
		public string propertyName = "height";
		public string propertyDescription = "";
		public float minimumHeight = 0f;
		public float maximumHeight = 0f;
		public float extrusionScaleFactor = 1f;

		public GeometryExtrusionWithAtlasOptions()
		{

		}
		public GeometryExtrusionWithAtlasOptions(GeometryExtrusionOptions extrusionOptions, UVModifierOptions uvOptions)
		{
			extrusionType = extrusionOptions.extrusionType;
			extrusionGeometryType = extrusionOptions.extrusionGeometryType;
			propertyName = extrusionOptions.propertyName;
			minimumHeight = extrusionOptions.minimumHeight;
			maximumHeight = extrusionOptions.maximumHeight;
			extrusionScaleFactor = extrusionOptions.extrusionScaleFactor;

			texturingType = uvOptions.texturingType;
			atlasInfo = uvOptions.atlasInfo;
		}

		public GeometryExtrusionWithAtlasOptions(GeometryExtrusionOptions extrusionOptions)
		{
			extrusionType = extrusionOptions.extrusionType;
			extrusionGeometryType = extrusionOptions.extrusionGeometryType;
			propertyName = extrusionOptions.propertyName;
			minimumHeight = extrusionOptions.minimumHeight;
			maximumHeight = extrusionOptions.maximumHeight;
			extrusionScaleFactor = extrusionOptions.extrusionScaleFactor;
		}

		public GeometryExtrusionWithAtlasOptions(UVModifierOptions uvOptions)
		{
			texturingType = uvOptions.texturingType;
			atlasInfo = uvOptions.atlasInfo;
		}
	}
}
