using System;
using System.Collections;
using Mapbox.Geocoding;
using Mapbox.Scripts.Utilities;
using UnityEngine;

namespace Scripts.Location
{
	public class EditorLocationProvider : MonoBehaviour, ILocationProvider
	{
		[SerializeField]
		string _forwardGeocodeQuery;

		[SerializeField]
		float _latitude;

		[SerializeField]
		float _longitude;

		[SerializeField]
		[Range(0, 359)]
		float _heading;

		ForwardGeocodeResource _resource;

		Coroutine _updateLocationRoutine;

		Vector2 _location;
		public Vector2 Location
		{
			get
			{
				return _location;
			}
		}

		public event EventHandler<HeadingUpdatedEventArgs> OnHeadingUpdated;
		public event EventHandler<LocationUpdatedEventArgs> OnLocationUpdated;

		void Start()
		{
			_resource = new ForwardGeocodeResource("");

			if (!string.IsNullOrEmpty(_forwardGeocodeQuery))
			{
				_resource.Query = _forwardGeocodeQuery;
				MapboxConvenience.Instance.Geocoder.Geocode(_resource, HandleGeocoderResponse);
			}
            else
            {
                _updateLocationRoutine = StartCoroutine(UpdateLocationRoutine());
            }
		}

		void Update()
		{
			if (OnHeadingUpdated != null)
			{
				OnHeadingUpdated(this, new HeadingUpdatedEventArgs() { Heading = _heading });
			}
		}

		void HandleGeocoderResponse(ForwardGeocodeResponse response)
		{
			var geocoords = response.Features[0].Center;
			UpdateLocation((float)geocoords.Latitude, (float)geocoords.Longitude);
		}

		IEnumerator UpdateLocationRoutine()
		{
			while (true)
			{
				_location = new Vector2(_latitude, _longitude);
				SendLocationUpdated();
				yield return new WaitForSeconds(1f);
			}
		}

		[ContextMenu("Update location")]
		public void UpdateLocation(float lat, float lon)
		{
			_latitude = lat;
			_longitude = lon;

			if (_updateLocationRoutine != null)
			{
				StopCoroutine(_updateLocationRoutine);
				_updateLocationRoutine = null;
			}
			_updateLocationRoutine = StartCoroutine(UpdateLocationRoutine());
		}

		void SendLocationUpdated()
		{
			if (OnLocationUpdated != null)
			{
				OnLocationUpdated(this, new LocationUpdatedEventArgs() { Location = _location });
			}
		}
	}
}
