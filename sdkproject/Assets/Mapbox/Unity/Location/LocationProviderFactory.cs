#if !UNITY_EDITOR
#define NOT_UNITY_EDITOR
#endif

namespace Mapbox.Unity.Location
{
	using System.Diagnostics;
	using UnityEngine;
	using Mapbox.Unity.Map;

	/// <summary>
	/// Singleton factory to allow easy access to various LocationProviders.
	/// This is meant to be attached to a game object.
	/// </summary>
	public class LocationProviderFactory : MonoBehaviour
	{
		[SerializeField]
		public AbstractMap mapManager;

		[SerializeField]
		AbstractLocationProvider _deviceLocationProvider;

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
				return _deviceLocationProvider;
			}
		}

		/// <summary>
		/// Create singleton instance and inject the DefaultLocationProvider upon initialization of this component. 
		/// </summary>
		private void Awake()
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
		[Conditional("UNITY_EDITOR")]
		void InjectEditorLocationProvider()
		{
			UnityEngine.Debug.Log("LocationProviderFactory: " + "Injected EDITOR Location Provider");
			DefaultLocationProvider = _editorLocationProvider;
		}

		/// <summary>
		/// Injects the device location provider.
		/// Depending on the platform, this method and calls to it will be stripped during compile.
		/// </summary>
		[Conditional("NOT_UNITY_EDITOR")]
		void InjectDeviceLocationProvider()
		{
			UnityEngine.Debug.Log("LocationProviderFactory: " + "Injected DEVICE Location Provider");
			DefaultLocationProvider = _deviceLocationProvider;
		}
	}
}
