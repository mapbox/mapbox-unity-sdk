using System;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace Mapbox.LandmarksModule.Unity
{
    [RequireComponent(typeof(LandmarksLayerModuleScript))]
    public class LandmarkHighlightBehaviour : MonoBehaviour
    {
        [Tooltip("Colour a landmark returns to when deselected. Should match the landmark material's own baseColorFactor, or deselecting will visibly change the building - white matches the shipped LandmarkMaterialGLTF.")]
        public Color DefaultColor = Color.white;

        [Tooltip("Colour applied to the landmark under the cursor when clicked. A saturated hue reads best against the untinted neighbours; red is the default.")]
        public Color HighlightColor = Color.red;

        [Tooltip("Build a MeshCollider per landmark so clicks can hit them. ON is required for selection to work at all. Turn it OFF if you only want the events and are doing your own hit-testing - mesh colliders are the most expensive part of loading a landmark, and every landmark in the cover gets one.")]
        public bool AddCollidersForSelection = true;

        [Tooltip("Cook the collider as a convex hull rather than the exact building geometry. Much cheaper, and enough when a click only needs to identify which building was hit. Turn OFF if clicks must follow the real silhouette - a concave building (a courtyard, an arch) will register hits in the gaps with this on.")]
        public bool UseConvexColliders = true;

        [Tooltip("Fires with the landmark GameObject that was just clicked. Highlighting itself is not conditional on anything being subscribed here - it always happens, this is purely a notification for other systems (e.g. a details panel).")]
        public UnityEvent<GameObject> VisualSelected;

        private readonly int _shaderColorPropertyName = Shader.PropertyToID("baseColorFactor");

        // Tinting through a property block rather than Renderer.material. Touching .material
        // instantiates a private copy of the material on first access and never frees it, so every
        // selection leaked one Material for the rest of the session.
        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        /// <summary>
        /// Id of the landmark currently highlighted, or null when nothing is selected. The same
        /// value is on the selected GameObject's <see cref="LandmarkComponent"/>; this is here so a
        /// <see cref="VisualSelected"/> handler wired in the Inspector, which receives only the
        /// GameObject, does not have to go looking for the component.
        /// </summary>
        public string SelectedLandmarkId { get; private set; }

        private GameObject _currentlySelectedBuilding;
        private LandmarksLayerModule _module;
        private Camera _camera;
        private MapBehaviourCore _map;
        private void Start()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                Debug.LogWarning($"{nameof(LandmarkHighlightBehaviour)} found no camera tagged MainCamera, so landmark selection is disabled.", this);
                enabled = false;
                return;
            }

            _map = FindObjectOfType<MapBehaviourCore>();
            if (_map == null)
            {
                Debug.LogWarning($"{nameof(LandmarkHighlightBehaviour)} found no {nameof(MapBehaviourCore)} in the scene, so landmark selection is disabled.", this);
                enabled = false;
                return;
            }

            // Named method rather than a lambda so OnDestroy can detach it. As a closure it could not
            // be unsubscribed, so destroying this component before the map initialized left the body
            // to run against a dead component.
            _map.Initialized += OnMapInitialized;
        }

        private void OnMapInitialized(MapboxMap mapboxMap)
        {
            _module = (LandmarksLayerModule)GetComponent<LandmarksLayerModuleScript>().ModuleImplementation;
            _module.BuildingLoaded += OnBuildingLoaded;
        }

        private void OnBuildingLoaded(GameObject o, string landmarkId)
        {
            // The id travels with the event because the geometry does not carry it. Stamping it on
            // the marker component is what lets a raycast hit answer which landmark it belongs to.
            o.AddComponent<LandmarkComponent>().LandmarkId = landmarkId;
            if (AddCollidersForSelection) AddCollidersToVisuals(o);
        }

        private void OnDestroy()
        {
            if (_map != null) _map.Initialized -= OnMapInitialized;
            if (_module != null) _module.BuildingLoaded -= OnBuildingLoaded;
        }

        private void Update()
        {
            if (TryGetClickPosition(out var clickPosition))
            {
                var ray = _camera.ScreenPointToRay(clickPosition);
                if (Physics.Raycast(ray, out var hit))
                {
                    var buildingComponent = hit.transform.gameObject.GetComponent<LandmarkComponent>();
                    if (buildingComponent != null)
                    {
                        // Highlighting is not conditional on anything being subscribed. This used to
                        // sit inside a null check on the event, so with nothing wired in the
                        // Inspector clicking a landmark did nothing at all and the component looked
                        // broken.
                        BuildingSelected(hit.transform.gameObject, buildingComponent);
                        VisualSelected?.Invoke(hit.transform.gameObject);
                    }
                    else
                    {
                        ClearSelectedBuilding();
                    }
                }
                else
                {
                    ClearSelectedBuilding();
                }
            }
        }

        // Gated on ENABLE_INPUT_SYSTEM, the same way Mapbox.Example's PointerInput is. Unity sets it
        // when Active Input Handling is "New" or "Both"; under "New" the legacy UnityEngine.Input
        // API throws every frame, which is what used to break clicking here. Package presence is
        // not the gate because com.unity.inputsystem is always installed as a dependency, and
        // legacy-only projects must keep taking the #else branch. The block is duplicated rather
        // than calling PointerInput because that type is internal to the examples assembly, which
        // this module does not reference.
        private static bool TryGetClickPosition(out Vector3 position)
        {
#if ENABLE_INPUT_SYSTEM
            // No mouse device (a touch-only player, for instance) is treated as no click.
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                position = default;
                return false;
            }

            position = mouse.position.ReadValue();
            return true;
