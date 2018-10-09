namespace Mapbox.Unity.Map
{
	using UnityEngine;

	public class SubLayerCustomStyleTiled : ISubLayerCustomStyleTiled
	{
		private GeometryMaterialOptions _materialOptions;
		public SubLayerCustomStyleTiled(GeometryMaterialOptions materialOptions)
		{
			_materialOptions = materialOptions;
		}
		public Material TopMaterial
		{
			get
			{
				return _materialOptions.materials[0].Materials[0];
			}
			set
			{
				if (_materialOptions.materials[0].Materials[0] != value)
				{
					_materialOptions.materials[0].Materials[0] = value;
					_materialOptions.HasChanged = true;
				}
			}
		}
		public Material SideMaterial
		{
			get
			{
				return _materialOptions.materials[1].Materials[0];
			}
			set
			{
				if (_materialOptions.materials[1].Materials[0] != value)
				{
					_materialOptions.materials[1].Materials[0] = value;
					_materialOptions.HasChanged = true;
				}
			}
		}

		public void SetAsStyle(Material topMaterial, Material sideMaterial = null)
		{
			_materialOptions.texturingType = UvMapType.Tiled;
			_materialOptions.materials[0].Materials[0] = topMaterial;
			_materialOptions.materials[1].Materials[0] = sideMaterial;
			_materialOptions.HasChanged = true;
		}

		public void SetAsStyle()
		{
			SetAsStyle(null, null);
		}

		public void SetMaterials(Material topMaterial, Material sideMaterial)
		{
			SetAsStyle(topMaterial, sideMaterial);
		}
	}

}


