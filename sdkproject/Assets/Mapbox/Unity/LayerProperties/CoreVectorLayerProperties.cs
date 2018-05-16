namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Filters;
	using UnityEngine;

	[Serializable]
	public class CoreVectorLayerProperties
	{
		[SerializeField]
		private string sourceId;
		[Tooltip("Is visualizer active.")]
		public bool isActive = true;
		[Tooltip("Name of the visualizer. ")]
		public string sublayerName = "untitled";
		[Tooltip("Primitive geometry type of the visualizer, allowed primitives - point, line, polygon")]
		public VectorPrimitiveType geometryType = VectorPrimitiveType.Polygon;
		[Tooltip("Name of the layer in the source tileset. This property is case sensitive.")]
		public string layerName = "layerName";
		[Tooltip("Snap features to the terrain elevation, use this option to draw features above terrain. ")]
		public bool snapToTerrain = true;
		[Tooltip("Groups features into one Unity GameObject.")]
		public bool groupFeatures = false;
		[Tooltip("Width of the line feature.")]
		public float lineWidth = 1.0f;
	}

	[Serializable]
	public class VectorFilterOptions
	{
		[SerializeField]
		private string _selectedLayerName;
		public List<LayerFilter> filters = new List<LayerFilter>();
		[Tooltip("Operator to combine filters. ")]
		public LayerFilterCombinerOperationType combinerType = LayerFilterCombinerOperationType.All;
	}
}
