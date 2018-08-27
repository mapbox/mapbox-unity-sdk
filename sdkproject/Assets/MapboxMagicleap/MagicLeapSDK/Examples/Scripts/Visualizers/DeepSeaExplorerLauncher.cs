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
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// Updates followers to face this object
    /// </summary>
    public class DeepSeaExplorerLauncher : MonoBehaviour
    {
        #region Private Variables
        [SerializeField, Tooltip("Position offset of the explorer's target relative to Reference Transform")]
        private Vector3 _positionOffset;

        [SerializeField, Tooltip("Prefab of the Deep Sea Explorer")]
        private GameObject _explorerPrefab;
        private FaceTargetPosition[] _followers;

        [SerializeField, Tooltip("Desired number of explorers. Each explorer will have a different mass and turning speed combination")]
        private int _numExplorers = 3;
        private float _minMass = 4;
        private float _maxMass = 16;
        private float _minTurningSpeed = 30;
        private float _maxTurningSpeed = 90;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Validates variables and creates the deep sea explorers
        /// </summary>
        void Awake ()
        {
            if (null == _explorerPrefab)
            {
                Debug.LogError("DeepSeaExplorerLauncher._deepSeaExplorer not set, disabling script.");
                enabled = false;
                return;
            }
        }

        /// <summary>
        /// Recreate explorers if we are reenabled while a target is found
        /// </summary>
        void OnEnable()
        {
            CreateExplorers();
        }

        /// <summary>
        /// Destroy all explorers immediately
        /// </summary>
        void OnDisable()
        {
            DestroyExplorers();
        }
        
        /// <summary>
        /// Update followers of the new position
        /// </summary>
        void Update()
        {
            Vector3 position = GetPosition();
            foreach (FaceTargetPosition follower in _followers)
            {
                if (follower)
                {
                    follower.TargetPosition = position;
                }
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Create the Deep Sea Explorers with unique parameters
        /// </summary>
        private void CreateExplorers()
        {
            if (null == _followers)
            {
                _followers = new FaceTargetPosition[_numExplorers];
            }

            float massInc = (_maxMass - _minMass) / _numExplorers;
            float turningSpeedInc = (_maxTurningSpeed - _minTurningSpeed) / _numExplorers;
            Vector3 position = GetPosition();
            for (int i = 0; i < _numExplorers; ++i)
            {
                if (_followers[i])
                {
                    continue;
                }

                GameObject explorer = Instantiate(_explorerPrefab, position, Quaternion.identity);

                _followers[i] = explorer.AddComponent<FaceTargetPosition>();
                _followers[i].TurningSpeed = _minTurningSpeed + (i * turningSpeedInc);

                // Mass would be inversely proportional to turning speed (lower mass leads to lower acceleration -> needs higher turning rate)
                Rigidbody body = explorer.GetComponent<Rigidbody>();
                if (body)
                {
                    body.mass = _maxMass - (i * massInc);
                }
            }
        }

        /// <summary>
        /// Destroy all explorers
        /// </summary>
        private void DestroyExplorers()
        {
            foreach (FaceTargetPosition follower in _followers)
            {
                if (follower)
                {
                    Destroy(follower.gameObject);
                }
            }
        }

        /// <summary>
        /// Calculate and return the position which the explorers should look at
        /// </summary>
        /// <returns>The absolute position of the new target</returns>
        private Vector3 GetPosition()
        {
            return transform.position + transform.TransformDirection(_positionOffset);
        }
        #endregion
    }
}
