using UnityEngine;
using UnityEditor;

namespace Lean.Touch
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(LeanSelectable))]
	public class LeanSelectable_Editor : Editor
	{
		// Draw the whole inspector
		public override void OnInspectorGUI()
		{
			Separator();

			BeginDisabled();
				DrawDefault("isSelected");
			EndDisabled();
			DrawDefault("HideWithFinger");
			
			Separator();

			DrawDefault("OnSelect");
			DrawDefault("OnSelectUp");
			DrawDefault("OnDeselect");

			Separator();
		}

		private void Separator()
		{
			EditorGUILayout.Separator();
		}

		private void DrawDefault(string name)
		{
			EditorGUI.BeginChangeCheck();
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty(name));
			}
			if (EditorGUI.EndChangeCheck() == true)
			{
				serializedObject.ApplyModifiedProperties();
			}
		}

		private void BeginDisabled()
		{
			EditorGUI.BeginDisabledGroup(true);
		}

		private void EndDisabled()
		{
			EditorGUI.EndDisabledGroup();
		}
	}
}