namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Unity.Utilities;
	using UnityEngine;

	[Serializable]
	public class VectorLayerProperties : LayerProperties
	{
		public VectorSourceType sourceType = VectorSourceType.MapboxStreets;
		public LayerSourceOptions sourceOptions = new LayerSourceOptions()
		{
			isActive = true,
			layerSource = MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreets)

		};
		[Tooltip("Use Mapbox style-optimized tilesets, remove any layers or features in the tile that are not represented by a Mapbox style. Style-optimized vector tiles are smaller, served over-the-wire, and a great way to reduce the size of offline caches.")]
		public bool useOptimizedStyle = false;
		[StyleSearch]
		public Style optimizedStyle;
		public LayerPerformanceOptions performanceOptions;
		[NodeEditorElementAttribute("Vector Sublayers")]
		public List<VectorSubLayerProperties> vectorSubLayers = new List<VectorSubLayerProperties>();
	}
}
