using UnityEngine;
using System.Collections.Generic;

namespace Lean.Touch
{
	// This class stores information about a single touch (or simulated touch)
	public class LeanFinger
	{
		// This is the hardware ID of the finger (or 0 & 1 for simulated fingers)
		public int Index;

		// This tells you how long this finger has been active (or inactive) in seconds
		public float Age;

		// This tells you if the finger is currently set (mouse click or touched on screen)
		public bool Set;

		// This tells you the 'Set' value of the last frame
		public bool LastSet;

		// This tells if you if the finger has just been tapped
		public bool Tap;

		// This tells you how many times this finger has been tapped
		public int TapCount;

		// This tells you if the finger was just swiped
		public bool Swipe;

		// This tells you the screen position of the touch on the frame it was first set
		public Vector2 StartScreenPosition;

		// This tells you the last screen position of the finger
		public Vector2 LastScreenPosition;

		// This tells you the current screen position of the finger
		public Vector2 ScreenPosition;

		// This tells you if the current finger had 'IsOverGui' set to true when it began touching the screen
		public bool StartedOverGui;

		// Used to store position snapshots, enable RecordFingers in LeanTouch to use this
		public List<LeanSnapshot> Snapshots = new List<LeanSnapshot>(1000);

		// This will return true if the current finger is currently touching the screen
		public bool IsActive
		{
			get
			{
				return LeanTouch.Fingers.Contains(this);
			}
		}

		// This will return the amount of seconds of snapshot footage is stored for this finger
		public float SnapshotDuration
		{
			get
			{
				if (Snapshots.Count > 0)
				{
					return Age - Snapshots[0].Age;
				}
				
				return 0.0f;
			}
		}

		// This will return true if the current finger is over any Unity GUI elements
		public bool IsOverGui
		{
			get
			{
				return LeanTouch.PointOverGui(ScreenPosition);
			}
		}

		// This tells you if the finger has just touched the screen
		public bool Down
		{
			get
			{
				return Set == true && LastSet == false;
			}
		}

		// This tells you if the finger has just been released from the screen
		public bool Up
		{
			get
			{
				return Set == false && LastSet == true;
			}
		}

		// This will return how far in pixels the finger has moved since the last recorded snapshot
		public Vector2 LastSnapshotScreenDelta
		{
			get
			{
				var snapshotCount = Snapshots.Count;
				
				if (snapshotCount > 0)
				{
					var snapshot = Snapshots[snapshotCount - 1];
					
					if (snapshot != null)
					{
						return ScreenPosition - snapshot.ScreenPosition;
					}
				}
				
				return Vector2.zero;
			}
		}

		public Vector2 LastSnapshotScaledDelta
		{
			get
			{
				return LastSnapshotScreenDelta * LeanTouch.ScalingFactor;
			}
		}

		// This will return how far in pixels the finger has moved since the last frame
		public Vector2 ScreenDelta
		{
			get
			{
				return ScreenPosition - LastScreenPosition;
			}
		}

		public Vector2 ScaledDelta
		{
			get
			{
				return ScreenDelta * LeanTouch.ScalingFactor;
			}
		}

		// This tells you how far this finger has moved since it began touching the screen
		public Vector2 SwipeScreenDelta
		{
			get
			{
				return ScreenPosition - StartScreenPosition;
			}
		}

		public Vector2 SwipeScaledDelta
		{
			get
			{
				return SwipeScreenDelta * LeanTouch.ScalingFactor;
			}
		}

		// This will return the ray of the finger's current position
		public Ray GetRay(Camera camera = null)
		{
			// Make sure the camera exists
			camera = LeanTouch.GetCamera(camera);

			if (camera != null)
			{
				return camera.ScreenPointToRay(ScreenPosition);
			}

			return default(Ray);
		}

		// This will return the ray of the finger's start position
		public Ray GetStartRay(Camera camera = null)
		{
			// Make sure the camera exists
			camera = LeanTouch.GetCamera(camera);

			if (camera != null)
			{
				return camera.ScreenPointToRay(StartScreenPosition);
			}
			
			return default(Ray);
		}

		// This will tell you how far the finger has moved in the past 'deltaTime' seconds
		public Vector2 GetSnapshotScreenDelta(float deltaTime)
		{
			return ScreenPosition - GetSnapshotScreenPosition(Age - deltaTime);
		}

		public Vector2 GetSnapshotScaledDelta(float deltaTime)
		{
			return GetSnapshotScreenDelta(deltaTime) * LeanTouch.ScalingFactor;
		}

		// This will return the recorded position of the current finger when it was at 'targetAge'
		public Vector2 GetSnapshotScreenPosition(float targetAge)
		{
			var screenPosition = ScreenPosition;

			LeanSnapshot.TryGetScreenPosition(Snapshots, targetAge, ref screenPosition);

			return screenPosition;
		}

		public Vector3 GetSnapshotWorldPosition(float targetAge, float distance, Camera camera = null)
		{
			// Make sure the camera exists
			camera = LeanTouch.GetCamera(camera);

			if (camera != null)
			{
				var screenPosition = GetSnapshotScreenPosition(targetAge);
				var point          = new Vector3(screenPosition.x, screenPosition.y, distance);
				
				return camera.ScreenToWorldPoint(point);
			}
			
			return default(Vector3);
		}

