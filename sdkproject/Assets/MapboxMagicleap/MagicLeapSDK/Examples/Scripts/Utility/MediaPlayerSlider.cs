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
    [DisallowMultipleComponent]
    public class MediaPlayerSlider : MediaPlayerButton
    {
        #region Public Events
        public System.Action<float> OnValueChanged;
        #endregion

        #region Private Variables
        [SerializeField, Tooltip("Local position of beginning point")]
        private Vector3 _beginRelative;

        [SerializeField, Tooltip("Local position of ending point")]
        private Vector3 _endRelative;

        [SerializeField, Tooltip("Handle of the Slider")]
        private Transform _handle;

        private float _value = 0;
        #endregion // Private Variables

        #region Public Properties
        /// <summary>
        /// Value represents a percentage, clamped in the range [0, 1] inclusive
        /// Invokes OnValueChanged if needed
        /// </summary>
        public float Value
        {
            get
            {
                return _value;
            }
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, _value))
                {
                    return;
                }

                _value = value;
                _handle.localPosition = Vector3.Lerp(_beginRelative, _endRelative, _value);
                if (OnValueChanged != null)
                {
                    OnValueChanged(_value);
                }
            }
        }
        #endregion // Public Properties

        #region Unity Methods
        private void Awake()
        {
            if (_handle == null)
            {
                Debug.LogError("Error MediaPlayerSlider._handle not set, disabling script.");
                enabled = false;
                return;
            }
        }

        protected override void OnEnable()
        {
            _handle.gameObject.GetComponent<Renderer>().enabled = true;

            OnControllerDrag += HandleControllerDrag;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            _handle.gameObject.GetComponent<Renderer>().enabled = false;

            OnControllerDrag -= HandleControllerDrag;

            base.OnDisable();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.TransformPoint(_beginRelative), 0.02f);
            Gizmos.DrawWireSphere(transform.TransformPoint(_endRelative), 0.02f);
        }
        #endregion // Unity Methods

        #region Event Handlers
        /// <summary>
        /// Find the point on the slider that's closest to the line defined by
        /// the controller's position and direction.
        /// </summary>
        /// <param name="controller">Information on the controller</param>
        private void HandleControllerDrag(MLInputController controller)
        {
            // Line 1 is the Controller's ray
            Vector3 P1World = controller.Position;
            Vector3 V1World = controller.Orientation * Vector3.forward;

            // Put controller position and orientation into slider space, as relative end points are in slider space
            Vector3 P1 = transform.InverseTransformPoint(P1World);
            Vector3 V1 = transform.InverseTransformVector(V1World);

            // Line 2 is the slider
            Vector3 P2 = _beginRelative;
            Vector3 V2 = _endRelative - _beginRelative;

            Vector3 deltaP = P2 - P1;
            float V21 = Vector3.Dot(V2, V1);
            float V11 = Vector3.Dot(V1, V1);
            float V22 = Vector3.Dot(V2, V2);

            float tNum = Vector3.Dot(deltaP, V21 * V1 - V11 * V2);
            float tDen = V22 * V11 - V21 * V21;

            // Lines are parallel
            if (Mathf.Approximately(tDen, 0.0f))
            {
                return;
            }

            // closest point on Line 2 to Line 1 is P2 + t * V2
            // but we're only concerned with t
            Value = tNum / tDen;
        }
        #endregion // Event Handlers
    }
}
