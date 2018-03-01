namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Unity.MeshGeneration.Factories;
	[Serializable]
	public class ElevationLayerProperties : LayerProperties
	{
		public LayerSourceOptions sourceOptions = new LayerSourceOptions()
		{
			layerSource = new Style()
			{
				Id = "mapbox.terrain-rgb"
			},
			isActive = true
		};
		public ElevationModificationOptions elevationLayerOptions;
		public UnityLayerOptions unityLayerOptions;
		public TerrainSideWallOptions sideWallOptions;
	}
}
