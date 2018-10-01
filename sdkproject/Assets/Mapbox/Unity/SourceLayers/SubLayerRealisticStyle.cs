namespace Mapbox.Unity.Map
{
	public class SubLayerRealisticStyle : ISubLayerRealisticStyle
	{
		private GeometryMaterialOptions _materialOptions;
		public SubLayerRealisticStyle(GeometryMaterialOptions materialOptions)
		{
			_materialOptions = materialOptions;
		}
		public void SetAsStyle()
		{
			_materialOptions.style = StyleTypes.Realistic;
			_materialOptions.HasChanged = true;
		}
	}

}


