using UnityEngine;

namespace UnityARInterface
{
    public class ARBase : MonoBehaviour
    {
        protected Transform GetRoot()
        {
            var camera = GetCamera();
            if (camera != null)
                return camera.transform.parent;

            return null;
        }

        protected float GetScale()
        {
            var root = GetRoot();
            if (root != null)
                return root.transform.localScale.x;

            return 1f;
        }

        // Returns the first enabled ARController
        protected ARController GetFirstEnabledControllerInChildren()
        {
            foreach (var controller in GetComponentsInChildren<ARController>())
            {
                if (controller.enabled)
                {
                    return controller;
                }
            }

            return null;
        }

        protected Camera GetCamera()
        {
            // Use the same camera as the ARController
            var arController = GetFirstEnabledControllerInChildren();
            if (arController != null)
                return arController.arCamera;

            // If we're on a camera then use that.
            var camera = GetComponent<Camera>();
            if (camera != null)
                return camera;

            // Fallback to main camera
            return Camera.main;
        }
    }
}
