using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Mapbox.Examples;
[CustomEditor(typeof(HeroBuildingSelectionUserInput))]
public class HeroBuildingSelectionUserInputEditor : Editor 
{

	override public void OnInspectorGUI()
	{
		HeroBuildingSelectionUserInput t = (HeroBuildingSelectionUserInput)target;
		if (GUILayout.Button("Set Camera Position"))
		{
			t.BakeCameraTransform();
		}
		DrawDefaultInspector();
	}
}
