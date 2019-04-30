namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using System.IO;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Data;

	[Serializable]
	public class CustomStyleBundle
	{
		public UvMapType texturingType = UvMapType.Tiled;
		public MaterialList[] materials = new MaterialList[2];
		public AtlasInfo atlasInfo;
		public ScriptablePalette colorPalette;

		public CustomStyleBundle()
		{
			materials = new MaterialList[2];
			materials[0] = new MaterialList();
			materials[1] = new MaterialList();
		}

		private void AssignAssets(StyleAssetPathBundle styleAssetPathBundle)
		{
			Material topMaterial = Resources.Load(styleAssetPathBundle.topMaterialPath, typeof(Material)) as Material;
			Material sideMaterial = Resources.Load(styleAssetPathBundle.sideMaterialPath, typeof(Material)) as Material;

			AtlasInfo atlas = Resources.Load(styleAssetPathBundle.atlasPath, typeof(AtlasInfo)) as AtlasInfo;
			ScriptablePalette palette = Resources.Load(styleAssetPathBundle.palettePath, typeof(ScriptablePalette)) as ScriptablePalette;

			materials[0].Materials[0] = new Material(topMaterial);
			materials[1].Materials[0] = new Material(sideMaterial);
			atlasInfo = atlas;
			colorPalette = palette;
		}

		public void SetDefaultAssets(UvMapType mapType = UvMapType.Atlas)
		{
			StyleAssetPathBundle styleAssetPathBundle = new StyleAssetPathBundle("Default", Constants.Path.MAP_FEATURE_STYLES_DEFAULT_STYLE_ASSETS);
			texturingType = mapType;
			AssignAssets(styleAssetPathBundle);
		}
	}

	[Serializable]
	public class GeometryMaterialOptions : ModifierProperties, ISubLayerTexturing

	{
		public override Type ModifierType
		{
			get
			{
				return typeof(MaterialModifier);
			}
		}
		private SubLayerDarkStyle _darkStyle;
		public ISubLayerDarkStyle DarkStyle
		{
			get
			{
				if (_darkStyle == null)
				{
					_darkStyle = new SubLayerDarkStyle(this);
				}
				return _darkStyle;
			}
		}

		private SubLayerLightStyle _lightStyle;
		public ISubLayerLightStyle LightStyle
		{
			get
			{
				if (_lightStyle == null)
				{
					_lightStyle = new SubLayerLightStyle(this);
				}
				return _lightStyle;
			}
		}

		private SubLayerColorStyle _colorStyle;
		public ISubLayerColorStyle ColorStyle
		{
			get
			{
				if (_colorStyle == null)
				{
					_colorStyle = new SubLayerColorStyle(this);
				}
				return _colorStyle;
			}
		}

		private SubLayerSimpleStyle _simpleStyle;
		public ISubLayerSimpleStyle SimpleStyle
		{
			get
			{
				if (_simpleStyle == null)
				{
					_simpleStyle = new SubLayerSimpleStyle(this);
				}
				return _simpleStyle;
			}
		}

		private SubLayerRealisticStyle _realisticStyle;
		public ISubLayerRealisticStyle RealisticStyle
		{
			get
			{
				if (_realisticStyle == null)
				{
					_realisticStyle = new SubLayerRealisticStyle(this);
				}
				return _realisticStyle;
			}
		}

		private SubLayerFantasyStyle _fantasyStyle;
		public ISubLayerFantasyStyle FantasyStyle
		{
			get
			{
				if (_fantasyStyle == null)
				{
					_fantasyStyle = new SubLayerFantasyStyle(this);
				}
				return _fantasyStyle;
			}
		}


		private SubLayerCustomStyle _customStyle;
		public ISubLayerCustomStyle CustomStyle
		{
			get
			{
				if (_customStyle == null)
				{
					_customStyle = new SubLayerCustomStyle(this);
				}
				return _customStyle;
			}
		}

		public StyleTypes style;

		public UvMapType texturingType = UvMapType.Tiled;
		public MaterialList[] materials = new MaterialList[2];
		public AtlasInfo atlasInfo;

		public float lightStyleOpacity = 1.0f;
		public float darkStyleOpacity = 1.0f;

		public Color colorStyleColor = Color.white;

		public SamplePalettes samplePalettes;

		public ScriptablePalette colorPalette;

		[SerializeField]
		public CustomStyleBundle customStyleOptions;

		public GeometryMaterialOptions()
		{
			materials = new MaterialList[2];
			materials[0] = new MaterialList();
			materials[1] = new MaterialList();
		}

		/// <summary>
		/// Sets up default values for GeometryMaterial Options.
		/// If style is set to Custom, user defined values will be used.
		/// </summary>
		public void SetDefaultMaterialOptions()
		{
			string styleName = style.ToString();

			if (customStyleOptions == null)
			{
				customStyleOptions = new CustomStyleBundle();
				customStyleOptions.SetDefaultAssets();
			}
			if (style == StyleTypes.Custom)
			{
				//nothing to do. Use custom settings
			}
			else
			{
				string samplePaletteName = samplePalettes.ToString();

				string path = Path.Combine(Constants.Path.MAP_FEATURE_STYLES_SAMPLES, Path.Combine(styleName, Constants.Path.MAPBOX_STYLES_ASSETS_FOLDER));

				StyleAssetPathBundle styleAssetPathBundle = new StyleAssetPathBundle(styleName, path, samplePaletteName);

				AssignAssets(styleAssetPathBundle);
			}

			switch (style)
			{
				case StyleTypes.Light:
					Color lightColor = materials[0].Materials[0].color;
					lightColor.a = lightStyleOpacity;
					materials[0].Materials[0].color = lightColor;

					lightColor = materials[1].Materials[0].color;
					lightColor.a = lightStyleOpacity;
					materials[1].Materials[0].color = lightColor;
					break;
				case StyleTypes.Dark:
					Color darkColor = materials[0].Materials[0].color;
					darkColor.a = darkStyleOpacity;
					materials[0].Materials[0].color = darkColor;

					darkColor = materials[1].Materials[0].color;
					darkColor.a = darkStyleOpacity;
					materials[1].Materials[0].color = darkColor;
					break;
				case StyleTypes.Color:
					Color color = colorStyleColor;
					materials[0].Materials[0].color = color;
					materials[1].Materials[0].color = color;
					break;
				default:
					break;
			}

			if (style == StyleTypes.Satellite)
			{
				texturingType = UvMapType.Tiled;
			}
			else
			{
				texturingType = (style != StyleTypes.Custom && style == StyleTypes.Simple) ? UvMapType.AtlasWithColorPalette : UvMapType.Atlas;
			}
		}

		private void AssignAssets(StyleAssetPathBundle styleAssetPathBundle)
		{
			Material topMaterial = Resources.Load(styleAssetPathBundle.topMaterialPath, typeof(Material)) as Material;
			Material sideMaterial = Resources.Load(styleAssetPathBundle.sideMaterialPath, typeof(Material)) as Material;

			AtlasInfo atlas = Resources.Load(styleAssetPathBundle.atlasPath, typeof(AtlasInfo)) as AtlasInfo;
			ScriptablePalette palette = Resources.Load(styleAssetPathBundle.palettePath, typeof(ScriptablePalette)) as ScriptablePalette;

			Material[] tempMaterials = new Material[2];


			for (int i = 0; i < materials.Length; i++)
			{
				if (materials[i].Materials[0] != null)
				{
					tempMaterials[i] = materials[i].Materials[0];
					materials[i].Materials[0] = null;
				}
			}

			materials[0].Materials[0] = new Material(topMaterial);
			materials[1].Materials[0] = new Material(sideMaterial);

			for (int i = 0; i < materials.Length; i++)
			{
				if (tempMaterials[i] != null)
				{
					tempMaterials[i].Destroy();
				}
			}

			Resources.UnloadUnusedAssets();

			atlasInfo = atlas;
			colorPalette = palette;

		}

		public void SetDefaultAssets(UvMapType mapType = UvMapType.Atlas)
		{
			StyleAssetPathBundle styleAssetPathBundle = new StyleAssetPathBundle("Default", Constants.Path.MAP_FEATURE_STYLES_DEFAULT_STYLE_ASSETS);
			texturingType = mapType;
			AssignAssets(styleAssetPathBundle);
		}

		/// <summary>
		/// Sets the type of the style.
		/// </summary>
		/// <param name="styleType">Style type.</param>
		public void SetStyleType(StyleTypes styleType)
		{
			style = styleType;
			HasChanged = true;
		}


		/// <summary>
		/// Gets the type of style used in the layer.
		/// </summary>
		/// <returns>The style type.</returns>
		public virtual StyleTypes GetStyleType()
		{
			return style;
		}

	}

	[Serializable]
	public class UVModifierOptions : ModifierProperties
	{
		public override Type ModifierType
		{
			get
			{
				return typeof(PolygonMeshModifier);
			}
		}
		public StyleTypes style;
		public UvMapType texturingType = UvMapType.Tiled;
		public AtlasInfo atlasInfo;

		public GeometryExtrusionWithAtlasOptions ToGeometryExtrusionWithAtlasOptions()
		{
			return new GeometryExtrusionWithAtlasOptions(this);
		}
	}

}
