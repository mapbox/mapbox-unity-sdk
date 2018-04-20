using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Map;
using Mapbox.Editor;

public class TextureStringBundle
{
	public Texture m_texture;

	public void Load()
	{
		
	}
	public TextureStringBundle(string name)
	{
		
	}
}


[CustomEditor(typeof(FakeAbstractMap))]
public class FakeAbstractMapEditor : Editor
{
	private bool _createNewStyle;

	private StyleTypes _newStyleTypeToCreate = StyleTypes.Simple;

	private string _newStyleName;

	private Dictionary<StyleTypes, TextureStringBundle> TSB_bundles = new Dictionary<StyleTypes, TextureStringBundle>()
	{
		{StyleTypes.Simple, new TextureStringBundle(StyleTypes.Simple.ToString())},
		{StyleTypes.Realistic, new TextureStringBundle(StyleTypes.Realistic.ToString())},
		{StyleTypes.Scifi, new TextureStringBundle(StyleTypes.Scifi.ToString())},
		{StyleTypes.Fantasy, new TextureStringBundle(StyleTypes.Fantasy.ToString())},
		{StyleTypes.Custom, new TextureStringBundle(StyleTypes.Custom.ToString())},
	};

	void OnEnable()
	{
		foreach (var key in TSB_bundles.Keys)
		{
			TSB_bundles[key].Load();
		}
	}

	override public void OnInspectorGUI()
	{

		FakeAbstractMap fam = (FakeAbstractMap)target;

		Texture texture = TSB_bundles[fam.m_styles].m_texture;

		GUI.DrawTexture(new Rect(10, 150, 60, 60), texture, ScaleMode.StretchToFill, true, 10.0F);
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		fam.m_styles = (StyleTypes)EditorGUILayout.EnumPopup("Style:", fam.m_styles);

		if(fam.m_styles == StyleTypes.Custom)
		{
			fam.m_style = EditorGUILayout.ObjectField("Style Link:", fam.m_style, typeof(ScriptableStyle), false) as ScriptableStyle;
			if (GUILayout.Button("Create New Style"))
			{
				_createNewStyle = true;
			}
			if(_createNewStyle)
			{
				_newStyleName = EditorGUILayout.TextField("New Style Name: ", _newStyleName);
				//_newStyleTypeToCreate = (StyleTypes)EditorGUILayout.EnumPopup("Style:", _newStyleTypeToCreate);
				if (GUILayout.Button("Create"))
				{
					if(string.IsNullOrEmpty(_newStyleName))
					{
						Debug.LogError("New style name is empty");
					}
					else
					{
						ScriptableStyle asset = ScriptableObject.CreateInstance<ScriptableStyle>();

						fam.m_style = asset;
						//string path = AssetDatabase.GetAssetPath(Selection.activeObject);
						//if (path == "")
						//{
						string path = "Assets";
						//}
						//else if (Path.GetExtension(path) != "")
						//{
						//	path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
						//}

						string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + _newStyleName + ".asset");

						AssetDatabase.CreateAsset(asset, assetPathAndName);

						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();
						//EditorUtility.FocusProjectWindow();
						//Selection.activeObject = asset;
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

		//fam.m_displayBundle.m_material = EditorGUILayout.ObjectField("Material:", fam.m_displayBundle.m_material, typeof(Material), false) as Material;
		//fam.m_displayBundle.m_atlasInfo = EditorGUILayout.ObjectField("AltasInfo:", fam.m_displayBundle.m_atlasInfo, typeof(AtlasInfo), false) as AtlasInfo;
		//fam.m_displayBundle.m_palette = EditorGUILayout.ObjectField("Palette:", fam.m_displayBundle.m_palette, typeof(ScriptablePalette), false) as ScriptablePalette;

		//if (GUILayout.Button("Add Feature"))
		//{
		//	fam.AddFeature();
		//}
		//DrawDefaultInspector();
	}
}
