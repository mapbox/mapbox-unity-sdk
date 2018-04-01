using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Location;
using Mapbox.Unity.Map;
using UnityEngine.UI;

public class RelocateMapByGPS : MonoBehaviour
{

	[SerializeField]
	AbstractMap _map;

	[SerializeField]
	Button _button;

	private void Start()
	{
		_button.onClick.AddListener(UpdateMapLocation);
	}

	public void UpdateMapLocation()
	{
		var location = LocationProviderFactory.Instance.DefaultLocationProvider.CurrentLocation;
		_map.SetCenterLatitudeLongitude(location.LatitudeLongitude);
		_map.GetComponent<Transform>().position = Camera.main.transform.position;
	}
}
