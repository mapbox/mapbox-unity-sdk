using UnityEngine;
using System.Collections.Generic;

namespace Lean.Touch
{
	// This class stores a snapshot of where a finger was at a previous point in time
	public class LeanSnapshot
	{
		// The age of the finger when this snapshot was created
		public float Age;
		
		// The screen position of the finger when this snapshot was created
		public Vector2 ScreenPosition;
		
		// This will return the world position of this snapshot based on the distance from the camera
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

		public static List<LeanSnapshot> InactiveSnapshots = new List<LeanSnapshot>(1000);

		// Return the last inactive snapshot, or allocate a new one
		public static LeanSnapshot Pop()
		{
			if (InactiveSnapshots.Count > 0)
			{
				var index    = InactiveSnapshots.Count - 1;
				var snapshot = InactiveSnapshots[index];

				InactiveSnapshots.RemoveAt(index);

				return snapshot;
			}

			return new LeanSnapshot();
		}

		// This will return the recorded position of the current finger when it was at 'targetAge'
		public static bool TryGetScreenPosition(List<LeanSnapshot> snapshots, float targetAge, ref Vector2 screenPosition)
		{
			if (snapshots != null && snapshots.Count > 0)
			{
				// Below start?
				var snapshotF = snapshots[0];

				if (targetAge <= snapshotF.Age)
				{
					screenPosition = snapshotF.ScreenPosition; return true;
				}

				// After end?
				var snapshotL = snapshots[snapshots.Count - 1];

				if (targetAge >= snapshotL.Age)
				{
					screenPosition = snapshotL.ScreenPosition; return true;
				}

				// Interpolate to find screenPosition at targetAge
				var lowerIndex = GetLowerIndex(snapshots, targetAge);
				var upperIndex = lowerIndex + 1;
				var lower      = snapshots[lowerIndex];
				var upper      = upperIndex < snapshots.Count ? snapshots[upperIndex] : lower;
				var across     = Mathf.InverseLerp(lower.Age, upper.Age, targetAge);

				screenPosition = Vector2.Lerp(lower.ScreenPosition, upper.ScreenPosition, across);

				return true;
			}

			return false;
		}

		// NOTE: Assumes snapshots does not contain any null elements
		public static bool TryGetSnapshot(List<LeanSnapshot> snapshots, int index, ref float age, ref Vector2 screenPosition)
		{
			if (index >= 0 && index < snapshots.Count)
			{
				var snapshot = snapshots[index];
				
				age            = snapshot.Age;
				screenPosition = snapshot.ScreenPosition;

				return true;
			}
			
			return true;
		}

		// This will get the index of the closest snapshot whose age is under targetAge
		// NOTE: Assumes snapshots does not contain any null elements
		public static int GetLowerIndex(List<LeanSnapshot> snapshots, float targetAge)
		{
			if (snapshots != null)
			{
				var count = snapshots.Count;

				if (count > 0)
				{
					for (var i = count - 1; i >= 0; i--)
					{
						if (snapshots[i].Age <= targetAge)
						{
							return i;
						}
					}
				}

				return 0;
			}

			return -1;
		}
	}
}