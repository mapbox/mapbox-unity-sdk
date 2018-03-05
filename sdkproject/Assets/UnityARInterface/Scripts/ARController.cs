using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEngine.Networking.PlayerConnection;
using UnityEditor.Networking.PlayerConnection;
#endif

namespace UnityARInterface
{
    public class ARController : MonoBehaviour
    {
        protected ARInterface m_ARInterface;

        [SerializeField]
        protected Camera m_ARCamera;
        public Camera arCamera { get { return m_ARCamera; } }

        [SerializeField]
        private bool m_PlaneDetection;

        [SerializeField]
        private bool m_LightEstimation;

        [SerializeField]
        private bool m_PointCloud;

        [SerializeField]
        private float m_Scale = 1f;

        public float scale
        {
            set
            {
                m_Scale = value;

                var root = m_ARCamera.transform.parent;
                if (root)
                {
                    var poiInRootSpace = root.InverseTransformPoint(pointOfInterest);
                    root.localPosition = m_InvRotation * (-poiInRootSpace * m_Scale) + pointOfInterest;
                }
            }

            get { return m_Scale; }
        }

        public Vector3 pointOfInterest;
        private Quaternion m_Rotation = Quaternion.identity;
        private Quaternion m_InvRotation = Quaternion.identity;
        public Quaternion rotation
        {
            get { return m_Rotation; }
            set
            {
                var root = m_ARCamera.transform.parent;
                if (root)
                {
                    m_Rotation = value;
                    m_InvRotation = Quaternion.Inverse(rotation);
                    var poiInRootSpace = root.InverseTransformPoint(pointOfInterest);

                    root.localPosition = m_InvRotation * (-poiInRootSpace * scale) + pointOfInterest;
                    root.localRotation = m_InvRotation;
                }
            }
        }

        public bool IsRunning
        {
            get
            {
                if (m_ARInterface == null)
                    return false;
                return m_ARInterface.IsRunning;
            }
        }

        public void AlignWithPointOfInterest(Vector3 position)
        {
            var root = m_ARCamera.transform.parent;
            if (root)
            {
                var poiInRootSpace = root.InverseTransformPoint(position - pointOfInterest);
                root.localPosition = m_InvRotation * (-poiInRootSpace * scale);
            }
        }

        void OnBeforeRender()
        {
            m_ARInterface.UpdateCamera(m_ARCamera);

            Pose pose = new Pose();
            if (m_ARInterface.TryGetPose(ref pose))
            {
                m_ARCamera.transform.localPosition = pose.position;
                m_ARCamera.transform.localRotation = pose.rotation;
                var parent = m_ARCamera.transform.parent;
                if (parent != null)
                    parent.localScale = Vector3.one * scale;
            }
        }

        protected virtual void SetupARInterface()
        {
            m_ARInterface = ARInterface.GetInterface();
        }

        private void OnEnable()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.simulateMouseWithTouches = true;

            if (m_ARInterface == null)
                SetupARInterface();

            // See if we are on a camera
            if (m_ARCamera == null)
                m_ARCamera = GetComponent<Camera>();

            // Fallback to main camera
            if (m_ARCamera == null)
                m_ARCamera = Camera.main;

            StopAllCoroutines();
            StartCoroutine(StartServiceRoutine());

        }

        IEnumerator StartServiceRoutine()
        {
            yield return m_ARInterface.StartService(GetSettings());
            if (IsRunning)
            {
                m_ARInterface.SetupCamera(m_ARCamera);
                Application.onBeforeRender += OnBeforeRender;
            }
            else
            {
                enabled = false;
            }
        }


        void OnDisable()
        {
            StopAllCoroutines();
            if (IsRunning)
            {
                m_ARInterface.StopService();
                Application.onBeforeRender -= OnBeforeRender;
            }
        }

        void Update()
        {
            m_ARInterface.Update();
        }

        public ARInterface.Settings GetSettings()
        {
            return new ARInterface.Settings()
            {
                enablePointCloud = m_PointCloud,
                enablePlaneDetection = m_PlaneDetection,
                enableLightEstimation = m_LightEstimation
            };
        }
    }
}
