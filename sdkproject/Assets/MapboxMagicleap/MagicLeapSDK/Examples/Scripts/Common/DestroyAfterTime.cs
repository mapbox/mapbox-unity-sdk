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

namespace MagicLeap
{
    /// <summary>
    /// Utility class to destroy after a set time.
    /// Note that the count down can be cancelled by destroying this script
    /// </summary>
    public class DestroyAfterTime : MonoBehaviour
    {
        #region Private Variables
        [SerializeField, Tooltip("Time delay before self-destruct")]
        private float _duration = 5;

        private float _timeStart;
        #endregion

        #region Properties
        public float Duration
        {
            set
            {
                _timeStart = Time.time;
                _duration = value;
            }
        }
        #endregion

        #region Unity Methods
        /// <summary>
        /// Start the self-destruct countdown
        /// </summary>
        void Start ()
        {
            _timeStart = Time.time;
        }

        /// <summary>
        /// Count down and destruction
        /// </summary>
        void Update()
        {
            if (Time.time > _timeStart + _duration)
            {
                Destroy(gameObject);
            }
        }
        #endregion
    }
}
