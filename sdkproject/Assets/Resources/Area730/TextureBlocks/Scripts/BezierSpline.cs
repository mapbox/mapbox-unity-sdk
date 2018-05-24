using System;
using UnityEngine;

namespace Area730
{
    public class BezierSpline : MonoBehaviour 
    {
        public Vector3[] points;

        public void Reset()
        {
            points = new Vector3[] {
			                            new Vector3(1f, 0f, 0f),
			                            new Vector3(2f, 0f, 0f),
			                            new Vector3(3f, 0f, 0f),
                                        new Vector3(4f, 0f, 0f)
		                            };


        }

        public Vector3 GetPoint(float t)
        {
            return transform.TransformPoint(Bezier.GetPoint(points[0], points[1], points[2], points[3], t));
        }

        public Vector3 GetVelocity(float t)
        {
            return transform.TransformPoint(Bezier.GetFirstDerivative(points[0], points[1], points[2], points[3], t)) - transform.position;
        }

        public Vector3 GetDirection(float t)
        {
            return GetVelocity(t).normalized;
        }

        public void AddCurve()
        {
            Vector3 point = points[points.Length - 1];
            Array.Resize(ref points, points.Length + 3);
            point.x += 1f;
            points[points.Length - 3] = point;
            point.x += 1f;
            points[points.Length - 2] = point;
            point.x += 1f;
            points[points.Length - 1] = point;
        }

        public int CurveCount
        {
            get
            {
                return (points.Length - 1) / 3;
            }
        }

    }
}
