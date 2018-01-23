using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Lean.Touch
{
	// If you add this component to your scene, then it will convert all mouse and touch data into easy to use data
	// You can access this data via Lean.Touch.LeanTouch.Instance.Fingers, or hook into the Lean.Touch.LeanTouch.On___ events.
	// NOTE: To prevent lag you may want to edit your ScriptExecutionOrder to force this to update before your scripts
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	public partial class LeanTouch : MonoBehaviour
	{
		// This contains all the active and enabled LeanTouch instances
		public static List<LeanTouch> Instances = new List<LeanTouch>();

		// This list contains all currently active fingers (including simulated ones)
		public static List<LeanFinger> Fingers = new List<LeanFinger>(10);

		// This list contains all currently inactive fingers (this allows for pooling and tapping)
		public static List<LeanFinger> InactiveFingers = new List<LeanFinger>(10);

		// This gets fired when a finger begins touching the screen (LeanFinger = The current finger)
		public static System.Action<LeanFinger> OnFingerDown;

		// This gets fired every frame a finger is touching the screen (LeanFinger = The current finger)
		public static System.Action<LeanFinger> OnFingerSet;

		// This gets fired when a finger stops touching the screen (LeanFinger = The current finger)
		public static System.Action<LeanFinger> OnFingerUp;

		// This gets fired when a finger taps the screen (this is when a finger begins and stops touching the screen within the 'TapThreshold' time) (LeanFinger = The current finger)
		public static System.Action<LeanFinger> OnFingerTap;

		// This gets fired when a finger swipes the screen (this is when a finger begins and stops touching the screen within the 'TapThreshold' time, and also moves more than the 'SwipeThreshold' distance) (LeanFinger = The current finger)
		public static System.Action<LeanFinger> OnFingerSwipe;

		// This gets fired every frame at least one finger is touching the screen (List = Fingers)
		public static System.Action<List<LeanFinger>> OnGesture;

		[Tooltip("This allows you to set how many seconds are required between a finger down/up for a tap to be registered")]
		public float TapThreshold = DefaultTapThreshold;

		public const float DefaultTapThreshold = 0.5f;

		public static float CurrentTapThreshold
		{
			get
			{
				return Instances.Count > 0 ? Instances[0].TapThreshold : DefaultTapThreshold;
			}
		}

		[Tooltip("This allows you to set how many pixels of movement (relative to the ReferenceDpi) are required within the TapThreshold for a swipe to be triggered")]
		public float SwipeThreshold = DefaultSwipeThreshold;

		public const float DefaultSwipeThreshold = 50.0f;

		public static float CurrentSwipeThreshold
		{
			get
			{
				return Instances.Count > 0 ? Instances[0].SwipeThreshold : DefaultSwipeThreshold;
			}
		}

		[Tooltip("This allows you to set the default DPI you want the input scaling to be based on")]
		public int ReferenceDpi = DefaultReferenceDpi;

		public const int DefaultReferenceDpi = 200;

		public static int CurrentReferenceDpi
		{
			get
			{
				return Instances.Count > 0 ? Instances[0].ReferenceDpi : DefaultReferenceDpi;
			}
		}

		[Tooltip("This allows you to set which layers your GUI is on, so it can be ignored by each finger")]
		public LayerMask GuiLayers = Physics.DefaultRaycastLayers;

		public static LayerMask CurrentGuiLayers
		{
			get
			{
				return Instances.Count > 0 ? Instances[0].GuiLayers : (LayerMask)Physics.DefaultRaycastLayers;
			}
		}

		[Tooltip("This allows you to enable recording of finger movements")]
		public bool RecordFingers = true;

		[Tooltip("This allows you to set the amount of pixels a finger must move for another snapshot to be stored")]
		public float RecordThreshold = 5.0f;

		[Tooltip("This allows you to set the maximum amount of seconds that can be recorded, 0 = unlimited")]
		public float RecordLimit = 10.0f;

		[Tooltip("This allows you to simulate multi touch inputs on devices that don't support them (e.g. desktop)")]
		public bool SimulateMultiFingers = true;

		[Tooltip("This allows you to set which key is required to simulate multi key twisting")]
		public KeyCode PinchTwistKey = KeyCode.LeftControl;

		[Tooltip("This allows you to set which key is required to simulate multi key dragging")]
		public KeyCode MultiDragKey = KeyCode.LeftAlt;

		[Tooltip("This allows you to set which texture will be used to show the simulated fingers")]
		public Texture2D FingerTexture;

		// This stores the highest mouse button index
		private static int highestMouseButton = 7;

		// Used to find if the GUI is in use
		private static List<RaycastResult> tempRaycastResults = new List<RaycastResult>(10);

		// Used to return non GUI fingers
		private static List<LeanFinger> filteredFingers = new List<LeanFinger>(10);

		// Used by RaycastGui
		private static PointerEventData tempPointerEventData;

		// Used by RaycastGui
		private static EventSystem tempEventSystem;

		// Returns the main instance
		public static LeanTouch Instance
		{
			get
			{
				return Instances.Count > 0 ? Instances[0] : null;
			}
		}

		// If you multiply this value with any other pixel delta (e.g. ScreenDelta), then it will become device resolution independant
		public static float ScalingFactor
		{
			get
			{
				// Get the current screen DPI
				var dpi = Screen.dpi;

				// If it's 0 or less, it's invalid, so return the default scale of 1.0
				if (dpi <= 0)
				{
					return 1.0f;
				}

				// DPI seems valid, so scale it against the reference DPI
				return Mathf.Sqrt(CurrentReferenceDpi) / Mathf.Sqrt(dpi);
			}
		}

		// Returns true if any mouse button is pressed
		public static bool AnyMouseButtonSet
		{
			get
			{
				for (var i = 0; i < highestMouseButton; i++)
				{
					if (Input.GetMouseButton(i) == true)
					{
						return true;
					}
				}

				return false;
			}
		}

		// This will return true if the mouse or any finger is currently using the GUI
		public static bool GuiInUse
		{
			get
			{
				// Legacy GUI in use?
				if (GUIUtility.hotControl > 0)
				{
					return true;
				}

				// New GUI in use?
				for (var i = Fingers.Count - 1; i >= 0; i--)
				{
					if (Fingers[i].StartedOverGui == true)
					{
						return true;
					}
				}

				return false;
			}
		}

		// If currentCamera is null, this will return the camera attached to gameObject, or return Camera.main
		public static Camera GetCamera(Camera currentCamera, GameObject gameObject = null)
		{
			if (currentCamera == null)
			{
				if (gameObject != null)
				{
					currentCamera = gameObject.GetComponent<Camera>();
				}

				if (currentCamera == null)
				{
					currentCamera = Camera.main;
				}
			}

			return currentCamera;
		}

		// Return the framerate independant damping factor
		public static float GetDampenFactor(float dampening, float deltaTime)
		{
			if (Application.isPlaying == false)
			{
				return 1.0f;
			}

			return 1.0f - Mathf.Exp(-dampening * deltaTime);
		}

		// This will return true if the 'screenPosition' is over any GUI elements
		public static bool PointOverGui(Vector2 screenPosition)
		{
			return RaycastGui(screenPosition).Count > 0;
		}

		// This will return all the RaycastResults under the 'screenPosition' using the current layerMask
		// The first result (0) should be the top most UI element
		public static List<RaycastResult> RaycastGui(Vector2 screenPosition)
		{
			return RaycastGui(screenPosition, CurrentGuiLayers);
		}

		// This will return all the RaycastResults under the 'screenPosition' using the specified layerMask
		// The first result (0) should be the top most UI element
		public static List<RaycastResult> RaycastGui(Vector2 screenPosition, LayerMask layerMask)
		{
			tempRaycastResults.Clear();

			var currentEventSystem = EventSystem.current;

			if (currentEventSystem != null)
			{
				// Create point event data for this event system?
				if (currentEventSystem != tempEventSystem)
				{
					tempEventSystem = currentEventSystem;

					if (tempPointerEventData == null)
					{
						tempPointerEventData = new PointerEventData(tempEventSystem);
					}
					else
					{
						tempPointerEventData.Reset();
					}
				}

				// Raycast event system at the specified point
				tempPointerEventData.position = screenPosition;

				currentEventSystem.RaycastAll(tempPointerEventData, tempRaycastResults);

				// Loop through all results and remove any that don't match the layer mask
				if (tempRaycastResults.Count > 0)
				{
					for (var i = tempRaycastResults.Count - 1; i >= 0; i--)
					{
						var raycastResult = tempRaycastResults[i];
						var raycastLayer  = 1 << raycastResult.gameObject.layer;

						if ((raycastLayer & layerMask) == 0)
						{
							tempRaycastResults.RemoveAt(i);
						}
					}
				}
			}

			return tempRaycastResults;
		}

		// If ignoreGuiFingers is set, Fingers will be filtered to remove any with StartedOverGui
		// If requiredFingerCount is greather than 0, this method will return null if the finger count doesn't match
		// If requiredSelectable is set, and its SelectingFinger isn't null, it will return just that finger
		public static List<LeanFinger> GetFingers(bool ignoreGuiFingers, int requiredFingerCount = 0, LeanSelectable requiredSelectable = null)
		{
			filteredFingers.Clear();

			if (requiredSelectable != null && requiredSelectable.SelectingFinger != null)
			{
				filteredFingers.Add(requiredSelectable.SelectingFinger);

				return filteredFingers;
			}

			for (var i = 0; i < Fingers.Count; i++)
			{
				var finger = Fingers[i];

				if (ignoreGuiFingers == true)
				{
					if (finger.StartedOverGui == false)
					{
						filteredFingers.Add(finger);
					}
				}
				else
				{
					filteredFingers.Add(finger);
				}
			}

			if (requiredFingerCount > 0)
			{
				if (filteredFingers.Count != requiredFingerCount)
				{
					return null;
				}
			}

			return filteredFingers;
		}

		protected virtual void Awake()
		{
#if UNITY_EDITOR
			// Set the finger texture?
			if (FingerTexture == null)
			{
				var guids = UnityEditor.AssetDatabase.FindAssets("FingerVisualization t:texture2d");

				if (guids.Length > 0)
				{
					var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);

					FingerTexture = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path) as Texture2D;
				}
			}
#endif
		}

		protected virtual void OnEnable()
		{
			Instances.Add(this);
		}

		protected virtual void OnDisable()
		{
			Instances.Remove(this);
		}

		protected virtual void Update()
		{
			// Only run the update methods if this is the first instance (i.e. if your scene has more than one LeanTouch component, only use the first)
			if (Instances[0] == this)
			{
				// Prepare old finger data for new information
				BeginFingers();

				// Poll current finger + mouse data and conver it to fingers
				PollFingers();

				// Process any no longer used fingers
				EndFingers();

				// Update events based on new finger data
				UpdateEvents();
			}
		}

		protected virtual void OnGUI()
		{
			// Show simulated multi fingers?
			if (FingerTexture != null && Input.touchCount == 0 && Fingers.Count > 1)
			{
				for (var i = Fingers.Count - 1; i >= 0; i--)
				{
					var finger = Fingers[i];

					// Don't show fingers that just went up, because real touches will be up the frame they release
					if (finger.Up == false)
					{
						var screenPosition = finger.ScreenPosition;
						var screenRect     = new Rect(0, 0, FingerTexture.width, FingerTexture.height);

						screenRect.center = new Vector2(screenPosition.x, Screen.height - screenPosition.y);

						GUI.DrawTexture(screenRect, FingerTexture);
					}
				}
			}
		}

		// Update all Fingers and InactiveFingers so they're ready for the new frame
		private void BeginFingers()
		{
			// Age inactive fingers
			for (var i = InactiveFingers.Count - 1; i >= 0; i--)
			{
				InactiveFingers[i].Age += Time.unscaledDeltaTime;
			}

			// Reset finger data
			for (var i = Fingers.Count - 1; i >= 0; i--)
			{
				var finger = Fingers[i];

				// Was this set to up last time? If so, it's now inactive
				if (finger.Up == true)
				{
					// Make finger inactive
					Fingers.RemoveAt(i); InactiveFingers.Add(finger);

					// Reset age so we can time how long it's been inactive
					finger.Age = 0.0f;

					// Pool old snapshots
					finger.ClearSnapshots();
				}
				else
				{
					finger.LastSet            = finger.Set;
					finger.LastScreenPosition = finger.ScreenPosition;

					finger.Set   = false;
					finger.Tap   = false;
					finger.Swipe = false;
				}
			}
		}

		// Update all Fingers based on the new finger data
		private void EndFingers()
		{
			for (var i = Fingers.Count - 1; i >= 0; i--)
			{
				var finger = Fingers[i];

				// Up?
				if (finger.Up == true)
				{
					// Tap or Swipe?
					if (finger.Age <= TapThreshold)
					{
						if (finger.SwipeScreenDelta.magnitude * ScalingFactor < SwipeThreshold)
						{
							finger.Tap       = true;
							finger.TapCount += 1;
						}
						else
						{
							finger.TapCount = 0;
							finger.Swipe    = true;
						}
					}
					else
					{
						finger.TapCount = 0;
					}
				}
				// Down?
				else if (finger.Down == false)
				{
					// Age it
					finger.Age += Time.unscaledDeltaTime;
				}
			}
		}

		// Read new hardware finger data
		private void PollFingers()
		{
			// Update real fingers
			if (Input.touchCount > 0)
			{
				for (var i = 0; i < Input.touchCount; i++)
				{
					var touch = Input.GetTouch(i);

					// Only poll fingers that are active?
					if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved)
					{
						AddFinger(touch.fingerId, touch.position);
					}
				}
			}
			// If there are no real touches, simulate some from the mouse?
			else if (AnyMouseButtonSet == true)
			{
				var screen        = new Rect(0, 0, Screen.width, Screen.height);
				var mousePosition = (Vector2)Input.mousePosition;

				// Is the mouse within the screen?
				if (screen.Contains(mousePosition) == true)
				{
					AddFinger(0, mousePosition);

					// Simulate pinch & twist?
					if (SimulateMultiFingers == true)
					{
						if (Input.GetKey(PinchTwistKey) == true)
						{
							var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

							AddFinger(1, center - (mousePosition - center));
						}
						// Simulate multi drag?
						else if (Input.GetKey(MultiDragKey) == true)
						{
							AddFinger(1, mousePosition);
						}
					}
				}
			}
		}

		private void UpdateEvents()
		{
			var fingerCount = Fingers.Count;

			if (fingerCount > 0)
			{
				for (var i = 0; i < fingerCount; i++)
				{
					var finger = Fingers[i];

					if (finger.Down  == true && OnFingerDown  != null) OnFingerDown(finger);
					if (finger.Set   == true && OnFingerSet   != null) OnFingerSet(finger);
					if (finger.Up    == true && OnFingerUp    != null) OnFingerUp(finger);
					if (finger.Tap   == true && OnFingerTap   != null) OnFingerTap(finger);
					if (finger.Swipe == true && OnFingerSwipe != null) OnFingerSwipe(finger);
				}

				if (OnGesture != null)
				{
					filteredFingers.Clear();
					filteredFingers.AddRange(Fingers);

					OnGesture(filteredFingers);
				}
			}
		}

		// Add a finger based on index, or return the existing one
		private void AddFinger(int index, Vector2 screenPosition)
		{
			var finger = FindFinger(index);

			// No finger found?
			if (finger == null)
			{
				var inactiveIndex = FindInactiveFingerIndex(index);

				// Use inactive finger?
				if (inactiveIndex >= 0)
				{
					finger = InactiveFingers[inactiveIndex]; InactiveFingers.RemoveAt(inactiveIndex);

					// Inactive for too long?
					if (finger.Age > TapThreshold)
					{
						finger.TapCount = 0;
					}

					// Reset values
					finger.Age     = 0.0f;
					finger.Set     = false;
					finger.LastSet = false;
					finger.Tap     = false;
					finger.Swipe   = false;
				}
				// Create new finger?
				else
				{
					finger = new LeanFinger();

					finger.Index = index;
				}

				finger.StartScreenPosition = screenPosition;
				finger.LastScreenPosition  = screenPosition;
				finger.ScreenPosition      = screenPosition;
				finger.StartedOverGui      = finger.IsOverGui;

				Fingers.Add(finger);
			}

			finger.Set            = true;
			finger.ScreenPosition = screenPosition;

			// Record?
			if (RecordFingers == true)
			{
				// Too many snapshots?
				if (RecordLimit > 0.0f)
				{
					if (finger.SnapshotDuration > RecordLimit)
					{
						var removeCount = LeanSnapshot.GetLowerIndex(finger.Snapshots, finger.Age - RecordLimit);

						finger.ClearSnapshots(removeCount);
					}
				}

				// Record snapshot?
				if (RecordThreshold > 0.0f)
				{
					if (finger.Snapshots.Count == 0 || finger.LastSnapshotScreenDelta.magnitude >= RecordThreshold)
					{
						finger.RecordSnapshot();
					}
				}
				else
				{
					finger.RecordSnapshot();
				}
			}
		}

		// Find the finger with the specified index, or return null
		private LeanFinger FindFinger(int index)
		{
			for (var i = Fingers.Count - 1; i>= 0; i--)
			{
				var finger = Fingers[i];

				if (finger.Index == index)
				{
					return finger;
				}
			}

			return null;
		}

		// Find the index of the inactive finger with the specified index, or return -1
		private int FindInactiveFingerIndex(int index)
		{
			for (var i = InactiveFingers.Count - 1; i>= 0; i--)
			{
				if (InactiveFingers[i].Index == index)
				{
					return i;
				}
			}

			return -1;
		}
	}
}