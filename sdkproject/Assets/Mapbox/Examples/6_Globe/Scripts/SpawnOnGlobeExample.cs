namespace Mapbox.Examples
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.Utilities;

	public class SpawnOnGlobeExample : MonoBehaviour
	{
		[SerializeField]
		FlatSphereTerrainFactory _globeFactory;

		[SerializeField]
		[Geocode]
		string[] _locations;

		[SerializeField]
		float _spawnScale = 100f;

		[SerializeField]
		GameObject _markerPrefab;

		void Start()
		{
			foreach (var locationString in _locations)
			{
				var instance = Instantiate(_markerPrefab);
				var location = Conversions.StringToLatLon(locationString);
				instance.transform.position = Conversions.GeoToWorldGlobePosition(location, _globeFactory.Radius);
				instance.transform.localScale = Vector3.one * _spawnScale;
				instance.transform.SetParent(transform);
			}
		}
	}
}