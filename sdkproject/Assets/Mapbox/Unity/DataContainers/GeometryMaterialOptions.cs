namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using System.IO;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Data;

	public class CustomStyleBundle
	{
		public Material topMaterial;
		public Material sideMaterial;
		public AtlasInfo atlasInfo;
		public ScriptablePalette colorPalette;
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

		public StyleTypes style;

		public UvMapType texturingType = UvMapType.Tiled;
		public MaterialList[] materials = new MaterialList[2];
		public AtlasInfo atlasInfo;

		public float lightStyleOpacity = 1.0f;
		public float darkStyleOpacity = 1.0f;

		public Color colorStyleColor = Color.white;

		public SamplePalettes samplePalettes;

		public ScriptablePalette colorPalette;

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
			if (style == StyleTypes.Custom)
			{
				//nothing to do. Use custom settings. 
				return;
			}
			else
			{
				string styleName = style.ToString();

				string samplePaletteName = samplePalettes.ToString();

				string path = Path.Combine(Constants.Path.MAP_FEATURE_STYLES_SAMPLES, Path.Combine(styleName, Constants.Path.MAPBOX_STYLES_ASSETS_FOLDER));

				StyleAssetPathBundle styleAssetPathBundle = new StyleAssetPathBundle(styleName, path, samplePaletteName);

				AssignAssets(styleAssetPathBundle);

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
				else if (style == StyleTypes.Simple)
				{
					texturingType = UvMapType.AtlasWithColorPalette;
				}
				else
				{
					texturingType = UvMapType.Atlas;
				}
			}
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

		public void SetDefaultAssets()
		{
			StyleAssetPathBundle styleAssetPathBundle = new StyleAssetPathBundle("Default", Constants.Path.MAP_FEATURE_STYLES_DEFAULT_STYLE_ASSETS);
			texturingType = UvMapType.Atlas;
			AssignAssets(styleAssetPathBundle);
		}

		public void SetDefaultStyleType(StyleTypes style)
		{
			throw new NotImplementedException();
		}
	}

	[Serializable]
	public class UVModifierOptions : ModifierProperties
	{
		public override Type ModifierType
		{
			get
			{
				return typeof(UvModifier);
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
