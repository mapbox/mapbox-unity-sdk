namespace Mapbox.Unity.Map
{
	public interface ISubLayerSimpleStyle : ISubLayerStyle
	{
		SamplePalettes PaletteType { get; set; }
		void SetAsStyle(SamplePalettes palette);
	}

}


