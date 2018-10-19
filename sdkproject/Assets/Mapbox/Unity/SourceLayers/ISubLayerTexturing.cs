namespace Mapbox.Unity.Map
{
	public interface ISubLayerTexturing
	{
		ISubLayerDarkStyle DarkStyle { get; }
		ISubLayerLightStyle LightStyle { get; }
		ISubLayerColorStyle ColorStyle { get; }

		ISubLayerRealisticStyle RealisticStyle { get; }
		ISubLayerFantasyStyle FantasyStyle { get; }
		ISubLayerSimpleStyle SimpleStyle { get; }

		ISubLayerCustomStyle CustomStyle { get; }

		/// <summary>
		/// Sets the type of the style.
		/// </summary>
		/// <param name="styleType">Style type.</param>
		void SetStyleType(StyleTypes styleType);

		/// <summary>
		/// Gets the type of style used in the layer.
		/// </summary>
		/// <returns>The style type.</returns>
		StyleTypes GetStyleType();
	}

}


