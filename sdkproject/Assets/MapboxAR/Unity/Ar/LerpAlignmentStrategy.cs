namespace Mapbox.Unity.Ar
{
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.Utils;

	public class LerpAlignmentStrategy : AbstractAlignmentStrategy
	{
		[SerializeField]
		private AbstractMap _map;
		bool _isInitialized;

		/// <summary>
		/// The time taken to move from the start to finish positions
		/// </summary>
		private float timeTakenDuringLerp = 1f;

		//Whether we are currently interpolating or not
		private bool _isLerping;

		//The start and finish positions for the interpolation
		private Vector3 _startPosition;
		private Vector3 _endPosition;

		//The Time.time value when we started the interpolation
		private float _timeStartedLerping;
		private Vector2d _startLatLong;
		private Vector2d _endLatlong;
		private Quaternion _startRotation;
		private Quaternion _endRotation;

		void Awake()
		{
			_map.OnInitialized += () => _isInitialized = true;
		}

		public override void OnAlignmentAvailable(Alignment alignment)
		{
			if (_isInitialized)
			{
				StartLerping(alignment);
			}
		}

		/// <summary>
		/// Called to begin the linear interpolation
		/// </summary>
		void StartLerping(Alignment alignment)
		{
			Debug.Log("Start Lerping");
			_isLerping = true;
			_timeStartedLerping = Time.time;
			timeTakenDuringLerp = Time.fixedDeltaTime;

			//We set the start position to the current position
			_startLatLong = _map.CenterLatitudeLongitude;

			//_endLatlong = _map.WorldToGeoPosition(alignment.Position);
			//_startPosition = _transform.position;
			//_endPosition = alignment.Position;

			// HACK : Snapping the map here. Lerping causes weird behaviour if the value is same.
			_map.transform.position = alignment.Position;

			_startRotation = _transform.rotation;
			_endRotation = Quaternion.Euler(0, alignment.Rotation, 0);
		}

		//We do the actual interpolation in FixedUpdate(), since we're dealing with a rigidbody
		void FixedUpdate()
		{
			if (_isInitialized && _isLerping)
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

				//var position = Vector3.Lerp(_startPosition, _endPosition, percentageComplete);
				var rotation = Quaternion.Lerp(_startRotation, _endRotation, percentageComplete);

				var inversed = Quaternion.Inverse(rotation);
				// Rotate ARoot, but place map on ARPlane.
				_transform.rotation = inversed;

				//_map.transform.position = position;
				//_transform.SetPositionAndRotation(position, rotation);

				//When we've completed the lerp, we set _isLerping to false
				if (percentageComplete >= 1.0f)
				{
					Debug.Log("Lerp Complete");
					_isLerping = false;
					OnAlignmentApplicationComplete();
				}
			}
		}


	}
}
