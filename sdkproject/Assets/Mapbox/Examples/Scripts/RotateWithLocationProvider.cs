namespace Mapbox.Examples
{
	using Mapbox.Unity.Location;
	using UnityEngine;

	public class RotateWithLocationProvider : MonoBehaviour
	{
		/// <summary>
		/// Location property used for rotation: false=Heading (default), true=Orientation  
		/// </summary>
		[SerializeField]
		[Tooltip("Per default 'UserHeading' (direction the device is moving) is used for rotation. Check to use 'DeviceOrientation' (where the device is facing)")]
		bool _useDeviceOrientation;

		/// <summary>
		/// 
		/// </summary>
		[SerializeField]
		[Tooltip("Only evaluated when 'Use Device Orientation' is checked. Subtracts UserHeading from DeviceOrientation. Useful if map is rotated by UserHeading and DeviceOrientation should be displayed relative to the heading.")]
		bool _subtractUserHeading;

		/// <summary>
		/// The rate at which the transform's rotation tries catch up to the provided heading.  
		/// </summary>
		[SerializeField]
		[Tooltip("The rate at which the transform's rotation tries catch up to the provided heading. ")]
		float _rotationFollowFactor = 1;

		/// <summary>
		/// Set this to true if you'd like to adjust the rotation of a RectTransform (in a UI canvas) with the heading.
		/// </summary>
		[SerializeField]
		bool _rotateZ;

		/// <summary>
		/// <para>Set this to true if you'd like to adjust the sign of the rotation angle.</para>
		/// <para>eg angle passed in 63.5, angle that should be used for rotation: -63.5.</para>
		/// <para>This might be needed when rotating the map and not objects on the map.</para>
		/// </summary>
		[SerializeField]
		[Tooltip("Set this to true if you'd like to adjust the sign of the rotation angle. eg angle passed in 63.5, angle that should be used for rotation: -63.5.")]
		bool _useNegativeAngle;

		/// <summary>
		/// Use a mock <see cref="T:Mapbox.Unity.Location.TransformLocationProvider"/>,
		/// rather than a <see cref="T:Mapbox.Unity.Location.EditorLocationProvider"/>.   
		/// </summary>
		[SerializeField]
		bool _useTransformLocationProvider;

		Quaternion _targetRotation;

		/// <summary>
		/// The location provider.
		/// This is public so you change which concrete <see cref="ILocationProvider"/> to use at runtime.  
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

		Vector3 _targetPosition;

		void Start()
		{
			LocationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
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

			float rotationAngle = _useDeviceOrientation ? location.DeviceOrientation : location.UserHeading;

			if (_useNegativeAngle) { rotationAngle *= -1f; }

			// 'Orientation' changes all the time, pass through immediately
			if (_useDeviceOrientation)
			{
				if (_subtractUserHeading)
				{
					if (rotationAngle > location.UserHeading)
					{
						rotationAngle = 360 - (rotationAngle - location.UserHeading);
					}
					else
					{
						rotationAngle = location.UserHeading - rotationAngle + 360;
					}

					if (rotationAngle < 0) { rotationAngle += 360; }
					if (rotationAngle >= 360) { rotationAngle -= 360; }
				}
				_targetRotation = Quaternion.Euler(getNewEulerAngles(rotationAngle));
			}
			else
			{
				// if rotating by 'Heading' only do it if heading has a new value
				if (location.IsUserHeadingUpdated)
				{
					_targetRotation = Quaternion.Euler(getNewEulerAngles(rotationAngle));
				}
			}
		}


		private Vector3 getNewEulerAngles(float newAngle)
		{
			var localRotation = transform.localRotation;
			var currentEuler = localRotation.eulerAngles;
			var euler = Mapbox.Unity.Constants.Math.Vector3Zero;

			if (_rotateZ)
			{
				euler.z = -newAngle;

				euler.x = currentEuler.x;
				euler.y = currentEuler.y;
			}
			else
			{
				euler.y = -newAngle;

				euler.x = currentEuler.x;
				euler.z = currentEuler.z;
			}

			return euler;
		}


		void Update()
		{
			transform.localRotation = Quaternion.Lerp(transform.localRotation, _targetRotation, Time.deltaTime * _rotationFollowFactor);
		}
	}
}
