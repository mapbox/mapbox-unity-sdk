using System.Collections;
using UnityEngine;
using Scripts.Location;
using System;

namespace Location
{
	public class DeviceLocationProvider : MonoBehaviour, ILocationProvider
	{
		Coroutine _pollRoutine;

		float _lastHeading;

		double _lastTimestamp;

		Vector2 _location;
		public Vector2 Location
		{
			get
			{
				return _location;
			}
		}

		public event EventHandler<LocationUpdatedEventArgs> OnLocationUpdated;
		public event EventHandler<HeadingUpdatedEventArgs> OnHeadingUpdated;

		void Start()
		{
			if (_pollRoutine == null)
			{
				_pollRoutine = StartCoroutine(PollLocationRoutine());
			}
		}

		IEnumerator PollLocationRoutine()
		{
			if (!Input.location.isEnabledByUser)
			{
				yield break;
			}
			Input.location.Start(5f, 5f);
			Input.compass.enabled = true;

			int maxWait = 20;
			while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
			{
				yield return new WaitForSeconds(1);
				maxWait--;
			}

			if (maxWait < 1)
			{
				yield break;
			}

			if (Input.location.status == LocationServiceStatus.Failed)
			{
				yield break;
			}

			while (true)
			{
				var heading = Input.compass.trueHeading;
				SendHeadingUpdated(heading);

				var timestamp = Input.location.lastData.timestamp;
				if (Input.location.status == LocationServiceStatus.Running && timestamp > _lastTimestamp)
				{
					_location = new Vector2(Input.location.lastData.latitude, Input.location.lastData.longitude);
					SendLocationUpdated(_location);
					_lastTimestamp = timestamp;
				}
				yield return null;
			}
		}

		void SendHeadingUpdated(float heading)
		{
			if (OnHeadingUpdated != null)
			{
				OnHeadingUpdated(this, new HeadingUpdatedEventArgs() { Heading = heading });
			}
		}

		void SendLocationUpdated(Vector2 location)
		{
			if (OnLocationUpdated != null)
			{
				OnLocationUpdated(this, new LocationUpdatedEventArgs() { Location = location});
			}
		}
	}
}