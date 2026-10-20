using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Mapbox.VectorModule.MeshGeneration.Unity;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mapbox.VectorModule.Editor
{
    [CustomEditor(typeof(ModifierStackObject), true)]
    public class ModifierStackEditor : UnityEditor.Editor
    {
        private Dictionary<SerializedProperty, List<UnityEditor.Editor>> m_Editors = new Dictionary<SerializedProperty, List<UnityEditor.Editor>>();
        private SerializedProperty m_MeshModifiers;
        private SerializedProperty m_GoModifiers;
        private SerializedProperty m_FilterStack;
        private SerializedProperty m_Settings;
        [SerializeField] private bool falseBool = false;
        private SerializedProperty m_FalseBool;
        private Texture2D _magnifier;
        private UnityEditor.Editor _filterEditor;
    
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

            m_MeshModifiers = serializedObject.FindProperty(nameof(ModifierStackObject.MeshModifiers));
            m_Editors.Add(m_MeshModifiers, new List<UnityEditor.Editor>());
            m_GoModifiers = serializedObject.FindProperty(nameof(ModifierStackObject.GoModifiers));
            m_Editors.Add(m_GoModifiers, new List<UnityEditor.Editor>());
            m_Settings = serializedObject.FindProperty(nameof(ModifierStackObject.Settings));
            _magnifier = EditorGUIUtility.FindTexture("d_ViewToolZoom");
        
            var editorObj = new SerializedObject(this);
            m_FalseBool = editorObj.FindProperty(nameof(falseBool));
            UpdateEditorList();
        
            ScriptableCreatorWindow.WindowClosed += () =>
            {
                m_MeshModifiers = serializedObject.FindProperty(nameof(ModifierStackObject.MeshModifiers));
                m_GoModifiers = serializedObject.FindProperty(nameof(ModifierStackObject.GoModifiers));
                UpdateEditorList();
            };
        }

        private void OnEnable()
        {
            TryInitializeEditor();
        }

        private void CreateFilterStack()
        {
            m_FilterStack = serializedObject.FindProperty(nameof(ModifierStackObject.Filters));
            if(m_FilterStack == null || m_FilterStack.objectReferenceValue == null)
            {
                ScriptableObject component = CreateInstance(nameof(VectorFilterStackObject));
                component.name = $"FilterStack";
                if (EditorUtility.IsPersistent(target))
                {
                    AssetDatabase.AddObjectToAsset(component, target);
                }

                _filterEditor = CreateEditor(component);
                m_FilterStack.objectReferenceValue = component;
                serializedObject.ApplyModifiedProperties();
            }
        }

        public override void OnInspectorGUI()
        {
            TryInitializeEditor();
            if (!_editorInitialized)
            {
                return;
            }

            if (!EditorUtility.IsPersistent(target))
                return;

            if (m_FilterStack == null)
                CreateFilterStack();

            serializedObject.Update();

            // HEADER
            DrawHeader();
            EditorGUILayout.Space(5);

            // SETTINGS
            CoreEditorUtils.DrawSplitter();
            DrawSettings();
            EditorGUILayout.Space(8);

            // FILTERS
            CoreEditorUtils.DrawSplitter();
            DrawFilters();
            EditorGUILayout.Space(8);

            // MESH MODIFIERS
            CoreEditorUtils.DrawSplitter();
            DrawMeshModifiers();
            EditorGUILayout.Space(8);

            // GAMEOBJECT MODIFIERS
            CoreEditorUtils.DrawSplitter();
            DrawGameObjectModifiers();

            EditorGUILayout.Space(10);
        }

        private void DrawHeader()
        {
            var nameProperty = serializedObject.FindProperty("m_Name");
            EditorGUILayout.LabelField(nameProperty.stringValue, EditorStyles.whiteLargeLabel);
            EditorGUILayout.Space(5);
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Settings);
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        private void DrawFilters()
        {
            if (_filterEditor == null)
                _filterEditor = CreateEditor(m_FilterStack.objectReferenceValue);

            if (_filterEditor != null)
            {
                EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUI.indentLevel++;
                    _filterEditor.OnInspectorGUI();
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawMeshModifiers()
        {
            EditorGUILayout.LabelField("Mesh Modifiers", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawMeshModifiers(m_MeshModifiers, typeof(ScriptableMeshModifierObject));
            }
        }

        private void DrawGameObjectModifiers()
        {
            EditorGUILayout.LabelField("GameObject Modifiers", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawMeshModifiers(m_GoModifiers, typeof(ScriptableGameObjectModifierObject));
            }
        }

        private void DrawMeshModifiers(SerializedProperty property, System.Type type)
        {
            if (property == null)
            {
                EditorGUILayout.HelpBox("Modifiers property is missing.", MessageType.Warning);
                return;
            }

            try
            {
                if (property.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("No modifiers added", MessageType.Info);
                }
                else
                {
                    // Draw each element inside its own subtle box so it sits nicely
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        SerializedProperty elementProp = property.GetArrayElementAtIndex(i);

                        // Each modifier gets a small framed area so it reads well inside the parent helpBox
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            EditorGUI.indentLevel++;
                            // Draw the modifier - keep existing behaviour, but pass the element
                            // Assuming DrawModifier(SerializedProperty listProperty, int index, ref SerializedProperty elementProperty) exists
                            DrawModifier(property, i, ref elementProp);
                            EditorGUI.indentLevel--;
                        }

                        EditorGUILayout.Space(4);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                EditorGUILayout.HelpBox($"Error drawing modifiers: {ex.Message}", MessageType.Error);
            }

            // Add / Open buttons aligned neatly
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("  Add Modifier  ", (GUIStyle)"minibuttonleft"))
                {
                    AddPassMenu(property, type);
                }

                // small spacer so the magnifier sits to the right
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(_magnifier, (GUIStyle)"minibuttonright", GUILayout.Width(30)))
                {
                    ScriptableCreatorWindow.Open(type, property);
                }
            }
        }


        private void AddPassMenu(SerializedProperty property, Type modType)
        {
            GenericMenu menu = new GenericMenu();
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom(modType);
            foreach (Type type in types)
            {
                var displayName = type
                    .GetCustomAttributes(typeof(DisplayNameAttribute), true)
                    .FirstOrDefault() as DisplayNameAttribute;
                string path = (displayName != null) ? displayName.DisplayName : type.Name;
                menu.AddItem(new GUIContent(path), false, (o) => AddComponent(property, o), type.Name);
            }
            menu.ShowAsContext();
        }
    
        private void AddComponent(SerializedProperty property, object type)
        {
            serializedObject.Update();

            ScriptableObject component = CreateInstance((string)type);
            component.name = $"{(string)type}";
            Undo.RegisterCreatedObjectUndo(component, "Add modifier");

            // Store this new effect as a sub-asset so we can reference it safely afterwards
            // Only when we're not dealing with an instantiated asset
            if (EditorUtility.IsPersistent(target))
            {
                AssetDatabase.AddObjectToAsset(component, target);
            }
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out var guid, out long localId);

            // Grow the list first, then add - that's how serialized lists work in Unity
            property.arraySize++;
            SerializedProperty componentProp = property.GetArrayElementAtIndex(property.arraySize - 1);
            componentProp.objectReferenceValue = component;

            UpdateEditorList();
            serializedObject.ApplyModifiedProperties();

            // Force save / refresh
            if (EditorUtility.IsPersistent(target))
            {
                ForceSave();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawModifier(SerializedProperty property, int index, ref SerializedProperty renderFeatureProperty)
        {
            Object modifierObjRef = renderFeatureProperty.objectReferenceValue;

            if (modifierObjRef != null)
            {
                bool hasChangedProperties = false;
                string title = ObjectNames.GetInspectorTitle(modifierObjRef);

                // Ensure the editor list has an editor for this modifier
                if (m_Editors.TryGetValue(property, out var editors))
                {
                    while (editors.Count <= index)
                        editors.Add(null);

                    if (editors[index] == null || editors[index].target != modifierObjRef)
                        editors[index] = CreateEditor(modifierObjRef);
                }

                UnityEditor.Editor modifierEditor = m_Editors[property][index];
                if (modifierEditor == null)
                {
                    EditorGUILayout.HelpBox("Missing modifier editor.", MessageType.Warning);
                    return;
                }

                // Update serialized object
                SerializedObject serializedModifierEditor = modifierEditor.serializedObject;
                serializedModifierEditor.Update();

                // Header foldout
                EditorGUI.BeginChangeCheck();
                SerializedProperty activeProperty = serializedModifierEditor.FindProperty("m_Active");
                bool displayContent = CoreEditorUtils.DrawHeaderToggle(
                    title,
                    renderFeatureProperty,
                    activeProperty,
                    pos => OnContextClick(modifierObjRef, property, pos, index)
                );
                hasChangedProperties |= EditorGUI.EndChangeCheck();

                // Draw inside framed box when expanded
                if (displayContent)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.Space(2);

                        // Reference field
                        // EditorGUILayout.ObjectField("Modifier Asset", renderFeatureProperty.objectReferenceValue, typeof(Object), false);

                        EditorGUILayout.Space(4);

                        EditorGUI.BeginChangeCheck();
                        modifierEditor.OnInspectorGUI();
                        hasChangedProperties |= EditorGUI.EndChangeCheck();

                        EditorGUILayout.Space(4);
                    }

                    EditorGUILayout.Space(4);
                }

                // Apply & save changes
                if (hasChangedProperties)
                {
                    serializedModifierEditor.ApplyModifiedProperties();
                    serializedObject.ApplyModifiedProperties();
                    ForceSave();
                }
            }
            else
            {
                // Missing modifier handling
                CoreEditorUtils.DrawHeaderToggle(
                    new GUIContent("Missing Modifier"),
                    renderFeatureProperty,
                    m_FalseBool,
                    pos => OnContextClick(null, property, pos, index)
                );
                m_FalseBool.boolValue = false;
            }
        }

        private void OnContextClick(Object obj, SerializedProperty property, Vector2 position, int id)
        {
            var menu = new GenericMenu();

            if (obj != null)
            {
                menu.AddItem(EditorGUIUtility.TrTextContent("Select in project"), false, () => { EditorGUIUtility.PingObject( obj ); });
            }
        
            if (id == 0)
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Up"));
            else
                menu.AddItem(EditorGUIUtility.TrTextContent("Move Up"), false, () => MoveComponent(property, id, -1));

            if (id == property.arraySize - 1)
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Down"));
            else
                menu.AddItem(EditorGUIUtility.TrTextContent("Move Down"), false, () => MoveComponent(property, id, 1));

            menu.AddSeparator(string.Empty);
            menu.AddItem(EditorGUIUtility.TrTextContent("Remove"), false, () => RemoveComponent(property, id));

            menu.DropDown(new Rect(position, Vector2.zero));
        }
    
        private void RemoveComponent(SerializedProperty arrayProperty, int id)
        {
            SerializedProperty property = arrayProperty.GetArrayElementAtIndex(id);
            Object component = property.objectReferenceValue;
            property.objectReferenceValue = null;

            Undo.SetCurrentGroupName(component == null ? "Remove Modifier" : $"Remove {component.name}");

            // remove the array index itself from the list
            arrayProperty.DeleteArrayElementAtIndex(id);
            UpdateEditorList();
            serializedObject.ApplyModifiedProperties();

        
            // var isAssetFile = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out var guid, out long localId);
            // var isStackFile = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(serializedObject.targetObject, out var stackGuid, out long stackLocalId);
        
            // Destroy the setting object after ApplyModifiedProperties(). If we do it before, redo
            // actions will be in the wrong order and the reference to the setting object in the
            // list will be lost.
            if (component != null && !AssetDatabase.IsMainAsset(component)) //guid == stackGuid
            {
                Undo.DestroyObjectImmediate(component);
            }

            // Force save / refresh
            ForceSave();
        }
    
        private void MoveComponent(SerializedProperty sproperty, int id, int offset)
        {
            Undo.SetCurrentGroupName("Move Render Feature");
            serializedObject.Update();
            sproperty.MoveArrayElement(id, id + offset);
            UpdateEditorList();
            serializedObject.ApplyModifiedProperties();

            // Force save / refresh
            ForceSave();
        }


        private void UpdateEditorList()
        {
            ClearEditorsList();
        
            if(!m_Editors.ContainsKey(m_MeshModifiers)) m_Editors.Add(m_MeshModifiers, new List<UnityEditor.Editor>());
            if(!m_Editors.ContainsKey(m_GoModifiers)) m_Editors.Add(m_GoModifiers, new List<UnityEditor.Editor>());
        
            for (int i = 0; i < m_MeshModifiers.arraySize; i++)
            {
                m_Editors[m_MeshModifiers].Add(CreateEditor(m_MeshModifiers.GetArrayElementAtIndex(i).objectReferenceValue));
            }
            for (int i = 0; i < m_GoModifiers.arraySize; i++)
            {
                m_Editors[m_GoModifiers].Add(CreateEditor(m_GoModifiers.GetArrayElementAtIndex(i).objectReferenceValue));
            }
        }
    
        private void ClearEditorsList()
        {
            foreach (var perProp in m_Editors.Values)
            {
                for (int i = perProp.Count - 1; i >= 0; --i)
                {
                    DestroyImmediate(perProp[i]);
                }
            }
        
            m_Editors.Clear();
        }
        
        private void ForceSave()
        {
            EditorUtility.SetDirty(target);
        }
    
        private string ValidateName(string name)
        {
            name = Regex.Replace(name, @"[^a-zA-Z0-9 ]", "");
            return name;
        }
    

    }
}