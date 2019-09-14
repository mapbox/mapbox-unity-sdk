using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using UnityEngine.XR.iOS;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;


public class UnityARBuildPostprocessor
{
    static List<ARReferenceImagesSet> imageSets = new List<ARReferenceImagesSet>();
	static List<ARReferenceObjectsSetAsset> objectSets = new List<ARReferenceObjectsSetAsset>();
    // Build postprocessor. Currently only needed on:
    // - iOS: no dynamic libraries, so plugin source files have to be copied into Xcode project
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target == BuildTarget.iOS)
            OnPostprocessBuildIOS(pathToBuiltProject);
    }

    [PostProcessScene]
    public static void OnPostProcessScene()
    {
        if (!BuildPipeline.isBuildingPlayer)
            return;

        foreach (ARReferenceImagesSet ar in UnityEngine.Resources.FindObjectsOfTypeAll<ARReferenceImagesSet>())
        {
            if (!imageSets.Contains(ar))
            {
                imageSets.Add(ar);
            }
        }

		foreach (ARReferenceObjectsSetAsset ar in UnityEngine.Resources.FindObjectsOfTypeAll<ARReferenceObjectsSetAsset>())
		{
			if (!objectSets.Contains(ar))
			{
				objectSets.Add(ar);
			}
		}

    }

    private static UnityARKitPluginSettings LoadSettings()
    {
        UnityARKitPluginSettings loadedSettings = Resources.Load<UnityARKitPluginSettings>("UnityARKitPlugin/ARKitSettings");
        if (loadedSettings == null)
        {
            loadedSettings = ScriptableObject.CreateInstance<UnityARKitPluginSettings>();
        }
        return loadedSettings;
    }

    // Replaces the first C++ macro with the given name in the source file. Only changes
    // single-line macro declarations, if multi-line macro declaration is detected, the
    // function returns without changing it. Macro name must be a valid C++ identifier.
    internal static bool ReplaceCppMacro(string[] lines, string name, string newValue)
    {
        bool replaced = false;
        Regex matchRegex = new Regex(@"^.*#\s*define\s+" + name);
        Regex replaceRegex = new Regex(@"^.*#\s*define\s+" + name + @"(:?|\s|\s.*[^\\])$");
        for (int i = 0; i < lines.Count(); i++)
        {
            if (matchRegex.Match(lines[i]).Success)
            {
                lines[i] = replaceRegex.Replace(lines[i], "#define " + name + " " + newValue);
                replaced = true;
            }
        }
        return replaced;
    }

    internal static void AddOrReplaceCppMacro(ref string[] lines, string name, string newValue)
    {
        if (ReplaceCppMacro(lines, name, newValue) == false)
        {
            Array.Resize(ref lines, lines.Length + 1);
            lines[lines.Length - 1] = "#define " + name + " " + newValue;
        }
    }

    static void UpdateDefinesInFile(string file, Dictionary<string, bool> valuesToUpdate)
    {
        string[] src = File.ReadAllLines(file);
        var copy = (string[])src.Clone();

        foreach (var kvp in valuesToUpdate)
            AddOrReplaceCppMacro(ref copy, kvp.Key, kvp.Value ? "1" : "0");

        if (!copy.SequenceEqual(src))
            File.WriteAllLines(file, copy);
    }

