using UnityEngine;

namespace Lean.Touch
{
	// This script allows you to tilt & pan the current GameObject (e.g. camera) by dragging your finger(s)
	[ExecuteInEditMode]
	public class LeanPitchYaw : MonoBehaviour
	{
		[Tooltip("Ignore fingers with StartedOverGui?")]
		public bool IgnoreGuiFingers = true;

		[Tooltip("Ignore fingers if the finger count doesn't match? (0 = any)")]
		public int RequiredFingerCount;

		[Tooltip("Does turning require an object to be selected?")]
		public LeanSelectable RequiredSelectable;

		[Tooltip("If you want the rotation to be scaled by the camera FOV, then set that here")]
		public Camera Camera;

		[Tooltip("Pitch of the rotation in degrees")]
		[Space(10.0f)]
		public float Pitch;

		[Tooltip("The strength of the pitch changes with vertical finger movement")]
		public float PitchSensitivity = 0.25f;

		[Tooltip("Limit the pitch to min/max?")]
		public bool PitchClamp = true;

		[Tooltip("The minimum pitch angle in degrees")]
		public float PitchMin = -90.0f;

		[Tooltip("The maximum pitch angle in degrees")]
		public float PitchMax = 90.0f;

		[Tooltip("Yaw of the rotation in degrees")]
		[Space(10.0f)]
		public float Yaw;

		[Tooltip("The strength of the yaw changes with horizontal finger movement")]
		public float YawSensitivity = 0.25f;

		[Tooltip("Limit the yaw to min/max?")]
		public bool YawClamp;

		[Tooltip("The minimum yaw angle in degrees")]
		public float YawMin = -45.0f;

		[Tooltip("The maximum yaw angle in degrees")]
		public float YawMax = 45.0f;

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

			if (Camera == null)
			{
				Camera = GetComponent<Camera>();
			}
		}

		protected virtual void LateUpdate()
		{
			// If we require a selectable and it isn't selected, skip
			if (RequiredSelectable != null && RequiredSelectable.IsSelected == false)
			{
				UpdateRotation();

				return;
			}

			// Get the fingers we want to use
			var fingers = LeanTouch.GetFingers(IgnoreGuiFingers, RequiredFingerCount);

			// Get the scaled average movement vector of these fingers
			var drag = LeanGesture.GetScaledDelta(fingers);

			// Get base sensitivity
			var sensitivity = GetSensitivity();

			// Adjust pitch
			Pitch += drag.y * PitchSensitivity * sensitivity;

			if (PitchClamp == true)
			{
				Pitch = Mathf.Clamp(Pitch, PitchMin, PitchMax);
			}

			// Adjust yaw
			Yaw -= drag.x * YawSensitivity * sensitivity;

			if (YawClamp == true)
			{
				Yaw = Mathf.Clamp(Yaw, YawMin, YawMax);
			}

			UpdateRotation();
		}

		private float GetSensitivity()
		{
			// Has a camera been set?
			if (Camera != null)
			{
				// Adjust sensitivity by FOV?
				if (Camera.orthographic == false)
				{
					return Camera.fieldOfView / 90.0f;
				}
			}

			return 1.0f;
		}

		private void UpdateRotation()
		{
			// Rotate to pitch and yaw values
			transform.localRotation = Quaternion.Euler(Pitch, Yaw, 0.0f);
		}
	}
}