namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	[Serializable]
	public class VectorLayerProperties : LayerProperties
	{
		public VectorSourceType sourceType = VectorSourceType.MapboxStreets;
		public LayerSourceOptions sourceOptions;
		public LayerPerformanceOptions performanceOptions;
		public GeometryStylingOptions defaultStylingOptions;
		public List<VectorSubLayerProperties> vectorSubLayers = new List<VectorSubLayerProperties>(2);
	}
}
