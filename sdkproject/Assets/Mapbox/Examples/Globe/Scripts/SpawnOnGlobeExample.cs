namespace Mapbox.Examples
{
	using UnityEngine;
	using Mapbox.Utils;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.Utilities;

	public class SpawnOnGlobeExample : MonoBehaviour
	{
		[SerializeField]
		FlatSphereTerrainFactory _globeFactory;

		[SerializeField]
		Vector2d[] _locations;

		[SerializeField]
		float _spawnScale = 100f;

		void Start()
		{
			foreach (var location in _locations)
			{
				var instance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				instance.transform.position = Conversions.GeoToWorldGlobePosition(location, _globeFactory.Radius);
				instance.transform.localScale = Vector3.one * _spawnScale;
				instance.transform.SetParent(transform);
			}
		}
	}
}