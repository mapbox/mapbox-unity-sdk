using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using NewTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using NewTouchPhase = UnityEngine.InputSystem.TouchPhase;
#endif

namespace Mapbox.Example.Scripts.MapInput
{
	/// <summary>
	/// Backend-agnostic touch phase. Both legacy <c>UnityEngine.TouchPhase</c> and
	/// <c>UnityEngine.InputSystem.TouchPhase</c> are mapped onto this enum so the
	/// MapInput call-sites don't need to know which input system is compiled in.
	/// </summary>
	internal enum PointerTouchPhase
	{
		None,
		Began,
		Moved,
		Stationary,
		Ended,
		Canceled
	}

	/// <summary>
	/// Thin abstraction over Unity's two input backends. <see cref="PointerInput"/>
	/// has exactly one of two implementations compiled in, selected by Unity's
	/// built-in <c>ENABLE_INPUT_SYSTEM</c> define (set when Active Input Handling
	/// in Player Settings is "New" or "Both"). Gating on <c>ENABLE_INPUT_SYSTEM</c>
	/// rather than package presence is intentional: <c>com.unity.inputsystem</c> is
	/// a hard dependency of this package, so it's always installed, but existing
	/// legacy-input projects must keep running the <c>#else</c> branch until the
	/// user explicitly opts in via Active Input Handling. MapInput.cs talks only
	/// to this interface, so the rest of the camera/input layer stays free of
	/// #if branching.
	/// </summary>
	internal interface IPointerInput
	{
		/// <summary>
		/// Called once from <c>MapInput.Initialize</c>. Legacy is a no-op; new-input
		/// path enables <c>EnhancedTouchSupport</c>.
		/// </summary>
		void EnableTouchSupport();

		int TouchCount { get; }
		Vector2 GetTouchPosition(int index);
		float GetTouchDeltaY(int index);
		int GetTouchId(int index);
		PointerTouchPhase GetTouchPhase(int index);
		/// <summary>
		/// Identifier suitable for <c>EventSystem.IsPointerOverGameObject(int)</c>.
		/// Returns <c>finger.index</c> on the new system and <c>fingerId</c> on legacy
		/// — both are what their respective EventSystem expects.
		/// </summary>
		int GetTouchPointerId(int index);

		Vector3 MousePosition { get; }
		bool MouseLeftPressedThisFrame { get; }
		bool MouseLeftHeld { get; }
		bool MouseRightPressedThisFrame { get; }
		bool MouseRightHeld { get; }
		/// <summary>
		/// Scroll-wheel delta normalized so both backends feed approximately the same
		/// magnitude into ZoomSensitivity (~0.1 per Windows notch). Returns 0 when not
		/// scrolling and when the mouse device is unavailable.
		/// </summary>
		float MouseScrollY { get; }
	}

#if ENABLE_INPUT_SYSTEM
	internal sealed class PointerInput : IPointerInput
	{
		public void EnableTouchSupport()
		{
			if (!UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled)
				UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
		}

		public int TouchCount => NewTouch.activeTouches.Count;
		public Vector2 GetTouchPosition(int index) => NewTouch.activeTouches[index].screenPosition;
		public float GetTouchDeltaY(int index) => NewTouch.activeTouches[index].delta.y;
		public int GetTouchId(int index) => NewTouch.activeTouches[index].touchId;
		public int GetTouchPointerId(int index) => NewTouch.activeTouches[index].finger.index;

		public PointerTouchPhase GetTouchPhase(int index)
		{
			switch (NewTouch.activeTouches[index].phase)
			{
				case NewTouchPhase.Began: return PointerTouchPhase.Began;
				case NewTouchPhase.Moved: return PointerTouchPhase.Moved;
				case NewTouchPhase.Stationary: return PointerTouchPhase.Stationary;
				case NewTouchPhase.Ended: return PointerTouchPhase.Ended;
				case NewTouchPhase.Canceled: return PointerTouchPhase.Canceled;
				default: return PointerTouchPhase.None;
			}
		}

		public Vector3 MousePosition =>
			Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;

		public bool MouseLeftPressedThisFrame =>
			Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
		public bool MouseLeftHeld =>
			Mouse.current != null && Mouse.current.leftButton.isPressed;

		public bool MouseRightPressedThisFrame =>
			Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
		public bool MouseRightHeld =>
			Mouse.current != null && Mouse.current.rightButton.isPressed;

		// Raw scroll.y is ~120 per notch on Windows; legacy Input.GetAxis returns
		// ~0.1 per notch. /1200f normalizes to the legacy scale so both backends feed
		// the same magnitude into ZoomSensitivity.
		public float MouseScrollY =>
			Mouse.current != null ? Mouse.current.scroll.ReadValue().y / 1200f : 0f;
	}
#else
	internal sealed class PointerInput : IPointerInput
	{
		public void EnableTouchSupport() { /* legacy touch is auto-enabled */ }

		public int TouchCount => Input.touchCount;
		public Vector2 GetTouchPosition(int index) => Input.GetTouch(index).position;
		public float GetTouchDeltaY(int index) => Input.GetTouch(index).deltaPosition.y;
		public int GetTouchId(int index) => Input.GetTouch(index).fingerId;
		public int GetTouchPointerId(int index) => Input.GetTouch(index).fingerId;

		public PointerTouchPhase GetTouchPhase(int index)
		{
			switch (Input.GetTouch(index).phase)
			{
				case TouchPhase.Began: return PointerTouchPhase.Began;
				case TouchPhase.Moved: return PointerTouchPhase.Moved;
				case TouchPhase.Stationary: return PointerTouchPhase.Stationary;
				case TouchPhase.Ended: return PointerTouchPhase.Ended;
				case TouchPhase.Canceled: return PointerTouchPhase.Canceled;
				default: return PointerTouchPhase.None;
			}
		}

		public Vector3 MousePosition => Input.mousePosition;
		public bool MouseLeftPressedThisFrame => Input.GetMouseButtonDown(0);
		public bool MouseLeftHeld => Input.GetMouseButton(0);
		public bool MouseRightPressedThisFrame => Input.GetMouseButtonDown(1);
		public bool MouseRightHeld => Input.GetMouseButton(1);

		// Input.GetAxis("Mouse ScrollWheel") returns 0 when not scrolling, so we
		// don't need the historic Mathf.Abs(mouseScrollDelta) > 0 short-circuit.
		public float MouseScrollY => Input.GetAxis("Mouse ScrollWheel");
	}
#endif
}
