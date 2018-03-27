using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Utils;

namespace UnityARInterface
{
    /// <summary>
    /// Since unity doesn't flag the Vector4 as serializable, we
    /// need to create our own version. This one will automatically convert
    /// between Vector4 and SerializableVector4
    /// </summary>
    [Serializable]
    public class SerializableVector4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public SerializableVector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public override string ToString()
        {
            return string.Format("[{0}, {1}, {2}, {3}]", x, y, z, w);
        }

        public static implicit operator Quaternion(SerializableVector4 serializedVector)
        {
            return new Quaternion(
                serializedVector.x,
                serializedVector.y,
                serializedVector.z,
                serializedVector.w);
        }

        public static implicit operator Vector4(SerializableVector4 serializedVector)
        {
            return new Vector4(
                serializedVector.x,
                serializedVector.y,
                serializedVector.z,
                serializedVector.w);
        }

        public static implicit operator SerializableVector4(Quaternion quaternion)
        {
            return new SerializableVector4(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
        }

        public static implicit operator SerializableVector4(Vector4 vector)
        {
            return new SerializableVector4(vector.x, vector.y, vector.z, vector.w);
        }
    }

    [Serializable]
    public class SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override string ToString()
        {
            return string.Format("[{0}, {1}, {2}]", x, y, z);
        }

        public static implicit operator Vector3(SerializableVector3 serializedVector)
        {
            return new Vector3(serializedVector.x, serializedVector.y, serializedVector.z);
        }

