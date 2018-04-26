using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Mapbox.Unity.Utilities
{
	public static class MapboxPathUtilities
	{
		public static string CreateFolder(string parentFolder, string newFolderName, bool saveAssets = false)
		{
			string guid = AssetDatabase.CreateFolder(parentFolder, newFolderName);
			string assetPath = AssetDatabase.GUIDToAssetPath(guid);
			return assetPath;
		}
	}
}

