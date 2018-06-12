#if !UNITY_EDITOR
#define NOT_UNITY_EDITOR
#endif

namespace Mapbox.Unity.Location
{
	using UnityEngine;
	using Mapbox.Unity.Map;
	using System.Text.RegularExpressions;

	/// <summary>
	/// Singleton factory to allow easy access to various LocationProviders.
	/// This is meant to be attached to a game object.
	/// </summary>
	public class LocationProviderFactory : MonoBehaviour
	{
		[SerializeField]
		public AbstractMap mapManager;

		[SerializeField]
		[Tooltip("Provider using Unity's builtin 'Input.Location' service")]
		AbstractLocationProvider _deviceLocationProviderUnity;

		[SerializeField]
		[Tooltip("Custom native Android location provider. If this is not set above provider is used")]
		DeviceLocationProviderAndroidNative _deviceLocationProviderAndroid;

		[SerializeField]
		AbstractLocationProvider _editorLocationProvider;

		[SerializeField]
		AbstractLocationProvider _transformLocationProvider;

		[SerializeField]
		bool _dontDestroyOnLoad;


		/// <summary>
		/// The singleton instance of this factory.
		/// </summary>
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

		/// <summary>
		/// The default location provider. 
		/// Outside of the editor, this will be a <see cref="T:Mapbox.Unity.Location.DeviceLocationProvider"/>.
		/// In the Unity editor, this will be an <see cref="T:Mapbox.Unity.Location.EditorLocationProvider"/>
		/// </summary>
		/// <example>
		/// Fetch location to set a transform's position:
		/// <code>
		/// void Update()
		/// {
		///     var locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
		///     transform.position = Conversions.GeoToWorldPosition(locationProvider.Location,
		///                                                         MapController.ReferenceTileRect.Center,
		///                                                         MapController.WorldScaleFactor).ToVector3xz();
		/// }
		/// </code>
		/// </example>
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

		/// <summary>
		/// Returns the serialized <see cref="T:Mapbox.Unity.Location.TransformLocationProvider"/>.
		/// </summary>
		public ILocationProvider TransformLocationProvider
		{
			get
			{
				return _transformLocationProvider;
			}
		}

		/// <summary>
		/// Returns the serialized <see cref="T:Mapbox.Unity.Location.EditorLocationProvider"/>.
		/// </summary>
		public ILocationProvider EditorLocationProvider
		{
			get
			{
				return _editorLocationProvider;
			}
		}

		/// <summary>
		/// Returns the serialized <see cref="T:Mapbox.Unity.Location.DeviceLocationProvider"/>
		/// </summary>
		public ILocationProvider DeviceLocationProvider
		{
			get
			{
				return _deviceLocationProviderUnity;
			}
		}

		/// <summary>
		/// Create singleton instance and inject the DefaultLocationProvider upon initialization of this component. 
		/// </summary>
		protected virtual void Awake()
		{
			if (Instance != null)
			{
				DestroyImmediate(gameObject);
				return;
			}
			Instance = this;

			if (_dontDestroyOnLoad)
			{
				DontDestroyOnLoad(gameObject);
			}

			InjectEditorLocationProvider();
			InjectDeviceLocationProvider();
		}

		/// <summary>
		/// Injects the editor location provider.
		/// Depending on the platform, this method and calls to it will be stripped during compile.
		/// </summary>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		void InjectEditorLocationProvider()
		{
			Debug.LogFormat("LocationProviderFactory: Injected EDITOR Location Provider - {0}", _editorLocationProvider.GetType());
			DefaultLocationProvider = _editorLocationProvider;
		}

		/// <summary>
		/// Injects the device location provider.
		/// Depending on the platform, this method and calls to it will be stripped during compile.
		/// </summary>
		[System.Diagnostics.Conditional("NOT_UNITY_EDITOR")]
		void InjectDeviceLocationProvider()
		{
			int AndroidApiVersion = 0;
			var regex = new Regex(@"(?<=API-)-?\d+");
			Match match = regex.Match(SystemInfo.operatingSystem); // eg 'Android OS 8.1.0 / API-27 (OPM2.171019.029/4657601)'
			if (match.Success) { int.TryParse(match.Groups[0].Value, out AndroidApiVersion); }
			Debug.LogFormat("{0} => API version: {1}", SystemInfo.operatingSystem, AndroidApiVersion);

			// only inject native provider if platform requirement is met
			// and script itself as well as parent game object are active
			if (Application.platform == RuntimePlatform.Android
				&& null != _deviceLocationProviderAndroid
				&& _deviceLocationProviderAndroid.enabled
				&& _deviceLocationProviderAndroid.transform.gameObject.activeInHierarchy
				// API version 24 => Android 7 (Nougat): we are using GnssStatus 'https://developer.android.com/reference/android/location/GnssStatus.html'
				// in the native plugin.
				// GnssStatus is not available with versions lower than 24
				&& AndroidApiVersion >= 24
			)
			{
				Debug.LogFormat("LocationProviderFactory: Injected native Android DEVICE Location Provider - {0}", _deviceLocationProviderAndroid.GetType());
				DefaultLocationProvider = _deviceLocationProviderAndroid;
			}
			else
			{
				Debug.LogFormat("LocationProviderFactory: Injected DEVICE Location Provider - {0}", _deviceLocationProviderUnity.GetType());
				DefaultLocationProvider = _deviceLocationProviderUnity;
			}
		}
	}
}
