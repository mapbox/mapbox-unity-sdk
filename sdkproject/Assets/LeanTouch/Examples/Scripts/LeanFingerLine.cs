#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4
	#define UNITY_OLD_LINE_RENDERER
#endif
using UnityEngine;
using UnityEngine.Events;

namespace Lean.Touch
{
	// This script will modify LeanFingerTrail to draw a straight line
	public class LeanFingerLine : LeanFingerTrail
	{
		// Event signatures
		[System.Serializable] public class Vector3Vector3Event : UnityEvent<Vector3, Vector3> {}
		[System.Serializable] public class Vector3Event : UnityEvent<Vector3> {}

		[Tooltip("The thickness scale per unit (0 = no scaling)")]
		public float ThicknessScale;

		[Tooltip("Limit the length (0 = none)")]
		public float LengthMin;

		[Tooltip("Limit the length (0 = none)")]
		public float LengthMax;

		[Tooltip("Should the line originate from a target point?")]
		public Transform Target;

		public Vector3Vector3Event OnLineFingerUp;

		public Vector3Event OnLineFingerUpVelocity;

		protected override void LinkFingerUp(Link link)
		{
			// Calculate points
			var start = GetStartPoint(link.Finger);
			var end   = GetEndPoint(link.Finger, start);

			if (OnLineFingerUp != null)
			{
				OnLineFingerUp.Invoke(start, end);
			}

			if (OnLineFingerUpVelocity != null)
			{
				OnLineFingerUpVelocity.Invoke(end - start);
			}
		}

		protected override void WritePositions(LineRenderer line, LeanFinger finger)
		{
			// Calculate points
			var start = GetStartPoint(finger);
			var end   = GetEndPoint(finger, start);

			// Adjust thickness?
			if (ThicknessScale > 0.0f)
			{
				var thickness = Vector3.Distance(start, end) * ThicknessScale;

#if UNITY_OLD_LINE_RENDERER
				line.SetWidth(thickness, thickness);
#else
				line.startWidth = thickness;
				line.endWidth   = thickness;
#endif
			}

			// Write positions
#if UNITY_OLD_LINE_RENDERER
			line.SetVertexCount(2);
#else
			line.numPositions = 2;
#endif

			line.SetPosition(0, start);
			line.SetPosition(1, end);
		}

		private Vector3 GetStartPoint(LeanFinger finger)
		{
			// Use target position?
			if (Target != null)
			{
				return Target.position;
			}

			// Make sure the camera exists
			var camera = LeanTouch.GetCamera(Camera, gameObject);

			if (camera != null)
			{
				// Get start and current world position of finger
				return finger.GetStartWorldPosition(Distance, camera);
			}

			return default(Vector3);
		}

		private Vector3 GetEndPoint(LeanFinger finger, Vector3 start)
		{
			// Make sure the camera exists
			var camera = LeanTouch.GetCamera(Camera, gameObject);

			if (camera != null)
			{
				// Cauculate distance based on start position, because the Target point may override Distance
				var distance = camera.WorldToScreenPoint(start).z;
				var end      = finger.GetWorldPosition(distance, camera);
				var length   = Vector3.Distance(start, end);

				// Limit the length?
				if (LengthMin > 0.0f && length < LengthMin)
				{
					length = LengthMin;
				}

				if (LengthMax > 0.0f && length > LengthMax)
				{
					length = LengthMax;
				}

				// Recalculate end
				return start + Vector3.Normalize(end - start) * length;
			}

			return default(Vector3);
		}
	}
}