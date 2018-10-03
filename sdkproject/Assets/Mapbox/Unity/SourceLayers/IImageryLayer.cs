namespace Mapbox.Unity.Map
{
	public interface IImageryLayer : ILayer
	{

		ImagerySourceType LayerSource { get; }

		/// <summary>
		/// Sets the data source for the image factory.
		/// </summary>
		/// <param name="imageSource"></param>
		void SetLayerSource(ImagerySourceType imageSource);

		/// <summary>
		/// Enables high quality images for selected image factory source.
		/// </summary>
		/// <param name="useRetina"></param>
		void UseRetina(bool useRetina);

		/// <summary>
		/// Enable Texture2D compression for image factory outputs.
		/// </summary>
		/// <param name="useCompression"></param>
		void UseCompression(bool useCompression);

		/// <summary>
		/// Enable Texture2D MipMap option for image factory outputs.
		/// </summary>
		/// <param name="useMipMap"></param>
		void UseMipMap(bool useMipMap);

		/// <summary>
		/// Change image layer settings.
		/// </summary>
		/// <param name="imageSource">Data source for the image provider.</param>
		/// <param name="useRetina">Enable/Disable high quality imagery.</param>
		/// <param name="useCompression">Enable/Disable Unity3d Texture2d image compression.</param>
		/// <param name="useMipMap">Enable/Disable Unity3d Texture2d image mipmapping.</param>
		void SetProperties(ImagerySourceType imageSource, bool useRetina, bool useCompression, bool useMipMap);
	}

}


