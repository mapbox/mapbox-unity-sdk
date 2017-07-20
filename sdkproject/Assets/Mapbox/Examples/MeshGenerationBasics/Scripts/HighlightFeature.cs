namespace Mapbox.Examples
{
	using UnityEngine;
	using System.Collections.Generic;

	public class HighlightFeature : MonoBehaviour
	{
		private List<Color> _original = new List<Color>();
		private Color _highlight = Color.red;
		private List<Material> _materials = new List<Material>();

		void Start()
		{
			foreach (var item in GetComponent<MeshRenderer>().materials)
			{
				_materials.Add(item);
				_original.Add(item.color);
			}
		}

		public void OnMouseEnter()
		{
			foreach (var item in _materials)
			{
				item.color = _highlight;
			}
		}

		public void OnMouseExit()
		{
			for (int i = 0; i < _materials.Count; i++)
			{
				_materials[i].color = _original[i];
			}
		}
	}
}