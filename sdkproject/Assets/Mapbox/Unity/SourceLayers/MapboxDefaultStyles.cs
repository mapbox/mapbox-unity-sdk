
namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using System.IO;
	using Mapbox.Unity.MeshGeneration.Data;

	/// <summary>
	/// MapboxDefaultStyles generates a new GeometryMaterialOptions object based on data contained in a MapFeatureStyleOptions. 
	/// </summary>
	public static class MapboxDefaultStyles
	{
		/// <summary>
		/// Returns a new GeometryMaterialOptions object; called from VectorLayerVisualizer.SetProperties.
		/// </summary>
		/// <returns>The geometry material options.</returns>
		/// <param name="mapFeatureStyleOptions">Map feature style options.</param>
		public static GeometryMaterialOptions GetGeometryMaterialOptions(MapFeatureStyleOptions mapFeatureStyleOptions)
		{
			StyleTypes styleType = mapFeatureStyleOptions.style;
			GeometryMaterialOptions geometryMaterialOptions;
			if (styleType == StyleTypes.Custom)
			{
				UnityEngine.Assertions.Assert.IsNotNull(mapFeatureStyleOptions.scriptableStyle, "Warning: Missing custom scriptable style.");
				geometryMaterialOptions = mapFeatureStyleOptions.scriptableStyle.geometryMaterialOptions;
			}
			else
			{
				string styleName = styleType.ToString();

				string path = Path.Combine(Constants.Path.MAP_FEATURE_STYLES_STYLES_SAMPLES, Path.Combine(styleName, Constants.Path.MAPBOX_STYLES_ASSETS_FOLDER));

				string topMaterialName = string.Format("{0}{1}", styleName, Constants.StyleAssetNames.TOP_MATERIAL_SUFFIX);
				string sideMaterialName = string.Format("{0}{1}", styleName, Constants.StyleAssetNames.SIDE_MATERIAL_SUFFIX);
				string atlasInfoName = string.Format("{0}{1}", styleName, Constants.StyleAssetNames.ALTAS_SUFFIX);
				string paletteName = string.Format("{0}{1}", styleName, Constants.StyleAssetNames.PALETTE_SUFFIX);

				string materialFolderPath = Path.Combine(path, Constants.Path.MAPBOX_STYLES_MATERIAL_FOLDER);
				string atlasFolderPath = Path.Combine(path, Constants.Path.MAPBOX_STYLES_ATLAS_FOLDER);
				string paletteFolderPath = Path.Combine(path, Constants.Path.MAPBOX_STYLES_PALETTES_FOLDER);

				string topMaterialPath = Path.Combine(materialFolderPath, topMaterialName);
				string sideMaterialPath = Path.Combine(materialFolderPath, sideMaterialName);
				string atlasPath = Path.Combine(atlasFolderPath, atlasInfoName);

				string palettePath = Path.Combine(paletteFolderPath, paletteName);

				Material topMaterial = Resources.Load(topMaterialPath, typeof(Material)) as Material;
				Material sideMaterial = Resources.Load(sideMaterialPath, typeof(Material)) as Material;

				AtlasInfo atlas = Resources.Load(atlasPath, typeof(AtlasInfo)) as AtlasInfo;
				ScriptablePalette pal = Resources.Load(palettePath, typeof(ScriptablePalette)) as ScriptablePalette;

				geometryMaterialOptions = new GeometryMaterialOptions();

				geometryMaterialOptions.texturingType = (styleType == StyleTypes.Simple) ? UvMapType.AtlasWithColorPalette : UvMapType.Atlas;

				geometryMaterialOptions.materials[0].Materials[0] = topMaterial;
				geometryMaterialOptions.materials[1].Materials[0] = sideMaterial;

				geometryMaterialOptions.atlasInfo = atlas;
				geometryMaterialOptions.colorPalette = pal;
			}

			return geometryMaterialOptions;
		}
	}
}