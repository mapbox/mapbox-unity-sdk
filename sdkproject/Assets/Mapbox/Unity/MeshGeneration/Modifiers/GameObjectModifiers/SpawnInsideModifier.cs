// HACK:
// This will work out of the box, but it's intended to be an example of how to approach
// procedural decoration like this.
// A better approach would be to operate on the geometry itself.

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using Mapbox.Unity.MeshGeneration.Components;
	using UnityEngine;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Spawn Inside Modifier")]
	public class SpawnInsideModifier : GameObjectModifier
	{
		[SerializeField]
		int _spawnRateInSquareMeters;

		[SerializeField]
		int _maxSpawn = 1000;

		[SerializeField]
		GameObject[] _prefabs;

		[SerializeField]
		LayerMask _layerMask;

		[SerializeField]
		bool _scaleDownWithWorld;

		[SerializeField]
		bool _randomizeScale;

		[SerializeField]
		bool _randomizeRotation;

		int _spawnedCount;

		public override void Run(FeatureBehaviour fb)
		{
			_spawnedCount = 0;
			var collider = fb.GetComponent<Collider>();
			var bounds = collider.bounds;
			var center = bounds.center;
			center.y = 0;

			var area = (int)(bounds.size.x * bounds.size.z);
			int spawnCount = Mathf.Min(area / _spawnRateInSquareMeters, _maxSpawn);
			while (_spawnedCount < spawnCount)
			{
				var x = Random.Range(-bounds.extents.x, bounds.extents.x);
				var z = Random.Range(-bounds.extents.z, bounds.extents.z);
				var ray = new Ray(bounds.center + new Vector3(x, 100, z), Vector3.down * 2000);

				RaycastHit hit;
				//Debug.DrawRay(ray.origin, ray.direction * 1000, Color.yellow, 1000);
				if (Physics.Raycast(ray, out hit, 150, _layerMask))
				{
					//Debug.DrawLine(ray.origin, hit.point, Color.red, 1000);
					var index = Random.Range(0, _prefabs.Length);
					var transform = ((GameObject)Instantiate(_prefabs[index], fb.transform, false)).transform;
					transform.position = hit.point;
					if (_randomizeRotation)
					{
						transform.localEulerAngles = new Vector3(0, Random.Range(-180f, 180f), 0);
					}
					if (!_scaleDownWithWorld)
					{
						transform.localScale = Vector3.one / transform.lossyScale.x;
					}

					if (_randomizeScale)
					{
						var scale = transform.localScale;
						var y = Random.Range(scale.y * .7f, scale.y * 1.3f);
						scale.y = y;
						transform.localScale = scale;
					}

					_spawnedCount++;
				}
			}
		}
	}
}
