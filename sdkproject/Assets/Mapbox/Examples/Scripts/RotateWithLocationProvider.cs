namespace Mapbox.Examples
{
	using Mapbox.Unity.Location;
	using UnityEngine;

	public class RotateWithLocationProvider : MonoBehaviour
	{
		/// <summary>
		/// The rate at which the transform's rotation tries catch up to the provided heading.  
		/// </summary>
		[SerializeField]
		float _rotationFollowFactor;

		/// <summary>
		/// Set this to true if you'd like to adjust the rotation of a RectTransform (in a UI canvas) with the heading.
		/// </summary>
		[SerializeField]
		bool _rotateZ;

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
			if (location.IsHeadingUpdated)
			{
				var euler = Mapbox.Unity.Constants.Math.Vector3Zero;
				if (_rotateZ)
				{
					euler.z = -location.Heading;
				}
				else
				{
					euler.y = location.Heading;
				}

				_targetRotation = Quaternion.Euler(euler);
			}
		}

		void Update()
		{
			transform.localRotation = Quaternion.Lerp(transform.localRotation, _targetRotation, Time.deltaTime * _rotationFollowFactor);
		}
	}
}
