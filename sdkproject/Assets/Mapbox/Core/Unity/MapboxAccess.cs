namespace Mapbox.Unity
{
    using System.IO;
    using UnityEngine;
    using System;
    using System.Collections;
    using Mapbox.Geocoding;
    using Mapbox.Directions;
    using Mapbox.Platform;
    using Mapbox.Unity.Utilities;

    public class MapboxAccess : IFileSource
    {
        readonly string _accessPath = Path.Combine(Application.streamingAssetsPath, Constants.Path.TOKEN_FILE);

        static MapboxAccess _instance = new MapboxAccess();
        public static MapboxAccess Instance
        {
            get
            {
                return _instance;
            }
        }

        MapboxAccess()
        {
            ValidateMapboxAccessFile();
            LoadAccessToken();
        }

        string _accessToken;
        public string AccessToken
        {
            get
            {
                return _accessToken;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new InvalidTokenException("Please configure your access token in the menu!");
                }
                _accessToken = value;
            }
        }

        void ValidateMapboxAccessFile()
        {
            if (!Directory.Exists(Application.streamingAssetsPath) || !File.Exists(_accessPath))
            {
                throw new InvalidTokenException("Please configure your access token in the menu!");
            }
        }

        void LoadAccessToken()
        {
#if  UNITY_EDITOR || !UNITY_ANDROID
            AccessToken = File.ReadAllText(_accessPath);
#else
            Runnable.Run(LoadMapboxAccess());
#endif
        }

        IEnumerator LoadMapboxAccess()
        {
            var request = new WWW(_accessPath);
            while (!request.isDone)
            {
                yield return 0;
            }
            AccessToken = request.text;
        }

        public IAsyncRequest Request(string url, Action<Response> callback)
        {
            var uriBuilder = new UriBuilder(url);

            string accessTokenQuery = "access_token=" + AccessToken;

            if (uriBuilder.Query != null && uriBuilder.Query.Length > 1)
            {
                uriBuilder.Query = uriBuilder.Query.Substring(1) + "&" + accessTokenQuery;
            }
            else
            {
                uriBuilder.Query = accessTokenQuery;
            }

            return new HTTPRequest(uriBuilder.ToString(), callback);
        }

        class InvalidTokenException : Exception
        {
            public InvalidTokenException(string message) : base(message)
            {
            }
        }

        /// <summary>
        /// Lazy geocoder.
        /// </summary>
        Geocoder _geocoder;
        public Geocoder Geocoder
        {
            get
            {
                if (_geocoder == null)
                {
                    _geocoder = new Geocoder(this);
                }
                return _geocoder;
            }
        }

        /// <summary>
        /// Lazy Directions.
        /// </summary>
        Directions _directions;
        public Directions Directions
        {
            get
            {
                if (_directions == null)
                {
                    _directions = new Directions(this);
                }
                return _directions;
            }
        }
    }
}
