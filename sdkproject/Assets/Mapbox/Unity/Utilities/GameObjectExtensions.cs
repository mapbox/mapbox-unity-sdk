using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameObjectExtensions
{
	public static void Destroy(this Object obj, bool deleteAsset = false)
	{
		if (Application.isEditor && !Application.isPlaying)
		{
			Debug.Log("Destroy Immediate");
			GameObject.DestroyImmediate(obj, deleteAsset);
		}
		else
		{
			Debug.Log("Destroy Delayed");
			GameObject.Destroy(obj);
		}
	}
}
