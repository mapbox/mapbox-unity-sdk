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

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
	/// <summary>
	/// This represents all the runtime control over meshing component in order to best visualize the
	/// affect changing parameters has over the meshing API.
	/// </summary>
	public class OcclusionMesh : MonoBehaviour
	{
		#region Private Variables
		[SerializeField, Tooltip("The spatial mapper from which to update mesh params.")]
		private MLSpatialMapper _mlSpatialMapper;

		[SerializeField, Tooltip("Visualizer for the meshing results.")]
		private MeshingVisualizer _meshingVisualizer;

		private MeshingVisualizer.RenderMode _renderMode = MeshingVisualizer.RenderMode.Occlusion;

		private static readonly Vector3 _boundlessExtentsSize = new Vector3(10.0f, 10.0f, 10.0f);

		private Camera _camera;
		#endregion

		#region Unity Methods
		/// <summary>
		/// Initializes component data and starts MLInput.
		/// </summary>
		void Awake()
		{
			if (_mlSpatialMapper == null)
			{
				Debug.LogError("Error MeshingExample._mlSpatialMapper is not set, disabling script.");
				enabled = false;
				return;
			}
			if (_meshingVisualizer == null)
			{
				Debug.LogError("Error MeshingExample._meshingVisualizer is not set, disabling script.");
				enabled = false;
				return;
			}

			_camera = Camera.main;

		}

		/// <summary>
		/// Set correct render mode for meshing and update meshing settings.
		/// </summary>
		void Start()
		{
			_meshingVisualizer.SetRenderers(_renderMode);

			_mlSpatialMapper.gameObject.transform.position = _camera.gameObject.transform.position;
			_mlSpatialMapper.gameObject.transform.localScale = _boundlessExtentsSize;

		}

		/// <summary>
		/// Update mesh polling center position to camera.
		/// </summary>
		void Update()
		{
			_mlSpatialMapper.gameObject.transform.position = _camera.gameObject.transform.position;
		}


		#endregion


	}
}

#endif
