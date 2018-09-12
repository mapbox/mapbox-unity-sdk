namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using System.Collections.Generic;
	using System;
	using System.Linq;
	using Mapbox.Unity.Map;

	public class ScriptableCreatorWindow : EditorWindow
	{
		Type _type;
		SerializedProperty _finalize;
		SerializedProperty _container;
		const float width = 620f;
		const float height = 600f;
		List<ScriptableObject> _assets;
		bool[] _showElement;
		Vector2 scrollPos;
		int _index = -1;
		private Action<UnityEngine.Object> _act;
		int activeIndex = 0;

		GUIStyle headerFoldout = new GUIStyle("Foldout");
		GUIStyle header;

		void OnEnable()
		{
			EditorApplication.playModeStateChanged += OnModeChanged;
		}

		void OnDisable()
		{
			EditorApplication.playModeStateChanged -= OnModeChanged;
		}

		void OnModeChanged(PlayModeStateChange state)
		{
			Close();
		}

		public static void Open(Type type, SerializedProperty p, int index = -1, Action<UnityEngine.Object> act = null, SerializedProperty containerProperty = null)
		{
			var window = GetWindow<ScriptableCreatorWindow>(true, "Select a module");
			window._type = type;
			window._finalize = p;
			window._container = containerProperty;
			window.position = new Rect(500, 200, width, height);
			window._act = act;
			if (index > -1)
			{
				window._index = index;
			}

			window.header = new GUIStyle("ShurikenModuleTitle")
			{
				font = (new GUIStyle("Label")).font,
				border = new RectOffset(15, 7, 4, 4),
				fixedHeight = 22,
				contentOffset = new Vector2(20f, -2f)
			};
		}

		void OnGUI()
		{
			if (_assets == null || _assets.Count == 0)
			{
				var list = AssetDatabase.FindAssets("t:" + _type.Name);
				_assets = new List<ScriptableObject>();
				foreach (var item in list)
				{
					var ne = AssetDatabase.GUIDToAssetPath(item);
					var asset = AssetDatabase.LoadAssetAtPath(ne, _type) as ScriptableObject;
					_assets.Add(asset);
				}
				_assets = _assets.OrderBy(x => x.GetType().Name).ThenBy(x => x.name).ToList();
			}

			var st = new GUIStyle();
			st.padding = new RectOffset(15, 15, 15, 15);
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos, st);
			for (int i = 0; i < _assets.Count; i++)
			{
				var asset = _assets[i];
				if (asset == null) //yea turns out this can happen
					continue;
				GUILayout.BeginHorizontal();

				var b = Header(string.Format("{0,-40} - {1, -15}", asset.GetType().Name, asset.name), i == activeIndex);

				if (b)
					activeIndex = i;
				if (GUILayout.Button(new GUIContent("Select"), header, GUILayout.Width(80)))
				{
					if (_act != null)
					{
						_act(asset);
					}
					else
					{
						if (_index == -1)
						{
							_finalize.arraySize++;
							_finalize.GetArrayElementAtIndex(_finalize.arraySize - 1).objectReferenceValue = asset;
							_finalize.serializedObject.ApplyModifiedProperties();
						}
						else
						{
							_finalize.GetArrayElementAtIndex(_index).objectReferenceValue = asset;
							_finalize.serializedObject.ApplyModifiedProperties();
						}
					}

					MapboxDataProperty mapboxDataProperty = (MapboxDataProperty)EditorHelper.GetTargetObjectOfProperty(_container);
					if (mapboxDataProperty != null)
					{
						mapboxDataProperty.HasChanged = true;
					}

					this.Close();
				}

				GUILayout.EndHorizontal();
				if (b)
				{
					EditorGUILayout.Space();
					EditorGUI.indentLevel += 4;
					GUI.enabled = false;
					var ed = UnityEditor.Editor.CreateEditor(asset);
					ed.hideFlags = HideFlags.NotEditable;
					ed.OnInspectorGUI();
					GUI.enabled = true;
					EditorGUI.indentLevel -= 4;
					EditorGUILayout.Space();
				}
				EditorGUILayout.Space();
			}
			EditorGUILayout.EndScrollView();
		}

		public static T CreateAsset<T>() where T : ScriptableObject
		{
			T asset = ScriptableObject.CreateInstance<T>();

			string path = AssetDatabase.GetAssetPath(Selection.activeObject);
			if (path == "")
			{
				path = "Assets";
			}
			else if (System.IO.Path.GetExtension(path) != "")
			{
				path = path.Replace(System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
			}

			string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + Selection.activeObject.name + "_" + typeof(T).Name + ".asset");

			AssetDatabase.CreateAsset(asset, assetPathAndName);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = asset;

			return asset;
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