namespace Mapbox.Editor
{
	using System.Collections;
	using UnityEditor;
	using UnityEngine;

	[InitializeOnLoad]
	public class ClearMbTilesCache : MonoBehaviour
	{


		[MenuItem("Mapbox/Clear Caches")]
		public static void ClearCachingFileSource()
		{
			Mapbox.Unity.MapboxAccess.Instance.ClearCache();
		}



	}
}