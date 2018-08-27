// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
//
// Copyright (c) 2018 Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Creator Agreement, located
// here: https://id.magicleap.com/creator-terms
//
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System.Collections;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// Class to visualize controller inputs
    /// </summary>
    [RequireComponent(typeof(ControllerConnectionHandler))]
    public class ControllerVisualizer : MonoBehaviour
    {
        #region Private Variables
        [SerializeField, Tooltip("The controller model.")]
        private GameObject _controllerModel;

        [Header("Controller Parts"), Space]
        [SerializeField, Tooltip("The controller's trigger model.")]
        private GameObject _trigger;

        [SerializeField, Tooltip("The controller's touchpad model.")]
        private GameObject _touchpad;

        [SerializeField, Tooltip("The controller's home button model.")]
        private GameObject _homeButton;

        [SerializeField, Tooltip("The controller's bumper button model.")]
        private GameObject _bumperButton;

        [SerializeField, Tooltip("The Game Object showing the touch model on the touchpad")]
        private Transform _touchIndicatorTransform;

        // Color when the button state is idle.
        private Color _defaultColor = Color.white;
        // Color when the button state is active.
        private Color _activeColor = Color.grey;

        private Material _touchpadMaterial;
        private Material _triggerMaterial;
        private Material _homeButtonMaterial;
        private Material _bumperButtonMaterial;

        private float _touchpadRadius;

        private ControllerConnectionHandler _controllerConnectionHandler;
        private bool _wasControllerValid = true;

        private const float MAX_TRIGGER_ROTATION = 35.0f;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Initialize variables, callbacks and check null references.
        /// </summary>
        void Start()
        {
            _controllerConnectionHandler = GetComponent<ControllerConnectionHandler>();

            if (!_controllerConnectionHandler.enabled)
            {
                Debug.LogError("Error ControllerVisualizer starting MLInput, disabling script.");
                enabled = false;
                return;
            }
            if (!_controllerModel)
            {
                Debug.LogError("Error ControllerVisualizer._controllerModel not set, disabling script.");
                enabled = false;
                return;
            }
            if (!_trigger)
            {
                Debug.LogError("Error ControllerVisualizer._trigger not set, disabling script.");
                enabled = false;
                return;
            }
            if (!_touchpad)
            {
                Debug.LogError("Error ControllerVisualizer._touchpad not set, disabling script.");
                enabled = false;
                return;
            }
            if (!_homeButton)
            {
                Debug.LogError("Error ControllerVisualizer._homeButton not set, disabling script.");
                enabled = false;
                return;
            }
            if (!_bumperButton)
            {
                Debug.LogError("Error ControllerVisualizer._bumperButton not set, disabling script.");
                enabled = false;
                return;
            }
            if (!_touchIndicatorTransform)
            {
                Debug.LogError("Error ControllerVisualizer._touchIndicatorTransform not set, disabling script.");
                enabled = false;
                return;
            }

            SetVisibility(_controllerConnectionHandler.IsControllerValid());

            MLInput.OnControllerButtonUp += HandleOnButtonUp;
            MLInput.OnControllerButtonDown += HandleOnButtonDown;

            _triggerMaterial = FindMaterial(_trigger);
            _touchpadMaterial = FindMaterial(_touchpad);
            _homeButtonMaterial = FindMaterial(_homeButton);
            _bumperButtonMaterial = FindMaterial(_bumperButton);

            // Calculate the radius of the touchpad's mesh
            Mesh mesh = _touchpad.GetComponent<MeshFilter>().mesh;
            _touchpadRadius = Vector3.Scale(mesh.bounds.extents, _touchpad.transform.lossyScale).x;
        }

        /// <summary>
        /// Update controller input based feedback.
        /// </summary>
        void Update()
        {
            UpdateTriggerVisuals();
            UpdateTouchpadIndicator();
            SetVisibility(_controllerConnectionHandler.IsControllerValid());
        }

        /// <summary>
        /// Stop input api and unregister callbacks.
        /// </summary>
        void OnDestroy()
        {
            if (MLInput.IsStarted)
            {
                MLInput.OnControllerButtonDown -= HandleOnButtonDown;
                MLInput.OnControllerButtonUp -= HandleOnButtonUp;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Sets the visual pressure indicator for the appropriate button MeshRenderers.
        /// <param name="renderer">The meshrenderer to modify.</param>
        /// <param name="pressure">The pressure sensitivy interpolant for the meshrendere.r</param>
        /// </summary>
        private void SetPressure(MeshRenderer renderer, float pressure)
        {
            if (renderer.material.HasProperty("_Cutoff"))
            {
                renderer.material.SetFloat("_Cutoff", pressure);
            }
        }

        /// <summary>
        /// Update the touchpad's indicator: (location, directions, color).
        /// Also updates the color of the touchpad, based on pressure.
        /// </summary>
        private void UpdateTouchpadIndicator()
        {
            if (!_controllerConnectionHandler.IsControllerValid())
            {
                return;
            }
            MLInputController controller = _controllerConnectionHandler.ConnectedController;
            Vector3 updatePosition = new Vector3(controller.Touch1PosAndForce.x, 0.0f, controller.Touch1PosAndForce.y);
            float touchY = _touchIndicatorTransform.localPosition.y;
            _touchIndicatorTransform.localPosition = new Vector3(updatePosition.x * _touchpadRadius, touchY, updatePosition.z * _touchpadRadius);

            if (controller.Touch1Active)
            {
                _touchIndicatorTransform.gameObject.SetActive(true);
                float angle = Mathf.Atan2(controller.Touch1PosAndForce.x, controller.Touch1PosAndForce.y);
                _touchIndicatorTransform.localRotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);
            }
            else
            {
                _touchIndicatorTransform.gameObject.SetActive(false);
            }

            float force = controller.Touch1PosAndForce.z;
            _touchpadMaterial.color = Color.Lerp(_defaultColor, _activeColor, force);
        }

        /// <summary>
        /// Update the rotation and visual color of the trigger.
        /// </summary>
        private void UpdateTriggerVisuals()
        {
            if (!_controllerConnectionHandler.IsControllerValid())
            {
                return;
            }
            MLInputController controller = _controllerConnectionHandler.ConnectedController;

            // Change the color of the trigger
            _triggerMaterial.color = Color.Lerp(_defaultColor, _activeColor, controller.TriggerValue);

            // Set the rotation of the trigger
            Vector3 eulerRot = _trigger.transform.localRotation.eulerAngles;
            eulerRot.x = Mathf.Lerp(0, MAX_TRIGGER_ROTATION, controller.TriggerValue);
            _trigger.transform.localRotation = Quaternion.Euler(eulerRot);
        }

        /// <summary>
        /// Attempt to get the Material of a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to search for a material.</param>
        /// <returns>Material of the GameObject, if it exists. Otherwise, null.</returns>
        private Material FindMaterial(GameObject gameObject)
        {
            MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
            return (renderer != null) ? renderer.material : null;
        }

        /// <summary>
        /// Sets the color of all Materials.
        /// </summary>
        /// <param name="color">The color to be applied to the materials.</param>
        private void SetAllMaterialColors(Color color)
        {
            _triggerMaterial.color = color;
            _touchpadMaterial.color = color;
            _homeButtonMaterial.color = color;
            _bumperButtonMaterial.color = color;
        }

        /// <summary>
        /// Coroutine to reset the home color back to the original color.
        /// </summary>
        private IEnumerator RestoreHomeColor()
        {
            yield return new WaitForSeconds(0.5f);
            _homeButtonMaterial.color = _defaultColor;
        }

        /// <summary>
        /// Set object visibility to value.
        /// </summary>
        /// <param name="value"> true or false to set visibility. </param>
        private void SetVisibility(bool value)
        {
            if (_wasControllerValid == value)
            {
                return;
            }

            Renderer[] rendererArray = _controllerModel.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in rendererArray)
            {
                r.enabled = value;
            }

            _wasControllerValid = value;
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles the event for button down.
        /// </summary>
        /// <param name="controller_id">The id of the controller.</param>
        /// <param name="button">The button that is being pressed.</param>
        private void HandleOnButtonDown(byte controllerId, MLInputControllerButton button)
        {
            MLInputController controller = _controllerConnectionHandler.ConnectedController;
            if (controller != null && controller.Id == controllerId &&
                button == MLInputControllerButton.Bumper)
            {
                // Sets the color of the Bumper to the active color.
                _bumperButtonMaterial.color = _activeColor;
            }
        }

        /// <summary>
        /// Handles the event for button up.
        /// </summary>
        /// <param name="controller_id">The id of the controller.</param>
        /// <param name="button">The button that is being released.</param>
        private void HandleOnButtonUp(byte controllerId, MLInputControllerButton button)
        {
            MLInputController controller = _controllerConnectionHandler.ConnectedController;
            if (controller != null && controller.Id == controllerId)
            {
                if (button == MLInputControllerButton.Bumper)
                {
                    // Sets the color of the Bumper to the default color.
                    _bumperButtonMaterial.color = _defaultColor;
                }

                else if (button == MLInputControllerButton.HomeTap)
                {
                    // Note: HomeTap is NOT a button. It's a physical button on the controller.
                    // But in the application side, the tap registers as a ButtonUp event and there is NO
                    // ButtonDown equivalent. We cannot detect holding down the Home (button). The OS will
                    // handle it as either a return to the icon grid or turning off the controller.
                    _homeButtonMaterial.color = _activeColor;
                    StartCoroutine(RestoreHomeColor());
                }
            }
        }
        #endregion
    }
}
