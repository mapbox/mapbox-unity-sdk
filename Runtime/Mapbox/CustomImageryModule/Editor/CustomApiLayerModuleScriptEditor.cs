using UnityEditor;
using UnityEngine;

namespace Mapbox.CustomImageryModule.Editor
{
    /// <summary>
    /// Hand-drawn so the fields appear grouped by what you are deciding, rather than in declaration
    /// order.
    ///
    /// Being hand-drawn also means a field is invisible until it is listed here. Source Type and
    /// Custom Source Id were both missing, which is why picking a Mapbox style through this module
    /// was not possible - the dropdown existed on the class but never reached the Inspector.
    ///
    /// Properties are resolved per draw rather than cached in OnEnable. OnEnable can run while the
    /// target is null - entering Play mode destroys and recreates the inspected object - and
    /// touching serializedObject then throws SerializedObjectNotCreatableException, which is not
    /// recoverable from inside the callback.
    /// </summary>
    [CustomEditor(typeof(CustomApiLayerModuleScript))]
    public class CustomApiLayerModuleScriptEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (target == null)
            {
                return;
            }

            serializedObject.Update();

            var settings = serializedObject.FindProperty("Settings");
            var sourceTypeProp = settings?.FindPropertyRelative("SourceType");
            var customSourceIdProp = settings?.FindPropertyRelative("CustomSourceId");
            var rejectTilesOutsideZoomProp = settings?.FindPropertyRelative("RejectTilesOutsideZoom");
            var clampDataLevelToMaxProp = settings?.FindPropertyRelative("DataSettings")
                ?.FindPropertyRelative("ClampDataLevelToMax");

            var customSourceSettingsProp = serializedObject.FindProperty("CustomSourceSettings");
            var modeProp = serializedObject.FindProperty("Mode");
            var dataZoomOffsetProp = serializedObject.FindProperty("DataZoomOffset");

            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);

            var isCustomSource = false;
            if (sourceTypeProp != null)
            {
                EditorGUILayout.PropertyField(sourceTypeProp);

                // Compared by name rather than cast to ImagerySourceType: that enum lives in
                // MapboxBaseModule, which this editor assembly does not reference, and a name is
                // safer than a hardcoded index if the enum is ever reordered.
                var names = sourceTypeProp.enumNames;
                var index = sourceTypeProp.enumValueIndex;
                isCustomSource = index >= 0 && index < names.Length && names[index] == "Custom";
            }

            // Only one of these is meaningful at a time, so only one is shown: a Mapbox style needs
            // no url, and a url-based source ignores the tileset id except as a cache key.
            if (isCustomSource)
            {
                if (customSourceIdProp != null)
                {
                    EditorGUILayout.PropertyField(customSourceIdProp);
                }

                EditorGUILayout.HelpBox(
                    "Custom: give a Url Format below, or a tileset id above to fetch by id.\n" +
                    "Url Format placeholders are {0}=Z, {1}=X, {2}=Y.",
                    MessageType.None);

                if (customSourceSettingsProp != null)
                {
                    EditorGUILayout.PropertyField(customSourceSettingsProp, true);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Fetches the chosen Mapbox style as raster tiles. Url Format is not needed and " +
                    "is ignored - switch Source Type to Custom to supply your own url.",
                    MessageType.None);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Layer Behaviour", EditorStyles.boldLabel);
            if (modeProp != null)
            {
                EditorGUILayout.PropertyField(modeProp);

                if (modeProp.enumValueIndex == (int)CustomImageryMode.Display)
                {
                    EditorGUILayout.HelpBox(
                        "Display draws on the map tiles. A tile holds one imagery slot, so if the map " +
                        "already has another imagery layer these two will overwrite each other. Use " +
                        "Data Only for a second source that something else reads, such as a minimap.",
                        MessageType.Info);
                }
            }

            if (dataZoomOffsetProp != null)
            {
                EditorGUILayout.PropertyField(dataZoomOffsetProp);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Tile Settings", EditorStyles.boldLabel);
            if (rejectTilesOutsideZoomProp != null)
            {
                EditorGUILayout.PropertyField(rejectTilesOutsideZoomProp);
            }
            if (clampDataLevelToMaxProp != null)
            {
                EditorGUILayout.PropertyField(clampDataLevelToMaxProp);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
