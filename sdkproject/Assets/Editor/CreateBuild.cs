using UnityEditor;

public class CreateBuild
{	
	static void BuildNow()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] { "Assets/Mapbox/Examples/VectorDataVisualization/VectorDataVisualization.unity"};
		buildPlayerOptions.locationPathName = "../Build/OSXBuild";
		buildPlayerOptions.target = BuildTarget.StandaloneOSXIntel64;
		buildPlayerOptions.options = BuildOptions.None;
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}
}