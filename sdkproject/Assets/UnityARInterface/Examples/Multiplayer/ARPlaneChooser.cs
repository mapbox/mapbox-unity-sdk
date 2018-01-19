using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityARInterface;

namespace Mongoose
{
    public class ARPlaneChooser : ARBase
    {
        [SerializeField]
        private GameObject m_PlanePrefab;

		[SerializeField]
		private string  lobbySceneStr;

        [SerializeField]
        private float m_LevelGeometrySize = 110;

        [SerializeField]
        private Vector3 m_LevelPlacementPosition = new Vector3(0f, 0f, 0f);

        private int m_PlaneLayer = 8;
		private bool doneChoosingPlane = false;

        private Dictionary<string, GameObject> m_Planes = new Dictionary<string, GameObject>();

        void OnEnable()
        {
            ARInterface.planeAdded += OnPlaneAdded;
            ARInterface.planeUpdated += OnPlaneUpdated;
			ARInterface.planeRemoved += OnPlaneRemoved;
			doneChoosingPlane = false;
        }

        void OnDisable()
        {
            ARInterface.planeAdded -= OnPlaneAdded;
            ARInterface.planeUpdated -= OnPlaneUpdated;
            ARInterface.planeRemoved -= OnPlaneRemoved;
        }

		void OnGUI()
		{
			if (!doneChoosingPlane) {
				GUI.Box (new Rect (Screen.width / 4, 10, Screen.width / 2, 100), "Select a flat surface to play on - have other player choose same surface from same side");
			}
		}

        protected virtual void CreateOrUpdateGameObject(BoundedPlane plane)
        {
            GameObject go;
            if (!m_Planes.TryGetValue(plane.id, out go))
            {
                var parent = Camera.main.transform.parent;
                go = Instantiate(m_PlanePrefab, parent);

                // Make sure we can pick them later
                foreach (var collider in go.GetComponentsInChildren<Collider>())
                    collider.gameObject.layer = m_PlaneLayer;

                m_Planes.Add(plane.id, go);
            }

            go.transform.localPosition = plane.center;
            go.transform.localRotation = plane.rotation;
            go.transform.localScale = new Vector3(plane.extents.x, 1f, plane.extents.y);
        }

        protected virtual void OnPlaneAdded(BoundedPlane plane)
        {
			if (m_PlanePrefab)
				CreateOrUpdateGameObject (plane);
        }

        protected virtual void OnPlaneUpdated(BoundedPlane plane)
        {
			if (m_PlanePrefab)
				CreateOrUpdateGameObject (plane);
        }

        protected virtual void OnPlaneRemoved(BoundedPlane plane)
        {
            GameObject go;
            if (m_Planes.TryGetValue(plane.id, out go))
            {
                Destroy(go);
                m_Planes.Remove(plane.id);
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
                StartCoroutine(MouseHandler());

			if (doneChoosingPlane) {
				//before loading lobby scene, get rid of the planes you created
				foreach (GameObject go in m_Planes.Values) {
                    Destroy(go);
                }
                m_Planes.Clear();

                LoadLobbyScene ();
			}
        }

		void LoadLobbyScene()
		{
			SceneManager.LoadScene (lobbySceneStr);
			GetComponent<ARPlaneChooser> ().enabled = false;
		}

        private IEnumerator MouseHandler()
        {
            var camera = GetCamera();
            while (!Input.GetMouseButtonUp(0))
            {
                Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                int layerMask = 1 << m_PlaneLayer;

                RaycastHit rayHit;
                if (Physics.Raycast(ray, out rayHit, float.MaxValue, layerMask))
                {
					var arController = GetFirstEnabledControllerInChildren();
					var planeTransform = rayHit.collider.transform.parent;
                    var planeScale = planeTransform.localScale;
                    var planeExtents = new Vector2(planeScale.x, planeScale.z);
                    var minPlaneDimension = Mathf.Min(planeExtents.x, planeExtents.y);

                    // Shrink the apparent plane slightly
                    minPlaneDimension *= .95f;

                    // Make the level placement align with the center point of the plane,
					arController.pointOfInterest = m_LevelPlacementPosition;
					arController.AlignWithPointOfInterest(planeTransform.position);

                    // Orient the geometry in accordance with the plane
					arController.rotation = planeTransform.localRotation;

                    // Compute the new scale so that the level geoemtry fits into the plane
                    arController.scale = m_LevelGeometrySize / minPlaneDimension;

                    doneChoosingPlane = true;
                    break;
                }

                yield return null;
            }
        }
    }
}
