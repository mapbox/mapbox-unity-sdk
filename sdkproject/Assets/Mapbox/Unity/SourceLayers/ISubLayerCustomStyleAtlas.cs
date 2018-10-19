namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;

	public interface ISubLayerCustomStyleAtlas : ISubLayerCustomStyleOptions, ISubLayerStyle
	{
		AtlasInfo UvAtlas { get; set; }
		void SetAsStyle(Material TopMaterial, Material SideMaterial, AtlasInfo uvAtlas);
	}

}


