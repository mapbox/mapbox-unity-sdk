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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// Component used to hook into the Hand Tracking script and attach
    /// primitive game objects to it's detected keypoint positions for
    /// each hand.
    /// </summary>
    public class HandTrackingVisualizer : MonoBehaviour
    {
        #region Private Variables
        [SerializeField, Tooltip("The hand to visualize.")]
        private MLHandType _handType;

        [SerializeField, Tooltip("The GameObject to use for the Hand Center.")]
        private Transform _center;

        [Header("Hand Keypoint Colors")]

        [SerializeField, Tooltip("The color assigned to the pinky finger keypoints.")]
        private Color _pinkyColor = Color.cyan;

        [SerializeField, Tooltip("The color assigned to the ring finger keypoints.")]
        private Color _ringColor = Color.red;

        [SerializeField, Tooltip("The color assigned to the middle finger keypoints.")]
        private Color _middleColor = Color.blue;

        [SerializeField, Tooltip("The color assigned to the index finger keypoints.")]
        private Color _indexColor = Color.magenta;

        [SerializeField, Tooltip("The color assigned to the thumb keypoints.")]
        private Color _thumbColor = Color.yellow;

        [SerializeField, Tooltip("The color assigned to the wrist keypoints.")]
        private Color _wristColor = Color.white;

        private List<Transform> _pinkyFinger;
        private List<Transform> _ringFinger;
        private List<Transform> _middleFinger;
        private List<Transform> _indexFinger;
        private List<Transform> _thumb;
        private List<Transform> _wrist;
        #endregion

        /// <summary>
        /// Returns the hand based on the hand type.
        /// </summary>
        private MLHand Hand
        {
            get
            {
                if (_handType == MLHandType.Left)
                {
                    return MLHands.Left;
                }
                else
                {
                    return MLHands.Right;
                }
            }
        }

        #region Unity Methods
        /// <summary>
        /// Initializes MLHands API.
        /// </summary>
        void Start()
        {
            MLResult result = MLHands.Start();
            if (!result.IsOk)
            {
                Debug.LogError("Error HandTrackingVisualizer starting MLHands, disabling script.");
                enabled = false;
                return;
            }

            Initialize();
        }

        /// <summary>
        /// Stops the communication to the MLHands API.
        /// </summary>
        void OnDestroy()
        {
            if (MLHands.IsStarted)
            {
                MLHands.Stop();
            }
        }

        /// <summary>
        /// Update the keypoint positions.
        /// </summary>
        void Update()
        {
            if (MLHands.IsStarted)
            {
                // Pinky
                for (int i = 0; i < Hand.Pinky.KeyPoints.Count; ++i)
                {
                    _pinkyFinger[i].position = Hand.Pinky.KeyPoints[i].Position;
                }

                // Ring
                for (int i = 0; i < Hand.Ring.KeyPoints.Count; ++i)
                {
                    _ringFinger[i].position = Hand.Ring.KeyPoints[i].Position;
                }

                // Middle
                for (int i = 0; i < Hand.Middle.KeyPoints.Count; ++i)
                {
                    _middleFinger[i].position = Hand.Middle.KeyPoints[i].Position;
                }

                // Index
                for (int i = 0; i < Hand.Index.KeyPoints.Count; ++i)
                {
                    _indexFinger[i].position = Hand.Index.KeyPoints[i].Position;
                }

                // Thumb
                for (int i = 0; i < Hand.Thumb.KeyPoints.Count; ++i)
                {
                    _thumb[i].position = Hand.Thumb.KeyPoints[i].Position;
                }

                // Wrist
                for (int i = 0; i < Hand.Wrist.KeyPoints.Count; ++i)
                {
                    _wrist[i].position = Hand.Wrist.KeyPoints[i].Position;
                }

                // Hand Center
                if (_center != null)
                {
                    _center.position = Hand.Center;
                }
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize the available KeyPoints.
        /// </summary>
        private void Initialize()
        {
            // Pinky
            _pinkyFinger = new List<Transform>();
            for (int i = 0; i < Hand.Pinky.KeyPoints.Count; ++i)
            {
                _pinkyFinger.Add(CreateKeyPoint(Hand.Pinky.KeyPoints[i], _pinkyColor).transform);
            }

            // Ring
            _ringFinger = new List<Transform>();
            for (int i = 0; i < Hand.Ring.KeyPoints.Count; ++i)
            {
                _ringFinger.Add(CreateKeyPoint(Hand.Ring.KeyPoints[i], _ringColor).transform);
            }

            // Middle
            _middleFinger = new List<Transform>();
            for (int i = 0; i < Hand.Middle.KeyPoints.Count; ++i)
            {
                _middleFinger.Add(CreateKeyPoint(Hand.Middle.KeyPoints[i], _middleColor).transform);
            }

            // Index
            _indexFinger = new List<Transform>();
            for (int i = 0; i < Hand.Index.KeyPoints.Count; ++i)
            {
                _indexFinger.Add(CreateKeyPoint(Hand.Index.KeyPoints[i], _indexColor).transform);
            }

            // Thumb
            _thumb = new List<Transform>();
            for (int i = 0; i < Hand.Thumb.KeyPoints.Count; ++i)
            {
                _thumb.Add(CreateKeyPoint(Hand.Thumb.KeyPoints[i], _thumbColor).transform);
            }

            // Wrist
            _wrist = new List<Transform>();
            for (int i = 0; i < Hand.Wrist.KeyPoints.Count; ++i)
            {
                _wrist.Add(CreateKeyPoint(Hand.Wrist.KeyPoints[i], _wristColor).transform);
            }
        }

        /// <summary>
        /// Create a GameObject for the desired KeyPoint.
        /// </summary>
        /// <param name="keyPoint"></param>
        /// <returns></returns>
        private GameObject CreateKeyPoint(MLKeyPoint keyPoint, Color color)
        {
            GameObject newObject;

            newObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newObject.transform.SetParent(transform);
            newObject.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            newObject.name = keyPoint.ToString();
            newObject.GetComponent<Renderer>().material.color = color;

            return newObject;
        }
        #endregion
    }
}
