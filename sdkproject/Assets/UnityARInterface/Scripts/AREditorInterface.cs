using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityARInterface
{
    public class AREditorInterface : ARInterface
    {
        float m_LastTime;
        enum State
        {
            Uninitialized,
            Initialized,
            WaitingToAddPlane1,
            WaitingToAddPlane2,
            Finished
        }

        State m_State;
        BoundedPlane[] m_FakePlanes;
        Pose m_CameraPose;
        bool m_WasMouseDownLastFrame;
        Vector3 m_LastMousePosition;
        Vector3 m_EulerAngles;
        Vector3[] m_PointCloud;

        public override IEnumerator StartService(Settings settings)
        {
            m_CameraPose = Pose.identity;
            m_CameraPose.position.Set(0, 0, 0);
            m_LastTime = Time.time;
            m_State = State.Initialized;
            m_FakePlanes = new BoundedPlane[2];
            m_FakePlanes[0] = new BoundedPlane()
            {
                id = "0x1",
                extents = new Vector2(2.2f, 2f),
                //extents = new Vector2(1f, 1f),
                rotation = Quaternion.AngleAxis(60f, Vector3.up),
                center = new Vector3(2f, -1f, 3f)
                //center = new Vector3(1f, 1f, 1f)
            };

            m_FakePlanes[1] = new BoundedPlane()
            {
                id = "0x2",
                extents = new Vector2(2f, 2f),
                rotation = Quaternion.AngleAxis(200f, Vector3.up),
                center = new Vector3(3f, 1f, 3f)
            };

            m_PointCloud = new Vector3[20];
            for (int i = 0; i < 20; ++i)
            {
                m_PointCloud[i] = new Vector3(
                    UnityEngine.Random.Range(-2f, 2f),
                    UnityEngine.Random.Range(-.5f, .5f),
                    UnityEngine.Random.Range(-2f, 2f));
            }

            IsRunning = true;
            return null;
        }

        public override void StopService()
        {
            IsRunning = false;
        }

        public override bool TryGetUnscaledPose(ref Pose pose)
        {
            pose = m_CameraPose;
            return true;
        }

        public override bool TryGetCameraImage(ref CameraImage cameraImage)
        {
            return false;
        }

        public override void SetupCamera(Camera camera)
        {

        }

        public override bool TryGetPointCloud(ref PointCloud pointCloud)
        {
            if (pointCloud.points == null)
                pointCloud.points = new List<Vector3>();

            pointCloud.points.Clear();
            pointCloud.points.AddRange(m_PointCloud);
            return true;
        }

        public override LightEstimate GetLightEstimate()
        {
            return new LightEstimate()
            {
                capabilities = LightEstimateCapabilities.None
            };
        }

		public override Matrix4x4 GetDisplayTransform()
		{
			return Matrix4x4.identity;
		}

        public override void UpdateCamera(Camera camera)
        {
            float speed = camera.transform.parent.localScale.x / 10f;
            float turnSpeed = 10f;
            var forward = m_CameraPose.rotation * Vector3.forward;
            var right = m_CameraPose.rotation * Vector3.right;
            var up = m_CameraPose.rotation * Vector3.up;

            if (Input.GetKey(KeyCode.W))
                m_CameraPose.position += forward * Time.deltaTime * speed;

            if (Input.GetKey(KeyCode.S))
                m_CameraPose.position -= forward * Time.deltaTime * speed;

            if (Input.GetKey(KeyCode.A))
                m_CameraPose.position -= right * Time.deltaTime * speed;

            if (Input.GetKey(KeyCode.D))
                m_CameraPose.position += right * Time.deltaTime * speed;

            if (Input.GetKey(KeyCode.Q))
                m_CameraPose.position += up * Time.deltaTime * speed;

            if (Input.GetKey(KeyCode.Z))
                m_CameraPose.position -= up * Time.deltaTime * speed;

            if (Input.GetMouseButton(1))
            {
                if (!m_WasMouseDownLastFrame)
                    m_LastMousePosition = Input.mousePosition;

                var deltaPosition = Input.mousePosition - m_LastMousePosition;
                m_EulerAngles.y += Time.deltaTime * turnSpeed * deltaPosition.x;
                m_EulerAngles.x -= Time.deltaTime * turnSpeed * deltaPosition.y;
                m_CameraPose.rotation = Quaternion.Euler(m_EulerAngles);
                m_LastMousePosition = Input.mousePosition;
                m_WasMouseDownLastFrame = true;
            }
            else
            {
                m_WasMouseDownLastFrame = false;
            }
        }

        public override void Update()
        {
            switch (m_State)
            {
                case State.Initialized:
                    m_State = State.WaitingToAddPlane1;
                    m_LastTime = Time.time;
                    break;

                case State.WaitingToAddPlane1:

                    if (Time.time - m_LastTime > 1f)
                    {
                        OnPlaneAdded(m_FakePlanes[0]);
                        m_LastTime = Time.time;
                        m_State = State.WaitingToAddPlane2;
                    }
                    break;

                case State.WaitingToAddPlane2:

                    if (Time.time - m_LastTime > 1f)
                    {
                        OnPlaneAdded(m_FakePlanes[1]);
                        m_LastTime = Time.time;
                        m_State = State.Finished;
                    }
                    break;
            }
        }
    }
}
