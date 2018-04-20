using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//[ExecuteInEditMode]
public class CameraSnap : MonoBehaviour 
{
	public void Snap()
	{
		
	}

	public void Update()
	{
		var view = SceneView.currentDrawingSceneView;
		if (view != null)
		{
			Debug.Log("snap");
			Camera.main.transform.position = view.pivot;
			Camera.main.transform.rotation = view.rotation;
		}
	}
}