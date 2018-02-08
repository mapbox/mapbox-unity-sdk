namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Factories;

	[CustomPropertyDrawer(typeof(CameraBoundsTileProviderOptions))]
	public class CameraBoundsTileProviderOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			//EditorGUI.indentLevel++;
			foreach (var item in property)
			{
				var subproperty = item as SerializedProperty;
				position.height = lineHeight;
				position.y += lineHeight;
				EditorGUI.PropertyField(position, subproperty, true);
			}
			//EditorGUI.indentLevel--;
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			int rows = property.CountInProperty();
			return rows * lineHeight;
		}
	}

	[CustomPropertyDrawer(typeof(RangeTileProviderOptions))]
	public class RangeTileProviderOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			//EditorGUI.indentLevel++;
			foreach (var item in property)
			{
				var subproperty = item as SerializedProperty;
				position.height = lineHeight;
				position.y += lineHeight;
				EditorGUI.PropertyField(position, subproperty, true);
			}
			//EditorGUI.indentLevel--;
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			int rows = property.CountInProperty();
			return rows * lineHeight;
		}
	}

	[CustomPropertyDrawer(typeof(RangeAroundTransformTileProviderOptions))]
	public class RangeAroundTransformTileProviderOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			//EditorGUI.indentLevel++;
			foreach (var item in property)
			{
				var subproperty = item as SerializedProperty;
				position.height = lineHeight;
				position.y += lineHeight;
				EditorGUI.PropertyField(position, subproperty, true);
			}
			//EditorGUI.indentLevel--;
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			int rows = property.CountInProperty();
			return rows * lineHeight;
		}
	}

	[CustomPropertyDrawer(typeof(ImageryRasterOptions))]
	public class ImageryRasterOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;
			showPosition = EditorGUI.Foldout(position, showPosition, label.text);
			//EditorGUI.indentLevel++;
			if (showPosition)
			{
				foreach (var item in property)
				{
					var subproperty = item as SerializedProperty;
					position.height = lineHeight;
					position.y += lineHeight;
					EditorGUI.PropertyField(position, subproperty, true);
				}
			}
			//EditorGUI.indentLevel--;
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			int rows = (showPosition) ? property.CountInProperty() : 1;
			return rows * lineHeight;
		}
	}

	[CustomPropertyDrawer(typeof(LayerSourceOptions))]
	public class LayerSourceOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;
			showPosition = EditorGUI.Foldout(position, showPosition, label.text);

			EditorGUI.indentLevel++;

			if (showPosition)
			{
				position.y += lineHeight;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("isActive"), true);

				position.y += lineHeight;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("layerSource"), true);
			}

			EditorGUI.indentLevel--;

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = 0.0f;
			if (showPosition)
			{
				height += EditorGUIUtility.singleLineHeight;
				height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("layerSource"), false);
			}
			else
			{
				height = EditorGUIUtility.singleLineHeight;
			}

			return height;
		}
	}

	[CustomPropertyDrawer(typeof(LayerPerformanceOptions))]
	public class LayerPerformanceOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;

		SerializedProperty isActiveProperty;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			isActiveProperty = property.FindPropertyRelative("isEnabled");

			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;
			var typePosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Enable Coroutines"));
			isActiveProperty.boolValue = EditorGUI.Toggle(typePosition, isActiveProperty.boolValue);

			if (isActiveProperty.boolValue == true)
			{
				EditorGUI.indentLevel++;
				position.y += lineHeight;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("entityPerCoroutine"), true);
				EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = 0.0f;
			if (isActiveProperty.boolValue == true)
			{
				height += (2 * EditorGUIUtility.singleLineHeight);
				//height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("layerSource"), false);
			}
			else
			{
				height = EditorGUIUtility.singleLineHeight;
			}

			return height;
		}
	}
	[CustomPropertyDrawer(typeof(Style))]
	public class StyleOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			//position.y += lineHeight;
			position.height = lineHeight;
			showPosition = EditorGUI.Foldout(position, showPosition, "Source Details");
			if (showPosition)
			{
				//EditorGUI.indentLevel++;
				foreach (var item in property)
				{
					//Debug.Log("here");
					var subproperty = item as SerializedProperty;
					if (subproperty.name == "UserName" || subproperty.name == "Modified")
					{
						return;
					}
					position.y += lineHeight;
					//position.height = lineHeight;
					EditorGUI.PropertyField(position, subproperty, true);
				}
				//EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			int rows = (showPosition) ? 4 : 2;
			//Debug.Log("Height - " + rows * lineHeight);
			return rows * lineHeight;
		}
	}
}