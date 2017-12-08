namespace Mapbox.Editor
{
	using UnityEditor;

	public class MapboxPackageProcessor : AssetPostprocessor
	{
		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			MapboxConfigurationWindow.ShowWindowOnImport();
		}

		//TODO: move all file handling in mapbox configuration window here
	}
}