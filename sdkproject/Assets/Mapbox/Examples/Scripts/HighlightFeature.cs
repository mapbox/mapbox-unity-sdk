namespace Mapbox.Examples
{
	using UnityEngine;
	using System.Collections.Generic;

	public class HighlightFeature : MonoBehaviour
	{
		static Material _highlightMaterial;

		private List<Material> _materials = new List<Material>();

		MeshRenderer _meshRenderer;

		void Start()
		{
			if (_highlightMaterial == null)
			{
				_highlightMaterial = new Material(Shader.Find("Unlit/Color"));
				_highlightMaterial.color = Color.red;
			}

			_meshRenderer = GetComponent<MeshRenderer>();
			foreach (var item in _meshRenderer.materials)
			{
				_materials.Add(item);
			}
		}

		public void OnMouseEnter()
		{
			_meshRenderer.sharedMaterial = _highlightMaterial;
		}

		public void OnMouseExit()
		{
			_meshRenderer.materials = _materials.ToArray();
		}
	}
}