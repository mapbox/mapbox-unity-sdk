namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.MeshGeneration.Factories;

	[Serializable]
	public class VectorLayerProperties : LayerProperties
	{
		public VectorSourceType sourceType = VectorSourceType.MapboxStreets;
		public LayerSourceOptions sourceOptions;
		public bool isStyleOptimized = false;
		[StyleSearch]
		public Style optimizedStyle;
		public LayerPerformanceOptions performanceOptions;
		public GeometryStylingOptions defaultStylingOptions;
		[NodeEditorElementAttribute("Vector Sublayers")]
		public List<VectorSubLayerProperties> vectorSubLayers = new List<VectorSubLayerProperties>();
	}
}
