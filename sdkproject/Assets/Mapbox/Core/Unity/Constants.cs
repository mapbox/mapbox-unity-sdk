namespace Mapbox.Unity
{
    using UnityEngine;

    public static class Constants
    {
        public static class Path
        {
            /// <summary>
            /// Access token file name. Intended to be located in StreamingAssets.
            /// </summary>
            public const string TOKEN_FILE = "MapboxAccess.text";
        }

        /// <summary>
        /// Store common vector constants to avoid the method access cost of Unity's convenience getters.
        /// </summary>
        public static class Math
        {
            public static readonly Vector3 Vector3Zero = new Vector3(0, 0, 0);
            public static readonly Vector3 Vector3Up = new Vector3(0, 1, 0);
            public static readonly Vector3 Vector3Down = new Vector3(0, -1, 0);
        }
    }
}