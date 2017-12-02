namespace Mapbox.Examples
{
	using UnityEngine;
	using Mapbox.Utils;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.Utilities;
	using System.Collections.Generic;

	public class SpawnOnMap : MonoBehaviour
	{
		[SerializeField]
		AbstractMap _map;

		[SerializeField]
		Vector2d[] _locations;

		[SerializeField]
		float _spawnScale = 100f;

		List<GameObject> _spawnedObjects;

		void Start()
		{
			_spawnedObjects = new List<GameObject>();
			foreach (var location in _locations)
			{
				var instance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				instance.transform.localPosition = _map.GeoToWorldPosition(location);
				instance.transform.localScale = Vector3.one * _spawnScale;
				_spawnedObjects.Add(instance);
			}
		}

		private void Update()
		{
			int count = _spawnedObjects.Count;
			for (int i = 0; i < count; i++)
			{
				var spawnedObject = _spawnedObjects[i];
				var location = _locations[i];
				spawnedObject.transform.localPosition = _map.GeoToWorldPosition(location);
			}
		}
	}
}