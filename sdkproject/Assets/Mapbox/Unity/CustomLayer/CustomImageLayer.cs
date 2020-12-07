using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;

namespace CustomImageLayerSample
{
    public class CustomImageLayer : MonoBehaviour
    {
        [SerializeField] private string _customTilesetId = "AerisHeatMap";
        [SerializeField] private string UrlFormat = "https://maps.aerisapi.com/anh3TB1Xu9Wr6cPndbPwF_EuOSGuqkH433UmnajaOP0MD9rpIh5dZ38g2SUwvu/flat,ftemperatures-max-text,admin/{0}/{1}/{2}/current.png";
        private AbstractMap _map;
        private CustomImageDataFetcher _fetcher;

        public void Start()
        {
            _map = FindObjectOfType<AbstractMap>();
            _fetcher = new CustomImageDataFetcher(UrlFormat);

            _fetcher.TextureReceived += TextureReceived;
            _fetcher.FetchingError += (tile, rasterTile, TileErrorEventArgs) => { Debug.Log(TileErrorEventArgs.Exceptions); };
            _map.OnTileFinished += LoadTile;
        }

        private void TextureReceived(UnityTile tile, Texture2D texture)
        {
            if (tile != null)
            {
                tile.SetRasterTexture(texture);
            }
            else
            {
                Debug.Log("here");
            }
        }

        private void LoadTile(UnityTile tile)
        {
            var tileLocal = tile;
            var tileId = tileLocal.CanonicalTileId;

            ApplyParentTexture(tile);
            _fetcher.FetchData(_customTilesetId, tileId, true, tile);
        }

        private void ApplyParentTexture(UnityTile tile)
        {
            var parent = tile.UnwrappedTileId.Parent;
            for (int i = 0; i < 16; i++)
            {
                var cacheItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(_customTilesetId, parent.Canonical);
                if (cacheItem != null && cacheItem.Texture2D != null)
                {
                    tile.SetParentTexture(parent, cacheItem.Texture2D);
                    break;
                }

                parent = parent.Parent;
            }
        }
    }
}