#else
            if (!UnityEngine.Input.GetMouseButtonDown(0))
            {
                position = default;
                return false;
            }

            position = UnityEngine.Input.mousePosition;
            return true;
#endif
        }

        private void AddCollidersToVisuals(GameObject go)
        {
            var meshFilter = go.GetComponent<MeshFilter>();

            // sharedMesh throughout: reading MeshFilter.mesh duplicates the mesh and leaks the copy,
            // and a collider only needs to read the geometry, never to own it.
            if (meshFilter == null || meshFilter.sharedMesh == null) return;

            var mc = go.AddComponent<MeshCollider>();

            // Cooking an exact MeshCollider from the render mesh is the most expensive single step
            // in bringing a landmark on screen, and it is paid for every landmark in the cover
            // whether or not anyone ever clicks one. A convex hull identifies the building just as
            // well for a pick, at a fraction of the cost. Set on the collider before sharedMesh, so
            // the cook happens once with the setting already applied.
            if (UseConvexColliders)
            {
                mc.convex = true;
            }

            mc.sharedMesh = meshFilter.sharedMesh;
        }

        private void ClearSelectedBuilding()
        {
            SelectedLandmarkId = null;
            Tint(_currentlySelectedBuilding, DefaultColor);
            // Cleared as well as recoloured. Leaving the reference behind meant the next selection
            // re-tinted a building that was already back to its default, and a destroyed one stayed
            // referenced.
            _currentlySelectedBuilding = null;
        }

        private void BuildingSelected(GameObject building, LandmarkComponent component)
        {
            Tint(_currentlySelectedBuilding, DefaultColor);
            _currentlySelectedBuilding = building;
            SelectedLandmarkId = component != null ? component.LandmarkId : null;
            Tint(_currentlySelectedBuilding, HighlightColor);
        }

        private void Tint(GameObject building, Color color)
        {
            if (building == null) return;

            var renderer = building.GetComponent<MeshRenderer>();
            if (renderer == null) return;

            // Read the existing block first so this does not discard overrides someone else set on
            // the same renderer.
            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(_shaderColorPropertyName, color);
            renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
