using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;

public class UnityARBuildPostprocessor
{
	// Build postprocessor. Currently only needed on:
	// - iOS: no dynamic libraries, so plugin source files have to be copied into Xcode project
	[PostProcessBuild]
	public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
	{
		if (target == BuildTarget.iOS)
			OnPostprocessBuildIOS(pathToBuiltProject);
	}

	private static UnityARKitPluginSettings LoadSettings()
	{
		UnityARKitPluginSettings loadedSettings = Resources.Load<UnityARKitPluginSettings> ("UnityARKitPlugin/ARKitSettings");
		if (loadedSettings == null) {
			loadedSettings = ScriptableObject.CreateInstance<UnityARKitPluginSettings> ();
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
			if (matchRegex.Match (lines [i]).Success) {
				lines [i] = replaceRegex.Replace (lines [i], "#define " + name + " " + newValue);
				replaced = true;
			}
		}
		return replaced;
	}

	internal static void AddOrReplaceCppMacro(ref string[] lines, string name, string newValue)
	{
		if (ReplaceCppMacro (lines, name, newValue) == false) {
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
		File.WriteAllText(plistPath, plist.WriteToString());

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
