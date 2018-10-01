namespace Mapbox.Unity.Map
{
	using UnityEngine;

	public interface ISubLayerCustomStyleTiled : ISubLayerCustomStyleOptions, ISubLayerStyle
	{
		void SetMaterials(Material TopMaterial, Material SideMaterial);
		void SetAsStyle(Material TopMaterial, Material SideMaterial = null);
	}

}


