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

		public StyleIconBundle(string styleName, string paletteName = "")
		{
			path = Path.Combine(Constants.Path.MAP_FEATURE_STYLES_SAMPLES, Path.Combine(styleName, string.Format("{0}Icon", (styleName + paletteName))));
		}
	}

	[CustomPropertyDrawer(typeof(GeometryMaterialOptions))]
	public class GeometryMaterialOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		private string objectId = "";
		bool showTexturing
		{
			get
			{
				return EditorPrefs.GetBool(objectId + "VectorSubLayerProperties_showTexturing");
			}
			set
			{
				EditorPrefs.SetBool(objectId + "VectorSubLayerProperties_showTexturing", value);
			}
		}

		private Dictionary<StyleTypes, StyleIconBundle> _styleIconBundles = new Dictionary<StyleTypes, StyleIconBundle>()
		{
			{StyleTypes.Simple, new StyleIconBundle(StyleTypes.Simple.ToString())},
			{StyleTypes.Realistic, new StyleIconBundle(StyleTypes.Realistic.ToString())},
			{StyleTypes.Fantasy, new StyleIconBundle(StyleTypes.Fantasy.ToString())},
			{StyleTypes.Light, new StyleIconBundle(StyleTypes.Light.ToString())},
			{StyleTypes.Dark, new StyleIconBundle(StyleTypes.Dark.ToString())},
			{StyleTypes.Color, new StyleIconBundle(StyleTypes.Color.ToString())},
			{StyleTypes.Satellite, new StyleIconBundle(StyleTypes.Satellite.ToString())},
		};

		private Dictionary<SamplePalettes, StyleIconBundle> _paletteIconBundles = new Dictionary<SamplePalettes, StyleIconBundle>()
		{
			{SamplePalettes.City, new StyleIconBundle(StyleTypes.Simple.ToString(), SamplePalettes.City.ToString())},
			{SamplePalettes.Cool, new StyleIconBundle(StyleTypes.Simple.ToString(), SamplePalettes.Cool.ToString())},
			{SamplePalettes.Rainbow, new StyleIconBundle(StyleTypes.Simple.ToString(), SamplePalettes.Rainbow.ToString())},
			{SamplePalettes.Urban, new StyleIconBundle(StyleTypes.Simple.ToString(), SamplePalettes.Urban.ToString())},
			{SamplePalettes.Warm, new StyleIconBundle(StyleTypes.Simple.ToString(), SamplePalettes.Warm.ToString())},
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
			foreach (var key in _paletteIconBundles.Keys)
			{
				_paletteIconBundles[key].Load();
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			objectId = property.serializedObject.targetObject.GetInstanceID().ToString();

			showTexturing = EditorGUILayout.Foldout(showTexturing, new GUIContent { text = "Texturing", tooltip = "Material options to texture the generated building geometry" });
			if (showTexturing)
			{
				LoadDefaultStyleIcons();
				EditorGUI.BeginProperty(position, label, property);

				var styleTypeLabel = new GUIContent { text = "Style Type", tooltip = "Texturing style for feature; choose from sample style or create your own by choosing Custom. " };
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

					var style = (StyleTypes)styleType.enumValueIndex;

					Texture2D thumbnailTexture = (Texture2D)_styleIconBundles[style].texture;

					if ((StyleTypes)styleType.enumValueIndex == StyleTypes.Simple)
					{
						var samplePaletteType = property.FindPropertyRelative("samplePalettes");
						var palette = (SamplePalettes)samplePaletteType.enumValueIndex;
						thumbnailTexture = (Texture2D)_paletteIconBundles[palette].texture;
					}

					string descriptionLabel = EnumExtensions.Description(style);
					EditorGUILayout.LabelField(new GUIContent(" ", thumbnailTexture), Constants.GUI.Styles.EDITOR_TEXTURE_THUMBNAIL_STYLE, GUILayout.Height(60), GUILayout.Width(EditorGUIUtility.labelWidth - 60));
					EditorGUILayout.TextArea(descriptionLabel, (GUIStyle)"wordWrappedLabel");

					GUILayout.EndHorizontal();

					switch ((StyleTypes)styleType.enumValueIndex)
					{
						case StyleTypes.Simple:
							var samplePaletteType = property.FindPropertyRelative("samplePalettes");
							var samplePaletteTypeLabel = new GUIContent { text = "Palette Type", tooltip = "Palette type for procedural colorization; choose from sample palettes or create your own by choosing Custom. " };

							GUIContent[] samplePaletteTypeGuiContent = new GUIContent[samplePaletteType.enumDisplayNames.Length];
							for (int i = 0; i < samplePaletteType.enumDisplayNames.Length; i++)
							{
								samplePaletteTypeGuiContent[i] = new GUIContent
								{
									text = samplePaletteType.enumDisplayNames[i]
								};
							}

							samplePaletteType.enumValueIndex = EditorGUILayout.Popup(samplePaletteTypeLabel, samplePaletteType.enumValueIndex, samplePaletteTypeGuiContent);
							break;
						case StyleTypes.Light:
							property.FindPropertyRelative("lightStyleOpacity").floatValue = EditorGUILayout.Slider("Opacity", property.FindPropertyRelative("lightStyleOpacity").floatValue, 0.0f, 1.0f);
							break;
						case StyleTypes.Dark:
							property.FindPropertyRelative("darkStyleOpacity").floatValue = EditorGUILayout.Slider("Opacity", property.FindPropertyRelative("darkStyleOpacity").floatValue, 0.0f, 1.0f);
							break;
						case StyleTypes.Color:
							property.FindPropertyRelative("colorStyleColor").colorValue = EditorGUILayout.ColorField("Color", property.FindPropertyRelative("colorStyleColor").colorValue);
							break;
						default:
							break;
					}
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
}