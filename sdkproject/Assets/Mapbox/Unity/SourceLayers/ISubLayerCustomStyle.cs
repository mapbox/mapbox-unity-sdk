namespace Mapbox.Unity.Map
{
	public interface ISubLayerCustomStyle
	{
		UvMapType TexturingType { get; set; }
		ISubLayerCustomStyleTiled Tiled { get; }
		ISubLayerCustomStyleAtlas TextureAtlas { get; }
		ISubLayerCustomStyleAtlasWithColorPallete TextureAtlasWithColorPallete { get; }
	}

}


