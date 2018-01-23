using UnityEngine;
using System.Collections.Generic;

namespace Lean.Touch
{
	// This class calculates gesture information based on a list of input fingers
	public static class LeanGesture
	{
		// Gets the average ScreenPosition of the fingers
		public static Vector2 GetScreenCenter()
		{
			return GetScreenCenter(LeanTouch.Fingers);
		}

		public static Vector2 GetScreenCenter(List<LeanFinger> fingers)
		{
			var center = default(Vector2); TryGetScreenCenter(fingers, ref center); return center;
		}
		
		public static bool TryGetScreenCenter(List<LeanFinger> fingers, ref Vector2 center)
		{
			if (fingers != null)
			{
				var total = Vector2.zero;
				var count = 0;

				for (var i = fingers.Count - 1; i >= 0; i--)
				{
					var finger = fingers[i];

					if (finger != null)
					{
						total += finger.ScreenPosition;
						count += 1;
					}
				}
				
				if (count > 0)
				{
					center = total / count; return true;
				}
			}

			return false;
		}
		
		// Gets the last average ScreenPosition of the fingers
		public static Vector2 GetLastScreenCenter()
		{
			return GetLastScreenCenter(LeanTouch.Fingers);
		}

		public static Vector2 GetLastScreenCenter(List<LeanFinger> fingers)
		{
			var center = default(Vector2); TryGetLastScreenCenter(fingers, ref center); return center;
		}
		
		public static bool TryGetLastScreenCenter(List<LeanFinger> fingers, ref Vector2 center)
		{
			if (fingers != null)
			{
				var total = Vector2.zero;
				var count = 0;

				for (var i = fingers.Count - 1; i >= 0; i--)
				{
					var finger = fingers[i];

					if (finger != null)
					{
						total += finger.LastScreenPosition;
						count += 1;
					}
				}
				
				if (count > 0)
				{
					center = total / count; return true;
				}
			}

			return false;
		}
		
		// Gets the average ScreenDelta of the fingers
		public static Vector2 GetScreenDelta()
		{
			return GetScreenDelta(LeanTouch.Fingers);
		}

		public static Vector2 GetScreenDelta(List<LeanFinger> fingers)
		{
			var delta = default(Vector2); TryGetScreenDelta(fingers, ref delta); return delta;
		}

		public static bool TryGetScreenDelta(List<LeanFinger> fingers, ref Vector2 delta)
		{
			if (fingers != null)
			{
				var total = Vector2.zero;
				var count = 0;

				for (var i = fingers.Count - 1; i >= 0; i--)
				{
					var finger = fingers[i];

					if (finger != null)
					{
						total += finger.ScreenDelta;
						count += 1;
					}
				}
				
				if (count > 0)
				{
					delta = total / count; return true;
				}
			}

			return false;
		}

		// Gets the average ScreenDelta * LeanTouch.ScalingFactor of the fingers
		public static Vector2 GetScaledDelta()
		{
			return GetScreenDelta() * LeanTouch.ScalingFactor;
		}

		public static Vector2 GetScaledDelta(List<LeanFinger> fingers)
		{
			return GetScreenDelta(fingers) * LeanTouch.ScalingFactor;
		}

		public static bool TryGetScaledDelta(List<LeanFinger> fingers, ref Vector2 delta)
		{
			if (TryGetScreenDelta(fingers, ref delta) == true)
			{
				delta *= LeanTouch.ScalingFactor; return true;
			}

			return false;
		}

		// Gets the average WorldDelta of the fingers
		public static Vector3 GetWorldDelta(float distance, Camera camera = null)
		{
			return GetWorldDelta(LeanTouch.Fingers, distance, camera);
		}

		public static Vector3 GetWorldDelta(List<LeanFinger> fingers, float distance, Camera camera = null)
		{
			var delta = default(Vector3); TryGetWorldDelta(fingers, distance, ref delta, camera); return delta;
		}

		public static bool TryGetWorldDelta(List<LeanFinger> fingers, float distance, ref Vector3 delta, Camera camera = null)
		{
			if (fingers != null)
			{
				var total = Vector3.zero;
				var count = 0;

				for (var i = fingers.Count - 1; i >= 0; i--)
				{
					var finger = fingers[i];

					if (finger != null)
					{
						total += finger.GetWorldDelta(distance, camera);
						count += 1;
					}
				}
					
				if (count > 0)
				{
					delta = total / count; return true;
				}
			}

			return false;
		}
		
