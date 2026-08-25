using System;
using Mapbox.BaseModule.Utilities;
using Mapbox.LandmarkModule.Unity;
using UnityEngine;
using UnityEngine.Events;

namespace Mapbox.LandmarkModule
{
    [RequireComponent(typeof(LandmarksLayerModuleScript))]
    public class LandmarkHighlightBehaviour : MonoBehaviour
    {
        [Tooltip("Colour a landmark returns to when deselected. Should match the landmark material's own baseColorFactor, or deselecting will visibly change the building - white matches the shipped LandmarkMaterialGLTF.")]
        public Color DefaultColor = Color.white;

        [Tooltip("Colour applied to the landmark under the cursor when clicked. A saturated hue reads best against the untinted neighbours; red is the default.")]
        public Color HightlightColor = Color.red;

        private readonly int _shaderColorPropertyName = Shader.PropertyToID("baseColorFactor");

        // Tinting through a property block rather than Renderer.material. Touching .material
        // instantiates a private copy of the material on first access and never frees it, so every
        // selection leaked one Material for the rest of the session.
        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        private GameObject _currentlySelectedBuilding;
        private LandmarksLayerModule _module;
        private Camera _camera;
        private void Start()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                Debug.LogWarning($"{nameof(LandmarkHighlightBehaviour)} found no camera tagged MainCamera, so landmark selection is disabled.", this);
                enabled = false;
                return;
            }

            var map = FindObjectOfType<MapBehaviourCore>();
            if (map == null)
            {
                Debug.LogWarning($"{nameof(LandmarkHighlightBehaviour)} found no {nameof(MapBehaviourCore)} in the scene, so landmark selection is disabled.", this);
                enabled = false;
                return;
            }

            map.Initialized += mapboxMap =>
            {
                _module = (LandmarksLayerModule)GetComponent<LandmarksLayerModuleScript>().ModuleImplementation;
                _module.BuildingLoaded += o =>
                {
                    o.AddComponent<LandmarkComponent>();
                    AddCollidersToVisuals(o);
                };
            };
        }

        private void Update()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                var ray = _camera.ScreenPointToRay(UnityEngine.Input.mousePosition);
                //(ModuleImplementation as LandmarksLayerModule).SelectionRaycast(ray);
                if (Physics.Raycast(ray, out var hit))
                {
                    var buildingComponent = hit.transform.gameObject.GetComponent<LandmarkComponent>();
                    if (buildingComponent != null)
                    {
                        if (_visualSelectedEvent != null)
                        {
                            BuildingSelected(hit.transform.gameObject);
                            _visualSelectedEvent.Invoke(hit.transform.gameObject);
                        }
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

        private void AddCollidersToVisuals(GameObject go)
        {
            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.mesh == null) return;

            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = meshFilter.mesh;
        }

        private void ClearSelectedBuilding()
        {
            Tint(_currentlySelectedBuilding, DefaultColor);
            // Cleared as well as recoloured. Leaving the reference behind meant the next selection
            // re-tinted a building that was already back to its default, and a destroyed one stayed
            // referenced.
            _currentlySelectedBuilding = null;
        }

        private void BuildingSelected(GameObject building)
        {
            Tint(_currentlySelectedBuilding, DefaultColor);
            _currentlySelectedBuilding = building;
            Tint(_currentlySelectedBuilding, HightlightColor);
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
        
        public UnityEvent<GameObject> _visualSelectedEvent;
    }
}
