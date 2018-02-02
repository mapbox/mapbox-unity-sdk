using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CIBuildScript : MonoBehaviour {


	[MenuItem("Build/Build iOS")]
	public static void BuildForIOS()
	{
		BuildPipeline.BuildPlayer(GetBuildPlayerOptions());
	}

	private static BuildPlayerOptions GetBuildPlayerOptions()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] 
		{ 
			"Assets/Mapbox/Main.unity",
			"Assets/Mapbox/Examples/1_Explorer/Explorer.unity",
			"Assets/Mapbox/Examples/2_LocationProvider/LocationProvider.unity",
			"Assets/Mapbox/Examples/3_VoxelMap/VoxelMap.unity",
			"Assets/Mapbox/Examples/4_VectorTileMaps/BasicVectorMap/BasicVectorMap.unity",
			"Assets/Mapbox/Examples/4_VectorTileMaps/InteractiveStyledVectorMap/InteractiveStyledVectorMap.unity",
			"Assets/Mapbox/Examples/4_VectorTileMaps/PoiVectorMap/PoiVectorMap.unity",
			"Assets/Mapbox/Examples/4_VectorTileMaps/TerrainVectorMap/TerrainVectorMap.unity",
			"Assets/Mapbox/Examples/5_ZoomableMap/ZoomableMap.unity",
			"Assets/Mapbox/Examples/6_Globe/Globe.unity",
			"Assets/Mapbox/Examples/7_Playground/Scenes/Directions.unity",
			"Assets/Mapbox/Examples/7_Playground/Scenes/ForwardGeoCoder.unity",
			"Assets/Mapbox/Examples/7_Playground/Scenes/RasterTile.unity",
			"Assets/Mapbox/Examples/7_Playground/Scenes/ReverseGeoCoder.unity",
			"Assets/Mapbox/Examples/7_Playground/Scenes/VectorTile.unity"
		};
		buildPlayerOptions.locationPathName = "../Build/iOSBuild";
		buildPlayerOptions.target = BuildTarget.iOS;
		buildPlayerOptions.options = BuildOptions.Il2CPP;
		return buildPlayerOptions;
	}
}
