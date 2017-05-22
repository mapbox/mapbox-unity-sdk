namespace Mapbox.Editor
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using System.Collections;
    using Mapbox.Json;
    using Mapbox.Unity.Utilities;

    public class MapboxConfigurationWindow : EditorWindow
    {
		static string _accessPath;
        static string _accessToken;
        static string _lastAccessToken;
        static string _validationCode;

        [MenuItem("Mapbox/Configure Access")]
        static void Init()
        {
			_accessPath = Path.Combine(Application.streamingAssetsPath, Mapbox.Unity.Constants.Path.TOKEN_FILE);

            Runnable.EnableRunnableInEditor();
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            if (!File.Exists(_accessPath))
            {
                File.WriteAllText(_accessPath, _accessToken);
            }

            _accessToken = File.ReadAllText(_accessPath);
			var window = (MapboxConfigurationWindow)GetWindow(typeof(MapboxConfigurationWindow));
            window.Show();
        }

        void OnGUI()
        {
            _accessToken = EditorGUILayout.TextField("Access Token", _accessToken);
            if (string.IsNullOrEmpty(_accessToken))
            {
                EditorGUILayout.HelpBox("You must have an access token!", MessageType.Error);
                if (GUILayout.Button("Get a token from mapbox.com for free"))
                {
                    Application.OpenURL("https://www.mapbox.com/studio/account/tokens/");
                }
            }
            else
            {
                if (!string.Equals(_accessToken, _lastAccessToken) || string.IsNullOrEmpty(_validationCode))
                {
                    Runnable.Run(ValidateToken(_accessToken));
                }

                var messageType = MessageType.Error;
                if (string.Equals(_validationCode, "TokenValid"))
                {
                    messageType = MessageType.Info;
                    File.WriteAllText(_accessPath, _accessToken);
                    EditorGUILayout.HelpBox("TokenValid: saved to /StreamingAssets/MapboxAccess.text", messageType);
                }
                else
                {
                    EditorGUILayout.HelpBox(_validationCode, messageType);
                }
                Repaint();
            }
        }

        IEnumerator ValidateToken(string token)
        {
            _lastAccessToken = token;

            // TODO: implement safer url formatting?
            var www = new WWW("https://api.mapbox.com/tokens/v2?access_token=" + token);
            while (!www.isDone)
            {
                yield return 0;
            }
            var json = www.text;
            if (!string.IsNullOrEmpty(json))
            {
                ParseTokenResponse(json);
            }
        }

        void ParseTokenResponse(string json)
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (dict.ContainsKey("code"))
            {
                _validationCode = dict["code"].ToString();
            }
        }
    }
}