using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;

public class PopupExample : PopupWindowContent
{

	bool _dropdown = false;

	public Type _type;

	private Action<UnityEngine.Object> _act;

	List<ScriptableObject> _assets;

	SerializedProperty _finalize;

	int _index = -1;

	Vector2 scrollPos;

	public override Vector2 GetWindowSize()
	{
		//int count = _assets.Count;
		//float h = count * 50;
		return new Vector2(250, 250);
	}

	Rect buttonRect;

	public override void OnGUI(Rect rect)
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

		if (_dropdown)
		{
			GenericMenu menu = new GenericMenu();
			for (int i = 0; i < _assets.Count; i++)
			{
				menu.AddItem(new GUIContent(_assets[i].name), false, Confirm, _assets[i]);
			}
			menu.ShowAsContext();
			return;
		}

		else
		{
			GUILayout.Label(String.Format("{0}s", _type.Name), EditorStyles.boldLabel);
			var st = new GUIStyle();
			st.padding = new RectOffset(0, 0, 15, 15);
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos, st);

			for (int i = 0; i < _assets.Count; i++)
			{
				var asset = _assets[i];
				if (asset == null) //yea turns out this can happen
					continue;
				var style = GUI.skin.button;
				style.alignment = TextAnchor.MiddleLeft;

				if (GUILayout.Button(asset.name, style))
				{
					Debug.Log(asset.name);
					Confirm(asset);
					/*
					if (_act != null)
					{
						Debug.Log("ACT!!");
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
					*/
				}
			}
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			if (GUILayout.Button("custom >"))
			{
				Rect newRect = GUILayoutUtility.GetLastRect();//new Rect(position.x + 50, 200, 200, 200);
				newRect.x += GetWindowSize().x;
				PopupWindow.Show(newRect, new PopUpConfirmExample(this));
				Debug.Log("custom >");
			}
			EditorGUILayout.EndScrollView();
		}
		/*
		if (GUILayout.Button("custom >"))
		{
			Rect newRect = GUILayoutUtility.GetLastRect();//new Rect(position.x + 50, 200, 200, 200);
			newRect.x += GetWindowSize().x;
			PopupWindow.Show(newRect, new PopUpConfirmExample(this));
			Debug.Log("custom >");
		}
		*/
		EditorGUILayout.LabelField("Compiling:", EditorApplication.isCompiling ? "Yes" : "No");


		//EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
		editorWindow.Repaint();
	}

	public void Confirm(object obj)
	{
		ScriptableObject asset = obj as ScriptableObject;
		if (_act != null)
		{
			Debug.Log("ACT!!");
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

	public override void OnOpen()
	{
		Debug.Log("Popup opened: " + this);
	}

	public override void OnClose()
	{
		Debug.Log("Popup closed: " + this);
	}

	public void SetNewClass(string n)
	{
		//selected.AddComponent(Type.GetType(name));
		Type type = Type.GetType(n);

		var myNewScriptabeObject = ScriptableObject.CreateInstance(n) as ScriptableObject;

		myNewScriptabeObject.name = n;

		AssetDatabase.CreateAsset(myNewScriptabeObject, "Assets/" + n);

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Confirm(myNewScriptabeObject);
		editorWindow.Close();
	}

	public PopupExample(Type type, SerializedProperty p, int index = -1, Action<UnityEngine.Object> act = null)
	{
		_type = type;
		_finalize = p;
		_act = act;
		if (index > -1)
		{
			_index = index;
		}
	}
}