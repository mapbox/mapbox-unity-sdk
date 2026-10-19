using System.Collections.Generic;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.ImageModule.Minimap.Unity;
// The module scripts live in this namespace, not one matching their ImageModule/Unity/ folder.
using Mapbox.Example.Scripts.ModuleBehaviours;
using UnityEditor;
using UnityEngine;

namespace Mapbox.ImageModule.Editor
{
    /// <summary>
    /// Exists for one field. The Source dropdown lists every layer module in the scene a minimap
    /// can draw, with "None" meaning the map's own imagery. A module has to be picked by name
    /// because an object field cannot: modules are components, several sit on one GameObject, and dragging that object makes
    /// Unity silently take whichever <see cref="ModuleConstructorScript"/> is ordered first.
    ///
    /// The scan covers the whole scene rather than the map's own GameObject, because a module does
    /// not have to be there: <c>CompositeLayerModuleScript</c> holds a list of module scripts and
    /// builds them wherever they live, so terrain and imagery are often on separate objects.
    ///
    /// Everything else draws normally, so new fields on the component appear without this file
    /// needing to know about them.
    /// </summary>
    [CustomEditor(typeof(MinimapScript))]
    public class MinimapScriptEditor : UnityEditor.Editor
    {
        private const string SourceModulePath = "SourceModule";
        private const string NoneLabel = "None (map imagery)";

        // Names, not types, for the two modules in the custom imagery assembly: that assembly
        // references this one, so referencing it back would be circular. The rest are real types,
        // which also picks up any subclass of them.
        private static readonly string[] CrossAssemblyModuleTypeNames =
        {
            "CustomApiLayerModuleScript",
            "CustomTerrainLayerModuleScript"
        };

