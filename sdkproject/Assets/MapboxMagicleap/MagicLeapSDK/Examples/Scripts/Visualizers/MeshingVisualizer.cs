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

#if UNITY_EDITOR || PLATFORM_LUMIN

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// This class allows you to change meshing properties at runtime, including the rendering mode.
    /// Manages the MLSpatialMapper behaviour and tracks the meshes.
    /// </summary>
    public class MeshingVisualizer : MonoBehaviour
    {
        public enum RenderMode
        {
            None,
            Wireframe,
            Occlusion
        }

#region Private Variables
        [SerializeField, Tooltip("The MLSpatialMapper from which to get update on mesh types.")]
        private MLSpatialMapper _mlSpatialMapper;

        [SerializeField, Tooltip("The material to apply for occlusion.")]
        private Material _occlusionMaterial;

        [SerializeField, Tooltip("The material to apply for wireframe rendering.")]
        private Material _wireframeMaterial;

        private RenderMode _renderMode = RenderMode.Wireframe;
#endregion

#region Unity Methods
        /// <summary>
        /// Start listening for MLSpatialMapper events.
        /// </summary>
        void Awake()
        {
            // Validate all required game objects.
            if (_mlSpatialMapper == null)
            {
                Debug.LogError("MeshingVisualizer._mlSpatialMapper is not set, disabling script!");
                enabled = false;
                return;
            }
        }

        /// <summary>
        /// Register for new and updated freagments.
        /// </summary>
        void Start()
        {
            _mlSpatialMapper.meshAdded += HandleOnMeshReady;
            _mlSpatialMapper.meshUpdated += HandleOnMeshReady;
        }

        /// <summary>
        /// Unregister callbacks.
        /// </summary>
        void OnDestroy()
        {
            _mlSpatialMapper.meshAdded -= HandleOnMeshReady;
            _mlSpatialMapper.meshUpdated -= HandleOnMeshReady;
        }
#endregion

#region Public Methods
        /// <summary>
        /// Set the render material on the meshes.
        /// </summary>
        /// <param name="mode">The render mode that should be used on the material.</param>
        public void SetRenderers(RenderMode mode)
        {
            // Set the render mode.
            _renderMode = mode;

            // Update the material applied to all the MeshRenderers.
            foreach (GameObject fragment in _mlSpatialMapper.meshIdToGameObjectMap.Values)
            {
                UpdateRenderer(fragment.GetComponent<MeshRenderer>());
            }
        }
#endregion

#region Private Methods
        /// <summary>
        /// Updates the currently selected render material on the MeshRenderer.
        /// </summary>
        /// <param name="meshRenderer">The MeshRenderer that should be updated.</param>
        private void UpdateRenderer(MeshRenderer meshRenderer)
        {
            if (meshRenderer != null)
            {
                // Toggle the GameObject(s) and set the correct materia based on the current RenderMode.
                if (_renderMode == RenderMode.None)
                {
                    meshRenderer.enabled = false;
                }
                else if (_renderMode == RenderMode.Wireframe)
                {
                    meshRenderer.enabled = true;
                    meshRenderer.material = _wireframeMaterial;
                }
                else if (_renderMode == RenderMode.Occlusion)
                {
                    meshRenderer.enabled = true;
                    meshRenderer.material = _occlusionMaterial;
                }
            }
        }
#endregion

#region Event Handlers
        /// <summary>
        /// Handles the MeshReady event, which tracks and assigns the correct mesh renderer materials.
        /// </summary>
        /// <param name="meshId">Id of the mesh that got added / upated.</param>
        private void HandleOnMeshReady(TrackableId meshId)
        {
            if (_mlSpatialMapper.meshIdToGameObjectMap.ContainsKey(meshId))
            {
                UpdateRenderer(_mlSpatialMapper.meshIdToGameObjectMap[meshId].GetComponent<MeshRenderer>());
            }
        }
#endregion
    }
}

#endif
