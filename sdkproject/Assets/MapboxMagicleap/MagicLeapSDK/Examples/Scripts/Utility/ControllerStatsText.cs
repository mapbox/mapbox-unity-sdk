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
using UnityEngine.Serialization;
using UnityEngine.XR.MagicLeap;

using System.Collections;

namespace MagicLeap
{
    /// <summary>
    /// This provides textual state feedback for the connected controller.
    /// </summary>
    [RequireComponent(typeof(Text), typeof(ControllerConnectionHandler))]
    public class ControllerStatsText : MonoBehaviour
    {
        #region Private Variables
        private ControllerConnectionHandler _controllerConnectionHandler;

        private Text _controllerStatsText;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Initializes component data and starts MLInput.
        /// </summary>
        void Awake()
        {
            _controllerStatsText = gameObject.GetComponent<Text>();
            _controllerStatsText.color = Color.white;

            _controllerConnectionHandler = GetComponent<ControllerConnectionHandler>();
        }

        /// <summary>
        /// Updates text with latest controller stats.
        /// </summary>
        void Update()
        {
            if (_controllerConnectionHandler.IsControllerValid())
            {
                MLInputController controller = _controllerConnectionHandler.ConnectedController;
                if (controller.Type == MLInputControllerType.Control)
                {
                    _controllerStatsText.text =
                    string.Format("" +
                        "Position:\t<i>{0}</i>\n" +
                        "Rotation:\t<i>{1}</i>\n\n" +
                        "Use CFUID Transforms:\t<i>{2}</i>\n\n" +
                        "CFUID Tracking Enabled:\t<i>{3}</i>\n\n" +
                        "<color=#ffc800>Buttons</color>\n" +
                        "Trigger:\t\t<i>{4}</i>\n" +
                        "Bumper:\t\t<i>{5}</i>\n\n" +
                        "<color=#ffc800>Touchpad</color>\n" +
                        "Location:\t<i>({6},{7})</i>\n" +
                        "Pressure:\t<i>{8}</i>\n\n" +
                        "<color=#ffc800>Gestures</color>\n" +
                        "<i>{9} {10}</i>",

                        controller.Position.ToString("n2"),
                        controller.Orientation.eulerAngles.ToString("n2"),
                        controller.UseCFUIDTransforms ? "Yes" : "No",
                        controller.IsCFUIDTrackingEnabled ? "Yes" : "No",
                        controller.TriggerValue.ToString("n2"),
                        controller.State.ButtonState[(int)MLInputControllerButton.Bumper],
                        controller.Touch1Active ? controller.Touch1PosAndForce.x.ToString("n2") : "0.00",
                        controller.Touch1Active ? controller.Touch1PosAndForce.y.ToString("n2") : "0.00",
                        controller.Touch1Active ? controller.Touch1PosAndForce.z.ToString("n2") : "0.00",
                        controller.TouchpadGesture.Type.ToString(),
                        controller.TouchpadGestureState.ToString());
                }
                else if (controller.Type == MLInputControllerType.MobileApp)
                {
                    _controllerStatsText.text =
                    string.Format("" +
                        "Position:\t<i>{0}</i>\n" +
                        "Rotation:\t<i>{1}</i>\n\n" +
                        "<color=#ffc800>Buttons</color>\n" +
                        "Trigger:\t\t<i>{2}</i>\n" +
                        "Bumper:\t\t<i>{3}</i>\n\n" +
                        "<color=#ffc800>Touchpad</color>\n" +
                        "Touch 1 Location: <i>({4},{5})</i>\n" +
                        "Touch 2 Location: <i>({6},{7})</i>\n\n" +
                        "<color=#ffc800>Gestures</color>\n" +
                        "<i>{8} {9}</i>",

                        "No information available",
                        controller.Orientation.eulerAngles.ToString("n2"),
                        controller.TriggerValue.ToString("n2"),
                        controller.State.ButtonState[(int)MLInputControllerButton.Bumper],
                        controller.Touch1Active ? controller.Touch1PosAndForce.x.ToString("n2") : "0.00",
                        controller.Touch1Active ? controller.Touch1PosAndForce.y.ToString("n2") : "0.00",
                        controller.Touch2Active ? controller.Touch2PosAndForce.x.ToString("n2") : "0.00",
                        controller.Touch2Active ? controller.Touch2PosAndForce.y.ToString("n2") : "0.00",
                        controller.TouchpadGesture.Type.ToString(),
                        controller.TouchpadGestureState.ToString());
                }
                else
                {
                    _controllerStatsText.text = "Invalid Controller!";
                }
            }
            else
            {
                _controllerStatsText.text = "";
            }
        }
        #endregion
    }
}
