using UnityEngine;

namespace Lean.Touch
{
	// This script can be used to spawn a GameObject via an event
	public class LeanSpawn : MonoBehaviour
	{
		[Tooltip("The prefab that gets spawned")]
		public Transform Prefab;

		[Tooltip("The distance from the finger the prefab will be spawned in world space")]
		public float Distance = 10.0f;

		[Tooltip("If spawning with velocity, rotate to it?")]
		public bool RotateToVelocity;

		[Tooltip("If spawning with velocity, scale it?")]
		public float VelocityMultiplier = 1.0f;

		public void SpawnWithVelocity(Vector3 start, Vector3 end)
		{
			if (Prefab != null)
			{
				// Vector between points
				var direction = end - start;

				// Angle between points
				var angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

				// Spawn and set transform
				var instance = Instantiate(Prefab);

				instance.position = start;
				instance.rotation = Quaternion.Euler(0.0f, 0.0f, -angle);

				// Apply 3D force?
				var rigidbody3D = instance.GetComponent<Rigidbody>();

				if (rigidbody3D != null)
				{
					rigidbody3D.velocity = direction * VelocityMultiplier;
				}

				// Apply 2D force?
				var rigidbody2D = instance.GetComponent<Rigidbody2D>();

				if (rigidbody2D != null)
				{
					rigidbody2D.velocity = direction * VelocityMultiplier;
				}
			}
		}

		public void Spawn()
		{
			if (Prefab != null)
			{
				Instantiate(Prefab, transform.position, transform.rotation);
			}
		}

		public void Spawn(LeanFinger finger)
		{
			if (Prefab != null && finger != null)
			{
				Instantiate(Prefab, finger.GetWorldPosition(Distance), transform.rotation);
			}
		}
	}
}