namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Components;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Map;
	using System;

	/// <summary>
	/// Style Modifier is a basic modifier which simply adds a TextureSelector script to the features.
	/// Logic is all pushed into this TextureSelector mono behaviour to make it's easier to change it in runtime.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Style Modifier")]
	public class StyleModifier : GameObjectModifier
	{

		[SerializeField]
		GeometryMaterialOptions _options;

		public override void SetProperties(ModifierProperties properties)
		{
			Debug.Log("StyleModifier.SetProperties...");
			var styleOptions = (MapFeatureStyleOptions)properties;
			string styleName = styleOptions.m_style.ToString();
			Debug.Log(styleName);

			//if style is set to custom AND there is a scriptable style set in the vectorSubLayerVisualizer...
			if(styleOptions.m_style == StyleTypes.Custom && styleOptions.m_scriptableStyle != null)
			{
				_options = new GeometryMaterialOptions();
				//_options = styleOptions.m_scriptableStyle.m_features[0].m_themes[0];
				_options = styleOptions.m_scriptableStyle.geometryMaterialOptions;


			}
			//geometryMaterialOptions
			//else, load resources and assign...
			else
			{
				if(styleOptions.m_style == StyleTypes.Custom)
				{
					Debug.LogError("WARNING: No custom scriptable style assigned in style link");
				}

				_options = new GeometryMaterialOptions();

				string _assetPathPrefix = "StyleAssets/";
				string path = string.Format("{0}{1}/{2}", _assetPathPrefix, styleName, styleName);

				string MatPath = path + "Material";
				string AtlasPath = path + "AtlasInfo";
				string PalettePath = path + "Palette";

				Material mat = Resources.Load(MatPath, typeof(Material)) as Material;
				AtlasInfo atlas = Resources.Load(AtlasPath, typeof(AtlasInfo)) as AtlasInfo;
				ScriptablePalette pal = Resources.Load(MatPath, typeof(ScriptablePalette)) as ScriptablePalette;

				for (int i = 0; i < _options.materials.Length; i++)
				{
					_options.materials[i].Materials[0] = mat;
				}
				_options.atlasInfo = atlas;
				_options.colorPalette = pal;

			}
			Debug.Log(_options.materials[0].Materials[0].name);
			Debug.Log(_options.atlasInfo.name);

		}

		//old code from material modifier...
		public override void Run(VectorEntity ve, UnityTile tile)
		{
			var min = Math.Min(_options.materials.Length, ve.MeshFilter.mesh.subMeshCount);
			var mats = new Material[min];

			if (_options.texturingType != UvMapType.Satellite)
			{
				for (int i = 0; i < min; i++)
				{
					mats[i] = _options.materials[i].Materials[UnityEngine.Random.Range(0, _options.materials[i].Materials.Length)];
				}
			}
			else
			{
				for (int i = 0; i < min; i++)
				{
					mats[i] = _options.materials[i].Materials[UnityEngine.Random.Range(0, _options.materials[i].Materials.Length)];
				}

				mats[0].mainTexture = tile.GetRasterData();
				mats[0].mainTextureScale = new Vector2(1f, 1f);
			}

			ve.MeshRenderer.materials = mats;
		}
	}
}
