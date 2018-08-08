namespace Mapbox.Unity.Ar
{
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Location;
	using UnityARInterface;
	using UnityEngine;
	using Mapbox.Unity.Utilities;
	using System;

	public class ManualSynchronizationContextBehaviour : MonoBehaviour, ISynchronizationContext
	{
		[SerializeField]
		AbstractMap _map;

		[SerializeField]
		Transform _mapCamera;

		[SerializeField]
		TransformLocationProvider _locationProvider;

		[SerializeField]
		AbstractAlignmentStrategy _alignmentStrategy;

		float _lastHeight;
		float _lastHeading = 0;

		public event Action<Alignment> OnAlignmentAvailable = delegate { };

		void Start()
		{
			_alignmentStrategy.Register(this);
			_map.OnInitialized += Map_OnInitialized;
			ARInterface.planeAdded += PlaneAddedHandler;
		}

		void OnDestroy()
		{
			_alignmentStrategy.Unregister(this);
			_locationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;
			ARInterface.planeAdded -= PlaneAddedHandler;
		}

		void Map_OnInitialized()
		{
			_map.OnInitialized -= Map_OnInitialized;
			_locationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
		}

		void LocationProvider_OnLocationUpdated(Location location)
		{
			if (location.IsLocationUpdated)
			{
				var alignment = new Alignment();
				var originalPosition = _map.Root.position;
				alignment.Rotation = -location.UserHeading + _map.Root.localEulerAngles.y;

				// Rotate our offset by the last heading.
				var rotation = Quaternion.Euler(0, -_lastHeading, 0);
				alignment.Position = rotation * (-Conversions.GeoToWorldPosition(location.LatitudeLongitude,
																				 _map.CenterMercator,
																				 _map.WorldRelativeScale).ToVector3xz() + originalPosition);
				alignment.Position.y = _lastHeight;

				OnAlignmentAvailable(alignment);

				// Reset camera to avoid confusion.
				var mapCameraPosition = Vector3.zero;
				mapCameraPosition.y = _mapCamera.localPosition.y;
				var mapCameraRotation = Vector3.zero;
				mapCameraRotation.x = _mapCamera.localEulerAngles.x;
				_mapCamera.localPosition = mapCameraPosition;
				_mapCamera.eulerAngles = mapCameraRotation;
			}
		}

		void PlaneAddedHandler(BoundedPlane plane)
		{
			_lastHeight = plane.center.y;
			Unity.Utilities.Console.Instance.Log(string.Format("AR Plane Height: {0}", _lastHeight), "yellow");
		}
	}
}
