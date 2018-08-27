// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
//
// Copyright (c) 2018 Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Creator Agreement, located
// here: https://id.magicleap.com/creator-terms
//
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// Manages plane rendering based on plane detection from Planes component.
    /// </summary>
    [RequireComponent(typeof(Planes))]
    public class PlaneVisualizer : MonoBehaviour
    {
        #region Public Variables
        [Tooltip("Object prefab to use for plane visualization.")]
        public GameObject PlaneVisualPrefab;

        [Header("Materials")]
        [Tooltip("Material used for wall planes.")]
        public Material WallMaterial;
        [Tooltip("Material used for floor planes.")]
        public Material FloorMaterial;
        [Tooltip("Material used for ceiling planes.")]
        public Material CeilingMaterial;
        [Tooltip("Material used for other types of planes.")]
        public Material DefaultMaterial;
        [Tooltip("Material used to show the planes")]
        public Material BorderMaterial;
        #endregion

        #region Private Members
        [Space, SerializeField, Tooltip("Text to display render mode.")]
        private Text _renderModeText;

        // List of all the planes being rendered
        private List<GameObject> _planeCache;
        private List<uint> _planeFlags;

        private bool _showBorder = true;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Initializes all variables and makes sure needed components exist
        /// </summary>
        void Awake()
        {
            if (PlaneVisualPrefab == null)
            {
                Debug.LogError("Error PlanesVisualizer.PlaneVisualPrefab is not set, disabling script.");
                enabled = false;
                return;
            }

            if (WallMaterial == null || FloorMaterial == null || CeilingMaterial == null || DefaultMaterial == null || BorderMaterial == null)
            {
                Debug.LogError("Error PlanesVisualizer.Materials is not set, disabling script.");
                enabled = false;
                return;
            }

            MeshRenderer planeRenderer = PlaneVisualPrefab.GetComponent<MeshRenderer>();
            if (planeRenderer == null)
            {
                Debug.LogError("Error PlanesVisualizer MeshRenderer component not found, disabling script.");
                enabled = false;
                return;
            }

            if (_renderModeText == null)
            {
                Debug.LogError("Error PlanesVisualizer._renderModeText is not set, disabling script.");
                enabled = false;
                return;
            }

            _planeCache = new List<GameObject>();
            _planeFlags = new List<uint>();

            UpdateStatusText();
        }

        /// <summary>
        /// Destroys all planes instances created
        /// </summary>
        void OnDestroy()
        {
            _planeCache.ForEach((GameObject go) => GameObject.Destroy(go));
            _planeCache.Clear();
            _planeFlags.Clear();
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// Updates planes and creates new planes based on detected planes.
        ///
        /// This function reuses previously allocated memory to convert all planes
        /// to the new ones by changing their transforms, it allocates new objects
        /// if the current result ammount is bigger than the ones already stored.
        /// </summary>
        /// <param name="p">The planes component</param>
        public void OnPlanesUpdate(MLWorldPlane[] planes)
        {
            int index = planes.Length > 0 ? planes.Length - 1 : 0;
            for (int i = index; i < _planeCache.Count; ++i)
            {
                _planeCache[i].SetActive(false);
            }

            for (int i = 0; i < planes.Length; ++i)
            {
                GameObject planeVisual;
                if (i < _planeCache.Count)
                {
                    planeVisual = _planeCache[i];
                    planeVisual.SetActive(true);
                }
                else
                {
                    planeVisual = Instantiate(PlaneVisualPrefab);
                    _planeCache.Add(planeVisual);
                    _planeFlags.Add(0);
                }

                planeVisual.transform.position = planes[i].Center;
                planeVisual.transform.rotation = planes[i].Rotation;
                planeVisual.transform.localScale = new Vector3(planes[i].Width, planes[i].Height, 1f);

                _planeFlags[i] = planes[i].Flags;
            }

            RefreshAllPlaneMaterials();
        }

        /// <summary>
        /// Toggle showing of borders and refresh all plane materials
        /// </summary>
        public void ToggleShowingPlanes()
        {
            _showBorder = !_showBorder;
            UpdateStatusText();
            RefreshAllPlaneMaterials();
        }
        #endregion

        #region Private Functions
        /// <summary>
        /// Refresh all the plane materials
        /// </summary>
        private void RefreshAllPlaneMaterials()
        {
            for (int i = 0; i < _planeCache.Count; ++i)
            {
                if (!_planeCache[i].activeSelf)
                {
                    continue;
                }

                Renderer planeRenderer = _planeCache[i].GetComponent<Renderer>();
                SetRenderTexture(planeRenderer, _planeFlags[i]);
            }
        }

        /// <summary>
        /// Sets correct texture to plane based on surface type
        /// </summary>
        /// <param name="renderer">The renderer component</param>
        /// <param name="flags">The flags of the plane containing the surface type</param>
        private void SetRenderTexture(Renderer renderer, uint flags)
        {
            if (_showBorder)
            {
                renderer.sharedMaterial = BorderMaterial;
            }
            else if ((flags & (uint)SemanticFlags.Wall) != 0)
            {
                renderer.sharedMaterial = WallMaterial;
            }
            else if ((flags & (uint)SemanticFlags.Floor) != 0)
            {
                renderer.sharedMaterial = FloorMaterial;
            }
            else if ((flags & (uint)SemanticFlags.Ceiling) != 0)
            {
                renderer.sharedMaterial = CeilingMaterial;
            }
            else
            {
                renderer.sharedMaterial = DefaultMaterial;
            }
        }

        /// <summary>
        /// Update render mode text to match current render mode.
        /// </summary>
        private void UpdateStatusText()
        {
            _renderModeText.text = string.Format("Render Mode: {0}", (_showBorder ? "Border" : "Texture"));
        }
        #endregion
    }
}
