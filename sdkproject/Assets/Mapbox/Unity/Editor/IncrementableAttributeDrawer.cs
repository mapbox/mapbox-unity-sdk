using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(IncrementableAttribute))]
public class IncrementableAttributeDrawer : PropertyDrawer
{

	private IncrementableAttribute _attributeValue = null;
	private IncrementableAttribute attributeValue
	{
		get
		{
			if (_attributeValue == null)
			{
				_attributeValue = (IncrementableAttribute)attribute;
			}
			return _attributeValue;
		}
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		SerializedProperty value = property;


		int incrementDirection = 0;

		int buttonWidth = 40;

		if (GUI.Button(new Rect(position.x, position.y, buttonWidth, position.height), ("-" + attributeValue.incrementBy)))
		{
			incrementDirection = -1;
		}

		if (GUI.Button(new Rect(position.width - buttonWidth, position.y, buttonWidth, position.height), ("+" + attributeValue.incrementBy)))
		{
			incrementDirection = 1;
		}

		string valueString = "";

		if (property.propertyType == SerializedPropertyType.Float)
		{
			property.floatValue += attributeValue.incrementBy * incrementDirection;
			valueString = property.floatValue.ToString();
		}
		else if (property.propertyType == SerializedPropertyType.Integer)
		{
			property.intValue += (int)attributeValue.incrementBy * incrementDirection;
			valueString = property.intValue.ToString();
		}

		EditorGUI.LabelField(new Rect(position.x + buttonWidth + 40, position.y, position.width - (buttonWidth * 2 + 80), position.height), property.name + ": " + valueString);
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return base.GetPropertyHeight(property, label);
	}
}