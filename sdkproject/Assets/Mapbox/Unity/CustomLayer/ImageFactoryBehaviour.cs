using System;
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
				Map.OnTileRegisteredToFactories += (t) =>
				{
					if (enabled)
					{
						ImageFactoryManager.RegisterTile(t);
					}
				};
				Map.OnTileDisposing += t =>
				{
					if (enabled)
					{
						ImageFactoryManager.UnregisterTile(t);
					}
				};
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

		private void OnEnable()
		{
			if (Map != null)
			{
				foreach (var tilePair in Map.MapVisualizer.ActiveTiles)
				{
					ImageFactoryManager.RegisterTile(tilePair.Value);
				}
			}
		}

		private void OnDisable()
		{
			if (Map != null)
			{
				foreach (var tilePair in Map.MapVisualizer.ActiveTiles)
				{
					var material = tilePair.Value.MeshRenderer.sharedMaterial;
					material.SetTexture(TextureFieldName, null);
					material.SetVector(TextureScaleOffsetFieldName, new Vector4(1, 1, 0, 0));
				}
				foreach (var tile in Map.MapVisualizer.GetInactiveTiles)
				{
					var material = tile.MeshRenderer.sharedMaterial;
					material.SetTexture(TextureFieldName, null);
					material.SetVector(TextureScaleOffsetFieldName, new Vector4(1, 1, 0, 0));
				}

			}
		}
	}
}
