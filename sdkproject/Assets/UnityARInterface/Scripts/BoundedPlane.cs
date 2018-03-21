using UnityEngine;

namespace UnityARInterface
{
    public struct BoundedPlane
    {
        public string id;
        public Vector3 center;
        public Vector2 extents;
        public Quaternion rotation;

        public Vector3 normal { get { return rotation * Vector3.up; } }
        public Plane plane { get { return new Plane(normal, center); } }
        public float width
        {
            get { return extents.x; }
            set { extents.x = value; }
        }
        public float height
        {
            get { return extents.y; }
            set { extents.y = value; }
        }

        public Vector3[] quad
        {
            get
            {
                Vector3[] points = new Vector3[4];
                var right = rotation * Vector3.right * extents.x / 2;
                var forward = rotation * Vector3.forward * extents.y / 2;

                points[0] = center + right - forward;
                points[1] = center + right + forward;
                points[2] = center + -right + forward;
                points[3] = center + -right - forward;

                return points;
            }
        }
    }
}
