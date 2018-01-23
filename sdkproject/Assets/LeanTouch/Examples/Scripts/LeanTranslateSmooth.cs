using UnityEngine;

namespace Lean.Touch
{
	// This script allows you to transform the current GameObject with smoothing
	public class LeanTranslateSmooth : LeanTranslate
	{
		[Tooltip("How smoothly this object moves to its target position")]
		public float Dampening = 10.0f;

		// The position we still need to add
		[HideInInspector]
		public Vector3 RemainingDelta;

		protected virtual void LateUpdate()
		{
			// Get t value
			var factor = LeanTouch.GetDampenFactor(Dampening, Time.deltaTime);

			// Dampen remainingDelta
			var newDelta = Vector3.Lerp(RemainingDelta, Vector3.zero, factor);

			// Shift this transform by the change in delta
			transform.position += RemainingDelta - newDelta;

			// Update remainingDelta with the dampened value
			RemainingDelta = newDelta;
		}

		protected override void Translate(Vector2 screenDelta)
		{
			// Make sure the camera exists
			var camera = LeanTouch.GetCamera(Camera, gameObject);

			if (camera != null)
			{
				// Store old position
				var oldPosition = transform.position;

				// Screen position of the transform
				var screenPosition = camera.WorldToScreenPoint(oldPosition);

				// Add the deltaPosition
				screenPosition += (Vector3)screenDelta;

				// Convert back to world space
				var newPosition = camera.ScreenToWorldPoint(screenPosition);

				// Add to delta
				RemainingDelta += newPosition - oldPosition;
			}
		}
	}
}