		// Gets the average ScreenPosition distance between the fingers
		public static float GetScreenDistance()
		{
			return GetScreenDistance(LeanTouch.Fingers);
		}

		public static float GetScreenDistance(List<LeanFinger> fingers)
		{
			var distance = default(float);
			var center   = default(Vector2);

			if (TryGetScreenCenter(fingers, ref center) == true)
			{
				TryGetScreenDistance(fingers, center, ref distance);
			}

			return distance;
		}
		
		public static float GetScreenDistance(List<LeanFinger> fingers, Vector2 center)
		{
			var distance = default(float); TryGetScreenDistance(fingers, center, ref distance); return distance;
		}
		
		public static bool TryGetScreenDistance(List<LeanFinger> fingers, Vector2 center, ref float distance)
		{
			if (fingers != null)
			{
				var total = 0.0f;
				var count = 0;

				for (var i = fingers.Count - 1; i >= 0; i--)
				{
					var finger = fingers[i];

					if (finger != null)
					{
						total += finger.GetScreenDistance(center);
						count += 1;
					}
				}
				
				if (count > 0)
				{
					distance = total / count; return true;
				}
			}

			return false;
		}
		
		// Gets the average ScreenPosition distance * LeanTouch.ScalingFactor between the fingers
		public static float GetScaledDistance()
		{
			return GetScreenDistance() * LeanTouch.ScalingFactor;
		}

		public static float GetScaledDistance(List<LeanFinger> fingers)
		{
			return GetScreenDistance(fingers) * LeanTouch.ScalingFactor;
		}

		public static float GetScaledDistance(List<LeanFinger> fingers, Vector2 center)
		{
			return GetScreenDistance(fingers, center) * LeanTouch.ScalingFactor;
		}

		public static bool TryGetScaledDistance(List<LeanFinger> fingers, Vector2 center, ref float distance)
		{
			if (TryGetScreenDistance(fingers, center, ref distance) == true)
			{
				distance *= LeanTouch.ScalingFactor; return true;
			}

			return false;
		}

		// Gets the last average ScreenPosition distance between all fingers
		public static float GetLastScreenDistance()
		{
			return GetLastScreenDistance(LeanTouch.Fingers);
		}

		public static float GetLastScreenDistance(List<LeanFinger> fingers)
		{
			var distance = default(float);
			var center   = default(Vector2);

			if (TryGetLastScreenCenter(fingers, ref center) == true)
			{
				TryGetLastScreenDistance(fingers, center, ref distance);
			}

			return distance;
		}
		
		public static float GetLastScreenDistance(List<LeanFinger> fingers, Vector2 center)
		{
			var distance = default(float); TryGetLastScreenDistance(fingers, center, ref distance); return distance;
		}
		
		public static bool TryGetLastScreenDistance(List<LeanFinger> fingers, Vector2 center, ref float distance)
		{
			if (fingers != null)
			{
				var total = 0.0f;
				var count = 0;

				for (var i = fingers.Count - 1; i >= 0; i--)
				{
					var finger = fingers[i];

					if (finger != null)
					{
						total += finger.GetLastScreenDistance(center);
						count += 1;
					}
				}
				
				if (count > 0)
				{
					distance = total / count; return true;
				}
			}

			return false;
		}

		// // Gets the last average ScreenPosition distance * LeanTouch.ScalingFactor between all fingers
		public static float GetLastScaledDistance()
		{
			return GetLastScreenDistance() * LeanTouch.ScalingFactor;
		}

		public static float GetLastScaledDistance(List<LeanFinger> fingers)
		{
			return GetLastScreenDistance(fingers) * LeanTouch.ScalingFactor;
		}

		public static float GetLastScaledDistance(List<LeanFinger> fingers, Vector2 center)
		{
			return GetLastScreenDistance(fingers, center) * LeanTouch.ScalingFactor;
		}

		public static bool TryGetLastScaledDistance(List<LeanFinger> fingers, Vector2 center, ref float distance)
		{
			if (TryGetLastScreenDistance(fingers, center, ref distance) == true)
			{
				distance *= LeanTouch.ScalingFactor; return true;
			}

			return false;
		}
		
		// Gets the pinch scale of the fingers
		public static float GetPinchScale(float wheelSensitivity = 0.0f)
		{
			return GetPinchScale(LeanTouch.Fingers, wheelSensitivity);
		}

		public static float GetPinchScale(List<LeanFinger> fingers, float wheelSensitivity = 0.0f)
		{
			var scale      = 1.0f;
			var center     = GetScreenCenter(fingers);
			var lastCenter = GetLastScreenCenter(fingers);

			TryGetPinchScale(fingers, center, lastCenter, ref scale, wheelSensitivity);

			return scale;
		}

