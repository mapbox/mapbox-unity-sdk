namespace Mapbox.Unity.Ar
{
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Location;
	using Mapbox.Utils;
	using UnityARInterface;
	using UnityEngine;
	using Mapbox.Unity.Utilities;
	using System;

	public class SimpleAutomaticSynchronizationContextBehaviour : MonoBehaviour, ISynchronizationContext
	{
		[SerializeField]
		Transform _arPositionReference;

		[SerializeField]
		AbstractMap _map;

		[SerializeField]
		bool _useAutomaticSynchronizationBias;

		[SerializeField]
		AbstractAlignmentStrategy _alignmentStrategy;

		[SerializeField]
		float _synchronizationBias = 1f;

		[SerializeField]
		float _arTrustRange = 10f;

		[SerializeField]
		float _minimumDeltaDistance = 2f;

		[SerializeField]
		float _minimumDesiredAccuracy = 5f;

		SimpleAutomaticSynchronizationContext _synchronizationContext;

		float _lastHeading;
		float _lastHeight;

		// TODO: move to "base" class SimpleAutomaticSynchronizationContext
		// keep it here for now as map position is also calculated here
		//private KalmanLatLong _kalman = new KalmanLatLong(3); // 3:very fast walking

		ILocationProvider _locationProvider;

		public event Action<Alignment> OnAlignmentAvailable = delegate { };

		public ILocationProvider LocationProvider
		{
			private get
			{
				if (_locationProvider == null)
				{
#if UNITY_EDITOR
					Debug.LogWarningFormat("SimpleAutomaticSynchronizationContextBehaviour, isRemoteConnected:{0}", UnityEditor.EditorApplication.isRemoteConnected);
					if (!UnityEditor.EditorApplication.isRemoteConnected)
					{
						_locationProvider = LocationProviderFactory.Instance.EditorLocationProvider;
					}
					else
					{
						_locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
					}
#else
					_locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
#endif
				}

				return _locationProvider;
			}
			set
			{
				if (_locationProvider != null)
				{
					_locationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;

				}
				_locationProvider = value;
				_locationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
			}
		}


		void Awake()
		{
			_alignmentStrategy.Register(this);
			_synchronizationContext = new SimpleAutomaticSynchronizationContext();
			_synchronizationContext.MinimumDeltaDistance = _minimumDeltaDistance;
			_synchronizationContext.ArTrustRange = _arTrustRange;
			_synchronizationContext.UseAutomaticSynchronizationBias = _useAutomaticSynchronizationBias;
			_synchronizationContext.SynchronizationBias = _synchronizationBias;
			_synchronizationContext.OnAlignmentAvailable += SynchronizationContext_OnAlignmentAvailable;
			_map.OnInitialized += Map_OnInitialized;


			// TODO: not available in ARInterface yet?!
			//UnityARSessionNativeInterface.ARSessionTrackingChangedEvent += UnityARSessionNativeInterface_ARSessionTrackingChanged;
			ARInterface.planeAdded += PlaneAddedHandler;
		}


		void OnDestroy()
		{
			_alignmentStrategy.Unregister(this);
			LocationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;
			ARInterface.planeAdded -= PlaneAddedHandler;
		}


		void Map_OnInitialized()
		{
			_map.OnInitialized -= Map_OnInitialized;

			// We don't want location updates until we have a map, otherwise our conversion will fail.
			LocationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
		}


		void PlaneAddedHandler(BoundedPlane plane)
		{
			_lastHeight = plane.center.y;
		}

		void LocationProvider_OnLocationUpdated(Location location)
		{
			if (location.IsLocationUpdated || location.IsUserHeadingUpdated)
			{
				// With this line, we can control accuracy of Gps updates. 
				// Be aware that we only get location information if it previously met
				// the conditions of DeviceLocationProvider:
				// * desired accuarracy in meters
				// * and update distance in meters
				if (location.Accuracy > _minimumDesiredAccuracy)
				{
					Unity.Utilities.Console.Instance.Log(
						string.Format(
							"Gps update ignored due to bad accuracy: {0:0.0} > {1:0.0}"
							, location.Accuracy
							, _minimumDesiredAccuracy
						)
						, "red"
					);
				}
				else
				{
					var latitudeLongitude = location.LatitudeLongitude;
					Unity.Utilities.Console.Instance.Log(
						string.Format(
							"Location[{0:yyyyMMdd-HHmmss}]: {1},{2}\tAccuracy: {3}\tHeading: {4}"
							, UnixTimestampUtils.From(location.Timestamp)
							, latitudeLongitude.x
							, latitudeLongitude.y
							, location.Accuracy
							, location.UserHeading
						)
						, "lightblue"
					);

					var position = _map.GeoToWorldPosition(latitudeLongitude, false);
					position.y = _map.Root.position.y;
					_synchronizationContext.AddSynchronizationNodes(location, position, _arPositionReference.localPosition);
				}
			}
		}


		void SynchronizationContext_OnAlignmentAvailable(Ar.Alignment alignment)
		{
			var position = alignment.Position;
			position.y = _lastHeight;
			alignment.Position = position;
			OnAlignmentAvailable(alignment);
		}
	}
}
