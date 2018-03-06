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
		private KalmanLatLong _kalman = new KalmanLatLong(3); // 3:very fast walking

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
						_locationProvider = LocationProviderFactory.Instance.TransformLocationProvider;
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

		void Start()
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
			//Unity.Utilities.Console.Instance.Log(string.Format("AR Plane Height: {0}", _lastHeight), "yellow");
		}

		//void UnityARSessionNativeInterface_ARSessionTrackingChanged(UnityEngine.XR.iOS.UnityARCamera camera)
		//{
		//	Unity.Utilities.Console.Instance.Log(string.Format("AR Tracking State Changed: {0}: {1}", camera.trackingState, camera.trackingReason), "silver");
		//}

		void LocationProvider_OnLocationUpdated(Location location)
		{
			string gpsLog = string.Format(
				"{1:yyyyMMdd HHmmss} {8:0.00000} / {9:0.00000}{0}locationUpdated:{2} headingUpdated:{3}{0}GPS accuracy:{4:0.0}{0}heading(truenorth):{5:0.0}{0}heading(magnetic):{6:0.0}{0}heading accuracy:{7:0.0}"
				, Environment.NewLine
				, UnixTimestampUtils.From(location.Timestamp)
				, location.IsLocationUpdated
				, location.IsHeadingUpdated
				, location.Accuracy
				, location.Heading
				, location.HeadingMagnetic
				, location.HeadingAccuracy
				, location.LatitudeLongitude.x
				, location.LatitudeLongitude.y
			);
			Unity.Utilities.Console.Instance.LogGps(gpsLog);

			if (location.IsLocationUpdated || location.IsHeadingUpdated)
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
					_kalman.Process(
						location.LatitudeLongitude.x
						, location.LatitudeLongitude.y
						, location.Accuracy
						, (long)location.Timestamp
					);
					location.LatitudeLongitude.x = _kalman.Lat;
					location.LatitudeLongitude.y = _kalman.Lng;
					location.Accuracy = (int)_kalman.Accuracy;

					var latitudeLongitude = location.LatitudeLongitude;
					Unity.Utilities.Console.Instance.Log(
						string.Format(
							"Location: {0},{1}\tAccuracy: {2}\tHeading: {3}"
							, latitudeLongitude.x
							, latitudeLongitude.y
							, location.Accuracy, location.Heading
						)
						, "lightblue"
					);

					var position = Conversions.GeoToWorldPosition(latitudeLongitude, _map.CenterMercator, _map.WorldRelativeScale).ToVector3xz();
					_synchronizationContext.AddSynchronizationNodes(location, position, _arPositionReference.localPosition);

					string positionLog = string.Format(
						"{1:yyyyMMdd HHmmss} {2}{0}hdop:{3} hding:{4}{0}centerMerc:{5}{0}relWorldScale:{6}{0}pos:{7}{0}arPosRef:{8}"
						, Environment.NewLine
						, UnixTimestampUtils.From(location.Timestamp)
						, latitudeLongitude
						, location.Accuracy
						, location.Heading
						, _map.CenterMercator
						, _map.WorldRelativeScale
						, position
						, _arPositionReference.localPosition
					);
					Unity.Utilities.Console.Instance.LogPosition(positionLog);
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
