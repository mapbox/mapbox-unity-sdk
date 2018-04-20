namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using System.Collections;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Data;

	public class StyleIconBundle
	{
		public string m_path;
		public Texture m_texture;
		private const string _assetPathPrefix = "StyleAssets/";

		public void Load()
		{
			if (m_texture == null)
			{
				m_texture = Resources.Load(m_path) as Texture;
			}
		}

		public StyleIconBundle(string p)
		{
			m_path = string.Format("{0}{1}/{2}", _assetPathPrefix, p, p);
		}
	}

	[CustomPropertyDrawer(typeof(MapFeatureStyleOptions))]
	public class MapFeatureStyleOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;

		private bool _createNewStyle;
		private bool _generateDefaultAssets;
		private string _newStyleName;

		private Dictionary<StyleTypes, StyleIconBundle> _stykeIconBundles = new Dictionary<StyleTypes, StyleIconBundle>()
		{
			{StyleTypes.Simple, new StyleIconBundle(StyleTypes.Simple.ToString())},
			{StyleTypes.Go, new StyleIconBundle(StyleTypes.Go.ToString())},
			{StyleTypes.Realistic, new StyleIconBundle(StyleTypes.Realistic.ToString())},
			{StyleTypes.Scifi, new StyleIconBundle(StyleTypes.Scifi.ToString())},
			{StyleTypes.Fantasy, new StyleIconBundle(StyleTypes.Fantasy.ToString())},
			{StyleTypes.Custom, new StyleIconBundle(StyleTypes.Custom.ToString())},
		};

		private void OnEnable()
		{
			foreach (var key in _stykeIconBundles.Keys)
			{
				_stykeIconBundles[key].Load();
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			OnEnable();
			EditorGUI.BeginProperty(position, label, property);
			//showPosition = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, lineHeight), showPosition, label.text);
			//EditorGUI.LabelField(new Rect(position.x, position.y, position.width, lineHeight), new GUIContent { text = "Style Options", tooltip = "Unity materials to be used for features. " });

			EditorGUI.indentLevel = 1;

			//StyleTypes m_styles;
			var typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Style Type", tooltip = "Use image texture from the Imagery source as texture for roofs. " });
			var selectedStyle = property.FindPropertyRelative("m_style");
			var styleToUse = property.FindPropertyRelative("m_scriptableStyle");

			//EditorGUI.indentLevel--;
			EditorGUI.indentLevel++;
			selectedStyle.enumValueIndex = EditorGUI.Popup(typePosition, selectedStyle.enumValueIndex, selectedStyle.enumDisplayNames);



			Texture texture = _stykeIconBundles[(StyleTypes)selectedStyle.enumValueIndex].m_texture;

			position.y += lineHeight;

			GUI.DrawTexture(new Rect(60, position.y, 60, 60), texture, ScaleMode.StretchToFill, true, 10.0F);

			if ((StyleTypes)selectedStyle.enumValueIndex == StyleTypes.Custom)
			{

				//var atlasInfo = property.FindPropertyRelative("atlasInfo");

				EditorGUI.ObjectField(new Rect(position.x, position.y, position.width, lineHeight), styleToUse, new GUIContent { text = "Style link", tooltip = "Atlas information scriptable object, this defines how the texture roof and wall texture atlases will be used.  " });
				//position.y += lineHeight;
				//styleToUse.objectReferenceValue = EditorGUILayout.ObjectField("Style Link:", styleToUse, typeof(ScriptableStyle), false) as ScriptableStyle;

				//EditorGUI.ObjectField("Style Link:", styleToUse, typeof(ScriptableStyle), false) as ScriptableStyle;
				if (GUILayout.Button("Create New Style"))
				{
					_createNewStyle = true;
				}
				if (_createNewStyle)
				{
					_newStyleName = EditorGUILayout.TextField("New Style Name: ", _newStyleName);
					_generateDefaultAssets = GUILayout.Toggle(_generateDefaultAssets, "Generate default assets");

					if (GUILayout.Button("Create"))
					{
						if (string.IsNullOrEmpty(_newStyleName))
						{
							Debug.LogError("New style name is empty");
						}
						else
						{
							ScriptableStyle asset = ScriptableObject.CreateInstance<ScriptableStyle>() as ScriptableStyle;

							asset.geometryMaterialOptions.texturingType = UvMapType.AtlasWithColorPalette;

							styleToUse.objectReferenceValue = asset;

							string path = "Assets/Resources/StyleAssets/User";

							string guid = AssetDatabase.CreateFolder(path, _newStyleName);
							string newFolderPath = AssetDatabase.GUIDToAssetPath(guid);

							string styleAssetPathAndName = AssetDatabase.GenerateUniqueAssetPath(newFolderPath + "/" + _newStyleName + ".asset");


							AssetDatabase.CreateAsset(asset, styleAssetPathAndName);

							if (_generateDefaultAssets)
							{

								Shader shader = Shader.Find("Standard");
								Material materialTemplate = new Material(shader);
								var atlasInfoTemplate = ScriptableObject.CreateInstance<AtlasInfo>();
								var paletteTemplate = ScriptableObject.CreateInstance<ScriptablePalette>();

								for (int i = 0; i < asset.geometryMaterialOptions.materials.Length; i++)
								{
									asset.geometryMaterialOptions.materials[i].Materials[0] = materialTemplate;
								}

								asset.geometryMaterialOptions.atlasInfo = atlasInfoTemplate;
								asset.geometryMaterialOptions.colorPalette = paletteTemplate;

								string materialPathAndName = AssetDatabase.GenerateUniqueAssetPath(newFolderPath + "/" + _newStyleName + "Material.asset");
								string atlasInfoPathAndName = AssetDatabase.GenerateUniqueAssetPath(newFolderPath + "/" + _newStyleName + "AtlasInfo.asset");
								string colorPalettePathAndName = AssetDatabase.GenerateUniqueAssetPath(newFolderPath + "/" + _newStyleName + "Palette.asset");

								AssetDatabase.CreateAsset(materialTemplate, materialPathAndName);
								AssetDatabase.CreateAsset(atlasInfoTemplate, atlasInfoPathAndName);
								AssetDatabase.CreateAsset(paletteTemplate, colorPalettePathAndName);
							}

							AssetDatabase.SaveAssets();
							AssetDatabase.Refresh();

							_createNewStyle = false;
						}
					}

					if (GUILayout.Button("Cancel"))
					{
						_createNewStyle = false;
					}
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
			//var sourceTypeProperty = property.FindPropertyRelative("m_styles");

			float height = 100.0f;
			//height += (((((VectorPrimitiveType)sourceTypeProperty.enumValueIndex == VectorPrimitiveType.Line)) ? 6.0f : 5.0f) * EditorGUIUtility.singleLineHeight);

			return height;
		}
	}
}
