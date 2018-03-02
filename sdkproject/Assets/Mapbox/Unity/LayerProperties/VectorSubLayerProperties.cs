namespace Mapbox.Unity.Map
{
	using System;
	[Serializable]
	public class VectorSubLayerProperties : LayerProperties
	{
		public CoreVectorLayerProperties coreOptions;
		public VectorFilterOptions filterOptions;
		public GeometryExtrusionOptions extrusionOptions;
		public GeometryMaterialOptions materialOptions = new GeometryMaterialOptions();
		//public GeometryStylingOptions stylingOptions;
		public LayerModifierOptions modifierOptions;
	}
}
