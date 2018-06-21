namespace Mapbox.Editor
{
	using UnityEngine;
	using System.Collections;
	using UnityEditor;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Modifiers;

	public class GameplaySectionDrawer
	{
		string objectId = "";

		bool showGameplay
		{
			get
			{
				return EditorPrefs.GetBool(objectId + "VectorSubLayerProperties_showGameplay");
			}
			set
			{
				EditorPrefs.SetBool(objectId + "VectorSubLayerProperties_showGameplay", value);
			}
		}

		public void DrawUI(SerializedProperty layerProperty, VectorPrimitiveType primitiveTypeProp)
		{
			showGameplay = EditorGUILayout.Foldout(showGameplay, "Gameplay");
			if (showGameplay)
			{
				if (primitiveTypeProp != VectorPrimitiveType.Point && primitiveTypeProp != VectorPrimitiveType.Custom)
				{
					EditorGUILayout.PropertyField(layerProperty.FindPropertyRelative("colliderOptions"));
				}

				else
				{
					layerProperty.FindPropertyRelative("honorBuildingIdSetting").boolValue = false;
				}
				DrawGoModifiers(layerProperty);
				EditorGUILayout.EndVertical();
			}
		}

		private void DrawGoModifiers(SerializedProperty property)
		{
			EditorGUILayout.BeginVertical();

			EditorGUILayout.LabelField(new GUIContent
			{
				text = "Game Object Modifiers",
				tooltip = "Modifiers that manipulate the GameObject after mesh generation."
			});
			var gofac = property.FindPropertyRelative("GoModifiers");
			for (int i = 0; i < gofac.arraySize; i++)
			{
				var ind = i;
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.BeginVertical();
				GUILayout.Space(5);
				gofac.GetArrayElementAtIndex(ind).objectReferenceValue =
					EditorGUILayout.ObjectField(gofac.GetArrayElementAtIndex(i).objectReferenceValue, typeof(GameObjectModifier),
						false) as ScriptableObject;
				EditorGUILayout.EndVertical();

				if (GUILayout.Button(new GUIContent("x"), GUILayout.Width(30)))
				{
					if (gofac.arraySize > 0)
					{
						gofac.DeleteArrayElementAtIndex(ind);
					}
					if (gofac.arraySize > 0)
					{
						gofac.DeleteArrayElementAtIndex(ind);
					}
				}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUI.indentLevel++;
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(EditorGUI.indentLevel * 12);
			Rect buttonRect = GUILayoutUtility.GetLastRect();

			if (GUILayout.Button(new GUIContent("Add New"), (GUIStyle)"minibuttonleft"))
			{
				PopupWindow.Show(buttonRect, new PopupSelectionMenu(typeof(GameObjectModifier), gofac));
				if (Event.current.type == EventType.Repaint) buttonRect = GUILayoutUtility.GetLastRect();
			}
			//EditorWindow.Repaint();
			//buttonRect = GUILayoutUtility.GetLastRect();
			if (GUILayout.Button(new GUIContent("Add Existing"), (GUIStyle)"minibuttonright"))
			{

				ScriptableCreatorWindow.Open(typeof(GameObjectModifier), gofac);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUI.indentLevel--;
			EditorGUILayout.EndVertical();
		}
	}
}
