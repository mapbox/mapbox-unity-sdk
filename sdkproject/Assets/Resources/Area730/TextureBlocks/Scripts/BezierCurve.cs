using UnityEngine;

namespace Area730
{
    public class BezierCurve : MonoBehaviour 
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

        public Vector3 GetLocalPoint(float t)
        {
            return Bezier.GetPoint(points[0], points[1], points[2], points[3], t);
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

        public Vector3 GetPointInWorld(int index)
        {
            return transform.TransformPoint(points[index]);
        }

        void OnDrawGizmos()
        {
            int lineSteps       = 20;
            Gizmos.color        = Color.cyan;
            Vector3 lineStart   = GetPoint( 0f );

            for ( int i = 1; i <= lineSteps; i++ )
            {
                Vector3 lineEnd = GetPoint( i / (float)lineSteps );
                
                Gizmos.DrawLine( lineStart, lineEnd );
                lineStart = lineEnd;
            }
        }

    }
}
