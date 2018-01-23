using UnityEngine;

namespace Lean.Touch
{
	// This script allows you to scale the current GameObject
	public class LeanScale : MonoBehaviour
	{
		[Tooltip("Ignore fingers with StartedOverGui?")]
		public bool IgnoreGuiFingers;

		[Tooltip("Allows you to force rotation with a specific amount of fingers (0 = any)")]
		public int RequiredFingerCount;

		[Tooltip("Does scaling require an object to be selected?")]
		public LeanSelectable RequiredSelectable;

		[Tooltip("If you want the mouse wheel to simulate pinching then set the strength of it here")]
		[Range(-1.0f, 1.0f)]
		public float WheelSensitivity;

		[Tooltip("The camera that will be used to calculate the zoom (None = MainCamera)")]
		public Camera Camera;

		[Tooltip("Should the scaling be performanced relative to the finger center?")]
		public bool Relative;

		[Tooltip("Should the scale value be clamped?")]
		public bool ScaleClamp;

		[Tooltip("The minimum scale value on all axes")]
		public Vector3 ScaleMin;

		[Tooltip("The maximum scale value on all axes")]
		public Vector3 ScaleMax;

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			Start();
		}
#endif

		protected virtual void Start()
		{
			if (RequiredSelectable == null)
			{
				RequiredSelectable = GetComponent<LeanSelectable>();
			}
		}

		protected virtual void Update()
		{
			// If we require a selectable and it isn't selected, cancel scaling
			if (RequiredSelectable != null && RequiredSelectable.IsSelected == false)
			{
				return;
			}

			// Get the fingers we want to use
			var fingers = LeanTouch.GetFingers(IgnoreGuiFingers, RequiredFingerCount);

			// Calculate the scaling values based on these fingers
			var pinchScale   = LeanGesture.GetPinchScale(fingers, WheelSensitivity);
			var screenCenter = LeanGesture.GetScreenCenter(fingers);

			// Perform the scaling
			Scale(pinchScale, screenCenter);
		}

		private void Scale(float pinchScale, Vector2 screenCenter)
		{
			// Make sure the scale is valid
			if (pinchScale > 0.0f)
			{
				var scale = transform.localScale;

				if (Relative == true)
				{
					// Make sure the camera exists
					var camera = LeanTouch.GetCamera(Camera, gameObject);

					if (camera != null)
					{
						// Screen position of the transform
						var screenPosition = camera.WorldToScreenPoint(transform.position);

						// Push the screen position away from the reference point based on the scale
						screenPosition.x = screenCenter.x + (screenPosition.x - screenCenter.x) * pinchScale;
						screenPosition.y = screenCenter.y + (screenPosition.y - screenCenter.y) * pinchScale;

						// Convert back to world space
						transform.position = camera.ScreenToWorldPoint(screenPosition);

						// Grow the local scale by scale
						scale *= pinchScale;
					}
				}
				else
				{
					// Grow the local scale by scale
					scale *= pinchScale;
				}

				if (ScaleClamp == true)
				{
					scale.x = Mathf.Clamp(scale.x, ScaleMin.x, ScaleMax.x);
					scale.y = Mathf.Clamp(scale.y, ScaleMin.y, ScaleMax.y);
					scale.z = Mathf.Clamp(scale.z, ScaleMin.z, ScaleMax.z);
				}

				transform.localScale = scale;
			}
		}
	}
}