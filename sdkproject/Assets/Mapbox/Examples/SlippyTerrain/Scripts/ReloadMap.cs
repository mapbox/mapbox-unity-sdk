namespace Mapbox.Examples
{
	using Mapbox.Geocoding;
	using UnityEngine.UI;
	using Mapbox.Unity.Map;
	using UnityEngine;

	public class ReloadMap : MonoBehaviour
	{
		[SerializeField]
		AbstractMap _map;

		[SerializeField]
		ForwardGeocodeUserInput _forwardGeocoder;

		[SerializeField]
		Slider _zoomSlider;

		void Awake()
		{
			_forwardGeocoder.OnGeocoderResponse += ForwardGeocoder_OnGeocoderResponse;
			_zoomSlider.onValueChanged.AddListener(Reload);
		}

		void ForwardGeocoder_OnGeocoderResponse(ForwardGeocodeResponse response)
		{
			_map.Initialize(response.Features[0].Center, (int)_zoomSlider.value);
		}

		void Reload(float value)
		{
			_map.Initialize(_map.CenterLatitudeLongitude, (int)value);
		}
	}
}