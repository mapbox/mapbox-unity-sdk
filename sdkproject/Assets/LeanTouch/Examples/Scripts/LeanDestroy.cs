using UnityEngine;

namespace Lean.Touch
{
	// This script will automatically destroy this GameObject after 'Seconds' seconds
	// If you want to manually destroy this GameObject, then disable this component, and call DestroyNow
	public class LeanDestroy : MonoBehaviour
	{
		[Tooltip("The amount of seconds remaining before this GameObject gets destroyed")]
		public float Seconds = 1.0f;

		protected virtual void Update()
		{
			Seconds -= Time.deltaTime;

			if (Seconds <= 0.0f)
			{
				DestroyNow();
			}
		}

		public void DestroyNow()
		{
			Destroy(gameObject);
		}
	}
}