namespace Mapbox.Unity.Map
{
	public interface ISubLayerLightStyle : ISubLayerStyle
	{
		float Opacity { get; set; }
		void SetAsStyle(float opacity);
	}

}


