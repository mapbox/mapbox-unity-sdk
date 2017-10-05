namespace Mapbox.Editor.Build
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;
	using System.Text;
	using UnityEditor.Build;

	/// <summary>
	/// Simple pre-build script to check for duplicate Android libraries
	/// </summary>
	public class PreBuildChecksEditor : IPreprocessBuild
	{
		public int callbackOrder { get { return 0; } }
		public void OnPreprocessBuild(BuildTarget target, string path)
		{

			if (BuildTarget.Android != target)
			{
				return;
			}

			Debug.Log("Mapbox prebuild checks for target '" + target);

			List<AndroidLibInfo> libInfo = new List<AndroidLibInfo>();
			foreach (var file in Directory.GetFiles(Application.dataPath, "*.jar", SearchOption.AllDirectories))
			{
				try
				{
					libInfo.Add(new AndroidLibInfo(file));
				}
				catch
				{
					Debug.LogWarningFormat("could not extract version from file name: [{0}]", file);
				}
			}
			foreach (var file in Directory.GetFiles(Application.dataPath, "*.aar", SearchOption.AllDirectories))
			{
				try
				{
					libInfo.Add(new AndroidLibInfo(file));
				}
				catch
				{
					Debug.LogWarningFormat("could not extract version from file name: [{0}]", file);
				}
			}

			var stats = libInfo.GroupBy(li => li.BaseFileName).OrderBy(g => g.Key);

			StringBuilder sb = new StringBuilder();
			foreach (var s in stats)
			{
				if (s.Count() > 1)
				{
					sb.AppendLine(string.Format(
						"{0}:{1}{2}"
						, s.Key
						, Environment.NewLine
						, string.Join(Environment.NewLine, s.Select(li => "\t" + li.AssetPath).ToArray())
					));
				}
			}
			if (sb.Length > 0)
			{
				Debug.LogErrorFormat("DUPLICATE ANDROID PLUGINS FOUND - BUILD WILL MOST LIKELY FAIL!!!{0}Resolve to continue.{0}{1}", Environment.NewLine, sb);
			}
		}
	}

	public class AndroidLibInfo
	{
		public AndroidLibInfo(string fullPath)
		{
			FullPath = fullPath;
			FullFileName = Path.GetFileName(fullPath);
			// TODO: find a better way to extract base file name
			// Mapbox telemetry lib uses different naming that other android libs
			// <name>-<major>.<minor>.<patch> vs. <name>-<major>-<minor>-<patch>
			// okio-1.13.0, support-v4-25.1.0 vs. mapbox-android-telemetry-2-1-0
			BaseFileName = FullFileName.Substring(0, FullFileName.LastIndexOf("-"));
			AssetPath = fullPath.Replace(Application.dataPath.Replace("Assets", ""), "");
		}

		public string FullPath { get; private set; }
		public string FullFileName { get; private set; }
		public string BaseFileName { get; private set; }
		public string AssetPath { get; private set; }
	}
}