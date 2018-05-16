using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(PopUpAttribute))]
public class PopUpAttributeDrawer : PropertyDrawer
{

	private PopUpAttribute _attributeValue = null;
	private PopUpAttribute attributeValue
	{
		get
		{
			if (_attributeValue == null)
			{
				_attributeValue = (PopUpAttribute)attribute;
			}
			return _attributeValue;
		}
	}

	Dictionary<string, string[]> lookup = new Dictionary<string, string[]>()
	{
		{"colors", new string[]{ "red", "orange", "pink", "blue", "green", "turqoise"}},
		{"names", new string[]{ "bob", "shelly", "simon", "jane", "mario", "holly" }},
	};


	class UserDataBundle
	{
		public SerializedProperty property;
		public string val; 

		public UserDataBundle(SerializedProperty p, string v)
		{
			property = p;
			val = v;
		}
	}


	private string storedName;

	public void Callback(object obj)
	{
		UserDataBundle userDataBundle = obj as UserDataBundle;
		if(userDataBundle == null)
		{
			return;
		}
		Debug.Log("Property: " + userDataBundle.property);
		Debug.Log("Selected: " + userDataBundle.val);

		storedName = userDataBundle.val;
	}

	public void Custom()
	{
		Debug.Log("Custom");
	}

	Rect buttonRect;

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		
			//GUILayout.Label("Editor window with Popup example", EditorStyles.boldLabel);
			//if (GUILayout.Button("Popup Options", GUILayout.Width(200)))
			//{
			//	PopupWindow.Show(buttonRect, new PopupExample());
			//}
			//if (Event.current.type == EventType.Repaint) buttonRect = GUILayoutUtility.GetLastRect();
		


		SerializedProperty value = property;

		int buttonWidth = 160;

		if (GUI.Button(new Rect(position.x, position.y, buttonWidth, position.height), (property.name)))
		{
			string[] names = lookup[property.name];
			//PopupWindow.Show(buttonRect, new PopupExample(names));

			/*
			if(!lookup.ContainsKey(property.name))
			{
				return;
			}
			GenericMenu menu = new GenericMenu();
			string[] names = lookup[property.name];
			for (int i = 0; i < names.Length; i++)
			{
				UserDataBundle userDataBundle = new UserDataBundle(property, names[i]);

				menu.AddItem(new GUIContent(names[i]), true, Callback, userDataBundle);
			}
			menu.AddItem(new GUIContent("Custom/AddNew"), false, Custom);
			menu.ShowAsContext();

		}
		if(string.IsNullOrEmpty(storedName))
		{
			return;
		}
		position.y += 20;
		EditorGUI.LabelField(new Rect(position.x + buttonWidth + 40, position.y, position.width - (buttonWidth * 2 + 80), 20), storedName);
*/
		}
		if (Event.current.type == EventType.Repaint) buttonRect = GUILayoutUtility.GetLastRect();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		float height = base.GetPropertyHeight(property, label);
		if(!string.IsNullOrEmpty(storedName))
		{
			height += 20;
		}
		return height;
	}
}