using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameObjectExtensions
{
	public static void Destroy(this Object obj, bool deleteAsset = false)
	{
		if (Application.isEditor && !Application.isPlaying)
		{
			GameObject.DestroyImmediate(obj, deleteAsset);
		}
		else
		{
			GameObject.Destroy(obj);
		}
	}

	public static void DelayDestroy(this Object obj, bool deleteAsset = false)
	{
		if (Application.isEditor && !Application.isPlaying)
		{
			if (obj != null)
			{
				UnityEditor.EditorApplication.delayCall += () => GameObject.DestroyImmediate(obj, deleteAsset);
			}
		}
	}
}
