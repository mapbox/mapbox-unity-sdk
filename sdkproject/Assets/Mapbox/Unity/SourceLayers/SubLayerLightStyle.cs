namespace Mapbox.Unity.Map
{
	public class SubLayerLightStyle : ISubLayerLightStyle
	{
		private GeometryMaterialOptions _materialOptions;
		public SubLayerLightStyle(GeometryMaterialOptions materialOptions)
		{
			_materialOptions = materialOptions;
		}

		public float Opacity
		{
			get
			{
				return _materialOptions.lightStyleOpacity;
			}

			set
			{
				_materialOptions.lightStyleOpacity = value;
				_materialOptions.HasChanged = true;
			}
		}

		public void SetAsStyle()
		{
			SetAsStyle(1.0f);
		}

		public void SetAsStyle(float opacity)
		{
			_materialOptions.style = StyleTypes.Light;
			_materialOptions.lightStyleOpacity = opacity;
			_materialOptions.HasChanged = true;
		}
	}

}


