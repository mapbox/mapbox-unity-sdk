using UnityEditor;
using System;
using System.Linq;
using UnityEngine;

namespace ModuleMachine
{
	[CustomPropertyDrawer(typeof(AddComponentModifierType))]
	class AddComponentModifierDrawer : PropertyDrawer
	{
		string[] _injectorTypes;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, GUIContent.none, property);
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), GUIContent.none);
			var injectorRect = new Rect(position.x, position.y, position.width, position.height);

			var injectorProperty = property.FindPropertyRelative("_type");

			if (Application.isPlaying)
			{
				EditorGUI.LabelField(injectorRect, injectorProperty.stringValue);

				EditorGUI.EndProperty();
				return;
			}

			if (_injectorTypes == null)
			{
				_injectorTypes = TypesImplementingInterface(typeof(IModifierComponent));
			}


			int selectedIndex = GetSelectedIndex(_injectorTypes, injectorProperty);
			EditorGUI.BeginChangeCheck();
			selectedIndex = EditorGUI.Popup(injectorRect, selectedIndex, _injectorTypes);
			if (EditorGUI.EndChangeCheck())
			{
				injectorProperty.stringValue = _injectorTypes[selectedIndex];
			}

			EditorGUI.EndProperty();
		}

		int GetSelectedIndex(string[] injectors, SerializedProperty injectorProperty)
		{
			for (int i = 0, injectorStringsLength = injectors.Length; i < injectorStringsLength; i++)
			{
				var injector = injectors[i];
				if (injector == injectorProperty.stringValue)
				{
					return i;
				}
			}

			return 0;
		}

		public string[] TypesImplementingInterface(Type desiredType)
		{
			return AppDomain
				.CurrentDomain
				.GetAssemblies()
				.SelectMany(assembly => assembly.GetTypes())
				.Where(desiredType.IsAssignableFrom).ToList().ConvertAll(x => x.ToString()).ToArray();
		}
	}
}