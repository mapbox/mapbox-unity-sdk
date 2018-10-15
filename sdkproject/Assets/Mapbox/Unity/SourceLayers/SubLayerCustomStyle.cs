namespace Mapbox.Unity.Map
{
	public class SubLayerCustomStyle : ISubLayerCustomStyle
	{
		private GeometryMaterialOptions _materialOptions;
		public SubLayerCustomStyle(GeometryMaterialOptions materialOptions)
		{
			_materialOptions = materialOptions;
		}

		public UvMapType TexturingType
		{
			get
			{
				return _materialOptions.texturingType;
			}

			set
			{
				if (_materialOptions.texturingType != value)
				{
					_materialOptions.texturingType = value;
					_materialOptions.HasChanged = true;
				}
			}
		}
		private SubLayerCustomStyleTiled _tiled;
		public ISubLayerCustomStyleTiled Tiled
		{
			get
			{
				if (_tiled == null)
				{
					_tiled = new SubLayerCustomStyleTiled(_materialOptions);
				}
				return _tiled;
			}
		}

		private SubLayerCustomStyleAtlas _textureAtlas;
		public ISubLayerCustomStyleAtlas TextureAtlas
		{
			get
			{
				if (_textureAtlas == null)
				{
					_textureAtlas = new SubLayerCustomStyleAtlas(_materialOptions);
				}
				return _textureAtlas;
			}
		}

		private SubLayerCustomStyleAtlasWithColorPallete _textureAtlasPallete;
		public ISubLayerCustomStyleAtlasWithColorPallete TextureAtlasWithColorPallete
		{
			get
			{
				if (_textureAtlasPallete == null)
				{
					_textureAtlasPallete = new SubLayerCustomStyleAtlasWithColorPallete(_materialOptions);
				}
				return _textureAtlasPallete;
			}
		}
	}

}


