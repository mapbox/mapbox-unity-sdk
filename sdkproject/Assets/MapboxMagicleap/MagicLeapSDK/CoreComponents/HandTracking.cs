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
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.XR.MagicLeap
{
    /// <summary>
    /// KeyPoseTypes flags enumeration. This enumeration lists the MLHandKeyPose enumerations as Flags so that
    /// more than one keyposes can be easily selected from the inspector.
    /// </summary>
    [Flags]
    public enum KeyPoseTypes
    {
        /// <summary/>
        Finger = (1 << MLHandKeyPose.Finger),
        /// <summary/>
        Fist = (1 << MLHandKeyPose.Fist),
        /// <summary/>
        Pinch = (1 << MLHandKeyPose.Pinch),
        /// <summary/>
        Thumb = (1 << MLHandKeyPose.Thumb),
        /// <summary/>
        L = (1 << MLHandKeyPose.L),
        /// <summary/>
        OpenHandBack = (1 << MLHandKeyPose.OpenHandBack),
        /// <summary/>
        Ok = (1 << MLHandKeyPose.Ok),
        /// <summary/>
        C = (1 <<  MLHandKeyPose.C),
        /// <summary/>
        NoPose = (1 <<  MLHandKeyPose.NoPose)
    }

    /// <summary>
    /// Component used to communicate with the MLHands API and manage
    /// which KeyPoses are currently being tracked by each hand.
    /// KeyPoses can be added and removed from the tracker during runtime.
    /// </summary>
    public class HandTracking : MonoBehaviour
    {
        #region Private Variables
        [Space, SerializeField, BitMask(typeof(KeyPoseTypes)), Tooltip("All KeyPoses to be tracked")]
        private KeyPoseTypes _trackedKeyPoses;

        [SerializeField]
        private MLKeyPointFilterLevel _keyPointFilterLevel = MLKeyPointFilterLevel.ExtraSmoothed;

        [SerializeField]
        private MLPoseFilterLevel _PoseFilterLevel = MLPoseFilterLevel.ExtraRobust;
        #endregion

        #region Public Properties
        public KeyPoseTypes TrackedKeyPoses { get; private set; }
        #endregion

        #region Unity Methods
        /// <summary>
        /// Initializes and finds references to all relevant components in the
        /// scene and registers required events.
        /// </summary>
        void OnEnable()
        {
            MLResult result = MLHands.Start();
            if (!result.IsOk)
            {
                Debug.LogError("Error HandTracking starting MLHands, disabling script.");
                enabled = false;
                return;
            }

            UpdateKeyPoseStates(true);

            MLHands.KeyPoseManager.SetKeyPointsFilterLevel(_keyPointFilterLevel);
            MLHands.KeyPoseManager.SetPoseFilterLevel(_PoseFilterLevel);
        }

        /// <summary>
        /// Stops the communication to the MLHands API and unregisters required events.
        /// </summary>
        void OnDisable()
        {
            if (MLHands.IsStarted)
            {
                // Disable all KeyPoses if MLHands was started
                // and is about to stop
                UpdateKeyPoseStates(false);
            }
            MLHands.Stop();
        }

        /// <summary>
        /// Update KeyPoses tracked if enum value changed.
        /// </summary>
        void Update()
        {
            if ((_trackedKeyPoses ^ TrackedKeyPoses) != 0)
            {
                UpdateKeyPoseStates(true);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds KeyPose if it's not there already.
        /// </summary>
        /// <param name="keyPose"> KeyPose to add. </param>
        public void AddKeyPose(KeyPoseTypes keyPose)
        {
            if ((keyPose & _trackedKeyPoses) != keyPose)
            {
                _trackedKeyPoses |= keyPose;
                UpdateKeyPoseStates(true);
            }
        }

        /// <summary>
        /// Removes KeyPose if it's there.
        /// </summary>
        /// <param name="keyPose"> KeyPose to remove. </param>
        public void RemoveKeyPose(KeyPoseTypes keyPose)
        {
            if ((keyPose & _trackedKeyPoses) == keyPose)
            {
                _trackedKeyPoses ^= keyPose;
                UpdateKeyPoseStates(true);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Get the KeyPoses enabled.
        /// </summary>
        /// <returns> The array of KeyPoses being tracked.</returns>
        private MLHandKeyPose[] GetKeyPoseTypes()
        {
            int[] enumValues = (int[])Enum.GetValues(typeof(KeyPoseTypes));
            List<MLHandKeyPose> keyPoses = new List<MLHandKeyPose>();

            TrackedKeyPoses = 0;
            KeyPoseTypes current;
            for (int i = 0; i < enumValues.Length; ++i)
            {
                current = (KeyPoseTypes)enumValues[i];
                if ((_trackedKeyPoses & current) == current)
                {
                    TrackedKeyPoses |= current;
                    keyPoses.Add((MLHandKeyPose)i);
                }
            }

            return keyPoses.ToArray();
        }

        /// <summary>
        /// Updates the list of KeyPoses internal to the magic leap device,
        /// enabling or disabling KeyPoses that should be tracked.
        /// </summary>
        private void UpdateKeyPoseStates(bool enableState = true)
        {
            MLHandKeyPose[] keyPoseTypes = GetKeyPoseTypes();

            // Early out in case there are no KeyPoses to enable.
            if (keyPoseTypes.Length == 0)
            {
                MLHands.KeyPoseManager.DisableAllKeyPoses();
                return;
            }

            bool status = MLHands.KeyPoseManager.EnableKeyPoses(keyPoseTypes, enableState, true);
            if (!status)
            {
                Debug.LogError("HandTracking failed during a call to enable tracked KeyPoses.\n"
                    + "Disabling HandTracking component.");
                enabled = false;
                return;
            }
        }
        #endregion
    }
}
