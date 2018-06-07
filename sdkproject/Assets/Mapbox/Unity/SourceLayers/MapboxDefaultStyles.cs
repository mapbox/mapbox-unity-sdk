
namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using System.IO;
	using Mapbox.Unity.MeshGeneration.Data;

	/// <summary>
	/// MapboxDefaultStyles generates a new GeometryMaterialOptions object based on data contained in a MapFeatureStyleOptions. 
	/// </summary>

	public class StyleAssetPathBundle
	{
		public string topMaterialPath;
		public string sideMaterialPath;
		public string atlasPath;
		public string palettePath;

		public StyleAssetPathBundle(string styleName, string path)
		{
			string topMaterialName = string.Format("{0}{1}", styleName, Constants.StyleAssetNames.TOP_MATERIAL_SUFFIX);
			string sideMaterialName = string.Format("{0}{1}", styleName, Constants.StyleAssetNames.SIDE_MATERIAL_SUFFIX);
			string atlasInfoName = string.Format("{0}{1}", styleName, Constants.StyleAssetNames.ALTAS_SUFFIX);
			string paletteName = string.Format("{0}{1}", styleName, Constants.StyleAssetNames.PALETTE_SUFFIX);

			string materialFolderPath = Path.Combine(path, Constants.Path.MAPBOX_STYLES_MATERIAL_FOLDER);
			string atlasFolderPath = Path.Combine(path, Constants.Path.MAPBOX_STYLES_ATLAS_FOLDER);
			string paletteFolderPath = Path.Combine(path, Constants.Path.MAPBOX_STYLES_PALETTES_FOLDER);

			topMaterialPath = Path.Combine(materialFolderPath, topMaterialName);
			sideMaterialPath = Path.Combine(materialFolderPath, sideMaterialName);
			atlasPath = Path.Combine(atlasFolderPath, atlasInfoName);
			palettePath = Path.Combine(paletteFolderPath, paletteName);
		}
	}

	public static class MapboxDefaultStyles
	{
		/// <summary>
		/// Returns a new GeometryMaterialOptions object; called from VectorLayerVisualizer.SetProperties.
		/// </summary>
		/// <returns>The geometry material options.</returns>
		/// <param name="geometryMaterialOptions">Map feature style options.</param>
		/// 

		public static GeometryMaterialOptions GetGeometryMaterialOptions(GeometryMaterialOptions geometryMaterialOptionsRef)
		{
			GeometryMaterialOptions geometryMaterialOptions;
			if (geometryMaterialOptionsRef.style == StyleTypes.Custom)
			{
				geometryMaterialOptions = geometryMaterialOptionsRef;
			}
			else
			{
				string styleName = geometryMaterialOptionsRef.style.ToString();

				string path = Path.Combine(Constants.Path.MAP_FEATURE_STYLES_SAMPLES, Path.Combine(styleName, Constants.Path.MAPBOX_STYLES_ASSETS_FOLDER));

				StyleAssetPathBundle styleAssetPathBundle = new StyleAssetPathBundle(styleName, path);

				geometryMaterialOptions = AssignAssets(new GeometryMaterialOptions(), styleAssetPathBundle);

				geometryMaterialOptions.style = geometryMaterialOptionsRef.style;

				if(geometryMaterialOptions.style == StyleTypes.Satellite)
				{
					geometryMaterialOptions.texturingType = UvMapType.Tiled;
				}
				else if (geometryMaterialOptions.style == StyleTypes.Simple)
				{
					geometryMaterialOptions.texturingType = UvMapType.AtlasWithColorPalette;
				}
				else
				{
					geometryMaterialOptions.texturingType = UvMapType.Atlas;
				}
			}
			return geometryMaterialOptions;
		}

		public static GeometryMaterialOptions AssignAssets(GeometryMaterialOptions geometryMaterialOptions, StyleAssetPathBundle styleAssetPathBundle)
		{
			Material topMaterial = Resources.Load(styleAssetPathBundle.topMaterialPath, typeof(Material)) as Material;
			Material sideMaterial = Resources.Load(styleAssetPathBundle.sideMaterialPath, typeof(Material)) as Material;

			AtlasInfo atlas = Resources.Load(styleAssetPathBundle.atlasPath, typeof(AtlasInfo)) as AtlasInfo;
			ScriptablePalette palette = Resources.Load(styleAssetPathBundle.palettePath, typeof(ScriptablePalette)) as ScriptablePalette;

			geometryMaterialOptions.materials[0].Materials[0] = topMaterial;
			geometryMaterialOptions.materials[1].Materials[0] = sideMaterial;
			geometryMaterialOptions.atlasInfo = atlas;
			geometryMaterialOptions.colorPalette = palette;

			return geometryMaterialOptions;
		}

		public static GeometryMaterialOptions GetDefaultAssets()
		{

			StyleAssetPathBundle styleAssetPathBundle = new StyleAssetPathBundle("Default", Constants.Path.MAP_FEATURE_STYLES_DEFAULT_STYLE_ASSETS);
			GeometryMaterialOptions geometryMaterialOptions = new GeometryMaterialOptions();
			geometryMaterialOptions.texturingType = UvMapType.Atlas;
			return AssignAssets(geometryMaterialOptions, styleAssetPathBundle);
		}
	}
}