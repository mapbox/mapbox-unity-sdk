using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace Mapbox.Editor
{
	public class PopupSelectionMenu : PopupWindowContent
	{

		private Type _type;

		private Action<UnityEngine.Object> _act;

		private List<Type> _modTypes;

		private SerializedProperty _finalize;

		private int _index = -1;

		private Vector2 scrollPos;

		public override Vector2 GetWindowSize()
		{
			return new Vector2(250, 250);
		}

		public override void OnGUI(Rect rect)
		{
			if (_modTypes == null || _modTypes.Count == 0)
			{
				_modTypes = new List<Type>();

				AppDomain currentDomain = AppDomain.CurrentDomain;
				Assembly[] assemblies = currentDomain.GetAssemblies();
				for (int i = 0; i < assemblies.Length; i++)
				{
					Type[] types = assemblies[i].GetTypes();
					for (int j = 0; j < types.Length; j++)
					{
						if (types[j].IsSubclassOf(_type))
						{
							_modTypes.Add(types[j]);
						}
					}
				}
			}

			GUILayout.Label(String.Format("{0}s", _type.Name), EditorStyles.boldLabel);
			var st = new GUIStyle();
			st.padding = new RectOffset(0, 0, 15, 15);
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos, st);

			for (int i = 0; i < _modTypes.Count; i++)
			{
				Type asset = _modTypes[i];
				if (asset == null) //yea turns out this can happen
					continue;
				var style = GUI.skin.button;
				style.alignment = TextAnchor.MiddleLeft;
				string shortTypeName = GetShortTypeName(asset.ToString());
				if (GUILayout.Button(shortTypeName, style))
				{
					CreateNewModiferInstance(asset, shortTypeName);
					editorWindow.Close();
				}
			}
			EditorGUILayout.EndScrollView();
		}

		private string GetShortTypeName(string input)
		{
			int pos = input.LastIndexOf(".", StringComparison.CurrentCulture) + 1;
			return input.Substring(pos, input.Length - pos);
		}

		private void CreateNewModiferInstance(Type type, string name)
		{
			var modifierInstance = ScriptableObject.CreateInstance(type);

			string pathCandidate = string.Format("Assets/New{0}.asset", name);
			string uniquePath = AssetDatabase.GenerateUniqueAssetPath(pathCandidate);

			modifierInstance.name = name;

			AssetDatabase.CreateAsset(modifierInstance, uniquePath);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			AddNewInstanceToArray(modifierInstance);
		}

		public void AddNewInstanceToArray(object obj)
		{
			ScriptableObject asset = obj as ScriptableObject;
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
		}

		public PopupSelectionMenu(Type t, SerializedProperty p, int index = -1, Action<UnityEngine.Object> act = null)
		{
			_type = t;
			_finalize = p;
			_act = act;
			if (index > -1)
			{
				_index = index;
			}
		}
	}
}