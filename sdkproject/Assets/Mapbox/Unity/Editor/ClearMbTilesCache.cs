namespace Mapbox.Editor
{
	using System.Collections;
	using UnityEditor;
	using UnityEngine;

	[InitializeOnLoad]
	public class ClearMbTilesCache : MonoBehaviour
	{


		[MenuItem("Mapbox/Clear Caches/Current Scene")]
		public static void ClearCachingFileSource()
		{
			Mapbox.Unity.MapboxAccess.Instance.ClearSceneCache();
		}


		[MenuItem("Mapbox/Clear Caches/All")]
		public static void ClearAllCachFiles()
		{
			Mapbox.Unity.MapboxAccess.Instance.ClearAllCacheFiles();
		}


	}
}