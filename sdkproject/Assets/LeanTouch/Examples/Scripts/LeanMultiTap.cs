using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Lean.Touch
{
	// This script calculates the multi-tap event
	// A multi-tap is where you press and release at least one finger at the same time
	public class LeanMultiTap : MonoBehaviour
	{
		// Event signature
		[System.Serializable] public class IntEvent : UnityEvent<int, int> {}

		[Tooltip("Ignore fingers with StartedOverGui?")]
		public bool IgnoreGuiFingers = true;

		[Tooltip("This is set to true the frame a multi-tap occurs")]
		public bool MultiTap;

		[Tooltip("This is set to the current multi-tap count")]
		public int MultiTapCount;

		[Tooltip("Highest number of fingers held down during this multi-tap")]
		public int HighestFingerCount;

		// Called when a multi-tap occurs (Int = multi-tap count, Int = highest finger count)
		public IntEvent OnMultiTap;

		// Seconds at least one finger has been held down
		private float age;

		// Previous fingerCount
		private int lastFingerCount;

		protected virtual void Update()
		{
			// Get fingers and calculate how many are still touching the screen
			var fingers     = LeanTouch.GetFingers(IgnoreGuiFingers);
			var fingerCount = GetFingerCount(fingers);

			// At least one finger set?
			if (fingerCount > 0)
			{
				// Did this just begin?
				if (lastFingerCount == 0)
				{
					age                = 0.0f;
					HighestFingerCount = fingerCount;
				}
				else if (fingerCount > HighestFingerCount)
				{
					HighestFingerCount = fingerCount;
				}
			}

			age += Time.unscaledDeltaTime;

			// Reset
			MultiTap = false;

			// Is a multi-tap still eligible?
			if (age <= LeanTouch.CurrentTapThreshold)
			{
				// All fingers released?
				if (fingerCount == 0 && lastFingerCount > 0)
				{
					MultiTapCount += 1;

					OnMultiTap.Invoke(MultiTapCount, HighestFingerCount);
				}
			}
			// Reset
			else
			{
				MultiTapCount      = 0;
				HighestFingerCount = 0;
			}

			lastFingerCount = fingerCount;
		}

		private int GetFingerCount(List<LeanFinger> fingers)
		{
			var count = 0;

			for (var i = fingers.Count - 1; i >= 0; i--)
			{
				if (fingers[i].Up == false)
				{
					count += 1;
				}
			}

			return count;
		}
	}
}