namespace Mapbox.Unity.Map
{
	public class SubLayerFantasyStyle : ISubLayerFantasyStyle
	{
		private GeometryMaterialOptions _materialOptions;
		public SubLayerFantasyStyle(GeometryMaterialOptions materialOptions)
		{
			_materialOptions = materialOptions;
		}
		public void SetAsStyle()
		{
			_materialOptions.style = StyleTypes.Fantasy;
			_materialOptions.HasChanged = true;
		}
	}

}


