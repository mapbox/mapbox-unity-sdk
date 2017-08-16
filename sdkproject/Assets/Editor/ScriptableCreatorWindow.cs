using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Mapbox.Geocoding;
using Mapbox.Unity;
using System;
using System.Linq;

namespace Mapbox.NodeEditor
{
	public class ScriptableCreatorWindow : EditorWindow
	{
		Type _type;
		SerializedProperty _finalize;
		const float width = 620f;
		const float height = 600f;
		string[] _assets;
		bool[] _showElement;
		Vector2 scrollPos;
		int _index = -1;
		private Action<UnityEngine.Object> _act;

		GUIStyle headerFoldout = new GUIStyle("Foldout");
		GUIStyle header = new GUIStyle("ShurikenModuleTitle")
		{
			font = (new GUIStyle("Label")).font,
			border = new RectOffset(15, 7, 4, 4),
			fixedHeight = 22,
			contentOffset = new Vector2(20f, -2f)
		};

		void OnEnable()
		{
			EditorApplication.playmodeStateChanged += OnModeChanged;

		}

		void OnDisable()
		{
			EditorApplication.playmodeStateChanged -= OnModeChanged;
		}

		void OnModeChanged()
		{
			Close();
		}

		public static void Open(Type type, SerializedProperty p, int index = -1, Action<UnityEngine.Object> act = null)
		{
			var window = GetWindow<ScriptableCreatorWindow>(true, "Select a module");
			window._type = type;
			window._finalize = p;
			window.position = new Rect(500, 200, width, height);
			window._assets = AssetDatabase.FindAssets("t:" + type.Name);
			window._showElement = new bool[window._assets.Count()];
			window._act = act;
			if (index > -1)
			{
				window._index = index;
			}
		}
		
		void OnGUI()
		{
			var st = new GUIStyle();
			st.padding = new RectOffset(15, 15, 15, 15);
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos, st);
			for (int i = 0; i < _assets.Length; i++)
			{
				var ne = AssetDatabase.GUIDToAssetPath(_assets[i]);
				var asset = AssetDatabase.LoadAssetAtPath(ne, _type);
				GUILayout.BeginHorizontal();

				_showElement[i] = Header(asset.name + " (" + asset.GetType().Name + ")", _showElement[i]);

				//_showElement[i] = EditorGUILayout.Foldout(_showElement[i], new GUIContent(asset.name));
				if (GUILayout.Button(new GUIContent("Select"), header, GUILayout.Width(80)))
				{
					if(_act != null)
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
					
					this.Close();
				}

				GUILayout.EndHorizontal();
				if (_showElement[i])
				{
					EditorGUILayout.Space();
					EditorGUI.indentLevel += 2;
					GUI.enabled = false;
					var ed = UnityEditor.Editor.CreateEditor(asset);
					ed.hideFlags = HideFlags.NotEditable;
					ed.OnInspectorGUI();
					GUI.enabled = true;
					EditorGUI.indentLevel -= 2;
					EditorGUILayout.Space();
				}
				EditorGUILayout.Space();
			}
			EditorGUILayout.EndScrollView();

			if (GUILayout.Button(new GUIContent("Create New Factory")))
			{
				//var fac = CreateInstance<Unity.MeshGeneration.Factories.TerrainFactory>();
				var fac = CreateAsset<Unity.MeshGeneration.Factories.TerrainFactory>();
				_finalize.arraySize++;
				_finalize.GetArrayElementAtIndex(_finalize.arraySize - 1).objectReferenceValue = fac;
				_finalize.serializedObject.ApplyModifiedProperties();
				this.Close();
			}
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