#if UNITY_IOS
    static void AddReferenceImageToResourceGroup(ARReferenceImage arri, string parentFolderFullPath, string projectRelativePath, PBXProject project)
	{

		ARResourceContents resourceContents = new ARResourceContents ();
		resourceContents.info = new ARResourceInfo ();
		resourceContents.info.author = "unity";
		resourceContents.info.version = 1;

		resourceContents.images = new ARResourceFilename[1];
		resourceContents.images [0] = new ARResourceFilename ();
		resourceContents.images [0].idiom = "universal";

		resourceContents.properties = new ARResourceProperties ();
		resourceContents.properties.width = arri.physicalSize;

		//add folder for reference image
		string folderToCreate = arri.imageName + ".arreferenceimage";
		string folderFullPath = Path.Combine (parentFolderFullPath, folderToCreate);
		string projectRelativeFolder = Path.Combine (projectRelativePath, folderToCreate);
		Directory.CreateDirectory (folderFullPath);
		project.AddFolderReference (folderFullPath, projectRelativeFolder);

		//copy file from texture asset
		string imagePath = AssetDatabase.GetAssetPath(arri.imageTexture);
		string imageFilename = Path.GetFileName (imagePath);
		var dstPath = Path.Combine(folderFullPath, imageFilename);
		File.Copy(imagePath, dstPath, true);
		project.AddFile (dstPath, Path.Combine (projectRelativeFolder, imageFilename));
		resourceContents.images [0].filename = imageFilename;

		//add contents.json file
		string contentsJsonPath = Path.Combine(folderFullPath, "Contents.json");
		File.WriteAllText (contentsJsonPath, JsonUtility.ToJson (resourceContents, true));
		project.AddFile (contentsJsonPath, Path.Combine (projectRelativeFolder, "Contents.json"));

	}

	static void AddReferenceImagesSetToAssetCatalog(ARReferenceImagesSet aris, string pathToBuiltProject, PBXProject project)
	{
		List<ARReferenceImage> processedImages = new List<ARReferenceImage> ();
		ARResourceGroupContents groupContents = new ARResourceGroupContents();
		groupContents.info = new ARResourceGroupInfo ();
		groupContents.info.author = "unity";
		groupContents.info.version = 1;
		string folderToCreate = "Unity-iPhone/Images.xcassets/" + aris.resourceGroupName + ".arresourcegroup";
		string folderFullPath = Path.Combine (pathToBuiltProject, folderToCreate);
		Directory.CreateDirectory (folderFullPath);
		project.AddFolderReference (folderFullPath, folderToCreate);
		foreach (ARReferenceImage arri in aris.referenceImages) {
			if (!processedImages.Contains (arri)) {
				processedImages.Add (arri); //get rid of dupes
				AddReferenceImageToResourceGroup(arri, folderFullPath, folderToCreate, project);
			}
		}

		groupContents.resources = new ARResourceGroupResource[processedImages.Count];
		int index = 0;
		foreach (ARReferenceImage arri in processedImages) {
			groupContents.resources [index] = new ARResourceGroupResource ();
			groupContents.resources [index].filename = arri.imageName + ".arreferenceimage";
			index++;
		}
		string contentsJsonPath = Path.Combine(folderFullPath, "Contents.json");
		File.WriteAllText (contentsJsonPath, JsonUtility.ToJson (groupContents, true));
		project.AddFile (contentsJsonPath, Path.Combine (folderToCreate, "Contents.json"));
	}

	static void AddReferenceObjectAssetToStreamingAssets(ARReferenceObjectAsset arro, string parentFolderFullPath, string projectRelativePath)
	{

		ARReferenceObjectResourceContents resourceContents = new ARReferenceObjectResourceContents ();
		resourceContents.info = new ARResourceInfo ();
		resourceContents.info.author = "unity";
		resourceContents.info.version = 1;

		resourceContents.objects = new ARResourceFilename[1];
		resourceContents.objects [0] = new ARResourceFilename ();
		resourceContents.objects [0].idiom = "universal";

		resourceContents.referenceObjectName = arro.objectName;

		//add folder for reference image
		string folderToCreate = arro.objectName + ".arreferenceobject";
		string folderFullPath = Path.Combine (parentFolderFullPath, folderToCreate);
		string projectRelativeFolder = Path.Combine (projectRelativePath, folderToCreate);
		Directory.CreateDirectory (folderFullPath);

		//copy file from refobject asset
		string objectPath = AssetDatabase.GetAssetPath(arro.referenceObject);
		string objectFilename = Path.GetFileName (objectPath);
		var dstPath = Path.Combine(folderFullPath, objectFilename);
		File.Copy(objectPath, dstPath, true);
		resourceContents.objects [0].filename = objectFilename;

		//add contents.json file
		string contentsJsonPath = Path.Combine(folderFullPath, "Contents.json");
		File.WriteAllText (contentsJsonPath, JsonUtility.ToJson (resourceContents, true));
	}

	static void AddReferenceObjectsSetAssetToStreamingAssets(ARReferenceObjectsSetAsset aros, string pathToBuiltProject)
	{
		List<ARReferenceObjectAsset> processedObjects = new List<ARReferenceObjectAsset> ();
		ARResourceGroupContents groupContents = new ARResourceGroupContents();
		groupContents.info = new ARResourceGroupInfo ();
		groupContents.info.author = "xcode";
		groupContents.info.version = 1;
		//On iOS, StreamingAssets end up at /Data/Raw
		string folderToCreate = "Data/Raw/ARReferenceObjects/" + aros.resourceGroupName + ".arresourcegroup";
		string folderFullPath = Path.Combine (pathToBuiltProject, folderToCreate);
		Directory.CreateDirectory (folderFullPath);
		foreach (ARReferenceObjectAsset arro in aros.referenceObjectAssets) {
			if (!processedObjects.Contains (arro)) {
				processedObjects.Add (arro); //get rid of dupes
				AddReferenceObjectAssetToStreamingAssets(arro, folderFullPath, folderToCreate);
			}
		}

		groupContents.resources = new ARResourceGroupResource[processedObjects.Count];
		int index = 0;
		foreach (ARReferenceObjectAsset arro in processedObjects) {
			groupContents.resources [index] = new ARResourceGroupResource ();
			groupContents.resources [index].filename = arro.objectName + ".arreferenceobject";
			index++;
		}
		string contentsJsonPath = Path.Combine(folderFullPath, "Contents.json");
		File.WriteAllText (contentsJsonPath, JsonUtility.ToJson (groupContents, true));
	}


	#if ARREFERENCEOBJECT_XCODE_ASSET_CATALOG
	static void AddReferenceObjectAssetToResourceGroup(ARReferenceObjectAsset arro, string parentFolderFullPath, string projectRelativePath, PBXProject project)
	{

		ARReferenceObjectResourceContents resourceContents = new ARReferenceObjectResourceContents ();
		resourceContents.info = new ARResourceInfo ();
		resourceContents.info.author = "unity";
		resourceContents.info.version = 1;

		resourceContents.objects = new ARResourceFilename[1];
		resourceContents.objects [0] = new ARResourceFilename ();
		resourceContents.objects [0].idiom = "universal";

		//add folder for reference image
		string folderToCreate = arro.objectName + ".arreferenceobject";
		string folderFullPath = Path.Combine (parentFolderFullPath, folderToCreate);
		string projectRelativeFolder = Path.Combine (projectRelativePath, folderToCreate);
		Directory.CreateDirectory (folderFullPath);
		project.AddFolderReference (folderFullPath, projectRelativeFolder);

		//copy file from texture asset
		string objectPath = AssetDatabase.GetAssetPath(arro.referenceObject);
		string objectFilename = Path.GetFileName (objectPath);
		var dstPath = Path.Combine(folderFullPath, objectFilename);
		File.Copy(objectPath, dstPath, true);
		project.AddFile (dstPath, Path.Combine (projectRelativeFolder, objectFilename));
		resourceContents.objects [0].filename = objectFilename;

		//add contents.json file
		string contentsJsonPath = Path.Combine(folderFullPath, "Contents.json");
		File.WriteAllText (contentsJsonPath, JsonUtility.ToJson (resourceContents, true));
		project.AddFile (contentsJsonPath, Path.Combine (projectRelativeFolder, "Contents.json"));

	}

	static void AddReferenceObjectsSetAssetToAssetCatalog(ARReferenceObjectsSetAsset aros, string pathToBuiltProject, PBXProject project)
	{
		List<ARReferenceObjectAsset> processedObjects = new List<ARReferenceObjectAsset> ();
		ARResourceGroupContents groupContents = new ARResourceGroupContents();
		groupContents.info = new ARResourceGroupInfo ();
		groupContents.info.author = "xcode";
		groupContents.info.version = 1;
		string folderToCreate = "Unity-iPhone/Images.xcassets/" + aros.resourceGroupName + ".arresourcegroup";
		string folderFullPath = Path.Combine (pathToBuiltProject, folderToCreate);
		Directory.CreateDirectory (folderFullPath);
		project.AddFolderReference (folderFullPath, folderToCreate);
		foreach (ARReferenceObjectAsset arro in aros.referenceObjectAssets) {
			if (!processedObjects.Contains (arro)) {
				processedObjects.Add (arro); //get rid of dupes
				AddReferenceObjectAssetToResourceGroup(arro, folderFullPath, folderToCreate, project);
			}
		}

		groupContents.resources = new ARResourceGroupResource[processedObjects.Count];
		int index = 0;
		foreach (ARReferenceObjectAsset arro in processedObjects) {
			groupContents.resources [index] = new ARResourceGroupResource ();
			groupContents.resources [index].filename = arro.objectName + ".arreferenceobject";
			index++;
		}
		string contentsJsonPath = Path.Combine(folderFullPath, "Contents.json");
		File.WriteAllText (contentsJsonPath, JsonUtility.ToJson (groupContents, true));
		project.AddFile (contentsJsonPath, Path.Combine (folderToCreate, "Contents.json"));
	}
	#endif //ARREFERENCEOBJECT_XCODE_ASSET_CATALOG

