using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;

public class PopupExample : PopupWindowContent
{

	Type _type;

	private Action<UnityEngine.Object> _act;

	List<ScriptableObject> _assets;

	SerializedProperty _finalize;

	int _index = -1;

	string MyNewModifierName;

	List<string> names;

	Vector2 scrollPos;

	public override Vector2 GetWindowSize()
	{
		//int count = _assets.Count;
		//float h = count * 50;
		return new Vector2(200, 250);
	}

	Rect buttonRect;

	public override void OnGUI(Rect rect)
	{
		GUILayout.Label(String.Format("{0}s", _type.Name), EditorStyles.boldLabel);

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
			if (GUILayout.Button(asset.name))
			{
				Debug.Log(asset.name);

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

				//GUILayout.BeginHorizontal();

				//var b = Header(string.Format("{0,-40} - {1, -15}", asset.GetType().Name, asset.name), i == activeIndex);

				//if (b)
				//	activeIndex = i;
				//if (GUILayout.Button(new GUIContent("Select"), header, GUILayout.Width(80)))
				//{

			}
		}
		if (GUILayout.Button("custom >"))
		{
			Rect newRect = GUILayoutUtility.GetLastRect();//new Rect(position.x + 50, 200, 200, 200);
			newRect.x += GetWindowSize().x;
			PopupWindow.Show(newRect, new PopUpConfirmExample(this));
			Debug.Log("custom >");
		}
		EditorGUILayout.EndScrollView();
		//EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
		editorWindow.Repaint();






		/*
		 * 
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
		 */
	}

	public override void OnOpen()
	{
		Debug.Log("Popup opened: " + this);
	}

	public override void OnClose()
	{
		Debug.Log("Popup closed: " + this);
	}

	public void Close(string n)
	{
		names.Add(n);
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