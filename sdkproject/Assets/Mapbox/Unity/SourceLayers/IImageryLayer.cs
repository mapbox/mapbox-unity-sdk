namespace Mapbox.Unity.Map
{
	public interface IImageryLayer : ILayer
	{
		/// <summary>
		/// Gets the `Data Source` for the `IMAGE` component.
		/// </summary>
		ImagerySourceType LayerSource { get; }

		/// <summary>
		/// Sets the `Data Source` for the `IMAGE` component. This can be one of the
		/// [Mapbox default styles](https://www.mapbox.com/api-documentation/#styles),
		/// or a custom style. The style url is set as the `Map ID`.
		/// </summary>
		/// <param name="imageSource">Source of imagery for map. Can be a Mapbox default, or custom style.</param>
		void SetLayerSource(ImagerySourceType imageSource);

		/// <summary>
		/// Enables or disables high quality images for the specified Data Source.
		/// resoluion when enabled is 1024px, and 512px when disabled. Satellite
		/// imagery is 512px when enabled, and 256 px when disabled. Changes to this
		/// may not take effect until the cache is cleared.
		/// </summary>
		/// <param name="useRetina">Boolean to toggle `Use Retina`.</param>
		void UseRetina(bool useRetina);

		/// <summary>
		/// Enables or disables Unity Texture2D compression for `IMAGE` outputs.
		/// Enable this if you need performance rather than a high resolution image.
		/// </summary>
		/// <param name="useCompression">Boolean to toggle `Use Compression`.</param>
		void UseCompression(bool useCompression);

		/// <summary>
		/// Enables or disables Unity Texture2D Mipmap for `IMAGE` outputs.
		/// Mipmaps are lists of progressively smaller versions of an image, used
		/// to optimize performance. Enabling mipmaps consumes more memory, but
		/// provides improved performance.
		/// </summary>
		/// <param name="useMipMap">Boolean to toggle `Use Mip Map`.</param>
		void UseMipMap(bool useMipMap);

		/// <summary>
		/// Changes the settings for the `IMAGE` component.
		/// </summary>
		/// <param name="imageSource">`Data Source` for the IMAGE component.</param>
		/// <param name="useRetina">Enables or disables high quality imagery.</param>
		/// <param name="useCompression">Enables or disables Unity Texture2D compression.</param>
		/// <param name="useMipMap">Enables or disables Unity Texture2D image mipmapping.</param>
		void SetProperties(ImagerySourceType imageSource, bool useRetina, bool useCompression, bool useMipMap);
	}

}
