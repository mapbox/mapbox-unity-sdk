using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Lean.Touch
{
	// This script fires events if a finger has been held for a certain amount of time without moving
	public class LeanFingerHeld : MonoBehaviour
	{
		// Event signature
		[System.Serializable] public class FingerEvent : UnityEvent<LeanFinger> {}

		// This class will store extra Finger data
		[System.Serializable]
		public class Link
		{
			// The finger associated with this link
			public LeanFinger Finger;

			// Was this finger held?
			public bool LastSet;

			// The total movement so we can ignore it if it gets too high
			public Vector2 TotalScaledDelta;
		}

		[Tooltip("If the finger started over the GUI, ignore it?")]
		public bool IgnoreIfStartedOverGui;

		[Tooltip("The finger must be held for this many seconds")]
		public float MinimumAge = 1.0f;

		[Tooltip("The finger cannot move more than this many pixels relative to the reference DPI")]
		public float MaximumMovement = 5.0f;

		// Called on the first frame the conditions are met
		public FingerEvent onFingerHeldDown;

		// Called on every frame the conditions are met
		public FingerEvent onFingerHeldSet;

		// Called on the last frame the conditions are met
		public FingerEvent onFingerHeldUp;

		// This contains all the active and enabled LeanFingerHeld instances
		public static List<LeanFingerHeld> Instances = new List<LeanFingerHeld>();

		// This gets fired when a finger begins being held on the screen (LeanFinger = The current finger)
		public static System.Action<LeanFinger> OnFingerHeldDown;

		// This gets fired when a finger is held on the screen (LeanFinger = The current finger)
		public static System.Action<LeanFinger> OnFingerHeldSet;

		// This gets fired when a finger stops being held on the screen (LeanFinger = The current finger)
		public static System.Action<LeanFinger> OnFingerHeldUp;

		// This stores all finger links
		private List<Link> links = new List<Link>();

		protected virtual void OnEnable()
		{
			Instances.Add(this);

			// Hook events
			LeanTouch.OnFingerDown += OnFingerDown;
			LeanTouch.OnFingerSet  += OnFingerSet;
			LeanTouch.OnFingerUp   += OnFingerUp;
		}

		protected virtual void OnDisable()
		{
			Instances.Remove(this);

			// Unhook events
			LeanTouch.OnFingerDown -= OnFingerDown;
			LeanTouch.OnFingerSet  -= OnFingerSet;
			LeanTouch.OnFingerUp   -= OnFingerUp;
		}

		private void OnFingerDown(LeanFinger finger)
		{
			// Ignore?
			if (IgnoreIfStartedOverGui == true && finger.StartedOverGui == true)
			{
				return;
			}

			// Try and find the link for this finger
			var link = FindLink(finger);

			if (link == null)
			{
				link = new Link();

				link.Finger = finger;

				links.Add(link);
			}

			// Reset its data
			link.LastSet          = false;
			link.TotalScaledDelta = Vector2.zero;
		}

		private void OnFingerSet(LeanFinger finger)
		{
			// Try and find the link for this finger
			var link = FindLink(finger);

			if (link != null)
			{
				// Has this finger been held for more than MinimumAge without moving more than MaximumMovement?
				var set = finger.Age >= MinimumAge && link.TotalScaledDelta.magnitude < MaximumMovement;

				link.TotalScaledDelta += finger.ScaledDelta;

				if (set == true && link.LastSet == false)
				{
					onFingerHeldDown.Invoke(finger);

					if (Instances[0] == this)
					{
						if (OnFingerHeldDown != null) OnFingerHeldDown(finger);
					}
				}

				if (set == true)
				{
					onFingerHeldSet.Invoke(finger);

					if (Instances[0] == this)
					{
						if (OnFingerHeldSet != null) OnFingerHeldSet(finger);
					}
				}

				if (set == false && link.LastSet == true)
				{
					onFingerHeldUp.Invoke(finger);

					if (Instances[0] == this)
					{
						if (OnFingerHeldUp != null) OnFingerHeldUp(finger);
					}
				}

				// Store last value
				link.LastSet = set;
			}
		}

		private void OnFingerUp(LeanFinger finger)
		{
			// Try and find the link for this finger
			var link = FindLink(finger);

			// Link exists?
			if (link != null)
			{
				if (link.LastSet == true)
				{
					onFingerHeldUp.Invoke(finger);

					if (Instances[0] == this)
					{
						if (OnFingerHeldUp != null) OnFingerHeldUp(finger);
					}
				}

				// Remove link from list
				links.Remove(link);
			}
		}

		private Link FindLink(LeanFinger finger)
		{
			for (var i = 0; i < links.Count; i++)
			{
				var link = links[i];

				if (link.Finger == finger)
				{
					return link;
				}
			}

			return null;
		}
	}
}