using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class ARPrerequisiteInfo : MonoBehaviour
{
}

#if UNITY_EDITOR
[CustomEditor(typeof(ARPrerequisiteInfo))]
public class ARPrerequisiteInfoEditor : Editor
{
	public override void OnInspectorGUI()
	{
		//serializedObject.Update();

		EditorGUILayout.HelpBox(" For AR examples to work as expected, please add layers with the following names \n ARGameObject \n Map \n Path \n Both", MessageType.Warning);

		//serializedObject.ApplyModifiedProperties();
	}
}
#endif

