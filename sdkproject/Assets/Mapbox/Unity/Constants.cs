namespace Mapbox.Unity
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System.Collections.Generic;
	using Mapbox.Unity.Map;

	public static class Constants
	{
		public const string SDK_VERSION = "1.4.1";

		public static class Path
		{
			public const string CONFIG_FILE = "MapboxConfiguration.txt";
			public const string SCENELIST = "Assets/Mapbox/Resources/Mapbox/ScenesList.asset";
			public const string SHOULD_COLLECT_LOCATION_KEY = "MAPBOX_SHOULD_COLLECT_LOCATION";
			public const string TELEMETRY_TURNSTILE_LAST_TICKS_EDITOR_KEY = "MAPBOX_TELEMETRY_TURNSTILE_LAST_TICKS_EDITOR";
			public const string TELEMETRY_TURNSTILE_LAST_TICKS_FALLBACK_KEY = "MAPBOX_TELEMETRY_TURNSTILE_LAST_TICKS_FALLBACK";
			public const string DID_PROMPT_CONFIGURATION = "MAPBOX_DID_PROMPT_CONFIGURATION";
			public static readonly string MAPBOX_RESOURCES_RELATIVE = System.IO.Path.Combine("Mapbox", "MapboxConfiguration");
			public static readonly string MAPBOX_RESOURCES_ABSOLUTE = System.IO.Path.Combine(System.IO.Path.Combine(Application.dataPath, "Resources"), "Mapbox");

			public static readonly string MAPBOX_ASSET_RESOURCES_RELATIVE = System.IO.Path.Combine("Mapbox", "Resources");
			public static readonly string MAP_FEATURE_STYLES_RELATIVE = System.IO.Path.Combine(MAPBOX_ASSET_RESOURCES_RELATIVE, "MapFeatureStyles");

			public static readonly string MAP_FEATURE_STYLES_DEFAULT_STYLE_ASSETS = System.IO.Path.Combine("MapFeatureStyles", "DefaultStyleAssets");
			public static readonly string MAP_FEATURE_STYLES_STYLES = System.IO.Path.Combine("MapFeatureStyles", "Styles");

			public static readonly string MAP_FEATURE_STYLES_STYLES_CUSTOM = System.IO.Path.Combine("Assets", System.IO.Path.Combine(MAP_FEATURE_STYLES_RELATIVE, System.IO.Path.Combine("Styles", "CustomStyles")));
			public static readonly string MAP_FEATURE_STYLES_STYLES_SAMPLES = System.IO.Path.Combine(MAP_FEATURE_STYLES_STYLES, "MapboxSampleStyles");
		
			public static readonly string MAPBOX_STYLES_STYLES_PATH = System.IO.Path.Combine("Assets", System.IO.Path.Combine("Mapbox", System.IO.Path.Combine("Resources", System.IO.Path.Combine("MapFeatureStyles", "Styles"))));//"Assets/Mapbox/Resources/MapboxStyles/Styles/";
			public static readonly string MAPBOX_STYLES_USER_ASSETS_PATH = System.IO.Path.Combine(MAPBOX_STYLES_STYLES_PATH, "User");

			public const string MAPBOX_STYLES_ASSETS_FOLDER = "Assets";
			public const string MAPBOX_STYLES_ATLAS_FOLDER = "Atlas";
			public const string MAPBOX_STYLES_MATERIAL_FOLDER = "Materials";
			public const string MAPBOX_STYLES_PALETTES_FOLDER = "Palettes";
			public const string MAPBOX_STYLES_TEXTURES_FOLDER = "Textures";
		}

		/// <summary>
		/// Store common vector constants to avoid the method access cost of Unity's convenience getters.
		/// </summary>
		public static class Math
		{
			public static readonly Vector3 Vector3Zero = new Vector3(0, 0, 0);
			public static readonly Vector3 Vector3Up = new Vector3(0, 1, 0);
			public static readonly Vector3 Vector3Down = new Vector3(0, -1, 0);
			public static readonly Vector3 Vector3One = new Vector3(1, 1, 1);
			public static readonly Vector3 Vector3Forward = new Vector3(0, 0, 1);
				
			public static Vector3 Vector3Right = new Vector3(1, 0, 0);
		}

		/// <summary>
		/// Store common style asset prefixes and suffixes to avoid string spelling errors in code.
		/// </summary>
		public static class StyleAssetNames
		{
			public const string ALTAS_SUFFIX = "AtlasInfo";
			public const string PALETTE_SUFFIX = "Palette";

			public const string TOP_MATERIAL_SUFFIX = "TopMaterial";
			public const string SIDE_MATERIAL_SUFFIX = "SideMaterial";

			public const string TOP_TEXTURE_SUFFIX = "TopTexture";
			public const string SIDE_TEXTURE_SUFFIX = "SideTexture";

			public static readonly string DEFAULT_TOP_TEXTURE_NAME = string.Format("Default{0}", TOP_TEXTURE_SUFFIX);
			public static readonly string DEFAULT_SIDE_TEXTURE_NAME = string.Format("Default{0}", SIDE_TEXTURE_SUFFIX);
		}

		/// <summary>
		/// Store style definition labels for description text in the inspector.
		/// </summary>
		public static class StyleLabels
		{
			public static Dictionary<StyleTypes, string> labels = new Dictionary<StyleTypes, string>()
			{
				{StyleTypes.Simple, "Simple style combines stylized vector designs with scriptable palettes to create a simple, procedurally colored rendering style."},
				{StyleTypes.Realistic, "Realistic style combines modern, urban designs with physically-based-rendering materials to help create a contemporary, realistic rendering style."},
				{StyleTypes.Fantasy, "Fantasy style combines old world medieval designs with physically-based-rendering materials to help create a fantasy rendering style."},
			};
		}
	}
}
