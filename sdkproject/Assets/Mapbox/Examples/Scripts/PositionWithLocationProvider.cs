namespace Mapbox.Examples
{
	using Mapbox.Unity.Location;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.Map;
	using UnityEngine;
	using Mapbox.Utils;

	public class PositionWithLocationProvider : MonoBehaviour
	{
		[SerializeField]
		private AbstractMap _map;

		/// <summary>
		/// The rate at which the transform's position tries catch up to the provided location.
		/// </summary>
		[SerializeField]
		float _positionFollowFactor;

		/// <summary>
		/// Use a mock <see cref="T:Mapbox.Unity.Location.TransformLocationProvider"/>,
		/// rather than a <see cref="T:Mapbox.Unity.Location.EditorLocationProvider"/>. 
		/// </summary>
		[SerializeField]
		bool _useTransformLocationProvider;

		bool _isInitialized;
		/// <summary>
		/// The time taken to move from the start to finish positions
		/// </summary>
		public float timeTakenDuringLerp = 1f;

		//Whether we are currently interpolating or not
		private bool _isLerping;

		//The start and finish positions for the interpolation
		private Vector3 _startPosition;
		private Vector3 _endPosition;

		//The Time.time value when we started the interpolation
		private float _timeStartedLerping;
		private Vector2d _startLatLong;
		private Vector2d _endLatlong;

		/// <summary>
		/// The location provider.
		/// This is public so you change which concrete <see cref="T:Mapbox.Unity.Location.ILocationProvider"/> to use at runtime.
		/// </summary>
		ILocationProvider _locationProvider;
		public ILocationProvider LocationProvider
		{
			private get
			{
				if (_locationProvider == null)
				{
					_locationProvider = _useTransformLocationProvider ?
						LocationProviderFactory.Instance.TransformLocationProvider : LocationProviderFactory.Instance.DefaultLocationProvider;
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
			LocationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
			_map.OnInitialized += () => _isInitialized = true;
		}

		void OnDestroy()
		{
			if (LocationProvider != null)
			{
				LocationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;
			}
		}

		void LocationProvider_OnLocationUpdated(Location location)
		{
			if (_isInitialized && location.IsLocationUpdated)
			{
				StartLerping(location);
			}
		}
		/// <summary>
		/// Called to begin the linear interpolation
		/// </summary>
		void StartLerping(Unity.Location.Location location)
		{
			_isLerping = true;
			_timeStartedLerping = Time.time;

			//We set the start position to the current position
			_startLatLong = _map.CenterLatitudeLongitude;
			_endLatlong = location.LatitudeLongitude;
			_startPosition = transform.position;
			_endPosition = _map.GeoToWorldPosition(_endLatlong, false);
		}

		//We do the actual interpolation in FixedUpdate(), since we're dealing with a rigidbody
		void FixedUpdate()
		{
			if (_isLerping)
			{
				//We want percentage = 0.0 when Time.time = _timeStartedLerping
				//and percentage = 1.0 when Time.time = _timeStartedLerping + timeTakenDuringLerp
				//In other words, we want to know what percentage of "timeTakenDuringLerp" the value
				//"Time.time - _timeStartedLerping" is.
				float timeSinceStarted = Time.time - _timeStartedLerping;
				float percentageComplete = timeSinceStarted / timeTakenDuringLerp;

				//Perform the actual lerping.  Notice that the first two parameters will always be the same
				//throughout a single lerp-processs (ie. they won't change until we hit the space-bar again
				//to start another lerp)
				transform.position = Vector3.Lerp(_startPosition, _endPosition, percentageComplete);

				//When we've completed the lerp, we set _isLerping to false
				if (percentageComplete >= 1.0f)
				{
					_isLerping = false;
				}
			}
		}
	}
}
