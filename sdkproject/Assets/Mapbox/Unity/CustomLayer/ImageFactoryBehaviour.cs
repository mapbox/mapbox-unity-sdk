using System;
using Mapbox.Unity.DataContainers;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;

namespace Mapbox.Unity.CustomLayer
{
	public class ImageFactoryBehaviour : MonoBehaviour
	{
		public AbstractMap Map;
		public CustomImageFactoryManager ImageFactoryManager;

		public string CustomTilesetId = "AerisHeatMap";
		public string UrlFormat = "";
		public string TextureFieldName = "_CustomOne";
		public string TextureScaleOffsetFieldName = "_CustomOne_ST";
		public string OpacityFieldName = "_CustomOne_Lerp";
		public float Opacity = 1;

		[HideInInspector] public bool DownloadFallbackImagery = false;
		public bool Retina = false;
		public bool Compress = false;
		public bool UseMipmap = false;

		public void Awake()
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
			ImageFactoryManager.FetchingError += (tile, rasterTile, args) =>
			{
				//Debug.Log(args.Exceptions[0]);
			};
			Map.OnTileRegisteredToFactories += (t) =>
			{
				if (isActiveAndEnabled)
				{
					ImageFactoryManager.RegisterTile(t);
					SetOpacity(t);
				}
			};
			Map.OnTileDisposing += t =>
			{
				//if (enabled)
				{
					ImageFactoryManager.UnregisterTile(t);
				}
			};
			Map.OnTileStopping += unityTile =>
			{
				//if (enabled)
				{
					if (unityTile != null && unityTile.Tiles.Count > 0)
					{
						ImageFactoryManager.UnregisterTile(unityTile);
					}
				}
			};
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
			SetOpacity();
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
					ImageFactoryManager.UnregisterTile(tilePair.Value);
				}
				foreach (var tile in Map.MapVisualizer.GetInactiveTiles)
				{
					var material = tile.MeshRenderer.sharedMaterial;
					material.SetTexture(TextureFieldName, null);
					material.SetVector(TextureScaleOffsetFieldName, new Vector4(1, 1, 0, 0));
					ImageFactoryManager.UnregisterTile(tile);
				}
			}
		}

		public void SetCustomFieldNames(string customTextureFieldName, string customTextureScaleOffsetFieldName, string customOpacityFieldName)
		{
			this.enabled = false;
			if (Map != null)
			{
				TextureFieldName = customTextureFieldName;
				TextureScaleOffsetFieldName = customTextureScaleOffsetFieldName;
				OpacityFieldName = customOpacityFieldName;
				ImageFactoryManager.SetMaterialFieldNames(customTextureFieldName, customTextureScaleOffsetFieldName);
			}
		}

		public void SetOpacity()
		{
			SetOpacity(Opacity);
		}

		public void SetOpacity(float Opacity)
		{
			this.Opacity = Opacity;
			foreach (var tilePair in Map.MapVisualizer.ActiveTiles)
			{
				tilePair.Value.MeshRenderer.sharedMaterial.SetFloat(OpacityFieldName, Opacity);
			}
		}

		public void SetOpacity(UnityTile tile)
		{
			tile.MeshRenderer.sharedMaterial.SetFloat(OpacityFieldName, Opacity);
		}
	}
}
