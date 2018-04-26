namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Utilities;
	using System.IO;
	using System.Collections;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Data;

	/// <summary>
	/// StyleIconBundle manages the loading of icon textures for MapFeatureStyleOptionsDrawer.
	/// </summary>
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
			path = Path.Combine(Constants.Path.MAP_FEATURE_STYLES_STYLES_SAMPLES, Path.Combine(styleName, string.Format("{0}Icon", styleName)));//styleName + "Icon"));
		}
	}

	/// <summary>
	/// MapFeatureStyleOptionsDrawer controls the inspector visualization of MapFeatureStyleOptions and includes functionality for selecting styles and creating new styles.
	/// </summary>
	[CustomPropertyDrawer(typeof(MapFeatureStyleOptions))]
	public class MapFeatureStyleOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;

		private bool _createNewStyle;
		private bool _generateDefaultAssets;
		private string _newStyleName;

		private Dictionary<StyleTypes, StyleIconBundle> _styleIconBundles = new Dictionary<StyleTypes, StyleIconBundle>()
		{
			{StyleTypes.Simple, new StyleIconBundle(StyleTypes.Simple.ToString())},
			{StyleTypes.Realistic, new StyleIconBundle(StyleTypes.Realistic.ToString())},
			{StyleTypes.Fantasy, new StyleIconBundle(StyleTypes.Fantasy.ToString())},
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

		/// <summary>
		/// Creates a new style.
		/// </summary>
		/// <returns>A new style.</returns>
		private ScriptableStyle CreateNewStyle()
		{

			ScriptableStyle newStyleAsset = ScriptableObject.CreateInstance<ScriptableStyle>() as ScriptableStyle;
			newStyleAsset.geometryMaterialOptions.texturingType = UvMapType.AtlasWithColorPalette;

			string newStylePath = MapboxPathUtilities.CreateFolder(Constants.Path.MAP_FEATURE_STYLES_STYLES_CUSTOM, _newStyleName);
			string newStyleName = string.Format("{0}Style.asset", _newStyleName); 

			AssetDatabase.CreateAsset(newStyleAsset, AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(newStylePath, newStyleName)));

			string newStyleAssetsPath = MapboxPathUtilities.CreateFolder(newStylePath, Constants.Path.MAPBOX_STYLES_ASSETS_FOLDER);

			Shader shader = Shader.Find("Standard");
			Material defaultTopMaterial = new Material(shader);
			Material defaultSideMaterial = new Material(shader);

			string defaultTexturePath = Path.Combine(Constants.Path.MAP_FEATURE_STYLES_DEFAULT_STYLE_ASSETS, Constants.Path.MAPBOX_STYLES_TEXTURES_FOLDER);

			string defaultTopTexturePath = Path.Combine(defaultTexturePath, Constants.StyleAssetNames.DEFAULT_TOP_TEXTURE_NAME);
			string defaultSideTexturePath = Path.Combine(defaultTexturePath, Constants.StyleAssetNames.DEFAULT_SIDE_TEXTURE_NAME);

			Texture defaultTopTexture = Resources.Load(defaultTopTexturePath, typeof(Texture)) as Texture;
			Texture defaultSideTexture = Resources.Load(defaultSideTexturePath, typeof(Texture)) as Texture;

			defaultTopMaterial.mainTexture = defaultTopTexture;
			defaultSideMaterial.mainTexture = defaultSideTexture;

			newStyleAsset.geometryMaterialOptions.materials[0].Materials[0] = defaultTopMaterial;
			newStyleAsset.geometryMaterialOptions.materials[1].Materials[0] = defaultSideMaterial;


			string atlasFolderPath = MapboxPathUtilities.CreateFolder(newStyleAssetsPath, Constants.Path.MAPBOX_STYLES_ATLAS_FOLDER);
			string materialFolderPath = MapboxPathUtilities.CreateFolder(newStyleAssetsPath, Constants.Path.MAPBOX_STYLES_MATERIAL_FOLDER);
			string paletteFolderPath = MapboxPathUtilities.CreateFolder(newStyleAssetsPath, Constants.Path.MAPBOX_STYLES_PALETTES_FOLDER);

			MapboxPathUtilities.CreateFolder(newStyleAssetsPath, Constants.Path.MAPBOX_STYLES_TEXTURES_FOLDER);
			
			string defaultAtlasInfoFolderPath = Path.Combine(Constants.Path.MAP_FEATURE_STYLES_DEFAULT_STYLE_ASSETS, Constants.Path.MAPBOX_STYLES_ATLAS_FOLDER);
			string defaultAtlasInfoPath = Path.Combine(defaultAtlasInfoFolderPath, "DefaultAtlasInfo");

			AtlasInfo defaultAtlasInfoReferece = Resources.Load(defaultAtlasInfoPath, typeof(AtlasInfo)) as AtlasInfo;
			AtlasInfo defaultAtlasInfo = Object.Instantiate(defaultAtlasInfoReferece) as AtlasInfo;

			var paletteTemplate = ScriptableObject.CreateInstance<ScriptablePalette>();

			newStyleAsset.geometryMaterialOptions.atlasInfo = defaultAtlasInfo;
			newStyleAsset.geometryMaterialOptions.colorPalette = paletteTemplate;

			string topMaterialName = string.Format("{0}{1}.asset", _newStyleName, Constants.StyleAssetNames.TOP_MATERIAL_SUFFIX);
			string topMaterialPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(materialFolderPath, topMaterialName));

			string sideMaterialName = string.Format("{0}{1}.asset", _newStyleName, Constants.StyleAssetNames.SIDE_MATERIAL_SUFFIX);
			string sideMaterialPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(materialFolderPath, sideMaterialName));

			string atlasInfoName = string.Format("{0}AtlasInfo.asset", _newStyleName);
			string atlasInfoPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(atlasFolderPath, atlasInfoName));

			string colorPaletteName = string.Format("{0}Palette.asset", _newStyleName);
			string colorPalettePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(paletteFolderPath, colorPaletteName));

			AssetDatabase.CreateAsset(defaultTopMaterial, topMaterialPath);
			AssetDatabase.CreateAsset(defaultSideMaterial, sideMaterialPath);

			AssetDatabase.CreateAsset(defaultAtlasInfo, atlasInfoPath);
			AssetDatabase.CreateAsset(paletteTemplate, colorPalettePath);

			EditorUtility.SetDirty(newStyleAsset);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			_createNewStyle = false;
			return newStyleAsset;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			LoadDefaultStyleIcons();
			EditorGUI.BeginProperty(position, label, property);
			EditorGUI.indentLevel = 1;

			var typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Style Type", tooltip = "Use image texture from the Imagery source as texture for roofs. " });
			var selectedStyle = property.FindPropertyRelative("style");
			var scriptableStyle = property.FindPropertyRelative("scriptableStyle");

			EditorGUI.indentLevel--;
			selectedStyle.enumValueIndex = EditorGUI.Popup(typePosition, selectedStyle.enumValueIndex, selectedStyle.enumDisplayNames);
			EditorGUI.indentLevel++;

			EditorGUI.indentLevel++;

			position.y += lineHeight;

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			StyleTypes styleType = (StyleTypes)selectedStyle.enumValueIndex;
			if(_styleIconBundles.ContainsKey(styleType))
			{
				Texture texture = _styleIconBundles[styleType].texture;
				GUI.DrawTexture(new Rect(50, position.y + 10, 60, 60), texture, ScaleMode.StretchToFill, true, 10.0F);
			}

			if(Constants.StyleLabels.labels.ContainsKey(styleType))
			{
				GUILayout.FlexibleSpace();
				GUIStyle descriptionLabelStyle = new GUIStyle();
				descriptionLabelStyle.wordWrap = true;
				float txtColor = 0.7f;
				descriptionLabelStyle.normal.textColor = new Color(txtColor, txtColor, txtColor);
				string descriptionLabel = Constants.StyleLabels.labels[styleType];
				GUI.Label(new Rect(160, position.y + 10, 300, 60), descriptionLabel, descriptionLabelStyle);
				GUILayout.FlexibleSpace();
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			if (styleType == StyleTypes.Custom)
			{
				EditorGUI.ObjectField(new Rect(position.x, position.y, position.width, lineHeight), scriptableStyle, new GUIContent { text = "Custom style:", tooltip = "Assign a custom style to this map feature, or create a new style by clicking the button below." });

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Create New Style", GUILayout.Width(380)))
				{
					_createNewStyle = true;
				}

				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				if (_createNewStyle)
				{
					position.y += lineHeight;
					_newStyleName = EditorGUILayout.TextField("New Style Name: ", _newStyleName);

					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Create", GUILayout.Width(190)))
					{
						if (string.IsNullOrEmpty(_newStyleName))
						{
							Debug.LogError("New style name is empty");
						}
						else
						{
							scriptableStyle.objectReferenceValue = CreateNewStyle();
						}
					}
					if (GUILayout.Button("Cancel", GUILayout.Width(190)))
					{
						_createNewStyle = false;
					}
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
				}
			}
			else
			{
				_createNewStyle = false;
			}
			EditorGUI.indentLevel = 0;
			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var selectedStyle = property.FindPropertyRelative("style");
			StyleTypes styleType = (StyleTypes)selectedStyle.enumValueIndex;
			float height = 100.0f;
			if(styleType == StyleTypes.Custom)
			{
				height = (_createNewStyle) ? 50.0f : 40.0f;
			}
			return height;
		}
	}
}


