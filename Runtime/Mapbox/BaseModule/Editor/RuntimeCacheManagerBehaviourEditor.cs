using Mapbox.BaseModule.Unity.ModuleBehaviours;
using UnityEditor;

namespace Mapbox.BaseModule.Editor
{
    [CustomEditor(typeof(RuntimeCacheManagerBehaviour))]
    public class RuntimeCacheManagerBehaviourEditor : UnityEditor.Editor
    {
        SerializedProperty createSqliteCache;
        SerializedProperty useCustomName;
        SerializedProperty customName;
        SerializedProperty createFileCache;

        private bool _editorInitialized;

        // Editor.OnEnable can run while the target is null - entering Play mode destroys and recreates
        // the inspected object - and touching serializedObject then throws
        // SerializedObjectNotCreatableException, which cannot be recovered from inside the callback.
        // Initialization is retried from OnInspectorGUI, which only runs once the target is real.
        private void TryInitializeEditor()
        {
            if (_editorInitialized || target == null)
            {
                return;
            }

            createSqliteCache  = serializedObject.FindProperty("CreateSqliteCache");
            useCustomName      = serializedObject.FindProperty("UseCustomName");
            customName   = serializedObject.FindProperty("CustomName");
            createFileCache    = serializedObject.FindProperty("CreateFileCache");

            _editorInitialized = true;
        }

        private void OnEnable()
        {
            TryInitializeEditor();
        }

        public override void OnInspectorGUI()
        {
            TryInitializeEditor();
            if (!_editorInitialized)
            {
                return;
            }

            serializedObject.Update();
            EditorGUILayout.PropertyField(useCustomName);
            if (useCustomName.boolValue)
            {
                EditorGUILayout.PropertyField(customName);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}