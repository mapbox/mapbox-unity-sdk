namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;

	public class SubLayerCustomStyleAtlasWithColorPallete : ISubLayerCustomStyleAtlasWithColorPallete
	{
		private GeometryMaterialOptions _materialOptions;
		public SubLayerCustomStyleAtlasWithColorPallete(GeometryMaterialOptions materialOptions)
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

		public AtlasInfo UvAtlas
		{
			get
			{
				return _materialOptions.atlasInfo;
			}

			set
			{
				if (_materialOptions.atlasInfo != value)
				{
					_materialOptions.atlasInfo = value;
					_materialOptions.HasChanged = true;
				}
			}
		}

		public ScriptablePalette ColorPalette
		{
			get
			{
				return _materialOptions.colorPalette;
			}

			set
			{
				if (_materialOptions.colorPalette != value)
				{
					_materialOptions.colorPalette = value;
					_materialOptions.HasChanged = true;
				}
			}
		}

		public void SetAsStyle(Material topMaterial, Material sideMaterial, AtlasInfo uvAtlas, ScriptablePalette palette)
		{
			_materialOptions.texturingType = UvMapType.Atlas;
			_materialOptions.materials[0].Materials[0] = topMaterial;
			_materialOptions.materials[1].Materials[0] = sideMaterial;
			_materialOptions.atlasInfo = uvAtlas;
			_materialOptions.colorPalette = palette;
			_materialOptions.HasChanged = true;
		}

		public void SetAsStyle()
		{
			_materialOptions.SetDefaultAssets(UvMapType.AtlasWithColorPalette);
			_materialOptions.HasChanged = true;
		}
	}

}


