using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;

public class PopupExample : PopupWindowContent
{

	private Type _type;

	private Action<UnityEngine.Object> _act;

	private List<ScriptableObject> _assets;

	private SerializedProperty _finalize;

	private int _index = -1;

	private Vector2 scrollPos;

	public override Vector2 GetWindowSize()
	{
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
				}
			}
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			if (GUILayout.Button("custom >"))
			{
				Rect newRect = GUILayoutUtility.GetLastRect();
				newRect.x += GetWindowSize().x;
				PopupWindow.Show(newRect, new PopUpConfirmExample(this));
				Debug.Log("custom >");
			}
			EditorGUILayout.EndScrollView();
		}
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
		//Type type = Type.GetType(n);

		//var myNewScriptabeObject;
		//switch(_type.ToString())
		//{
		//	case("Mapbox.Unity.MeshGeneration.Modifiers.MeshModifier"):
		//		break;
		//	case("Mapbox.Unity.MeshGeneration.Modifiers.GameObjectModifier"):
		//		break;
		//}
		var myNewScriptabeObject = ScriptableObject.CreateInstance<Mapbox.Unity.MeshGeneration.Modifiers.GameObjectModifier>() as Mapbox.Unity.MeshGeneration.Modifiers.GameObjectModifier;
		//object foo = GetFoo();
		//Type t = typeof(_type.GetType());
		//string bar = (string)Convert.ChangeType(foo, t);

		//var myNewScriptabeObject = ScriptableObject.CreateInstance<_type.GetType()>() as _type;


		myNewScriptabeObject.name = n;

		AssetDatabase.CreateAsset(myNewScriptabeObject, "Assets/" + n + ".asset");

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Confirm(myNewScriptabeObject);
		editorWindow.Close();
	}

	public PopupExample(Type type, SerializedProperty p, int index = -1, Action<UnityEngine.Object> act = null)
	{
		_type = type;
		Debug.Log(_type.ToString());
		_finalize = p;
		_act = act;
		if (index > -1)
		{
			_index = index;
		}
	}
}