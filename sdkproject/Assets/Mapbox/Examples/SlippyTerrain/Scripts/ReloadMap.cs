namespace Mapbox.Examples
{
	using Mapbox.Geocoding;
	using UnityEngine.UI;
	using Mapbox.Unity.Map;
	using UnityEngine;

	public class ReloadMap : MonoBehaviour
	{
		Camera _camera;
		Vector3 _cameraStartPos;
		AbstractMap _map;

		[SerializeField]
		ForwardGeocodeUserInput _forwardGeocoder;

		[SerializeField]
		Slider _zoomSlider;

		void Awake()
		{
			_camera = Camera.main;
			_cameraStartPos = _camera.transform.position;
			_map = FindObjectOfType<AbstractMap>();
			_forwardGeocoder.OnGeocoderResponse += ForwardGeocoder_OnGeocoderResponse;
			_zoomSlider.onValueChanged.AddListener(Reload);
		}

		void ForwardGeocoder_OnGeocoderResponse(ForwardGeocodeResponse response)
		{
			_camera.transform.position = _cameraStartPos;
			_map.Initialize(response.Features[0].Center, (int)_zoomSlider.value);
		}

		void Reload(float value)
		{
			_camera.transform.position = _cameraStartPos;
			_map.Initialize(_map.CenterLatitudeLongitude, (int)value);
		}
	}
}