        public static implicit operator SerializableVector3(Vector3 vector)
        {
            return new SerializableVector3(vector.x, vector.y, vector.z);
        }
    }

    [Serializable]
    public class SerializableVector2
    {
        public float x;
        public float y;

        public SerializableVector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return string.Format("[{0}, {1}]", x, y);
        }

        public static implicit operator Vector2(SerializableVector2 serializedVector)
        {
            return new Vector2(serializedVector.x, serializedVector.y);
        }

        public static implicit operator SerializableVector2(Vector2 vector)
        {
            return new SerializableVector2(vector.x, vector.y);
        }
    }

    [Serializable]
    public class SerializableBoundedPlane
    {
        public byte[] identifier;
        public SerializableVector3 center;
        public SerializableVector4 rotation;
        public SerializableVector2 extents;

        public SerializableBoundedPlane(
            byte[] identifier,
            SerializableVector3 center,
            SerializableVector4 rotation,
            SerializableVector2 extents)
        {
            this.identifier = identifier;
            this.center = center;
            this.rotation = rotation;
            this.extents = extents;
        }

        public static implicit operator SerializableBoundedPlane(BoundedPlane plane)
        {
            return new SerializableBoundedPlane(
                Encoding.UTF8.GetBytes(plane.id),
                plane.center,
                plane.rotation,
                plane.extents);
        }

        public static implicit operator BoundedPlane(SerializableBoundedPlane serializedPlane)
        {
            return new BoundedPlane()
            {
                id = Encoding.UTF8.GetString(serializedPlane.identifier),
                center = serializedPlane.center,
                extents = serializedPlane.extents,
                rotation = serializedPlane.rotation
            };
        }
    }

    [Serializable]
    public class SerializableSubMessage
    {
        public Guid subMessageId;
        public byte[] data { get; private set; }

        public SerializableSubMessage(Guid subMessageId, object data)
        {
            this.subMessageId = subMessageId;
            if (data != null)
                this.data = data.SerializeToByteArray();
        }

        public T GetDataAs<T>() where T : class
        {
            return data.Deserialize<T>();
        }
    }

    [Serializable]
    public class SerializableEnableVideo
    {
        public bool enableVideo;
        public SerializableEnableVideo(bool enableVideo)
        {
            this.enableVideo = enableVideo;
        }
    }

    [Serializable]
    public class SerializableARSettings
    {
        public bool enablePointCloud;
        public bool enablePlaneDetection;
        public bool enableLightEstimation;

        public SerializableARSettings(bool pointCloud, bool planeDetection, bool lightEstimation)
        {
            enablePointCloud = pointCloud;
            enablePlaneDetection = planeDetection;
            enableLightEstimation = lightEstimation;
        }

        public static implicit operator SerializableARSettings(ARInterface.Settings settings)
        {
            return new SerializableARSettings(
                settings.enablePointCloud,
                settings.enablePlaneDetection,
                settings.enableLightEstimation);
        }

        public static implicit operator ARInterface.Settings(SerializableARSettings serializedSettings)
        {
            return new ARInterface.Settings()
            {
                enableLightEstimation = serializedSettings.enableLightEstimation,
                enablePlaneDetection = serializedSettings.enablePlaneDetection,
                enablePointCloud = serializedSettings.enablePointCloud
            };
        }
    }

    [Serializable]
    public class SerializableMatrix4x4
    {
        public SerializableVector4 column0;
        public SerializableVector4 column1;
        public SerializableVector4 column2;
        public SerializableVector4 column3;

        public SerializableMatrix4x4(
            SerializableVector4 column0,
            SerializableVector4 column1,
            SerializableVector4 column2,
            SerializableVector4 column3)
        {
            this.column0 = column0;
            this.column1 = column1;
            this.column2 = column2;
            this.column3 = column3;
        }

        public static implicit operator SerializableMatrix4x4(Matrix4x4 matrix)
        {
            return new SerializableMatrix4x4(
                matrix.GetColumn(0),
                matrix.GetColumn(1),
                matrix.GetColumn(2),
                matrix.GetColumn(3));

        }

        public static implicit operator Matrix4x4(SerializableMatrix4x4 serializedMatrix)
        {
            return new Matrix4x4(
                serializedMatrix.column0,
                serializedMatrix.column1,
                serializedMatrix.column2,
                serializedMatrix.column3);
        }
    }


    [Serializable]
    public class SerializableFrame
    {
        public SerializableMatrix4x4 projectionMatrix;
        public SerializableVector3 cameraPosition;
        public SerializableVector4 cameraRotation;
		public SerializableMatrix4x4 displayTransform;

        public SerializableFrame(
            SerializableMatrix4x4 projectionMatrix,
            SerializableVector3 cameraPosition,
            SerializableVector4 cameraRotation,
			SerializableMatrix4x4 displayTransform)
        {
            this.projectionMatrix = projectionMatrix;
            this.cameraPosition = cameraPosition;
            this.cameraRotation = cameraRotation;
			this.displayTransform = displayTransform;
        }
    }

    [Serializable]
    public class SerializableScreenCaptureParams
    {
        public int width;
        public int height;
        public int format;

        public SerializableScreenCaptureParams(int width, int height, int format)
        {
            this.width = width;
            this.height = height;
            this.format = format;
        }
    }

    [Serializable]
    public class SerializablePointCloud
    {
        public SerializableVector3[] points;

        public SerializablePointCloud(ARInterface.PointCloud pointCloud)
        {
            points = new SerializableVector3[pointCloud.points.Count];
            for (int i = 0; i < points.Length; ++i)
            {
                points[i] = pointCloud.points[i];
            }
        }

        public SerializablePointCloud(Vector3[] points)
        {
            this.points = new SerializableVector3[points.Length];
            for (int i = 0; i < points.Length; ++i)
            {
                this.points[i] = points[i];
            }
        }

        public IEnumerable<Vector3> asEnumerable
        {
            get
            {
                for (int i = 0; i < points.Length; ++i)
                {
                    yield return points[i];
                }
            }
        }
    }

    [Serializable]
    public class SerializableLightEstimate
    {
        public int capabilities;
        public float ambientIntensity;
        public float ambientColorTemperature;

        public SerializableLightEstimate(ARInterface.LightEstimateCapabilities capabilities, float ambientIntensity, float ambientColorTemperature)
        {
            this.capabilities = (int)capabilities;
            this.ambientIntensity = ambientIntensity;
            this.ambientColorTemperature = ambientColorTemperature;
        }

        public static implicit operator SerializableLightEstimate(ARInterface.LightEstimate lightEstimate)
        {
            return new SerializableLightEstimate(
                lightEstimate.capabilities,
                lightEstimate.ambientIntensity,
                lightEstimate.ambientColorTemperature);
        }

        public static implicit operator ARInterface.LightEstimate(SerializableLightEstimate serializedLightEstimate)
        {
            return new ARInterface.LightEstimate()
            {
                capabilities = (ARInterface.LightEstimateCapabilities)serializedLightEstimate.capabilities,
                ambientIntensity = serializedLightEstimate.ambientIntensity,
                ambientColorTemperature = serializedLightEstimate.ambientColorTemperature,
            };
        }
    }
}
