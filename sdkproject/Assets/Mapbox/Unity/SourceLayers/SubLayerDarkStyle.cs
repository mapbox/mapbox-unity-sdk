namespace Mapbox.Unity.Map
{
	public class SubLayerDarkStyle : ISubLayerDarkStyle
	{
		private GeometryMaterialOptions _materialOptions;
		public SubLayerDarkStyle(GeometryMaterialOptions materialOptions)
		{
			_materialOptions = materialOptions;
		}

		public float Opacity
		{
			get
			{
				return _materialOptions.darkStyleOpacity;
			}

			set
			{
				_materialOptions.darkStyleOpacity = value;
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
			_materialOptions.darkStyleOpacity = opacity;
			_materialOptions.HasChanged = true;
		}
	}

}


