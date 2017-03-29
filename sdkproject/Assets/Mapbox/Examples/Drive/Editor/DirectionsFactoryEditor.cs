namespace Mapbox.Examples.Drive
{
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(DirectionsHelper))]
    public class DirectionsHelperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DirectionsHelper myScript = (DirectionsHelper)target;
            if (GUILayout.Button("Create"))
            {
                myScript.Query();
            }
        }
    }
}