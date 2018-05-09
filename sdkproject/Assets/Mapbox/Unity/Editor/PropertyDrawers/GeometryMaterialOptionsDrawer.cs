namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.VectorTile.ExtensionMethods;
	using System.IO;
	using System.Collections.Generic;

	public class StyleIconBundle
	{
		public string path;
		public Texture texture;

		public void Load()
		{
			if (texture == null)
			{
				texture = Resources.Load(path) as Texture;
			}
		}

		public StyleIconBundle(string styleName)
		{
			path = Path.Combine(Constants.Path.MAP_FEATURE_STYLES_SAMPLES, Path.Combine(styleName, string.Format("{0}Icon", styleName)));
		}
	}

	[CustomPropertyDrawer(typeof(GeometryMaterialOptions))]
	public class GeometryMaterialOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;

		private Dictionary<StyleTypes, StyleIconBundle> _styleIconBundles = new Dictionary<StyleTypes, StyleIconBundle>()
		{
			{StyleTypes.Simple, new StyleIconBundle(StyleTypes.Simple.ToString())},
			{StyleTypes.Realistic, new StyleIconBundle(StyleTypes.Realistic.ToString())},
			{StyleTypes.Fantasy, new StyleIconBundle(StyleTypes.Fantasy.ToString())},
			{StyleTypes.Light, new StyleIconBundle(StyleTypes.Light.ToString())},
			{StyleTypes.Dark, new StyleIconBundle(StyleTypes.Dark.ToString())},
			{StyleTypes.Satellite, new StyleIconBundle(StyleTypes.Satellite.ToString())},
		};

		/// <summary>
		/// Loads the default style icons.
		/// </summary>
		private void LoadDefaultStyleIcons()
		{
			foreach (var key in _styleIconBundles.Keys)
			{
				_styleIconBundles[key].Load();
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			LoadDefaultStyleIcons();
			EditorGUI.BeginProperty(position, label, property);
			EditorGUI.LabelField(new Rect(position.x, position.y, position.width, lineHeight), new GUIContent { text = "Material Options", tooltip = "Unity materials to be used for features. " });
			EditorGUI.indentLevel++;

			position.y += lineHeight;
			var styleTypePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Texturing Style", tooltip = "Texturing style for feature; choose from sample style or create your own by choosing Custom. " });
			var styleType = property.FindPropertyRelative("style");

			EditorGUI.indentLevel--;
			EditorGUI.indentLevel--;
			styleType.enumValueIndex = EditorGUI.Popup(styleTypePosition, styleType.enumValueIndex, styleType.enumDisplayNames);
			EditorGUI.indentLevel++;
			EditorGUI.indentLevel++;
			position.y += lineHeight;
			if ((StyleTypes)styleType.enumValueIndex != StyleTypes.Custom)
			{

				GUILayout.BeginHorizontal();

				if (_styleIconBundles.ContainsKey((StyleTypes)styleType.enumValueIndex))
				{
					Texture texture = _styleIconBundles[(StyleTypes)styleType.enumValueIndex].texture;
					GUI.DrawTexture(new Rect(52, position.y + 10, 60, 60), texture, ScaleMode.StretchToFill, true, 10.0F);
				}

				if (Constants.GUI.StyleLabels.labels.ContainsKey((StyleTypes)styleType.enumValueIndex))
				{
					string descriptionLabel = Constants.GUI.StyleLabels.labels[(StyleTypes)styleType.enumValueIndex];
					GUI.Label(new Rect(200, position.y + 10, 300, 60), descriptionLabel, Constants.GUI.Styles.EDITOR_TEXTURE_STYLE_DESCRIPTION_STYLE);
				}
				GUILayout.EndHorizontal();
			}
			else
			{
				var texturingType = property.FindPropertyRelative("texturingType");
				var texturingTypeGUI = new GUIContent { text = "Texturing Type", tooltip = EnumExtensions.Description((UvMapType)texturingType.enumValueIndex) };

				EditorGUI.indentLevel++;
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), texturingType, texturingTypeGUI);

				EditorGUI.indentLevel--;
				EditorGUI.indentLevel--;

				EditorGUI.indentLevel++;
				EditorGUI.indentLevel++;

				var matList = property.FindPropertyRelative("materials");
				if (matList.arraySize == 0)
				{
					matList.arraySize = 2;
				}

				var roofMat = matList.GetArrayElementAtIndex(0);
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), roofMat, new GUIContent { text = "Top Material", tooltip = "Unity material to use for extruded top/roof mesh. " });
				position.y += EditorGUI.GetPropertyHeight(roofMat);

				var wallMat = matList.GetArrayElementAtIndex(1);
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), wallMat, new GUIContent { text = "Side Material", tooltip = "Unity material to use for extruded side/wall mesh. " });
				position.y += EditorGUI.GetPropertyHeight(wallMat);

				if ((UvMapType)texturingType.enumValueIndex == UvMapType.Atlas)
				{
					position.y += lineHeight;
					var atlasInfo = property.FindPropertyRelative("atlasInfo");
					EditorGUI.ObjectField(new Rect(position.x, position.y, position.width, lineHeight), atlasInfo, new GUIContent { text = "Altas Info", tooltip = "Atlas information scriptable object, this defines how the texture roof and wall texture atlases will be used.  " });
				}
				if ((UvMapType)texturingType.enumValueIndex == UvMapType.AtlasWithColorPalette)
				{
					position.y += lineHeight;
					var atlasInfo = property.FindPropertyRelative("atlasInfo");
					EditorGUI.ObjectField(new Rect(position.x, position.y, position.width, lineHeight), atlasInfo, new GUIContent { text = "Altas Info", tooltip = "Atlas information scriptable object, this defines how the texture roof and wall texture atlases will be used.  " });
					position.y += lineHeight;
					var colorPalette = property.FindPropertyRelative("colorPalette");
					EditorGUI.ObjectField(new Rect(position.x, position.y, position.width, lineHeight), colorPalette, new GUIContent { text = "Color Palette", tooltip = "Color palette scriptable object, allows texture features to be procedurally colored at runtime. Requires materials that use the MapboxPerRenderer shader. " });
					position.y += lineHeight;

					EditorGUI.LabelField(new Rect(position.x, position.y, position.width, lineHeight * 2.0f), new GUIContent { text = "Note: Atlas With Color Palette requires materials that use the MapboxPerRenderer shader."}, Constants.GUI.Styles.EDITOR_NOTE_STYLE);

				}
			}
			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			float height = 0.0f;
			var style = property.FindPropertyRelative("style");
			if((StyleTypes)style.enumValueIndex == StyleTypes.Custom)
			{		
				height += (4.0f * lineHeight);
				var matList = property.FindPropertyRelative("materials");

				for (int i = 0; i < matList.arraySize; i++)
				{
					var matInList = matList.GetArrayElementAtIndex(i);
					height += EditorGUI.GetPropertyHeight(matInList);
				}
				var texturingType = property.FindPropertyRelative("texturingType");
				if ((UvMapType)texturingType.enumValueIndex == UvMapType.Atlas)
				{
					height += lineHeight;
				}
				else if ((UvMapType)texturingType.enumValueIndex == UvMapType.AtlasWithColorPalette)
				{
					height += (3.0f * lineHeight);
				}
			}
			else
			{
				height = (7.0f * lineHeight);;
			}
			return height;
		}
	}
}
