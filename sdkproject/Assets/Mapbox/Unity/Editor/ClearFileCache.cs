namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;

	[InitializeOnLoad]
	public class ClearFileCache : MonoBehaviour
	{


		[MenuItem("Mapbox/Clear File Cache")]
		public static void ClearAllCachFiles()
		{
			Unity.MapboxAccess.Instance.ClearAndReinitCacheFiles();
		}

		[MenuItem("Mapbox/Show Cache Folder")]
		public static void ShowCacheFolder()
		{
			EditorUtility.RevealInFinder(Application.persistentDataPath);
		}
	}
}
