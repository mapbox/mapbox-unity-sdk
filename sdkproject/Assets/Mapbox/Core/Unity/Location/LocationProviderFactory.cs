#if !UNITY_EDITOR
#define NOT_UNITY_EDITOR
#endif

namespace Mapbox.Unity.Location
{
    using System.Diagnostics;
    using UnityEngine;

    public class LocationProviderFactory : MonoBehaviour
    {
        [SerializeField]
        DeviceLocationProvider _deviceLocationProvider;

        [SerializeField]
        EditorLocationProvider _editorLocationProvider;

        [SerializeField]
        TransformLocationProvider _transformLocationProvider;

        private static LocationProviderFactory _instance;
        public static LocationProviderFactory Instance
        {
            get
            {
                return _instance;
            }

            private set
            {
                _instance = value;
            }
        }

        ILocationProvider _defaultLocationProvider;
        public ILocationProvider DefaultLocationProvider
        {
            get
            {
                return _defaultLocationProvider;
            }
            set
            {
                _defaultLocationProvider = value;
            }
        }

        public TransformLocationProvider TransformLocationProvider
        {
            get
            {
                return _transformLocationProvider;
            }
        }

        public EditorLocationProvider EditorLocationProvider
        {
            get
            {
                return _editorLocationProvider;
            }
        }

        public DeviceLocationProvider DeviceLocationProvider
        {
            get
            {
                return _deviceLocationProvider;
            }
        }

        private void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InjectEditorLocationProvider();
            InjectDeviceLocationProvider();
        }

        [Conditional("UNITY_EDITOR")]
        void InjectEditorLocationProvider()
        {
            UnityEngine.Debug.Log("LocationProviderFactory: " + "Injected EDITOR Location Provider");
            DefaultLocationProvider = _editorLocationProvider;
        }

        [Conditional("NOT_UNITY_EDITOR")]
        void InjectDeviceLocationProvider()
        {
            UnityEngine.Debug.Log("LocationProviderFactory: " + "Injected DEVICE Location Provider");
            DefaultLocationProvider = _deviceLocationProvider;
        }
    }
}
