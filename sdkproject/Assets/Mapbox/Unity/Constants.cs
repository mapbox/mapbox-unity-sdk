namespace Mapbox.Unity
{
    using UnityEngine;

    public static class Constants
    {
		public const string SDK_VERSION = "1.0.0";
			
        public static class Path
        {
            /// <summary>
            /// Access token file name. Intended to be located in StreamingAssets.
            /// </summary>
            public const string TOKEN_FILE = "MapboxAccess.text";
			public const string IS_TELEMETRY_ENABLED_KEY = "IS_MAPBOX_TELEMETRY_ENABLED";
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
		}
    }
}