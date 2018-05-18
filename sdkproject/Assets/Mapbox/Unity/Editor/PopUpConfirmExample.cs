using UnityEngine;
using UnityEditor;
using System;

public class PopUpConfirmExample : PopupWindowContent
{

	string MyNewModifierName;
	Type _type;

	PopupExample popup;

	public override Vector2 GetWindowSize()
	{
		return new Vector2(200, 100);
	}

	Rect buttonRect;

	public override void OnGUI(Rect rect)
	{
		GUILayout.Label("New thing:", EditorStyles.boldLabel);

		MyNewModifierName = EditorGUILayout.TextField("Name: ", MyNewModifierName);

		EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
		if (GUILayout.Button("Create"))
		{
			/*
			Debug.Log("Create");
			CreateClass.Create(MyNewModifierName, popup._type);

			if (popup != null)
			{
				popup.SetNewClass(MyNewModifierName);
			}
			editorWindow.Close();
			*/
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

	public PopUpConfirmExample(PopupExample p)
	{
		popup = p;
	}
}