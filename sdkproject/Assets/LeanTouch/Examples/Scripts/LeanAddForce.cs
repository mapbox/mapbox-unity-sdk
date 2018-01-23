using UnityEngine;

namespace Lean.Touch
{
	// This will add a 2D or 3D force to all the selected objects
	public class LeanAddForce : MonoBehaviour
	{
		[Tooltip("The strength of the force")]
		public float ForceMultiplier = 1.0f;

		[Tooltip("Should the force be based on recorded finger positions?")]
		public bool NoRelease;

		public void ApplyForce(Vector3 force)
		{
			TryApplyForce(transform, force);
		}

		public void ApplyForce(LeanFinger finger)
		{
			// Loop through all selectables
			for (var i = 0; i < LeanSelectable.Instances.Count; i++)
			{
				var selectable = LeanSelectable.Instances[i];

				// Is or was this selected?
				if (selectable.IsSelected == true || selectable.SelectingFinger == finger)
				{
					var force = finger.SwipeScaledDelta;

					if (NoRelease == true)
					{
						// The amount of seconds we consider valid for a swipe
						var tapThreshold = LeanTouch.Instance.TapThreshold;

						// Get the scaled delta position between now, and 'swipeThreshold' seconds ago
						force = finger.GetSnapshotScaledDelta(tapThreshold);
					}

					TryApplyForce(selectable, force);
				}
			}
		}

		private void TryApplyForce(Component component, Vector3 force)
		{
			// Apply 3D force?
			var rigidbody = component.GetComponentInParent<Rigidbody>();

			if (rigidbody != null)
			{
				rigidbody.AddForce(force * ForceMultiplier);
			}

			// Apply 2D force?
			var rigidbody2D = component.GetComponentInParent<Rigidbody2D>();

			if (rigidbody2D != null)
			{
				rigidbody2D.AddForce(force * ForceMultiplier);
			}
		}
	}
}