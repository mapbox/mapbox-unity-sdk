using UnityEngine;
using UnityEngine.Events;

namespace Lean.Touch
{
	// This script calls the OnFingerDrag event while a finger is moving across the screen
	public class LeanFingerDrag : MonoBehaviour
	{
		// Event signature
		[System.Serializable] public class LeanFingerEvent : UnityEvent<LeanFinger> {}

		[Tooltip("If the finger is over the GUI, ignore it?")]
		public bool IgnoreIfOverGui;

		[Tooltip("If the finger started over the GUI, ignore it?")]
		public bool IgnoreIfStartedOverGui;

		public LeanFingerEvent OnFingerDrag;

		protected virtual void OnEnable()
		{
			// Hook events
			LeanTouch.OnFingerSet += FingerSet;
		}

		protected virtual void OnDisable()
		{
			// Unhook events
			LeanTouch.OnFingerSet -= FingerSet;
		}

		private void FingerSet(LeanFinger finger)
		{
			// Ignore?
			if (IgnoreIfOverGui == true && finger.IsOverGui == true)
			{
				return;
			}

			if (IgnoreIfStartedOverGui == true && finger.StartedOverGui == true)
			{
				return;
			}

			if (finger.ScreenDelta.magnitude <= 0.0f)
			{
				return;
			}

			// Call event
			OnFingerDrag.Invoke(finger);
		}
	}
}