		public static bool TryGetPinchScale(List<LeanFinger> fingers, Vector2 center, Vector2 lastCenter, ref float scale, float wheelSensitivity = 0.0f)
		{
			var distance     = GetScreenDistance(fingers, center);
			var lastDistance = GetLastScreenDistance(fingers, lastCenter);

			if (lastDistance > 0.0f)
			{
				scale = distance / lastDistance; return true;
			}

			if (wheelSensitivity != 0.0f)
			{
				var scroll = Input.mouseScrollDelta.y;

				if (scroll > 0.0f)
				{
					scale = 1.0f - wheelSensitivity; return true;
				}
				
				if (scroll < 0.0f)
				{
					scale = 1.0f + wheelSensitivity; return true;
				}
			}

			return false;
		}

		// Gets the pinch ratio of the fingers (reciprocal of pinch scale)
		public static float GetPinchRatio(float wheelSensitivity = 0.0f)
		{
			return GetPinchRatio(LeanTouch.Fingers, wheelSensitivity);
		}

		public static float GetPinchRatio(List<LeanFinger> fingers, float wheelSensitivity = 0.0f)
		{
			var ratio      = 1.0f;
			var center     = GetScreenCenter(fingers);
			var lastCenter = GetLastScreenCenter(fingers);

			TryGetPinchRatio(fingers, center, lastCenter, ref ratio, wheelSensitivity);

			return ratio;
		}

		public static bool TryGetPinchRatio(List<LeanFinger> fingers, Vector2 center, Vector2 lastCenter, ref float ratio, float wheelSensitivity = 0.0f)
		{
			var distance     = GetScreenDistance(fingers, center);
			var lastDistance = GetLastScreenDistance(fingers, lastCenter);

			if (distance > 0.0f)
			{
				ratio = lastDistance / distance;

				return true;
			}

			if (wheelSensitivity != 0.0f)
			{
				var scroll = Input.mouseScrollDelta.y;

				if (scroll > 0.0f)
				{
					ratio = 1.0f + wheelSensitivity; return true;
				}
				
				if (scroll < 0.0f)
				{
					ratio = 1.0f - wheelSensitivity; return true;
				}
			}

			return false;
		}

		// Gets the average twist of the fingers in degrees
		public static float GetTwistDegrees()
		{
			return GetTwistDegrees(LeanTouch.Fingers);
		}

		public static float GetTwistDegrees(List<LeanFinger> fingers)
		{
			return GetTwistRadians(fingers) * Mathf.Rad2Deg;
		}

		public static float GetTwistDegrees(List<LeanFinger> fingers, Vector2 center, Vector2 lastCenter)
		{
			return GetTwistRadians(fingers, center, lastCenter) * Mathf.Rad2Deg;
		}

		public static bool TryGetTwistDegrees(List<LeanFinger> fingers, Vector2 center, Vector2 lastCenter, ref float degrees)
		{
			if (TryGetTwistRadians(fingers, center, lastCenter, ref degrees) == true)
			{
				degrees *= Mathf.Rad2Deg;

				return true;
			}

			return false;
		}

		// Gets the average twist of the fingers in radians
		public static float GetTwistRadians()
		{
			return GetTwistRadians(LeanTouch.Fingers);
		}

		public static float GetTwistRadians(List<LeanFinger> fingers)
		{
			var center     = LeanGesture.GetScreenCenter(fingers);
			var lastCenter = LeanGesture.GetLastScreenCenter(fingers);
			
			return GetTwistRadians(fingers, center, lastCenter);
		}

		public static float GetTwistRadians(List<LeanFinger> fingers, Vector2 center, Vector2 lastCenter)
		{
			var radians = default(float); TryGetTwistRadians(fingers, center, lastCenter, ref radians); return radians;
		}

		public static bool TryGetTwistRadians(List<LeanFinger> fingers, Vector2 center, Vector2 lastCenter, ref float radians)
		{
			if (fingers != null)
			{
				var total = 0.0f;
				var count = 0;

				for (var i = fingers.Count - 1; i >= 0; i--)
				{
					var finger = fingers[i];

					if (finger != null)
					{
						total += finger.GetDeltaRadians(center, lastCenter);
						count += 1;
					}
				}
				
				if (count > 0)
				{
					radians = total / count; return true;
				}
			}

			return false;
		}
	}
}