#endif //UNITY_IOS

	private static void OnPostprocessBuildIOS(string pathToBuiltProject)
	{
		// We use UnityEditor.iOS.Xcode API which only exists in iOS editor module
#if UNITY_IOS
		string projPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";

		UnityEditor.iOS.Xcode.PBXProject proj = new UnityEditor.iOS.Xcode.PBXProject();
		proj.ReadFromString(File.ReadAllText(projPath));
		proj.AddFrameworkToProject(proj.TargetGuidByName("Unity-iPhone"), "ARKit.framework", false);
		string target = proj.TargetGuidByName("Unity-iPhone");
		Directory.CreateDirectory(Path.Combine(pathToBuiltProject, "Libraries/Unity"));

		// Check UnityARKitPluginSettings
		UnityARKitPluginSettings ps = LoadSettings();
		string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
		PlistDocument plist = new PlistDocument();
		plist.ReadFromString(File.ReadAllText(plistPath));
		PlistElementDict rootDict = plist.root;

		// Get or create array to manage device capabilities
		const string capsKey = "UIRequiredDeviceCapabilities";
		PlistElementArray capsArray;
		PlistElement pel;
		if (rootDict.values.TryGetValue(capsKey, out pel)) {
			capsArray = pel.AsArray();
		}
		else {
			capsArray = rootDict.CreateArray(capsKey);
		}
		// Remove any existing "arkit" plist entries
		const string arkitStr = "arkit";
		capsArray.values.RemoveAll(x => arkitStr.Equals(x.AsString()));
		if (ps.AppRequiresARKit) {
			// Add "arkit" plist entry
			capsArray.AddString(arkitStr);
		}

		const string shareString = "UIFileSharingEnabled";
		rootDict.SetBoolean(shareString, true);

		File.WriteAllText(plistPath, plist.WriteToString());

		foreach(ARReferenceImagesSet ar in imageSets)
		{
			AddReferenceImagesSetToAssetCatalog(ar, pathToBuiltProject, proj);
		}

		foreach(ARReferenceObjectsSetAsset objSet in objectSets)
		{
			AddReferenceObjectsSetAssetToStreamingAssets(objSet, pathToBuiltProject);
		}

		//TODO: remove this when XCode actool is able to handles ARResources despite deployment target
		if (imageSets.Count > 0)
		{
			proj.SetBuildProperty(target, "IPHONEOS_DEPLOYMENT_TARGET", "11.3");
		}

		// Add or replace define for facetracking
		UpdateDefinesInFile(pathToBuiltProject + "/Classes/Preprocessor.h", new Dictionary<string, bool>() {
			{ "ARKIT_USES_FACETRACKING", ps.m_ARKitUsesFacetracking }
		});

		string[] filesToCopy = new string[]
		{
			
		};

		for(int i = 0 ; i < filesToCopy.Length ; ++i)
		{
			var srcPath = Path.Combine("../PluginSource/source", filesToCopy[i]);
			var dstLocalPath = "Libraries/" + filesToCopy[i];
			var dstPath = Path.Combine(pathToBuiltProject, dstLocalPath);
			File.Copy(srcPath, dstPath, true);
			proj.AddFileToBuild(target, proj.AddFile(dstLocalPath, dstLocalPath));
		}

		File.WriteAllText(projPath, proj.WriteToString());
#endif // #if UNITY_IOS
	}
}