        private readonly List<ModuleConstructorScript> _modules = new List<ModuleConstructorScript>();
        private readonly List<string> _labels = new List<string>();
        private readonly HashSet<ModuleConstructorScript> _reachable = new HashSet<ModuleConstructorScript>();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var iterator = serializedObject.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(iterator);
                    }
                    continue;
                }

                if (iterator.propertyPath == SourceModulePath)
                {
                    DrawSourcePicker(iterator);
                    continue;
                }

                if (!IsShownForCurrentModes(iterator.propertyPath))
                {
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// The reference fields only make sense in one mode each, so the others are hidden rather
        /// than left as empty slots that look like they need filling.
        /// </summary>
        private bool IsShownForCurrentModes(string propertyPath)
        {
            switch (propertyPath)
            {
                case "ViewCenter":
                    return CenterIs(MinimapScript.CenterMode.Transform);
                case "ViewCenterCamera":
                    return CenterIs(MinimapScript.CenterMode.CameraView);
                case "OrientationTransform":
                    return RotationIs(MinimapScript.RotationMode.Transform);
                case "OrientationRoot":
                    return !RotationIs(MinimapScript.RotationMode.None);
                default:
                    return true;
            }
        }

        private bool CenterIs(MinimapScript.CenterMode mode)
        {
            return serializedObject.FindProperty("Center").enumValueIndex == (int)mode;
        }

        private bool RotationIs(MinimapScript.RotationMode mode)
        {
            return serializedObject.FindProperty("Rotation").enumValueIndex == (int)mode;
        }

        /// <summary>
        /// Only modules whose data a minimap can actually draw. A composite carries no data of its
        /// own, and vector or component modules produce meshes rather than textures, so offering any
        /// of them would only lead to a runtime error.
        /// </summary>
        private static bool CanFeedMinimap(ModuleConstructorScript module)
        {
            if (module is StaticApiLayerModuleScript || module is TerrainLayerModuleScript)
            {
                return true;
            }

            var typeName = module.GetType().Name;
            foreach (var name in CrossAssemblyModuleTypeNames)
            {
                if (typeName == name)
                {
                    return true;
                }
            }

            return false;
        }

        private void DrawSourcePicker(SerializedProperty sourceModuleProp)
        {
            var map = serializedObject.FindProperty("Map").objectReferenceValue as MapBehaviourCore;
            var current = sourceModuleProp.objectReferenceValue as ModuleConstructorScript;

            // Cheap enough to run every repaint: it walks the map's own components and the composite
            // lists they point at, with no scene scan. Only the module list needs FindObjectsByType,
            // and that waits for a click.
            BuildReachableSet(map);

            var rect = EditorGUILayout.GetControlRect();
            var fieldRect = EditorGUI.PrefixLabel(
                rect, new GUIContent("Source", sourceModuleProp.tooltip));

            // A dropdown button rather than a popup: a popup has to be handed every option at draw
            // time, which would mean scanning the scene on every Inspector repaint. This only builds
            // the list when it is opened.
            if (EditorGUI.DropdownButton(fieldRect, new GUIContent(DescribeSelection(current, map)), FocusType.Keyboard))
            {
                ShowSourceMenu(fieldRect, map, current);
            }

            // Reachability is worth calling out separately from existence: a module the map never
            // builds looks identical in the scene to one it does, and produces an empty minimap.
            if (current != null && map != null && !_reachable.Contains(current))
            {
                EditorGUILayout.HelpBox(
                    $"'{current.name}' is in the scene but the map never builds it, so it will have no " +
                    $"data. Either move the component onto '{map.name}', or add it to a " +
                    "CompositeLayerModuleScript that the map does build.",
                    MessageType.Warning);
            }
        }

        private string DescribeSelection(ModuleConstructorScript current, MapBehaviourCore map)
        {
            if (current == null)
            {
                return NoneLabel;
            }

            var label = $"{current.GetType().Name}  ({current.gameObject.name})";
            if (map != null && !_reachable.Contains(current))
            {
                label += "  [not built by map]";
            }
            return label;
        }

        private void ShowSourceMenu(Rect rect, MapBehaviourCore map, ModuleConstructorScript current)
        {
            CollectModules(map);

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent(NoneLabel), current == null, () => Assign(null));

            if (_modules.Count > 0)
            {
                menu.AddSeparator(string.Empty);
                for (var i = 0; i < _modules.Count; i++)
                {
                    var module = _modules[i];
                    menu.AddItem(new GUIContent(_labels[i]), module == current, () => Assign(module));
                }
            }

            menu.DropDown(rect);
        }

        /// <summary>
        /// Menu callbacks run after OnInspectorGUI has returned, so this takes a fresh
        /// SerializedObject rather than touching the one being drawn.
        /// </summary>
        private void Assign(ModuleConstructorScript module)
        {
            var so = new SerializedObject(target);
            so.FindProperty(SourceModulePath).objectReferenceValue = module;
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// Fills the candidate list. Expects <see cref="_reachable"/> to be current - the draw pass
        /// rebuilds it every repaint, and this only ever runs from a click inside that pass, so it
        /// does not rebuild it again.
        /// </summary>
        private void CollectModules(MapBehaviourCore map)
        {
            _modules.Clear();
            _labels.Clear();

            var found = Object.FindObjectsByType<ModuleConstructorScript>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            var typeCounts = new Dictionary<string, int>();
            foreach (var module in found)
            {
                if (module == null || !CanFeedMinimap(module))
                {
                    continue;
                }

                var typeName = module.GetType().Name;
                typeCounts.TryGetValue(typeName, out var seen);
                typeCounts[typeName] = seen + 1;

                // The owning object is part of the label because modules are spread across objects
                // once composites are in play, and two of a type is legitimate - two custom imagery
                // layers on different styles, say.
                // GenericMenu treats '/' as a submenu separator, so an object named "a/b"
                // would silently nest instead of appearing as one entry.
                var objectName = module.gameObject.name.Replace("/", "\u2215");
                var label = $"{typeName}  ({objectName})";
                if (!module.enabled && !_reachable.Contains(module))
                {
                    // Disabled and not pulled in by a composite - the map will skip it.
                    label += "  [disabled]";
                }
                else if (map != null && !_reachable.Contains(module))
                {
                    label += "  [not built by map]";
                }

                _modules.Add(module);
                _labels.Add(label);
            }
        }

        /// <summary>
        /// The modules the map actually builds: the ones on its own GameObject, plus everything
        /// reachable through composites from those, transitively.
        /// </summary>
        private void BuildReachableSet(MapBehaviourCore map)
        {
            _reachable.Clear();
            if (map == null)
            {
                return;
            }

            var pending = new Queue<ModuleConstructorScript>();

            // Top level only: the map skips disabled module scripts when it collects its own
            // components. Composite children are NOT filtered that way - CompositeLayerModuleScript
            // builds every non-null entry regardless of enabled - so the check belongs here and not
            // in the loop below, or a disabled module inside a composite would be reported as
            // unreachable when the map does in fact build it.
            foreach (var module in map.GetComponents<ModuleConstructorScript>())
            {
                if (module != null && module.enabled)
                {
                    pending.Enqueue(module);
                }
            }

            while (pending.Count > 0)
            {
                var module = pending.Dequeue();
                if (!_reachable.Add(module))
                {
                    continue;
                }

                if (module is CompositeLayerModuleScript composite && composite.ModuleConstructorScripts != null)
                {
                    foreach (var child in composite.ModuleConstructorScripts)
                    {
                        if (child != null)
                        {
                            pending.Enqueue(child);
                        }
                    }
                }
            }
        }
    }
}
