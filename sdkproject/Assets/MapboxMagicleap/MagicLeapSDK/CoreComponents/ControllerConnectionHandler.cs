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

using System;
using UnityEngine;

namespace UnityEngine.XR.MagicLeap
{
    /// <summary>
    /// Class to automatically handle connection/disconnection events of an input device. By default,
    /// all device types are allowed but it could be modified through the inspector to limit which types to
    /// allow. This class automatically handles the disconnection/reconnection of controllers. This class
    /// keeps only the first allowed connected controller (See public property ConnectedController)
    /// </summary>
    public sealed class ControllerConnectionHandler : MonoBehaviour
    {
        #region Public Enum
        /// <summary>
        /// Flags to determine which input devices to allow
        /// </summary>
        [Flags]
        public enum DeviceTypesAllowed : int
        {
            MobileApp = 1 << 0,
            ControllerLeft = 1 << 1,
            ControllerRight = 1 << 2,
        }
        #endregion

        #region Private Variables
        [SerializeField, BitMask(typeof(DeviceTypesAllowed)), Tooltip("Bitmask on which devices to allow.")]
        private DeviceTypesAllowed _devicesAllowed = (DeviceTypesAllowed)~0;
        #endregion

        #region Public Variables
        /// <summary>
        /// Getter for the first allowed connected device, could return null.
        /// </summary>
        public MLInputController ConnectedController { get; private set; }
        #endregion

        #region Public Events
        /// <summary>
        /// Invoked only when the current controller was invalid and a controller attempted to connect.
        /// First parameter is the newly connected controller if allowed, otherwise null.
        /// </summary>
        public System.Action<MLInputController> OnControllerConnected;

        /// <summary>
        /// Invoked only when the current controller disconnects.
        /// </summary>
        public System.Action OnControllerDisconnected;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Starts the MLInput, initializes the first controller, and registers the connection handlers
        /// </summary>
        private void Awake()
        {
            MLInputConfiguration config = new MLInputConfiguration(MLInputConfiguration.DEFAULT_TRIGGER_DOWN_THRESHOLD,
                                                        MLInputConfiguration.DEFAULT_TRIGGER_UP_THRESHOLD,
                                                        true);
            MLResult result = MLInput.Start(config);
            if (!result.IsOk)
            {
                Debug.LogError("Error ControllerConnectionHandler starting MLInput, disabling script.");
                enabled = false;
                return;
            }

            ConnectedController = GetAllowedInput();
            RegisterConnectionHandlers();
        }

        /// <summary>
        /// Unregisters the connection handlers and stops the MLInput
        /// </summary>
        private void OnDestroy()
        {
            if (MLInput.IsStarted)
            {
                UnregisterConnectionHandlers();
                MLInput.Stop();
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Registers the on connection/disconnection handlers.
        /// </summary>
        private void RegisterConnectionHandlers()
        {
            MLInput.OnControllerConnected += HandleOnControllerConnected;
            MLInput.OnControllerDisconnected += HandleOnControllerDisconnected;
        }

        /// <summary>
        /// Unregisters the on connection/disconnection handlers.
        /// </summary>
        private void UnregisterConnectionHandlers()
        {
            MLInput.OnControllerDisconnected -= HandleOnControllerDisconnected;
            MLInput.OnControllerConnected -= HandleOnControllerConnected;
        }

        /// <summary>
        /// Gets the first input device that's connected and allowed
        /// </summary>
        /// <returns>The first connected allowed device if any, null otherwise</returns>
        private MLInputController GetAllowedInput()
        {
            for (int i = 0; i < 2; ++i)
            {
                MLInputController controller = MLInput.GetController(i);
                if (IsDeviceAllowed(controller))
                {
                    return controller;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if a controller exists, is connected, and is allowed.
        /// </summary>
        /// <param name="controller">The controller to be checked for</param>
        /// <returns>True if the controller exists, is connected, and is allowed</returns>
        private bool IsDeviceAllowed(MLInputController controller)
        {
            if (controller == null || !controller.Connected)
            {
                return false;
            }

            return (((_devicesAllowed & DeviceTypesAllowed.MobileApp) != 0 && controller.Type == MLInputControllerType.MobileApp) ||
                ((_devicesAllowed & DeviceTypesAllowed.ControllerLeft) != 0 && controller.Type == MLInputControllerType.Control && controller.Hand == MLInput.Hand.Left) ||
                ((_devicesAllowed & DeviceTypesAllowed.ControllerRight) != 0 && controller.Type == MLInputControllerType.Control && controller.Hand == MLInput.Hand.Right));
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Checks if the controller is set and connected. This method
        /// does not check if the controller is of the allowed device type
        /// since that's handled by the connection/disconnection handlers.
        /// Should not be called from Awake() or OnEnable().
        /// </summary>
        /// <returns>True if the controller is ready for use, false otherwise</returns>
        public bool IsControllerValid()
        {
            return (ConnectedController != null && ConnectedController.Connected);
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles the event when a controller connects. If the current controller
        /// is not valid, this will check if the new controller is allowed and uses
        /// it if so. Otherwise, no change will happen.
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        private void HandleOnControllerConnected(byte controllerId)
        {
            if (!IsControllerValid())
            {
                MLInputController newController = MLInput.GetController(controllerId);
                if (IsDeviceAllowed(newController))
                {
                    ConnectedController = newController;
                }

                if (OnControllerConnected != null)
                {
                    OnControllerConnected(ConnectedController);
                }
            }
        }

        /// <summary>
        /// Handles the event when a controller disconnects. If the disconnected
        /// controller happens to be what we're using, we set our reference to null.
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        private void HandleOnControllerDisconnected(byte controllerId)
        {
            if (ConnectedController != null && ConnectedController.Id == controllerId)
            {
                ConnectedController = null;
                if (OnControllerDisconnected != null)
                {
                    OnControllerDisconnected();
                }
            }
        }
        #endregion
    }

}
