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

		/// <summary>
		/// Sets the type of the style.
		/// </summary>
		/// <param name="styleType">Style type.</param>
		public void SetStyleType(StyleTypes styleType)
		{
			style = styleType;
		}

		/// <summary>
		/// Sets the layer to use the realistic style.
		/// </summary>
		public virtual void SetRealisticStyle()
		{
			style = StyleTypes.Realistic;
			HasChanged = true;
		}

		/// <summary>
		/// Sets the layer to use the fantasy style.
		/// </summary>
		public virtual void SetFantasyStyle()
		{
			style = StyleTypes.Fantasy;
			HasChanged = true;
		}

		/// <summary>
		/// Sets the type of the simple style palette.
		/// </summary>
		/// <param name="palette">Palette.</param>
		public virtual void SetSimpleStylePaletteType(SamplePalettes palette)
		{
			samplePalettes = palette;
			HasChanged = true;
		}

		/// <summary>
		/// Sets the light style opacity.
		/// </summary>
		/// <param name="opacity">Opacity.</param>
		public virtual void SetLightStyleOpacity(float opacity)
		{
			lightStyleOpacity = Mathf.Clamp(opacity, 0.0f, 1.0f);
			HasChanged = true;
		}

		/// <summary>
		/// Sets the dark style opacity.
		/// </summary>
		/// <param name="opacity">Opacity.</param>
		public virtual void SetDarkStyleOpacity(float opacity)
		{
			darkStyleOpacity = Mathf.Clamp(opacity, 0.0f, 1.0f);
			HasChanged = true;
		}

		/// <summary>
		/// Sets the color of the color style.
		/// </summary>
		/// <param name="color">Color.</param>
		public virtual void SetColorStyleColor(Color color)
		{
			colorStyleColor = color;
			HasChanged = true;
		}

		/// <summary>
		/// Sets the texturing (UV) type of the custom style.
		/// </summary>
		/// <param name="uvMapType">Uv map type.</param>
		public virtual void SetTexturingType(UvMapType uvMapType)
		{
			texturingType = uvMapType;
			HasChanged = true;
		}

		/// <summary>
		/// Sets the custom style top material.
		/// </summary>
		/// <param name="material">Material.</param>
		public virtual void SetTopMaterial(Material material)
		{
			materials[0].Materials[0] = material;
			HasChanged = true;
		}

		/// <summary>
		/// Sets the custom style side material.
		/// </summary>
		/// <param name="material">Material.</param>
		public virtual void SetSideMaterial(Material material)
		{
			materials[1].Materials[0] = material;
			HasChanged = true;
		}

		/// <summary>
		/// Sets the custom style top and side materials.
		/// </summary>
		/// <param name="topMaterial">Top material.</param>
		/// <param name="sideMaterial">Side material.</param>
		public virtual void SetMaterials(Material topMaterial, Material sideMaterial)
		{
			materials[0].Materials[0] = topMaterial;
			materials[1].Materials[0] = sideMaterial;
			HasChanged = true;
		}

		/// <summary>
		/// Sets the custom style uv atlas.
		/// </summary>
		/// <param name="atlas">Atlas.</param>
		public virtual void SetUvAtlas(AtlasInfo atlas)
		{
			atlasInfo = atlas;
			HasChanged = true;
		}

		/// <summary>
		/// Sets the custom style color palette.
		/// </summary>
		/// <param name="palette">Palette.</param>
		public virtual void SetColorPalette(ScriptablePalette palette)
		{
			colorPalette = palette;
			HasChanged = true;
		}

		/// <summary>
		/// Sets the custom style assets using a CustomStyleBundle object.
		/// </summary>
		/// <param name="customStyleBundle">Custom style bundle.</param>
		public virtual void SetCustomStyleAssets(CustomStyleBundle customStyleBundle)
		{
			materials[0].Materials[0] = (customStyleBundle.sideMaterial != null) ? customStyleBundle.sideMaterial : materials[0].Materials[0];
			materials[1].Materials[0] = (customStyleBundle.topMaterial != null) ? customStyleBundle.topMaterial : materials[1].Materials[0];
			atlasInfo = (customStyleBundle.atlasInfo != null) ? customStyleBundle.atlasInfo : atlasInfo;
			colorPalette = (customStyleBundle.colorPalette != null) ? customStyleBundle.colorPalette : colorPalette;
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

		/// <summary>
		/// Gets the type of simple style palette used in the layer.
		/// </summary>
		/// <returns>The simple style palette type.</returns>
		public virtual SamplePalettes GetSimpleStylePaletteType()
		{
			return samplePalettes;
		}

		/// <summary>
		/// Gets the light style opacity.
		/// </summary>
		/// <returns>The light style opacity.</returns>
		public virtual float GetLightStyleOpacity()
		{
			return lightStyleOpacity;
		}

		/// <summary>
		/// Gets the dark style opacity.
		/// </summary>
		/// <returns>The dark style opacity.</returns>
		public virtual float GetDarkStyleOpacity()
		{
			return darkStyleOpacity;
		}

		/// <summary>
		/// Gets the color of the color style.
		/// </summary>
		/// <returns>The color style color.</returns>
		public virtual Color GetColorStyleColor()
		{
			return colorStyleColor;
		}

		/// <summary>
		/// Gets the type of the custom style texturing.
		/// </summary>
		/// <returns>The custom texturing type.</returns>
		public virtual UvMapType GetTexturingType()
		{
			return texturingType;
		}

		/// <summary>
		/// Gets the custom top material.
		/// </summary>
		/// <returns>The custom top material.</returns>
		public virtual Material GetTopMaterial()
		{
			return materials[0].Materials[0];
		}

		/// <summary>
		/// Gets the custom side material.
		/// </summary>
		/// <returns>The custom side material.</returns>
		public virtual Material GetSideMaterial()
		{
			return materials[1].Materials[0];
		}

		/// <summary>
		/// Gets the custom uv atlas.
		/// </summary>
		/// <returns>The custom uv atlas.</returns>
		public virtual AtlasInfo GetUvAtlas()
		{
			return atlasInfo;
		}

		/// <summary>
		/// Gets the custom color palette.
		/// </summary>
		/// <returns>The custom color palette.</returns>
		public virtual ScriptablePalette GetColorPalette()
		{
			return colorPalette;
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
