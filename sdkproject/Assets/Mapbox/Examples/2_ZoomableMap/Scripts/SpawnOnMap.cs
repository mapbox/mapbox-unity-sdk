namespace Mapbox.Examples
{
	using UnityEngine;
	using Mapbox.Utils;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.Utilities;
	using System.Collections.Generic;
	using Mapbox.Unity.Ar;

	public class SpawnOnMap : MonoBehaviour
	{
		[SerializeField]
		AbstractMap _map;

		[SerializeField]
		CentralizedARLocator _nodeManager;

		[SerializeField]
		[Geocode]
		string[] _locationStrings;
		Vector2d[] _locations;

		[SerializeField]
		float _spawnScale = 100f;

		[SerializeField]
		GameObject _markerPrefab;

		List<GameObject> _spawnedObjects;

		bool _isMapInitialized = false;
		private void Awake()
		{
			_locations = new Vector2d[_locationStrings.Length];
			_nodeManager.OnAlignmentComplete += OnMapInitialized;
			//_map.OnInitialized += OnMapInitialized;
		}

		void OnMapInitialized()
		{
			_nodeManager.OnAlignmentComplete -= OnMapInitialized;
			_spawnedObjects = new List<GameObject>();
			for (int i = 0; i < _locationStrings.Length; i++)
			{
				var locationString = _locationStrings[i];
				_locations[i] = Conversions.StringToLatLon(locationString);
				var instance = Instantiate(_markerPrefab);
				instance.transform.localPosition = _map.GeoToWorldPosition(_locations[i], true);
				instance.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
				//instance.transform.SetParent(_map.transform);
				_spawnedObjects.Add(instance);
			}
			_isMapInitialized = true;
		}

		private void Update()
		{
			if (_isMapInitialized)
			{
				int count = _spawnedObjects.Count;
				for (int i = 0; i < count; i++)
				{
					var spawnedObject = _spawnedObjects[i];
					var location = _locations[i];
					spawnedObject.transform.localPosition = _map.GeoToWorldPosition(location, true);
					spawnedObject.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
				}
			}

		}
	}
}