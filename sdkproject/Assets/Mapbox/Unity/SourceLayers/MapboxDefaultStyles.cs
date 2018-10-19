
namespace Mapbox.Unity.Map
{
	using System.IO;

	/// <summary>
	/// MapboxDefaultStyles generates a new GeometryMaterialOptions object based on data contained in a MapFeatureStyleOptions. 
	/// </summary>

	public class StyleAssetPathBundle
	{
		public string topMaterialPath;
		public string sideMaterialPath;
		public string atlasPath;
		public string palettePath;

		public StyleAssetPathBundle(string styleName, string path, string samplePaletteName = "")
		{
			string topMaterialName = string.Format("{0}{1}", styleName, Constants.StyleAssetNames.TOP_MATERIAL_SUFFIX);
			string sideMaterialName = string.Format("{0}{1}", styleName, Constants.StyleAssetNames.SIDE_MATERIAL_SUFFIX);
			string atlasInfoName = string.Format("{0}{1}", styleName, Constants.StyleAssetNames.ALTAS_SUFFIX);
			string paletteName = (styleName == "Simple") ? samplePaletteName : string.Format("{0}{1}", styleName, Constants.StyleAssetNames.PALETTE_SUFFIX);

			string materialFolderPath = Path.Combine(path, Constants.Path.MAPBOX_STYLES_MATERIAL_FOLDER);
			string atlasFolderPath = Path.Combine(path, Constants.Path.MAPBOX_STYLES_ATLAS_FOLDER);
			string paletteFolderPath = Path.Combine(path, Constants.Path.MAPBOX_STYLES_PALETTES_FOLDER);

			topMaterialPath = Path.Combine(materialFolderPath, topMaterialName);
			sideMaterialPath = Path.Combine(materialFolderPath, sideMaterialName);
			atlasPath = Path.Combine(atlasFolderPath, atlasInfoName);
			palettePath = Path.Combine(paletteFolderPath, paletteName);
		}
	}
}