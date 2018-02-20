using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityARInterface
{
    public class MatchingScalePicker : ARBase
    {
        [SerializeField]
        private Transform m_LevelGeometry;

        private Bounds GetRenderBounds(GameObject go)
        {
            var totalBounds = new Bounds();
            totalBounds.SetMinMax(Vector3.one * Mathf.Infinity, -Vector3.one * Mathf.Infinity);
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var bounds = renderer.bounds;
                var totalMin = totalBounds.min;
                totalMin.x = Mathf.Min(totalMin.x, bounds.min.x);
                totalMin.y = Mathf.Min(totalMin.y, bounds.min.y);
                totalMin.z = Mathf.Min(totalMin.z, bounds.min.z);

                var totalMax = totalBounds.max;
                totalMax.x = Mathf.Max(totalMax.x, bounds.max.x);
                totalMax.y = Mathf.Max(totalMax.y, bounds.max.y);
                totalMax.z = Mathf.Max(totalMax.z, bounds.max.z);

                totalBounds.SetMinMax(totalMin, totalMax);
            }

            return totalBounds;
        }

        private void Update()
        {
            if (Input.GetMouseButton(0))
            {
                var camera = GetCamera();

                Ray ray = camera.ScreenPointToRay(Input.mousePosition);

                var planeLayer = GetComponent<ARPlaneVisualizer>().planeLayer;
                int layerMask = 1 << planeLayer;

                RaycastHit rayHit;
                if (Physics.Raycast(ray, out rayHit, float.MaxValue, layerMask))
                {
                    var arController = GetFirstEnabledControllerInChildren();
                    var bounds = GetRenderBounds(m_LevelGeometry.gameObject);
                    var maxLevelDimension = Mathf.Max(bounds.size.x, bounds.size.z);
                    var planeTransform = rayHit.collider.transform.parent;
                    var planeScale = planeTransform.localScale;
                    var planeExtents = new Vector2(planeScale.x, planeScale.z);
                    var minPlaneDimension = Mathf.Min(planeExtents.x, planeExtents.y);

                    arController.pointOfInterest = bounds.center;
                    arController.scale = maxLevelDimension / minPlaneDimension;
                    arController.rotation = planeTransform.localRotation;
                    arController.AlignWithPointOfInterest(planeTransform.position);
                }
            }
        }
    }
}
