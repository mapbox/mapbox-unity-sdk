namespace Mapbox.Unity.Map
{
	public class SubLayerSimpleStyle : ISubLayerSimpleStyle
	{
		private GeometryMaterialOptions _materialOptions;
		public SubLayerSimpleStyle(GeometryMaterialOptions materialOptions)
		{
			_materialOptions = materialOptions;
		}

		public SamplePalettes PaletteType
		{
			get
			{
				return _materialOptions.samplePalettes;
			}

			set
			{
				if (_materialOptions.samplePalettes != value)
				{
					_materialOptions.samplePalettes = value;
					_materialOptions.HasChanged = true;
				}
			}
		}

		public void SetAsStyle()
		{
			SetAsStyle(SamplePalettes.City);
		}

		public void SetAsStyle(SamplePalettes palette)
		{
			_materialOptions.style = StyleTypes.Fantasy;
			_materialOptions.samplePalettes = palette;
			_materialOptions.HasChanged = true;
		}
	}

}


