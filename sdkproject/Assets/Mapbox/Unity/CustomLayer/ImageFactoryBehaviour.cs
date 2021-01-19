using System;
using Mapbox.Unity.Map;
using UnityEngine;

namespace CustomImageLayerSample
{
	[RequireComponent(typeof(AbstractMap))]
	public class ImageFactoryBehaviour : MonoBehaviour
	{
		public AbstractMap Map;
		public ImageFactoryManager ImageFactoryManager;
		public bool DownloadFallbackImagery = false;
		public string CustomTilesetId = "AerisHeatMap";
		public string UrlFormat = "https://maps.aerisapi.com/anh3TB1Xu9Wr6cPndbPwF_EuOSGuqkH433UmnajaOP0MD9rpIh5dZ38g2SUwvu/flat,ftemperatures-max-text,admin/{0}/{1}/{2}/current.png";
		public string TextureFieldName = "_CustomOne";
		public string TextureScaleOffsetFieldName = "_CustomOne_ST";

		public void Awake()
		{
			if (enabled)
			{
				ImageFactoryManager = new CustomImageFactoryManager(UrlFormat, CustomTilesetId, DownloadFallbackImagery, TextureFieldName, TextureScaleOffsetFieldName);
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