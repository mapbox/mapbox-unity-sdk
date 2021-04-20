using Mapbox.Unity.DataContainers;
using Mapbox.Unity.Map;
using UnityEngine;

namespace Mapbox.Unity.CustomLayer
{
	[RequireComponent(typeof(AbstractMap))]
	public class ImageFactoryBehaviour : MonoBehaviour
	{
		public AbstractMap Map;
		public ImageFactoryManager ImageFactoryManager;
		
		public string CustomTilesetId = "AerisHeatMap";
		public string UrlFormat = "";
		public string TextureFieldName = "_CustomOne";
		public string TextureScaleOffsetFieldName = "_CustomOne_ST";

		[HideInInspector] public bool DownloadFallbackImagery = false;
		public bool Retina = false;
		public bool Compress = false;
		public bool UseMipmap = false;

		public void Awake()
		{
			if (enabled)
			{
				var imageSettings = new ImageryLayerProperties();
				imageSettings.rasterOptions = new ImageryRasterOptions()
				{
					useRetina = Retina,
					useCompression = Compress,
					useMipMap = UseMipmap
				};
				imageSettings.sourceOptions = new LayerSourceOptions()
				{
					layerSource = new Style()
					{
						Name = CustomTilesetId,
						Id = CustomTilesetId
					}
				};

				ImageFactoryManager = new CustomImageFactoryManager(UrlFormat, imageSettings, DownloadFallbackImagery, TextureFieldName, TextureScaleOffsetFieldName);
				ImageFactoryManager.FetchingError += (tile, rasterTile, args) => { Debug.Log(args.Exceptions[0]); };
				Map.OnTileRegisteredToFactories += ImageFactoryManager.RegisterTile;
				Map.OnTileDisposing += tile => { ImageFactoryManager.UnregisterTile(tile); };
			}
		}

		public void Start()
		{

		}

		private void OnValidate()
		{
			if (Map == null)
			{
				Map = GetComponent<AbstractMap>();
			}
		}
	}
}
