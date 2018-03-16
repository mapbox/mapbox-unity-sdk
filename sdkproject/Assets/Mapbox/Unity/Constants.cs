namespace Mapbox.Unity
{
	using UnityEngine;

	public static class Constants
	{
		public const string SDK_VERSION = "1.4.0";

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
	}
}