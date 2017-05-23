#if UNITY_IOS
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class Mapbox_iOS_build : MonoBehaviour
{
	[PostProcessBuild]
	public static void AppendBuildProperty(BuildTarget buildTarget, string pathToBuiltProject)
	{
		if (buildTarget == BuildTarget.iOS)
		{
			PBXProject proj = new PBXProject();
			// path to pbxproj file
			string projPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";

			var file = File.ReadAllText(projPath);
			proj.ReadFromString(file);
			string target = proj.TargetGuidByName("Unity-iPhone");

			proj.AddBuildProperty(target, "HEADER_SEARCH_PATHS", "$(SRCROOT)/Libraries/Mapbox/Core/Plugins/iOS/MapboxMobileEvents/include");
			proj.AddBuildProperty(target, "OTHER_LDFLAGS", "-ObjC");

			File.WriteAllText(projPath, proj.WriteToString());
		}
	}
}
#endif
