// HACK:
// This will work out of the box, but it's intended to be an example of how to approach
// procedural decoration like this.
// A better approach would be to operate on the geometry itself.

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Components;
	using UnityEngine;
	using System.Collections.Generic;
	using System;

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

		private Dictionary<GameObject, List<GameObject>> _objects;
		private Queue<GameObject> _pool;

		public override void Initialize()
		{
			if (_objects == null || _pool == null)
			{
				_objects = new Dictionary<GameObject, List<GameObject>>();
				_pool = new Queue<GameObject>();
			}
		}

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			_spawnedCount = 0;
			var collider = ve.GameObject.GetComponent<Collider>();
			var bounds = collider.bounds;
			var center = bounds.center;
			center.y = 0;

			var area = (int)(bounds.size.x * bounds.size.z);
			int spawnCount = Mathf.Min(area / _spawnRateInSquareMeters, _maxSpawn);
			while (_spawnedCount < spawnCount)
			{
				var x = UnityEngine.Random.Range(-bounds.extents.x, bounds.extents.x);
				var z = UnityEngine.Random.Range(-bounds.extents.z, bounds.extents.z);
				var ray = new Ray(bounds.center + new Vector3(x, 100, z), Vector3.down * 2000);

				RaycastHit hit;
				//Debug.DrawRay(ray.origin, ray.direction * 1000, Color.yellow, 1000);
				if (Physics.Raycast(ray, out hit, 150, _layerMask))
				{
					//Debug.DrawLine(ray.origin, hit.point, Color.red, 1000);
					var index = UnityEngine.Random.Range(0, _prefabs.Length);
					var transform = GetObject(index, ve.GameObject).transform;
					transform.position = hit.point;
					if (_randomizeRotation)
					{
						transform.localEulerAngles = new Vector3(0, UnityEngine.Random.Range(-180f, 180f), 0);
					}
					if (!_scaleDownWithWorld)
					{
						transform.localScale = Vector3.one / tile.TileScale;
					}

					if (_randomizeScale)
					{
						var scale = transform.localScale;
						var y = UnityEngine.Random.Range(scale.y * .7f, scale.y * 1.3f);
						scale.y = y;
						transform.localScale = scale;
					}

				}
				_spawnedCount++;
			}
		}

		public override void OnPoolItem(VectorEntity vectorEntity)
		{
			if(_objects.ContainsKey(vectorEntity.GameObject))
			{
				foreach (var item in _objects[vectorEntity.GameObject])
				{
					item.SetActive(false);
					_pool.Enqueue(item);
				}

				_objects[vectorEntity.GameObject].Clear();
				_objects.Remove(vectorEntity.GameObject);
			}
		}

		public override void ClearCaches()
		{
			foreach (var go in _pool)
			{
				Destroy(go);
			}
			_pool.Clear();
			foreach (var tileObject in _objects)
			{
				foreach (var go in tileObject.Value)
				{
					Destroy(go);
				}
			}
			_objects.Clear();
		}

		private GameObject GetObject(int index, GameObject go)
		{
			GameObject ob;
			if (_pool.Count > 0)
			{
				ob = _pool.Dequeue();
				ob.SetActive(true);
				ob.transform.SetParent(go.transform);
			}
			else
			{
				ob = ((GameObject)Instantiate(_prefabs[index], go.transform, false));
			}
			if (_objects.ContainsKey(go))
			{
				_objects[go].Add(ob);
			}
			else
			{
				_objects.Add(go, new List<GameObject>() { ob });
			}
			return ob;
		}
	}
}
