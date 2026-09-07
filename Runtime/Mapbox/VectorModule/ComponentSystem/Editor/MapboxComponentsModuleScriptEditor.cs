using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Mapbox.VectorModule.ComponentSystem.BuildingComponentVisualizer;
using UnityEditor;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem.Editor
{
    [CustomEditor(typeof(MapboxComponentsModuleScript))]
    public class MapboxComponentsModuleScriptEditor : UnityEditor.Editor
    {
        private SerializedProperty _layerVisualizersProp;
        private GUIStyle _boxStyle;
        private List<Type> _visualizerTypes;

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

            _editorInitialized = true;

            _layerVisualizersProp = serializedObject.FindProperty("_layerVisualizers");

            // Find all ScriptableObject types that implement IPerformanceLayerVisualizer
            _visualizerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    typeof(IPerformanceLayerVisualizer).IsAssignableFrom(t) &&
                    !t.IsAbstract)
                .ToList();
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

            DrawPropertiesOtherThanVisualizerList();

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(8, 8, 8, 8),
                    margin = new RectOffset(4, 4, 8, 8)
                };
            }

            // --- show info box if list is empty ---
            if (_layerVisualizersProp.arraySize == 0)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox("No visualizers added.\nUse the buttons below to add or create a new visualizer.",
                    MessageType.Info);
                EditorGUILayout.Space(8);
            }

            for (int i = 0; i < _layerVisualizersProp.arraySize; i++)
            {
                var element = _layerVisualizersProp.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(element, GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();

                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    _layerVisualizersProp.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                if (element.objectReferenceValue != null)
                {
                    var editor = CreateEditor(element.objectReferenceValue);
                    if (editor != null)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        editor.OnInspectorGUI();
                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.Space(8);

            // --- button row ---
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Empty Slot", GUILayout.Height(28)))
            {
                AddEmptySlot();
            }

            if (GUILayout.Button("Add Layer Visualizer", GUILayout.Height(28)))
            {
                ShowAddVisualizerMenu();
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws every serialized field except the visualizer list, which gets its own custom UI
        /// below. Enumerated rather than listed by name so a field added to
        /// <see cref="MapboxComponentsModuleScript"/> shows up without anyone remembering this file -
        /// RejectTilesOutsideZoom, ClampDataLevelToMax and CacheSize were invisible here for exactly
        /// that reason, which left subclassing as the only way to change them.
        /// </summary>
        private void DrawPropertiesOtherThanVisualizerList()
        {
            var iterator = serializedObject.GetIterator();
            var drewAny = false;

            // enterChildren on the first step only: we want top-level fields, and their own children
            // are drawn by PropertyField's foldout.
            for (var enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                if (iterator.propertyPath == "m_Script") continue;
                if (iterator.propertyPath == _layerVisualizersProp.propertyPath) continue;

                EditorGUILayout.PropertyField(iterator, true);
                drewAny = true;
            }

            if (drewAny)
                EditorGUILayout.Space(8);
        }

        private void AddEmptySlot()
        {
            serializedObject.Update();
            int newIndex = _layerVisualizersProp.arraySize;
            _layerVisualizersProp.InsertArrayElementAtIndex(newIndex);

            // ensure it's null, not a duplicate
            var el = _layerVisualizersProp.GetArrayElementAtIndex(newIndex);
            el.objectReferenceValue = null;

            serializedObject.ApplyModifiedProperties();
        }


        private void ShowAddVisualizerMenu()
        {
            GenericMenu menu = new GenericMenu();

            if (_visualizerTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No visualizer types found"));
            }
            else
            {
                foreach (var type in _visualizerTypes)
                {
                    var displayName = type
                        .GetCustomAttributes(typeof(DisplayNameAttribute), true)
                        .FirstOrDefault() as DisplayNameAttribute;
                    string name = (displayName != null)
                        ? displayName.DisplayName
                        : ObjectNames.NicifyVariableName(type.Name);
                    menu.AddItem(new GUIContent(name), false, () => CreateAndAddVisualizer(type));
                }
            }

            menu.ShowAsContext();
        }

        private void CreateAndAddVisualizer(Type type)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Visualizer",
                type.Name + ".asset",
                "asset",
                "Select a location to save the new visualizer asset."
            );

            if (string.IsNullOrEmpty(path))
                return; // cancelled

            var instance = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            serializedObject.Update();
            int newIndex = _layerVisualizersProp.arraySize;
            _layerVisualizersProp.InsertArrayElementAtIndex(newIndex);
            _layerVisualizersProp.GetArrayElementAtIndex(newIndex).objectReferenceValue = instance;
            serializedObject.ApplyModifiedProperties();
        }
    }
}