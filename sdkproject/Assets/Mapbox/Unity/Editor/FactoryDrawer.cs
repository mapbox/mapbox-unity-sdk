//using UnityEngine;
//using System.Collections;
//using UnityEditor;
//using Mapbox.Unity.MeshGeneration.Factories;
//using System;
//using Mapbox.Unity.MeshGeneration;

//namespace Mapbox.Editor.NodeEditor
//{
//	[CustomPropertyDrawer(typeof(AssignmentTypeAttribute))]
//	public class TypeAttributeDrawer : PropertyDrawer
//	{
//		float y = 0;

//		public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
//		{
//			EditorGUI.BeginProperty(position, label, prop);
//			var att = attribute as AssignmentTypeAttribute;
//			//var list = prop.FindPropertyRelative("List");
//			y = position.y;
//			for (int i = 0; i < prop.arraySize; i++)
//			{
//				Rect textFieldPosition = position;
//				Rect nameRect = new Rect(position.x, y, position.width - 60, 20);
//				Rect buttonRect = new Rect(position.width - 40, y, 25, 20);

//				GUI.enabled = false;
//				prop.objectReferenceValue = EditorGUI.ObjectField(nameRect, new GUIContent("Script"), prop.objectReferenceValue, att.Type, false) as ScriptableObject;
//				GUI.enabled = true;

//				//DrawTextField(nameRect, list.GetArrayElementAtIndex(i), new GUIContent(att.Type.Name));
//				if (GUI.Button(buttonRect, new GUIContent("E")))
//				{
//					Mapbox.Editor.ScriptableCreatorWindow.Open(att.Type, prop);
//				}
//				buttonRect = new Rect(position.width - 15, y, 25, 20);
//				if (GUI.Button(buttonRect, new GUIContent("-")))
//				{
//					//prop.DeleteArrayElementAtIndex(i);
//				}
//				y += 20;
//			}

//			Rect buttonRect2 = new Rect(position.x, y, position.width, 20);
//			if (GUI.Button(buttonRect2, new GUIContent("Add New")))
//			{
//				Mapbox.Editor.ScriptableCreatorWindow.Open(att.Type, prop);
//			}
//			EditorGUI.EndProperty();
//		}

//		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//		{
//			return base.GetPropertyHeight(property, label);// + property.FindPropertyRelative("List").arraySize * 20 + 10;
//		}

//		void DrawTextField(Rect position, SerializedProperty prop, GUIContent label)
//		{
//			if (prop.objectReferenceValue != null)
//			{
//				EditorGUI.BeginChangeCheck();
//				string value = EditorGUI.TextField(position, label, prop.objectReferenceValue.name + " (" + prop.objectReferenceValue.GetType().Name + ")");
//				if (EditorGUI.EndChangeCheck())
//					prop.stringValue = value;
//			}
//			else
//			{
//				EditorGUI.BeginChangeCheck();
//				string value = EditorGUI.TextField(position, label, "Not set");
//				if (EditorGUI.EndChangeCheck())
//					prop.stringValue = value;
//			}
//		}
//	}

//	public class TypeRAttribute : PropertyAttribute
//	{
//		Type type;

//		public TypeRAttribute(Type t)
//		{
//			this.type = t;
//		}
//	}
//}