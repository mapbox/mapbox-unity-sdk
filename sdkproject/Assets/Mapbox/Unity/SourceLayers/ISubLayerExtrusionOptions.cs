using Mapbox.Unity.Map;

namespace Mapbox.Unity.SourceLayers
{
	public interface ISubLayerExtrusionOptions
	{
		/// <summary>
		/// Disable mesh extrusion for the features in this layer.
		/// </summary>
		void DisableExtrusion();

		/// <summary>
		/// Sets the height value to be used for Absolute Height extrusion type.
		/// Same field is used for the maximum height of Range Extrusion type so beware
		/// of possible side effects.
		/// </summary>
		/// <param name="absoluteHeight">Fixed height value for all features in the layer.</param>
		void SetAbsoluteHeight(float absoluteHeight);

		/// <summary>
		/// Change the minimum and maximum height values used for Range Height option.
		/// Maximum height is also used for Absolute Height option so beware of possible side
		/// effects.
		/// </summary>
		/// <param name="minHeight">Lower bound to be used for extrusion</param>
		/// <param name="maxHeight">Top bound to be used for extrusion</param>
		void SetHeightRange(float minHeight, float maxHeight);

		/// <summary>
		/// Sets the extrusion multiplier which will be used only in the Y axis (height).
		/// </summary>
		/// <param name="multiplier">Multiplier value.</param>
		void SetExtrusionMultiplier(float multiplier);

		/// <summary>
		/// Changes extrusion type to "Absolute height" and extrudes all features by
		/// the given fixed value.
		/// </summary>
		/// <param name="extrusionGeometryType">Option to create top and side polygons after extrusion.</param>
		/// <param name="height">Extrusion value</param>
		/// <param name="extrusionScaleFactor">Height multiplier</param>
		void EnableAbsoluteExtrusion(ExtrusionGeometryType extrusionGeometryType, float height, float extrusionScaleFactor = 1);

		/// <summary>
		/// Changes extrusion type to "Property" and extrudes all features by
		/// the choosen property's value.
		/// </summary>
		/// <param name="extrusionGeometryType">Option to create top and side polygons after extrusion.</param>
		/// <param name="propertyName">Name of the property to use for extrusion</param>
		/// <param name="extrusionScaleFactor">Height multiplier</param>
		void EnablePropertyExtrusion(ExtrusionGeometryType extrusionGeometryType, string propertyName = "height", float extrusionScaleFactor = 1);

		/// <summary>
		/// Changes extrusion type to "Minimum Height" and extrudes all features by
		/// the choosen property's value such that all vertices (roof) will be
		/// flat at the lowest vertex elevation (after terrain elevation taken into account).
		/// </summary>
		/// <param name="extrusionGeometryType">Option to create top and side polygons after extrusion.</param>
		/// <param name="propertyName">Name of the property to use for extrusion</param>
		/// <param name="extrusionScaleFactor">Height multiplier</param>
		void EnableMinExtrusion(ExtrusionGeometryType extrusionGeometryType, string propertyName = "height", float extrusionScaleFactor = 1);

		/// <summary>
		/// Changes extrusion type to "Range Height" and extrudes all features by
		/// the choosen property's value such that all vertices (roof) will be
		/// flat at the highest vertex elevation (after terrain elevation taken into account).
		/// </summary>
		/// <param name="extrusionGeometryType">Option to create top and side polygons after extrusion.</param>
		/// <param name="propertyName">Name of the property to use for extrusion</param>
		/// <param name="extrusionScaleFactor">Height multiplier</param>
		void EnableMaxExtrusion(ExtrusionGeometryType extrusionGeometryType, string propertyName = "height", float extrusionScaleFactor = 1);

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
		void EnableRangeExtrusion(ExtrusionGeometryType extrusionGeometryType, float minHeight, float maxHeight, float extrusionScaleFactor = 1);
	}
}