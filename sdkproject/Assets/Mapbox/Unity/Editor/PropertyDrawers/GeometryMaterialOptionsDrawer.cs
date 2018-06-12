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

			//position.y += lineHeight;
			var styleTypeLabel = new GUIContent { text = "Texturing Style", tooltip = "Texturing style for feature; choose from sample style or create your own by choosing Custom. " };
			var styleType = property.FindPropertyRelative("style");

			GUIContent[] styleTypeGuiContent = new GUIContent[styleType.enumDisplayNames.Length];
			for (int i = 0; i < styleType.enumDisplayNames.Length; i++)
			{
				styleTypeGuiContent[i] = new GUIContent
				{
					text = styleType.enumDisplayNames[i]
				};
			}

			styleType.enumValueIndex = EditorGUILayout.Popup(styleTypeLabel, styleType.enumValueIndex, styleTypeGuiContent);
			EditorGUI.indentLevel++;
			if ((StyleTypes)styleType.enumValueIndex != StyleTypes.Custom)
			{
				GUILayout.BeginHorizontal();

				Texture2D thumbnailTexture = (Texture2D)_styleIconBundles[(StyleTypes)styleType.enumValueIndex].texture;

				string descriptionLabel = EnumExtensions.Description((StyleTypes)styleType.enumValueIndex);
				EditorGUILayout.LabelField(new GUIContent(" ", thumbnailTexture), Constants.GUI.Styles.EDITOR_TEXTURE_THUMBNAIL_STYLE, GUILayout.Height(60), GUILayout.Width(EditorGUIUtility.labelWidth - 60));
				EditorGUILayout.TextArea(descriptionLabel, (GUIStyle)"wordWrappedLabel");

				GUILayout.EndHorizontal();
			}
			else
			{
				var texturingType = property.FindPropertyRelative("texturingType");

				int valIndex = texturingType.enumValueIndex == 0 ? 0 : texturingType.enumValueIndex + 1;
				var texturingTypeGUI = new GUIContent { text = "Texturing Type", tooltip = EnumExtensions.Description((UvMapType)valIndex) };

				EditorGUILayout.PropertyField(texturingType, texturingTypeGUI);

				var matList = property.FindPropertyRelative("materials");
				if (matList.arraySize == 0)
				{
					matList.arraySize = 2;
				}
				GUILayout.Space(-lineHeight);
				var roofMat = matList.GetArrayElementAtIndex(0);
				EditorGUILayout.PropertyField(roofMat, new GUIContent { text = "Top Material", tooltip = "Unity material to use for extruded top/roof mesh. " });

				GUILayout.Space(-lineHeight);
				var wallMat = matList.GetArrayElementAtIndex(1);
				EditorGUILayout.PropertyField(wallMat, new GUIContent { text = "Side Material", tooltip = "Unity material to use for extruded side/wall mesh. " });

				if ((UvMapType)texturingType.enumValueIndex + 1 == UvMapType.Atlas)
				{
					var atlasInfo = property.FindPropertyRelative("atlasInfo");
					EditorGUILayout.ObjectField(atlasInfo, new GUIContent { text = "Altas Info", tooltip = "Atlas information scriptable object, this defines how the texture roof and wall texture atlases will be used.  " });
				}
				if ((UvMapType)texturingType.enumValueIndex + 1 == UvMapType.AtlasWithColorPalette)
				{
					var atlasInfo = property.FindPropertyRelative("atlasInfo");
					EditorGUILayout.ObjectField(atlasInfo, new GUIContent { text = "Altas Info", tooltip = "Atlas information scriptable object, this defines how the texture roof and wall texture atlases will be used.  " });
					var colorPalette = property.FindPropertyRelative("colorPalette");
					EditorGUILayout.ObjectField(colorPalette, new GUIContent { text = "Color Palette", tooltip = "Color palette scriptable object, allows texture features to be procedurally colored at runtime. Requires materials that use the MapboxPerRenderer shader. " });

					EditorGUILayout.LabelField(new GUIContent { text = "Note: Atlas With Color Palette requires materials that use the MapboxPerRenderer shader." }, Constants.GUI.Styles.EDITOR_NOTE_STYLE);
				}
			}
			EditorGUI.indentLevel--;
			EditorGUI.EndProperty();
		}
	}
}
