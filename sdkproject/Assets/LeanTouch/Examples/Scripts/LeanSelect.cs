using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Lean.Touch
{
	// This script allows you to select LeanSelectable components
	public class LeanSelect : MonoBehaviour
	{
		public enum SelectType
		{
			Raycast3D,
			Overlap2D,
			CanvasUI
		}

		public enum SearchType
		{
			GetComponent,
			GetComponentInParent,
			GetComponentInChildren
		}

		public enum ReselectType
		{
			KeepSelected,
			Deselect,
			DeselectAndSelect,
			SelectAgain
		}

		public SelectType SelectUsing;

		[Tooltip("This stores the layers we want the raycast/overlap to hit (make sure this GameObject's layer is included!)")]
		public LayerMask LayerMask = Physics.DefaultRaycastLayers;

		[Tooltip("The camera used to calculate the ray (None = MainCamera)")]
		public Camera Camera;

		[Tooltip("How should the selected GameObject be searched for the LeanSelectable component?")]
		public SearchType Search;

		[Tooltip("The currently selected LeanSelectables")]
		public List<LeanSelectable> CurrentSelectables;

		[Tooltip("If you select an already selected selectable, what should happen?")]
		public ReselectType Reselect;

		[Tooltip("Automatically deselect the CurrentSelectable if Select gets called with null?")]
		public bool AutoDeselect;

		[Tooltip("Automatically deselect a LeanSelectable when the selecting finger goes up?")]
		public bool DeselectOnUp;

		[Tooltip("The maximum amount of selectables that can be selected at once (0 = Unlimited)")]
		public int MaxSelectables;

		// NOTE: This must be called from somewhere
		public void SelectStartScreenPosition(LeanFinger finger)
		{
			SelectScreenPosition(finger, finger.StartScreenPosition);
		}

		// NOTE: This must be called from somewhere
		public void SelectScreenPosition(LeanFinger finger)
		{
			SelectScreenPosition(finger, finger.ScreenPosition);
		}

		// NOTE: This must be called from somewhere
		public void SelectScreenPosition(LeanFinger finger, Vector2 screenPosition)
		{
			// Stores the component we hit (Collider or Collider2D)
			var component = default(Component);

			switch (SelectUsing)
			{
				case SelectType.Raycast3D:
				{
					// Make sure the camera exists
					var camera = LeanTouch.GetCamera(Camera, gameObject);

					if (camera != null)
					{
						var ray = camera.ScreenPointToRay(screenPosition);
						var hit = default(RaycastHit);

						if (Physics.Raycast(ray, out hit, float.PositiveInfinity, LayerMask) == true)
						{
							component = hit.collider;
						}
					}
				}
				break;

				case SelectType.Overlap2D:
				{
					// Make sure the camera exists
					var camera = LeanTouch.GetCamera(Camera, gameObject);

					if (camera != null)
					{
						var point = camera.ScreenToWorldPoint(screenPosition);

						component = Physics2D.OverlapPoint(point, LayerMask);
					}
				}
				break;

				case SelectType.CanvasUI:
				{
					var results = LeanTouch.RaycastGui(screenPosition, LayerMask);

					if (results != null && results.Count > 0)
					{
						component = results[0].gameObject.transform;
					}
				}
				break;
			}

			// Select the component
			Select(finger, component);
		}

		public void Select(LeanFinger finger, Component component)
		{
			// Stores the selectable we will search for
			var selectable = default(LeanSelectable);

			// Was a collider found?
			if (component != null)
			{
				switch (Search)
				{
					case SearchType.GetComponent:           selectable = component.GetComponent          <LeanSelectable>(); break;
					case SearchType.GetComponentInParent:   selectable = component.GetComponentInParent  <LeanSelectable>(); break;
					case SearchType.GetComponentInChildren: selectable = component.GetComponentInChildren<LeanSelectable>(); break;
				}
			}

			// Select the selectable
			Select(finger, selectable);
		}

		public LeanSelectable FindSelectable(LeanFinger finger)
		{
			for (var i = CurrentSelectables.Count - 1; i >= 0; i--)
			{
				var currentSelectable = CurrentSelectables[i];

				if (currentSelectable.SelectingFinger == finger)
				{
					return currentSelectable;
				}
			}

			return null;
		}

		public bool IsSelected(LeanSelectable selectable)
		{
			// Loop through all current selectables
			for (var i = CurrentSelectables.Count - 1; i >= 0; i--)
			{
				var currentSelectable = CurrentSelectables[i];

				if (currentSelectable == selectable)
				{
					return true;
				}
			}
			return false;
		}

		public void Select(LeanFinger finger, List<LeanSelectable> selectables)
		{
			var selectableCount = 0;

			// Deselect missing selectables
			if (CurrentSelectables != null)
			{
				for (var i = CurrentSelectables.Count - 1; i >= 0; i--)
				{
					var currentSelectable = CurrentSelectables[i];

					if (currentSelectable != null)
					{
						if (selectables != null && selectables.Contains(currentSelectable) == false)
						{
							CurrentSelectables.RemoveAt(i);

							currentSelectable.Deselect();
						}
					}
				}
			}

			// Add new selectables
			if (selectables != null)
			{
				for (var i = selectables.Count - 1; i >= 0; i--)
				{
					var selectable = selectables[i];

					if (selectable != null)
					{
						if (CurrentSelectables == null || CurrentSelectables.Contains(selectable) == false)
						{
							Select(finger, selectable);
						}

						selectableCount += 1;
					}
				}
			}

			// Nothing was selected?
			if (selectableCount == 0)
			{
				// Deselect?
				if (AutoDeselect == true)
				{
					DeselectAll();
				}
			}
		}

		public void Select(LeanFinger finger, LeanSelectable selectable)
		{
			// Something was selected?
			if (selectable != null && selectable.isActiveAndEnabled == true)
			{
				if (selectable.HideWithFinger == true)
				{
					for (var i = CurrentSelectables.Count - 1; i >= 0; i--)
					{
						var currentSelectable = CurrentSelectables[i];

						if (currentSelectable.HideWithFinger == true && currentSelectable.IsSelected == true)
						{
							return;
						}
					}
				}

				// Did we select a new LeanSelectable?
				if (IsSelected(selectable) == false)
				{
					// Deselect some if we have too many
					if (MaxSelectables > 0)
					{
						var extras = CurrentSelectables.Count - MaxSelectables + 1;

						for (var i = extras - 1; i >= 0; i--)
						{
							var currentSelectable = CurrentSelectables[i];

							currentSelectable.Deselect();

							CurrentSelectables.RemoveAt(i);
						}
					}

					// Add to selection and select
					CurrentSelectables.Add(selectable);

					selectable.Select(finger);
				}
				// Did we reselect the current LeanSelectable?
				else
				{
					switch (Reselect)
					{
						case ReselectType.Deselect:
						{
							selectable.Deselect();

							CurrentSelectables.Remove(selectable);
						}
						break;

						case ReselectType.DeselectAndSelect:
						{
							// Change current
							selectable.Deselect();

							// Call select event on current
							selectable.Select(finger);
						}
						break;

						case ReselectType.SelectAgain:
						{
							// Call select event on current
							selectable.Select(finger);
						}
						break;
					}
				}
			}
			// Nothing was selected?
			else
			{
				// Deselect?
				if (AutoDeselect == true)
				{
					DeselectAll();
				}
			}
		}

		[ContextMenu("Deselect All")]
		public void DeselectAll()
		{
			// Loop through all current selectables and deselect if not null
			if (CurrentSelectables != null)
			{
				for (var i = CurrentSelectables.Count - 1; i >= 0; i--)
				{
					var currentSelectable = CurrentSelectables[i];

					if (currentSelectable != null)
					{
						currentSelectable.Deselect();
					}
				}

				// Clear
				CurrentSelectables.Clear();
			}
		}

		protected virtual void Update()
		{
			if (DeselectOnUp == true)
			{
				if (CurrentSelectables != null)
				{
					for (var i = CurrentSelectables.Count - 1; i >= 0; i--)
					{
						var currentSelectable = CurrentSelectables[i];

						if (currentSelectable != null)
						{
							// Selecting finger no longer down?
							if (currentSelectable.SelectingFinger != null && currentSelectable.SelectingFinger.Set == false)
							{
								currentSelectable.Deselect();

								CurrentSelectables.RemoveAt(i);
							}
						}
					}
				}
			}
		}
	}
}