namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Unity.MeshGeneration.Factories;
	[Serializable]
	public class ElevationLayerProperties : LayerProperties
	{
		public ElevationSourceType sourceType = ElevationSourceType.MapboxTerrain;
		public LayerSourceOptions sourceOptions = new LayerSourceOptions()
		{
			layerSource = new Style()
			{
				Id = "mapbox.terrain-rgb"
			},
			isActive = true
		};
		public ElevationRequiredOptions requiredOptions;
		public ElevationModificationOptions modificationOptions;
		public UnityLayerOptions unityLayerOptions;
		public TerrainSideWallOptions sideWallOptions;
	}
}
