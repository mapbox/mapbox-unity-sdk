namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;

	public class SubLayerCustomStyleAtlas : ISubLayerCustomStyleAtlas
	{
		private GeometryMaterialOptions _materialOptions;
		public SubLayerCustomStyleAtlas(GeometryMaterialOptions materialOptions)
		{
			_materialOptions = materialOptions;
		}
		public Material TopMaterial
		{
			get
			{
				return _materialOptions.customStyleOptions.materials[0].Materials[0];
			}
			set
			{
				if (_materialOptions.customStyleOptions.materials[0].Materials[0] != value)
				{
					_materialOptions.customStyleOptions.materials[0].Materials[0] = value;
					_materialOptions.HasChanged = true;
				}
			}
		}
		public Material SideMaterial
		{
			get
			{
				return _materialOptions.customStyleOptions.materials[1].Materials[0];
			}
			set
			{
				if (_materialOptions.customStyleOptions.materials[1].Materials[0] != value)
				{
					_materialOptions.customStyleOptions.materials[1].Materials[0] = value;
					_materialOptions.HasChanged = true;
				}
			}
		}

		public AtlasInfo UvAtlas
		{
			get
			{
				return _materialOptions.customStyleOptions.atlasInfo;
			}

			set
			{
				if (_materialOptions.customStyleOptions.atlasInfo != value)
				{
					_materialOptions.customStyleOptions.atlasInfo = value;
					_materialOptions.HasChanged = true;
				}
			}
		}
		public void SetAsStyle(Material topMaterial, Material sideMaterial, AtlasInfo uvAtlas)
		{
			_materialOptions.customStyleOptions.texturingType = UvMapType.Atlas;
			_materialOptions.customStyleOptions.materials[0].Materials[0] = topMaterial;
			_materialOptions.customStyleOptions.materials[1].Materials[0] = sideMaterial;
			_materialOptions.customStyleOptions.atlasInfo = uvAtlas;
			_materialOptions.HasChanged = true;
		}

		public void SetAsStyle()
		{
			_materialOptions.customStyleOptions.SetDefaultAssets();
			_materialOptions.HasChanged = true;
		}
	}

}


