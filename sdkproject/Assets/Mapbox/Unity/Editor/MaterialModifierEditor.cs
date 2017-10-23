namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Editor.NodeEditor;
	using Mapbox.Unity.MeshGeneration.Modifiers;

	[CustomEditor(typeof(MaterialModifier))]
	public class MaterialModifierEditor : UnityEditor.Editor
	{

		private MonoScript script;
		private MaterialModifier _modifier;
		private SerializedProperty _materials;
		private SerializedProperty _projectImagery;
		GUIStyle headerFoldout;
		GUIStyle header;
		bool[] _unfoldElements;
		private string entry;

		private void OnEnable()
		{
			script = MonoScript.FromScriptableObject((MaterialModifier)target);
			_materials = serializedObject.FindProperty("_materials");
			_projectImagery = serializedObject.FindProperty("_projectMapImagery");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			GUI.enabled = false;
			script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
			GUI.enabled = true;

			if (header == null)
			{
				headerFoldout = new GUIStyle("Foldout");
				_unfoldElements = new bool[_materials.arraySize];

				header = new GUIStyle("ShurikenModuleTitle")
				{
					font = (new GUIStyle("Label")).font,
					border = new RectOffset(15, 7, 4, 4),
					fixedHeight = 22,
					contentOffset = new Vector2(20f, -2f)
				};
			}

			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(_projectImagery, new GUIContent("Project Map Imagery"));
			EditorGUILayout.HelpBox("If checked, it'll use the map imagery (in Unity Tile saved by ImageFactory) on the first material. Sample usecase is to have satellite imagery on building roofs.", MessageType.Info);

			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Materials");
			EditorGUILayout.HelpBox("Material Modifier will select and apply one material from each stack below to the submesh with same index. Default demos have top polygon (i.e. roofs) as first submesh and side polygons (i.e. walls) as second submesh.", MessageType.Info);
			for (int i = 0; i < _materials.arraySize; i++)
			{
				GUILayout.BeginHorizontal();
				_unfoldElements[i] = Header("Submesh " + i + " Material", _unfoldElements[i]);

				if (GUILayout.Button(new GUIContent("Remove"), header, GUILayout.Width(80)))
				{
					_materials.DeleteArrayElementAtIndex(i);
					break;
				}
				GUILayout.EndHorizontal();

				if (_unfoldElements[i])
				{
					EditorGUILayout.PropertyField(_materials.GetArrayElementAtIndex(i).FindPropertyRelative("Materials.Array.size"));
					EditorGUI.indentLevel = 3;

					for (int j = 0; j < _materials.GetArrayElementAtIndex(i).FindPropertyRelative("Materials.Array.size").intValue; j++)
					{
						var prop = _materials.GetArrayElementAtIndex(i).FindPropertyRelative("Materials").GetArrayElementAtIndex(j);
						EditorGUILayout.PropertyField(prop);
					}

				}
				EditorGUILayout.Space();
			}

			if (GUILayout.Button(new GUIContent("Add New Empty"), (GUIStyle)"minibuttonleft"))
			{
				_materials.arraySize++;
				var test = (bool[])_unfoldElements.Clone();
				_unfoldElements = new bool[_materials.arraySize];
				for (int i = 0; i < test.Length; i++)
				{
					_unfoldElements[i] = test[i];
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		public bool Header(string title, bool show)
		{
			var rect = GUILayoutUtility.GetRect(16f, 22f, header);
			GUI.Box(rect, title, header);

			var foldoutRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
			var e = Event.current;

			if (e.type == EventType.Repaint)
				headerFoldout.Draw(foldoutRect, false, false, show, false);

			if (e.type == EventType.MouseDown)
			{
				if (rect.Contains(e.mousePosition))
				{
					show = !show;

					e.Use();
				}
			}

			return show;
		}
	}
}