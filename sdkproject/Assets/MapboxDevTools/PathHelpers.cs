namespace Mapbox.Unity.Utilities.DebugTools
{
	using UnityEngine;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	public static class PathHelpers
	{
		static readonly string kScenesPath = Path.Combine(Application.dataPath, "Mapbox/Examples");

		public static List<string> AllScenes
		{
			get
			{
				List<FileInfo> files = DirSearch(new DirectoryInfo(kScenesPath), "*.unity");
				List<string> assetRefs = new List<string>();
				foreach (var fi in files)
				{
					if (fi.Name.StartsWith(".", System.StringComparison.Ordinal))
					{
						continue;
					}
					assetRefs.Add(GetRelativeAssetPathFromFullPath(fi.FullName));
				}
				return assetRefs;
			}
		}

		static List<FileInfo> DirSearch(DirectoryInfo d, string searchFor)
		{
			List<FileInfo> founditems = d.GetFiles(searchFor).ToList();

			DirectoryInfo[] dis = d.GetDirectories();
			foreach (DirectoryInfo di in dis)
			{
				founditems.AddRange(DirSearch(di, searchFor));
			}

			return founditems;
		}
		static string GetRelativeAssetPathFromFullPath(string fullPath)
		{
			fullPath = CleanPathSeparators(fullPath);
			if (fullPath.Contains(Application.dataPath))
			{
				return fullPath.Replace(Application.dataPath, "Assets");
			}
			Debug.LogWarning("Path does not point to a location within Assets: " + fullPath);
			return null;
		}

		static string CleanPathSeparators(string s)
		{
			const string forwardSlash = "/";
			const string backSlash = "\\";
			return s.Replace(backSlash, forwardSlash);
		}
	}
}