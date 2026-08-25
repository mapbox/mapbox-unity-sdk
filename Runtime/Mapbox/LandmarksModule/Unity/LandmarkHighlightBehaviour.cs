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
        public Color DefaultColor = Color.white;
        public Color HightlightColor = Color.red;

        private readonly int _shaderColorPropertyName = Shader.PropertyToID("baseColorFactor");
        private GameObject _currentlySelectedBuilding;
        private LandmarksLayerModule _module;
        private Camera _camera;
        private void Start()
        {
            _camera = Camera.main;
            var map = FindObjectOfType<MapBehaviourCore>();
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
            var mesh = go.GetComponent<MeshFilter>().mesh;			
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
        }

        private void ClearSelectedBuilding()
        {
            if (_currentlySelectedBuilding != null)
            {
                _currentlySelectedBuilding.GetComponent<MeshRenderer>().material.SetColor(_shaderColorPropertyName, DefaultColor);
            }
        }
        
        private void BuildingSelected(GameObject building)
        {
            if (_currentlySelectedBuilding != null)
            {
                _currentlySelectedBuilding.GetComponent<MeshRenderer>().material.SetColor(_shaderColorPropertyName, DefaultColor);
            }

            _currentlySelectedBuilding = building;
            _currentlySelectedBuilding.GetComponent<MeshRenderer>().material.SetColor(_shaderColorPropertyName, HightlightColor);
        }
        
        public UnityEvent<GameObject> _visualSelectedEvent;
    }
}
