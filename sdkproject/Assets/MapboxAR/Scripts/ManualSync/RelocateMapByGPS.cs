namespace Mapbox.Examples
{
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

		[SerializeField]
		Transform _mapTransform;

		private void Start()
		{
			_button.onClick.AddListener(UpdateMapLocation);
		}

		private void UpdateMapLocation()
		{
			var location = LocationProviderFactory.Instance.DefaultLocationProvider.CurrentLocation;
			_map.UpdateMap(location.LatitudeLongitude,_map.AbsoluteZoom);
			var playerPos = Camera.main.transform.position;
			_mapTransform.position = new Vector3(playerPos.x, _mapTransform.position.y, playerPos.z);
		}
	}
}
