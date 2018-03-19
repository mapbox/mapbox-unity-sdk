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
		public ElevationLayerType elevationLayerType = ElevationLayerType.FlatTerrain;
		public ElevationRequiredOptions requiredOptions = new ElevationRequiredOptions();
		public ElevationModificationOptions modificationOptions = new ElevationModificationOptions();
		public UnityLayerOptions unityLayerOptions = new UnityLayerOptions();
		public TerrainSideWallOptions sideWallOptions = new TerrainSideWallOptions();
	}
}
