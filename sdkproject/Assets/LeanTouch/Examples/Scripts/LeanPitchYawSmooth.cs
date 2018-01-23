using UnityEngine;

namespace Lean.Touch
{
	// This modifies LeanPitchYaw to be smooth
	public class LeanPitchYawSmooth : LeanPitchYaw
	{
		[Tooltip("How sharp the rotation value changes update")]
		[Space(10.0f)]
		public float Dampening = 3.0f;

		private float currentPitch;

		private float currentYaw;

		protected virtual void OnEnable()
		{
			currentPitch = Pitch;
			currentYaw   = Yaw;
		}

		protected override void LateUpdate()
		{
			// Call LeanPitchYaw.LateUpdate
			base.LateUpdate();

			// Get t value
			var factor = LeanTouch.GetDampenFactor(Dampening, Time.deltaTime);

			// Lerp the current values to the target ones
			currentPitch = Mathf.Lerp(currentPitch, Pitch, factor);
			currentYaw   = Mathf.Lerp(currentYaw  , Yaw  , factor);

			// Rotate camera to pitch and yaw values
			transform.localRotation = Quaternion.Euler(currentPitch, currentYaw, 0.0f);
		}
	}
}