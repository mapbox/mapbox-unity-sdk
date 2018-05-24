using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Area730
{
    [CustomEditor(typeof(BezierSpline))]
    public class BezierSplineInspector : Editor 
    {
        private BezierSpline spline;
        private Transform   handleTransform;
        private Quaternion  handleRotation;

        private const int lineSteps = 10;

        private void OnSceneGUI()
        {
            spline          = target as BezierSpline;
            handleTransform = spline.transform;
            handleRotation  = Tools.pivotRotation == PivotRotation.Local ? handleTransform.rotation : Quaternion.identity;

            Vector3 p0 = ShowPoint(0);
            for (int i = 1; i < spline.points.Length; i += 3)
            {
                Vector3 p1 = ShowPoint(i);
                Vector3 p2 = ShowPoint(i + 1);
                Vector3 p3 = ShowPoint(i + 2);

                Handles.color = Color.gray;
                Handles.DrawLine(p0, p1);
                Handles.DrawLine(p2, p3);

                Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
                p0 = p3;
            }

        }

        private Vector3 ShowPoint(int index)
        {
            Vector3 point = handleTransform.TransformPoint(spline.points[index]);

            EditorGUI.BeginChangeCheck();

            point = Handles.DoPositionHandle(point, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(spline, "Move Point");
                EditorUtility.SetDirty(spline);
                Vector3 newLocalPos = handleTransform.InverseTransformPoint(point);
                newLocalPos.y = 0;
                spline.points[index] = newLocalPos;
            }
            return point;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            spline = target as BezierSpline;
            if (GUILayout.Button("Add Curve"))
            {
                Undo.RecordObject(spline, "Add Curve");
                spline.AddCurve();
                EditorUtility.SetDirty(spline);
            }
        }


    }
}
