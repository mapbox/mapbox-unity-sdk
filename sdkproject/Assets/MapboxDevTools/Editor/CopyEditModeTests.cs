using System;
using UnityEditor;
using UnityEngine;


namespace Mapbox.Tests
{

	[InitializeOnLoad]
	public static class CopyEditModeTests
	{

		[MenuItem("Mapbox/DevTools/Copy EditMode tests to PlayMode tests")]
		private static void CopyEditModeTestFiles()
		{

			// check if destination folder exists
			string destinationFolderName = "DoNotRenameOrRemove_MapboxPlayModeTests";
			string[] destinationFolderGuids = AssetDatabase.FindAssets(destinationFolderName);
			if (null == destinationFolderGuids || 0 == destinationFolderGuids.Length)
			{
				Debug.LogErrorFormat("destination folder not found: [{0}]", destinationFolderName);
				return;
			}
			if (destinationFolderGuids.Length > 1)
			{
				Debug.LogErrorFormat("several destination folders found: [{0}]", destinationFolderName);
				return;
			}
			string destinationFolderPath = AssetDatabase.GUIDToAssetPath(destinationFolderGuids[0]);
			Debug.LogFormat("destination folder: [{0}]", destinationFolderPath);


			// delete test files already existing in destintation folder
			string[] oldTestAssetGuids = AssetDatabase.FindAssets("Tests t:Script", new string[] { destinationFolderPath });
			if (null != oldTestAssetGuids && oldTestAssetGuids.Length > 0)
			{
				foreach (var oldTestAssetGuid in oldTestAssetGuids)
				{
					string oldTestAssetPath = AssetDatabase.GUIDToAssetPath(oldTestAssetGuid);
					Debug.LogFormat("deleting old test file: [{0}]", oldTestAssetPath);
					if (!FileUtil.DeleteFileOrDirectory(oldTestAssetPath))
					{
						Debug.LogErrorFormat("failed to delete: [{0}]", oldTestAssetPath);
					}
					// also delete .meta files to avoid warnings console
					string metaFile = oldTestAssetPath + ".meta";
					if (!FileUtil.DeleteFileOrDirectory(metaFile))
					{
						Debug.LogErrorFormat("failed to delete: [{0}]", metaFile);
					}
				}

				// force synchronous update, otherwise scripts get compiled asynchronously
				AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			}

			// copy test files according to naming convention
			string[] editModeTestAssetGuids = AssetDatabase.FindAssets("Tests t:Script");
			foreach (var testAssetGuid in editModeTestAssetGuids)
			{
				string testAssetSourcePath = AssetDatabase.GUIDToAssetPath(testAssetGuid);
				Debug.LogFormat("copying [{0}]", testAssetSourcePath);
				try
				{
					string fileName = System.IO.Path.GetFileName(testAssetSourcePath);
					FileUtil.CopyFileOrDirectory(testAssetSourcePath, destinationFolderPath + "/" + fileName);
				}
				catch (Exception ex)
				{
					Debug.LogErrorFormat("failed to copy [{0}]{1}{2}", testAssetSourcePath, Environment.NewLine, ex);
				}
			}

			AssetDatabase.Refresh();
		}
	}
}
