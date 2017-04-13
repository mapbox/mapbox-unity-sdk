using Mapbox.Geocoding;
using Mapbox.Scripts.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Mapbox;
using System.Linq;

namespace Scripts.Location
{
	public class LocationLogger : MonoBehaviour
	{
        [SerializeField]
        Text _locationText;

        [SerializeField]
		Text _latitudeText;

        Geocoder _geocoder;
        bool _hasResponse;
        GeoCoordinate _coordinate;
        private ReverseGeocodeResource _resource;
        private float _timerMax = 1;
        private float _timer;

        ILocationProvider _locationProvider;
		public ILocationProvider LocationProvider
		{
			private get
			{
				if (_locationProvider == null)
				{
					_locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
				}

				return _locationProvider;
			}
			set
			{
				if (_locationProvider != null)
				{
					_locationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;
					_locationProvider.OnHeadingUpdated -= LocationProvider_OnHeadingUpdated;
				}
				_locationProvider = value;
				_locationProvider.OnHeadingUpdated += LocationProvider_OnHeadingUpdated;
				_locationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
			}
		}

		void Start()
		{
			LocationProvider.OnHeadingUpdated += LocationProvider_OnHeadingUpdated;
			LocationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
            _geocoder = MapboxConvenience.Instance.Geocoder;
            _timer = _timerMax;
            //gameObject.SetActive(false);
        }

		void OnDestroy()
		{
			if (LocationProvider != null)
			{
				LocationProvider.OnHeadingUpdated -= LocationProvider_OnHeadingUpdated;
				LocationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;
			}
		}

		void LocationProvider_OnHeadingUpdated(object sender, HeadingUpdatedEventArgs e)
		{
			//_headingText.text = "Heading: " + e.Heading;
		}

		void LocationProvider_OnLocationUpdated(object sender, LocationUpdatedEventArgs e)
		{
			_latitudeText.text = e.Location.x.ToString("F2") +  " / " + e.Location.y.ToString("F2");

            if (_timer <= 0)
            {
                _coordinate.Latitude = e.Location.x;
                _coordinate.Longitude = e.Location.y;
                if (_resource == null)
                    _resource = new ReverseGeocodeResource(_coordinate);

                _resource.Query = _coordinate;
                _geocoder.Geocode(_resource, HandleGeocoderResponse);
                _timer = _timerMax;
            }
            _timer -= Time.deltaTime;
        }
        
        void HandleGeocoderResponse(ForwardGeocodeResponse res)
        {
            if(res.Features.Any())
                _locationText.text = res.Features[0].Text;
        }

    }
}