		// This will return the angle between the finger and the reference point, relative to the screen
		public float GetRadians(Vector2 referencePoint)
		{
			return Mathf.Atan2(ScreenPosition.x - referencePoint.x, ScreenPosition.y - referencePoint.y);
		}

		public float GetDegrees(Vector2 referencePoint)
		{
			return GetRadians(referencePoint) * Mathf.Rad2Deg;
		}

		// This will return the angle between the last finger position and the reference point, relative to the screen
		public float GetLastRadians(Vector2 referencePoint)
		{
			return Mathf.Atan2(LastScreenPosition.x - referencePoint.x, LastScreenPosition.y - referencePoint.y);
		}

		public float GetLastDegrees(Vector2 referencePoint)
		{
			return GetLastRadians(referencePoint) * Mathf.Rad2Deg;
		}

		// This will return the delta between GetAngle and GetLastAngle
		public float GetDeltaRadians(Vector2 referencePoint)
		{
			return GetDeltaRadians(referencePoint, referencePoint);
		}

		public float GetDeltaRadians(Vector2 referencePoint, Vector2 lastReferencePoint)
		{
			var a = GetLastRadians(lastReferencePoint);
			var b = GetRadians(referencePoint);
			var d = Mathf.Repeat(a - b, Mathf.PI * 2.0f);
			
			if (d > Mathf.PI)
			{
				d -= Mathf.PI * 2.0f;
			}
			
			return d;
		}

		public float GetDeltaDegrees(Vector2 referencePoint)
		{
			return GetDeltaRadians(referencePoint, referencePoint) * Mathf.Rad2Deg;
		}

		public float GetDeltaDegrees(Vector2 referencePoint, Vector2 lastReferencePoint)
		{
			return GetDeltaRadians(referencePoint, lastReferencePoint) * Mathf.Rad2Deg;
		}

		// This will return the distance between the finger and the reference point
		public float GetScreenDistance(Vector2 point)
		{
			return Vector2.Distance(ScreenPosition, point);
		}

		public float GetScaledDistance(Vector2 point)
		{
			return GetScreenDistance(point) * LeanTouch.ScalingFactor;
		}

		// This will return the distance between the last finger and the reference point
		public float GetLastScreenDistance(Vector2 point)
		{
			return Vector2.Distance(LastScreenPosition, point);
		}

		public float GetLastScaledDistance(Vector2 point)
		{
			return GetLastScreenDistance(point) * LeanTouch.ScalingFactor;
		}

		// This will return the start world position of this finger based on the distance from the camera
		public Vector3 GetStartWorldPosition(float distance, Camera camera = null)
		{
			// Make sure the camera exists
			camera = LeanTouch.GetCamera(camera);

			if (camera != null)
			{
				var point = new Vector3(StartScreenPosition.x, StartScreenPosition.y, distance);
				
				return camera.ScreenToWorldPoint(point);
			}
			
			return default(Vector3);
		}

		// This will return the last world position of this finger based on the distance from the camera
		public Vector3 GetLastWorldPosition(float distance, Camera camera = null)
		{
			// Make sure the camera exists
			camera = LeanTouch.GetCamera(camera);

			if (camera != null)
			{
				var point = new Vector3(LastScreenPosition.x, LastScreenPosition.y, distance);
				
				return camera.ScreenToWorldPoint(point);
			}
			
			return default(Vector3);
		}

		// This will return the world position of this finger based on the distance from the camera
		public Vector3 GetWorldPosition(float distance, Camera camera = null)
		{
			// Make sure the camera exists
			camera = LeanTouch.GetCamera(camera);

			if (camera != null)
			{
				var point = new Vector3(ScreenPosition.x, ScreenPosition.y, distance);
				
				return camera.ScreenToWorldPoint(point);
			}
			
			return default(Vector3);
		}

		// This will return the change in world position of this finger based on the distance from the camera
		public Vector3 GetWorldDelta(float distance, Camera camera = null)
		{
			return GetWorldDelta(distance, distance, camera);
		}

		public Vector3 GetWorldDelta(float lastDistance, float distance, Camera camera = null)
		{
			// Make sure the camera exists
			camera = LeanTouch.GetCamera(camera);

			if (camera != null)
			{
				return GetWorldPosition(distance, camera) - GetLastWorldPosition(lastDistance, camera);
			}
			
			return default(Vector3);
		}

		// Clear snapshots and pool them, count = -1 for all
		public void ClearSnapshots(int count = -1)
		{
			// Clear old ones only?
			if (count > 0 && count <= Snapshots.Count)
			{
				for (var i = 0; i < count; i++)
				{
					LeanSnapshot.InactiveSnapshots.Add(Snapshots[i]);
				}
				
				Snapshots.RemoveRange(0, count);
			}
			// Clear all?
			else if (count < 0)
			{
				LeanSnapshot.InactiveSnapshots.AddRange(Snapshots);
				
				Snapshots.Clear();
			}
		}

		// Records a snapshot of the current finger
		public void RecordSnapshot()
		{
			// Get an unused snapshot and set it up
			var snapshot = LeanSnapshot.Pop();

			snapshot.Age            = Age;
			snapshot.ScreenPosition = ScreenPosition;

			// Add to list
			Snapshots.Add(snapshot);
		}
	}
}