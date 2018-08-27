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
using UnityEngine.UI;
using UnityEngine.Events;

namespace UnityEngine.XR.MagicLeap
{
    [Flags]
    public enum OrientationFlags
    {
        Vertical = MLWorldPlanesQueryFlags.Vertical,
        Horizontal = MLWorldPlanesQueryFlags.Horizontal,
        Arbitrary = MLWorldPlanesQueryFlags.Arbitrary,
    }

    [Flags]
    public enum SemanticFlags
    {
        Ceiling = MLWorldPlanesQueryFlags.SemanticCeiling,
        Floor = MLWorldPlanesQueryFlags.SemanticFloor,
        Wall = MLWorldPlanesQueryFlags.SemanticWall,
    }

    /// <summary>
    /// Planes is a MonoBehaviour meant to handle all communication with the ML API
    /// including querying, parameter construction and callback information.
    /// </summary>
    public class Planes : MonoBehaviour
    {
        #region Public Variables
        /// <summary>
        /// The latest result from the last query to the Planes API. Planes in
        /// the array are ordered from largest first based on it's surface area.
        /// </summary>
        public MLWorldPlane[] PlanesResult { get; private set; }

        [Header("Query Parameters")]
        [Tooltip("Maximum number of planes to get at the same time.")]
        public uint MaxPlaneCount = 512;

        [SerializeField, BitMask(typeof(SemanticFlags)), Space]
        public SemanticFlags SemanticFlags;

        [SerializeField, BitMask(typeof(OrientationFlags))]
        public OrientationFlags OrientationFlags;

        [Space, Tooltip("Minimum area of searched for planes.")]
        public float MinPlaneArea = 0.04f;

        [Space, Tooltip("Flag specifying if inner planes should be detected.")]
        public bool InnerPlanes = true;

        [Space, Tooltip("Flag specifying if results should ignore holes.")]
        public bool IgnoreHoles = false;

        [Tooltip("Perimeter until which to ignore holes and include in plane if IgnoreHoles is not set")]
        public float MinHoleLength = 0.0f;

        [Space, Tooltip("Flag specifying if planes should be oriented to gravity.")]
        public bool OrientToGravity = true;
        #endregion

        #region Private Variables
        // The cached query flags
        private MLWorldPlanesQueryFlags _queryFlags  = MLWorldPlanesQueryFlags.Vertical;

        // The cached query parameters
        private MLWorldPlanesQueryParams _queryParams = new MLWorldPlanesQueryParams();

        // Flag for checking if currently quering
        private bool _isQuerying = false;
        #endregion

        #region Events
        [System.Serializable]
        public class PlanesUpdateEvent : UnityEvent<MLWorldPlane[]> { }

        [Space]
        public PlanesUpdateEvent OnUpdateEvent;
        #endregion

        #region Unity Methods
        public void OnValidate()
        {
            if (MinHoleLength < 0.0f)
            {
                Debug.LogWarning("Planes component can't have MinHoleLength less than 0.0f, setting back to default.");
                MinHoleLength = 0.0f;
            }
            if (MinPlaneArea < 0.04f)
            {
                Debug.LogWarning("Planes component can't have MinPlaneArea less than 0.04, setting back to default.");
                MinPlaneArea = 0.04f;
            }
        }

        /// <summary>
        /// Initializes variables and makes sure they started correctly
        /// </summary>
        void OnEnable()
        {
            MLResult result = MLWorldPlanes.Start();
            if (!result.IsOk)
            {
                Debug.LogError("Error Planes starting MLWorldPlanes, disabling script.");
                enabled = false;
                return;
            }
        }

        /// <summary>
        /// Stops and destroys plane polling
        /// </summary>
        void OnDisable()
        {
            if (MLWorldPlanes.IsStarted)
            {
                MLWorldPlanes.Stop();
            }
        }

        /// <summary>
        /// Updates planes
        /// </summary>
        void Update()
        {
            if (!_isQuerying)
            {
                QueryPlanes();
            }
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// Utility function used to determine if the passed in MLWorldPlane object's
        /// flags contain the passed in SemanticFlags.
        /// </summary>
        /// <param name="plane">The MLWorldPlane object to be checked</param>
        /// <param name="flag">The SemanticFlags to be checked</param>
        public static bool DoesPlaneHaveFlag(MLWorldPlane plane, SemanticFlags flag)
        {
            return (plane.Flags & (uint)flag) == (uint)flag;
        }

        /// <summary>
        /// Utility function used to determine if the passed in MLWorldPlane object's
        /// flags contain the passed in OrientationFlags.
        /// </summary>
        /// <param name="plane">The MLWorldPlane object to be checked</param>
        /// <param name="flag">The OrientationFlags to be checked</param>
        public static bool DoesPlaneHaveFlag(MLWorldPlane plane, OrientationFlags flag)
        {
            return (plane.Flags & (uint)flag) == (uint)flag;
        }
        #endregion

        #region Private Functions
        /// <summary>
        /// Queries the Planes API with all of the set query flags and parameters
        /// and sets the Planes[] when finished. Based on the query flags that
        /// are passed in, extraction and calculation times may vary.
        /// </summary>
        private bool QueryPlanes()
        {
            // Construct flag data
            _queryFlags = (MLWorldPlanesQueryFlags)OrientationFlags;
            _queryFlags |= (MLWorldPlanesQueryFlags)SemanticFlags;
            if (InnerPlanes)
            {
                _queryFlags |= MLWorldPlanesQueryFlags.Inner;
            }

            if (IgnoreHoles)
            {
                _queryFlags |= MLWorldPlanesQueryFlags.IgnoreHoles;
            }
            if (OrientToGravity)
            {
                _queryFlags |= MLWorldPlanesQueryFlags.OrientToGravity;
            }

            _queryParams.Flags = _queryFlags;
            _queryParams.BoundsCenter = transform.position;
            _queryParams.MaxResults = MaxPlaneCount;
            _queryParams.BoundsExtents = transform.localScale;
            _queryParams.BoundsRotation = transform.rotation;
            _queryParams.MinHoleLength = MinHoleLength;
            _queryParams.MinPlaneArea = MinPlaneArea;

            MLResult result = MLWorldPlanes.GetPlanes(_queryParams, HandleOnReceivedPlanes);

            if (result.IsOk)
            {
                _isQuerying = true;
                return true;
            }
            // TODO: Print result in failure case while preventing log spam.

            return false;
        }

        /// <summary>
        /// Handles the result that is recieved from the query to the Planes API.
        /// <param name="result">The resulting status of the query</param>
        /// <param name="planes">The planes recieved from the query</param>
        /// </summary>
        private void HandleOnReceivedPlanes(MLResult result, MLWorldPlane[] planes)
        {
            if (result.IsOk)
            {
                PlanesResult = planes;
                if (OnUpdateEvent != null)
                {
                    OnUpdateEvent.Invoke(planes);
                }
            }
            else
            {
                Debug.LogError(string.Format("Query to MLWorldPlanes resulted in {0}.", result));
            }

            _isQuerying = false;
        }
        #endregion
    }
}
