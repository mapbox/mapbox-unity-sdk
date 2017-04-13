// HACK: Does this belong here?
#if !UNITY_EDITOR
#define NOT_UNITY_EDITOR
#endif

using System.Diagnostics;
using Mapbox.Scripts.Utilities;
using Location;
using UnityEngine;

namespace Scripts.Location
{
	public class LocationProviderFactory : SingletonBehaviour<LocationProviderFactory>
	{
		[SerializeField]
		DeviceLocationProvider _deviceLocationProvider;

		[SerializeField]
		EditorLocationProvider _editorLocationProvider;

		[SerializeField]
		TransformLocationProvider _transformLocationProvider;

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

		public override void Awake()
		{
			base.Awake();
			InjectEditorLocationProvider();
			InjectDeviceLocationProvider();
		}

		[Conditional("UNITY_EDITOR")]
		void InjectEditorLocationProvider()
		{
			UnityEngine.Debug.Log("LocationProviderFactory: " + "Injected EDITOR Location Provider");
			_defaultLocationProvider = _editorLocationProvider;
		}

		[Conditional("NOT_UNITY_EDITOR")]
		void InjectDeviceLocationProvider()
		{
			UnityEngine.Debug.Log("LocationProviderFactory: " + "Injected DEVICE Location Provider");
			_defaultLocationProvider = _deviceLocationProvider;
		}
	}
}
