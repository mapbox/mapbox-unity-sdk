namespace Mapbox.Unity
{
	using UnityEngine;

	public static class Constants
	{
		public const string SDK_VERSION = "2.1.1";
		public const string SDK_SKU_ID = "05";

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

			public static readonly string MAPBOX_USER = System.IO.Path.Combine("Assets", System.IO.Path.Combine("Mapbox", "User"));
			public static readonly string MAPBOX_USER_MODIFIERS = System.IO.Path.Combine(MAPBOX_USER, "Modifiers");

			public static readonly string MAP_FEATURE_STYLES_DEFAULT_STYLE_ASSETS = System.IO.Path.Combine("MapboxStyles", "DefaultStyleAssets");
			public static readonly string MAP_FEATURE_STYLES_SAMPLES = System.IO.Path.Combine(System.IO.Path.Combine("MapboxStyles", "Styles"), "MapboxSampleStyles");

			public const string MAPBOX_STYLES_ASSETS_FOLDER = "Assets";
			public const string MAPBOX_STYLES_ATLAS_FOLDER = "Atlas";
			public const string MAPBOX_STYLES_MATERIAL_FOLDER = "Materials";
			public const string MAPBOX_STYLES_PALETTES_FOLDER = "Palettes";
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
			public static readonly Vector3 Vector3Unused = new Vector3(float.MinValue, float.MinValue, float.MinValue);

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
		}

		public static class GUI
		{
			public static class Colors
			{
				public static readonly Color EDITOR_TEXT_COLOR = new Color(0.7f, 0.7f, 0.7f);
				public static readonly Color EDITOR_NOTE_COLOR = new Color(1.0f, 0.8f, 0.0f);
				public static readonly Color EDITOR_FEATURE_DEFAULT_COLOR = new Color(0.1764706f, 0.8509805f, 1.0f, 1.0f);
			}

			public static class Styles
			{
				public static readonly GUIStyle EDITOR_NOTE_STYLE = new GUIStyle { wordWrap = true, normal = { textColor = Colors.EDITOR_NOTE_COLOR } };
				public static readonly GUIStyle EDITOR_TEXTURE_STYLE_DESCRIPTION_STYLE = new GUIStyle { wordWrap = true, normal = { textColor = Colors.EDITOR_TEXT_COLOR } };
				public static readonly GUIStyle EDITOR_TEXTURE_THUMBNAIL_STYLE = new GUIStyle { imagePosition = ImagePosition.ImageOnly, wordWrap = true };
			}
		}
	}
}
