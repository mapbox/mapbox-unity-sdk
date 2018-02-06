namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;

	[CustomPropertyDrawer(typeof(MapExtentOptions))]
	public class MapExtentOptionsDrawer : PropertyDrawer
	{
		static string extTypePropertyName = "extentType";
		//static string wName = "west";
		//static string eName = "east";
		//static string nName = "north";
		//static string sName = "south";

		//static string cameraName = "camera";
		//static string tgtTransName = "targetTransform";
		//static string visibleBufName = "visibleBuffer";
		//static string disposeBufName = "disposeBuffer";
		//static string updateIntName = "updateInterval";


		static float lineHeight = EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			position.height = lineHeight;

			// Draw label.
			var kindPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			var kindProperty = property.FindPropertyRelative(extTypePropertyName);


			//var wProp = property.FindPropertyRelative(wName);
			//var eProp = property.FindPropertyRelative(eName);
			//var nProp = property.FindPropertyRelative(nName);
			//var sProp = property.FindPropertyRelative(sName);
			//var cameraProp = property.FindPropertyRelative(cameraName);
			//var transformProp = property.FindPropertyRelative(tgtTransName);
			//var bufProp = property.FindPropertyRelative(visibleBufName);
			//var disposeProp = property.FindPropertyRelative(disposeBufName);
			//var updateProp = property.FindPropertyRelative(updateIntName);



			kindProperty.enumValueIndex = EditorGUI.Popup(kindPosition, kindProperty.enumValueIndex, kindProperty.enumDisplayNames);

			var kind = (MapExtentType)kindProperty.enumValueIndex;


			EditorGUI.indentLevel++;

			var rect = new Rect(position.x, position.y + lineHeight, position.width, lineHeight);

			switch (kind)
			{
				case MapExtentType.CameraBounds:
					EditorGUI.PropertyField(rect, property.FindPropertyRelative("cameraBoundsOptions"), new GUIContent { text = "CameraOptions-" });
					break;
				case MapExtentType.RangeAroundCenter:
					EditorGUI.PropertyField(rect, property.FindPropertyRelative("rangeAroundCenterOptions"), new GUIContent { text = "RangeAroundCenter" });
					//EditorGUI.PropertyField(rect, nProp);
					//rect.y += rect.height;
					//EditorGUI.PropertyField(rect, sProp);
					//rect.y += rect.height;
					//EditorGUI.PropertyField(rect, eProp);
					//rect.y += rect.height;
					//EditorGUI.PropertyField(rect, wProp);
					break;
				case MapExtentType.RangeAroundTransform:
					EditorGUI.PropertyField(rect, property.FindPropertyRelative("rangeAroundTransformOptions"), new GUIContent { text = "RangeAroundTransform" });
					break;
				default:
					break;
			}

			EditorGUI.indentLevel--;

			EditorGUI.EndProperty();


		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var kindProperty = property.FindPropertyRelative(extTypePropertyName);

			var kind = (MapExtentType)kindProperty.enumValueIndex;

			int rows = property.CountInProperty() + 1;

			//switch (kind)
			//{
			//	case LayerMatcher.Kind.None:
			//		// Nothing to do.
			//		break;
			//	case LayerMatcher.Kind.Property:
			//		rows += 1;
			//		break;
			//	case LayerMatcher.Kind.PropertyRange:
			//		rows += 3;
			//		break;
			//	case LayerMatcher.Kind.PropertyRegex:
			//		rows += 2;
			//		break;
			//	case LayerMatcher.Kind.PropertyValue:
			//		rows += 2;
			//		break;
			//}

			return rows * EditorGUIUtility.singleLineHeight;
		}

	}
}