using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(CameraSnap))]
public class CameraSnapEditor : Editor
{
	override public void OnInspectorGUI()
	{
		CameraSnap cs = (CameraSnap)target;

		if (GUILayout.Button("Snap"))
		{
			cs.Snap();
		}

	}
}
