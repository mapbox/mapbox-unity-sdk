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

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using System;

namespace MagicLeap
{
    /// <summary>
    /// This represents a virtual controller visualization that mimics the current state of the
    /// Mobile Device running the Magic Leap Mobile Application. Button presses, touch pad are all represented along with
    /// the orientation of the mobile device. There is no position information available
    /// </summary>
    [RequireComponent(typeof(ControllerConnectionHandler))]
    public class MCAExample : MonoBehaviour
    {
        #region Private Variables
        [SerializeField, Tooltip("The highlight for the left button.")]
        private GameObject _leftButtonHighlight;

        [SerializeField, Tooltip("The highlight for the right button.")]
        private GameObject _rightButtonHighlight;

        [SerializeField, Tooltip("The indicator for the home tap.")]
        private GameObject _homeTapIndicator;

        [SerializeField, Tooltip("Number of seconds to show home tap.")]
        private float _homeActiveDuration = 0.5f;
        private float _timeToDeactivateHome = 0;

        [SerializeField, Tooltip("The indicator for the first touch.")]
        private GameObject _touch1Indicator;

        [SerializeField, Tooltip("The indicator for the second touch.")]
        private GameObject _touch2Indicator;

        [SerializeField, Tooltip("The keyboard input text.")]
        private Text _keyboardText;

        [SerializeField, Tooltip("Renderer of the Mesh")]
        private MeshRenderer _modelRenderer;

        private Color _origColor;

        private ControllerConnectionHandler _controllerConnectionHandler;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Initializes component data, starts MLInput, validates parameters, initializes indicator states
        /// </summary>
        void Awake()
        {
            MLResult result = MLInput.Start();
            if (!result.IsOk)
            {
                Debug.LogError("Error MCAExample starting MLInput, disabling script.");
                enabled = false;
                return;
            }
            if (_leftButtonHighlight == null)
            {
                Debug.LogError("Error MCAExample._moveButtonHighlight is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_rightButtonHighlight == null)
            {
                Debug.LogError("Error MCAExample._appButtonHighlight is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_homeTapIndicator == null)
            {
                Debug.LogError("Error MCAExample._homeTapIndicator is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_touch1Indicator == null)
            {
                Debug.LogError("Error MCAExample._touch1Indicator is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_touch2Indicator == null)
            {
                Debug.LogError("Error MCAExample._touch2Indicator is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_keyboardText == null)
            {
                Debug.LogError("Error MCAExample._keyboardText is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_modelRenderer == null)
            {
                Debug.LogError("Error MCAExample._modelRenderer is not set, disabling script.");
                enabled = false;
                return;
            }

            _leftButtonHighlight.SetActive(false);
            _rightButtonHighlight.SetActive(false);
            _homeTapIndicator.SetActive(false);
            _touch1Indicator.SetActive(false);
            _touch2Indicator.SetActive(false);

            _keyboardText.text = "";
            _origColor = _modelRenderer.material.color;

            _controllerConnectionHandler = GetComponent<ControllerConnectionHandler>();

            MLInput.OnControllerButtonDown += HandleOnButtonDown;
            MLInput.OnControllerButtonUp += HandleOnButtonUp;
            MLInput.OnTriggerDown += HandleOnTriggerDown;
            MLInput.OnTriggerUp += HandleOnTriggerUp;
        }

        /// <summary>
        /// Updates effects on different input responses via input polling mechanism.
        /// </summary>
        void Update()
        {
            if (_controllerConnectionHandler.IsControllerValid())
            {
                MLInputController controller = _controllerConnectionHandler.ConnectedController;
                _modelRenderer.material.color = _origColor;
                UpdateTouchIndicator(_touch1Indicator, controller.Touch1Active, controller.Touch1PosAndForce);
                UpdateTouchIndicator(_touch2Indicator, controller.Touch2Active, controller.Touch2PosAndForce);
                UpdateHighlights();
            }
            else
            {
                _modelRenderer.material.color = Color.red;
            }
        }

        /// <summary>
        /// Cleans up the component.
        /// </summary>
        void OnDestroy()
        {
            if (MLInput.IsStarted)
            {
                MLInput.OnTriggerUp -= HandleOnTriggerUp;
                MLInput.OnTriggerDown -= HandleOnTriggerDown;
                MLInput.OnControllerButtonUp -= HandleOnButtonUp;
                MLInput.OnControllerButtonDown -= HandleOnButtonDown;
                MLInput.Stop();
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Turn off HomeTap visualizer after certain time.
        /// </summary>
        private void UpdateHighlights()
        {
            if (_timeToDeactivateHome < Time.time)
            {
                _homeTapIndicator.SetActive(false);
            }
        }

        /// <summary>
        /// Update visualizers for touchpad.
        /// </summary>
        /// <param name="indicator"> Visual object to place on touch position. </param>
        /// <param name="active"> State of the touch. </param>
        /// <param name="pos"> Raw data for touchpad touch position. </param>
        private void UpdateTouchIndicator(GameObject indicator, bool active, Vector3 pos)
        {
            indicator.SetActive(active);
            indicator.transform.localPosition = new Vector3(pos.x * 0.042f,
                pos.y * 0.042f + 0.01f, indicator.transform.localPosition.z);
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles the event for button down.
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="button">The button that is being pressed.</param>
        private void HandleOnButtonDown(byte controllerId, MLInputControllerButton button)
        {
            MLInputController controller = _controllerConnectionHandler.ConnectedController;
            if (controller != null && controller.Id == controllerId &&
                button == MLInputControllerButton.Bumper)
            {
                _leftButtonHighlight.SetActive(true);
            }
        }

        /// <summary>
        /// Handles the event for button up.
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="button">The button that is being released.</param>
        private void HandleOnButtonUp(byte controllerId, MLInputControllerButton button)
        {
            MLInputController controller = _controllerConnectionHandler.ConnectedController;
            if (controller != null && controller.Id == controllerId)
            {
                if (button == MLInputControllerButton.Bumper)
                {
                    _leftButtonHighlight.SetActive(false);
                }
                else if (button == MLInputControllerButton.HomeTap)
                {
                    _homeTapIndicator.SetActive(true);
                    _timeToDeactivateHome = Time.time + _homeActiveDuration;
                }
            }
        }

        /// <summary>
        /// Handles the event for trigger down
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="value">The trigger value</param>
        private void HandleOnTriggerDown(byte controllerId, float value)
        {
            MLInputController controller = _controllerConnectionHandler.ConnectedController;
            if (controller != null && controller.Id == controllerId)
            {
                _rightButtonHighlight.SetActive(true);
            }
        }

        /// <summary>
        /// Handles the event for trigger up
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="value">The trigger value</param>
        private void HandleOnTriggerUp(byte controllerId, float value)
        {
            MLInputController controller = _controllerConnectionHandler.ConnectedController;
            if (controller != null && controller.Id == controllerId)
            {
                _rightButtonHighlight.SetActive(false);
            }
        }

        /// <summary>
        /// Keyboard events are propagated via Unity's event system. OnGUI is the preferred way
        /// to catch these events.
        /// </summary>
        private void OnGUI()
        {
            Event e = Event.current;

            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Backspace)
                {
                    if (_keyboardText.text.Length > 0)
                    {
                        _keyboardText.text = _keyboardText.text.Substring(0, _keyboardText.text.Length - 1);
                    }
                }
                else if (e.keyCode == KeyCode.Return)
                {
                    _keyboardText.text += "\n";
                }
                else if (!Char.IsControl(e.character))
                {
                    _keyboardText.text += e.character;
                }
            }
        }
        #endregion
    }
}
