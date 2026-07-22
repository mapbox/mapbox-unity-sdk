using Mapbox.Example.Scripts.Map;
using UnityEditor;
using UnityEngine;

namespace Mapbox.Example.Editor
{
    [CustomEditor(typeof(MapboxMapBehaviour))]
    public class MapboxMapBehaviourEditor : UnityEditor.Editor
    {
        private SerializedProperty _mapInfoProp;
        private SerializedProperty _unityContextProp;

        private SerializedProperty _tileCreatorProp;
        private SerializedProperty _defaultTileMaterialProp;
        private SerializedProperty _tileProviderProp;
        private SerializedProperty _dataFetcherProp;
        private SerializedProperty _cacheManagerProp;
        private SerializedProperty _locationFactoryProp;
        private SerializedProperty _initializeOnStart;

        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;

        private bool _overrideModulesFold = false;
        private bool _settingsFold = false;

        private void OnEnable()
        {
            _mapInfoProp       = serializedObject.FindProperty("MapInformation");
            _unityContextProp  = serializedObject.FindProperty("UnityContext");

            _tileCreatorProp   = serializedObject.FindProperty("_tileCreatorBehaviour");
            _defaultTileMaterialProp = serializedObject.FindProperty("_defaultTileMaterial");
            _tileProviderProp  = serializedObject.FindProperty("TileProvider");
            _dataFetcherProp   = serializedObject.FindProperty("DataFetcher");
            _cacheManagerProp  = serializedObject.FindProperty("CacheManager");
            _locationFactoryProp = serializedObject.FindProperty("LocationFactory");
            _initializeOnStart  = serializedObject.FindProperty("InitializeOnStart");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EnsureStyles();

            EditorGUILayout.Space(2);
            using (new EditorGUILayout.VerticalScope(_boxStyle))
            {
                if (_mapInfoProp != null)
                {
                    EditorGUILayout.PropertyField(_mapInfoProp);
                }
                else
                {
                    EditorGUILayout.HelpBox("MapInformation (Serializable) field not found or not serializable.", MessageType.Warning);
                }
            }
        
            EditorGUILayout.Space(6);
        
            using (new EditorGUILayout.VerticalScope(_boxStyle))
            {
                if (_unityContextProp != null)
                {
                    EditorGUILayout.PropertyField(_unityContextProp);
                }
                else
                {
                    EditorGUILayout.HelpBox("UnityContext (Serializable) field not found or not serializable.", MessageType.Warning);
                }
            }

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.VerticalScope(_boxStyle))
            {
                _overrideModulesFold = EditorGUILayout.Foldout(_overrideModulesFold, "Override Modules");
                if (_overrideModulesFold)
                {
                    EditorGUILayout.PropertyField(_tileCreatorProp, new GUIContent("Tile Creator Behaviour"));
                    EditorGUILayout.PropertyField(_tileProviderProp, new GUIContent("Tile Provider"));
                    EditorGUILayout.PropertyField(_dataFetcherProp, new GUIContent("Data Fetcher"));
                    EditorGUILayout.PropertyField(_cacheManagerProp, new GUIContent("Cache Manager"));
                    EditorGUILayout.PropertyField(_locationFactoryProp, new GUIContent("Location Factory"));

                    if (_tileCreatorProp == null && _tileProviderProp == null && _dataFetcherProp == null &&
                        _cacheManagerProp == null && _locationFactoryProp == null)
                    {
                        EditorGUILayout.HelpBox("Drag & drop your script components here.", MessageType.Info);
                    }
                }
            }
            
            EditorGUILayout.Space(8);
            using (new EditorGUILayout.VerticalScope(_boxStyle))
            {
                _settingsFold = EditorGUILayout.Foldout(_settingsFold, "Settings");
                if (_settingsFold)
                {
                    EditorGUILayout.PropertyField(_initializeOnStart, new GUIContent("Initialize On Start"));
                    EditorGUILayout.PropertyField(_defaultTileMaterialProp, new GUIContent("Default Tile Material"));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    
        private void EnsureStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12
                };
            }

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(8, 8, 8, 8),
                    margin  = new RectOffset(0, 0, 0, 0)
                };
            }
        }
    }
}
