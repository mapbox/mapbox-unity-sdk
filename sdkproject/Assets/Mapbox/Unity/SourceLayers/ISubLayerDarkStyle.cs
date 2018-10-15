namespace Mapbox.Unity.Map
{
	public interface ISubLayerDarkStyle : ISubLayerStyle
	{
		float Opacity { get; set; }
		void SetAsStyle(float opacity);
	}

}


