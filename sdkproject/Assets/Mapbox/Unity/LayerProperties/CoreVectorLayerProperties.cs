namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Filters;
	[Serializable]
	public class CoreVectorLayerProperties
	{
		public bool isActive = true;
		public string sublayerName = "untitled";
		public VectorPrimitiveType geometryType = VectorPrimitiveType.Polygon;
		public string layerName = "layerName";
		public bool snapToTerrain = true;
		public bool groupFeatures = false;
		public float lineWidth = 1.0f;
	}

	[Serializable]
	public class VectorFilterOptions
	{
		public List<LayerFilter> filters = new List<LayerFilter>();
		public LayerFilterCombinerOperationType combinerType = LayerFilterCombinerOperationType.All;
	}
}
