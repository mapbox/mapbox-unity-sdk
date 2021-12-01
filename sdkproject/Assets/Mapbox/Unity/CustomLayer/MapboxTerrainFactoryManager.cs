using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Factories.TerrainStrategies;
using UnityEngine;

namespace Mapbox.Unity.CustomLayer
{
	public sealed class MapboxTerrainFactoryManager : ImageFactoryManager
	{
		public TerrainStrategy TerrainStrategy;
		public string ShaderElevationTextureFieldName = "_HeightTexture";
		public string ShaderElevationTextureScaleOffsetFieldName = "_HeightTexture_ST";

		private ElevationLayerProperties _elevationSettings;
		private bool _isUsingShaderSolution = true;

		private Dictionary<UnityTile, Tile> _tileTracker = new Dictionary<UnityTile, Tile>();
		private Dictionary<CanonicalTileId, RasterTile> _requestedTiles = new Dictionary<CanonicalTileId, RasterTile>();
		private Dictionary<int, HashSet<UnityTile>> _tileWaitingList = new Dictionary<int, HashSet<UnityTile>>();

		public MapboxTerrainFactoryManager(
			ElevationLayerProperties elevationSettings,
			TerrainStrategy terrainStrategy,
			bool downloadFallbackImagery) : base(elevationSettings.sourceOptions, downloadFallbackImagery)
		{
			_elevationSettings = elevationSettings;
			TerrainStrategy = terrainStrategy;
			_isUsingShaderSolution = !_elevationSettings.colliderOptions.addCollider;
		}

		public override void RegisterTile(UnityTile tile)
		{
			if (_tileTracker.ContainsKey(tile))
			{
				Debug.Log("Tile is already in tracking list?");
			}

			if (TerrainStrategy is IElevationBasedTerrainStrategy)
			{
				if (_isUsingShaderSolution)
				{
					ApplyParentTexture(tile);
				}

				RasterTile dataTile = null;
				var parentId = tile.UnwrappedTileId.Parent.Parent.Canonical;
				if (_requestedTiles.ContainsKey(parentId))
				{
					dataTile = _requestedTiles[parentId];
					if (tile != null)
					{
						tile.AddTile(dataTile);
					}
					if (!_tileWaitingList.ContainsKey(dataTile.Key))
					{
						_tileWaitingList.Add(dataTile.Key, new HashSet<UnityTile>());
					}
					_tileTracker.Add(tile, dataTile);
					_tileWaitingList[dataTile.Key].Add(tile);
				}
				else
				{
					dataTile = CreateTile(parentId, _sourceSettings.Id);
					_requestedTiles.Add(parentId, dataTile);
					if (tile != null)
					{
						tile.AddTile(dataTile);
					}
					if (!_tileWaitingList.ContainsKey(dataTile.Key))
					{
						_tileWaitingList.Add(dataTile.Key, new HashSet<UnityTile>());
					}
					_tileWaitingList[dataTile.Key].Add(tile);
					_tileTracker.Add(tile, dataTile);
					_fetcher.FetchData(dataTile, _sourceSettings.Id, tile.CanonicalTileId, tile);
				}

				// if (!_tileWaitingList.ContainsKey(dataTile.Key))
				// {
				// 	_tileWaitingList.Add(dataTile.Key, new List<UnityTile>());
				// }
				// _tileWaitingList[dataTile.Key].Add(tile);
				// _fetcher.FetchData(dataTile, _sourceSettings.Id, tile.CanonicalTileId, tile);
			}
			else
			{
				//reseting height data
				tile.SetHeightData( null);
				TerrainStrategy.RegisterTile(tile, false);
			}
		}

		protected override void OnTextureReceived(UnityTile unityTile, RasterTile dataTile)
		{
			if (_tileWaitingList.ContainsKey(dataTile.Key))
			{
				foreach (var utile in _tileWaitingList[dataTile.Key])
				{
					SetTexture(utile, dataTile);
				}
				_tileWaitingList.Remove(dataTile.Key);
			}

			TextureReceived(unityTile, dataTile);
		}

		public override void UnregisterTile(UnityTile tile, bool clearData = true)
		{
			if (_tileTracker.ContainsKey(tile))
			{
				var requestedTile = _tileTracker[tile];
				if (_tileWaitingList.ContainsKey(requestedTile.Key))
				{
					if (_tileWaitingList[requestedTile.Key].Contains(tile))
					{
						_tileWaitingList[requestedTile.Key].Remove(tile);

					}

					if (_tileWaitingList[requestedTile.Key].Count == 0)
					{
						_fetcher.CancelFetching(tile.UnwrappedTileId, _sourceSettings.Id);
						MapboxAccess.Instance.CacheManager.TileDisposed(tile, _sourceSettings.Id);
					}
				}
				tile.RemoveTile(_tileTracker[tile]);
				TerrainStrategy.UnregisterTile(tile);
				_tileTracker.Remove(tile);
			}
		}

		protected override RasterTile CreateTile(CanonicalTileId tileId, string tilesetId)
		{
			RasterTile rasterTile;

			// if (tilesetId.StartsWith("mapbox://", StringComparison.Ordinal))
			// {
			// 	  dem tiles will be here in the future
			// }
			// else
			{
				if (SystemInfo.supportsAsyncGPUReadback)
				{
					rasterTile = new RawPngRasterTile(tileId, tilesetId, true);
				}
				else
				{
					rasterTile = new RawPngRasterTile(tileId, tilesetId, false);
				}
			}

#if UNITY_EDITOR
			rasterTile.IsMapboxTile = true;
#endif

			return rasterTile;
		}

		protected override void SetTexture(UnityTile unityTile, RasterTile dataTile)
		{
			var cachedTileIdForCallbackCheck = unityTile.CanonicalTileId;
			if (dataTile != null && dataTile.Texture2D != null)
			{
				//if collider is disabled, we switch to a shader based solution
				//no elevated mesh is generated
				if (_isUsingShaderSolution)
				{
					var tileZoom = unityTile.UnwrappedTileId.Z;
					var parentZoom = dataTile.Id.Z;
					var scale = 1f;
					var offsetX = 0f;
					var offsetY = 0f;

					var current = unityTile.UnwrappedTileId;
					var currentParent = current.Parent;

					for (int i = tileZoom - 1; i >= parentZoom; i--)
					{
						scale /= 2;

						var bottomLeftChildX = currentParent.X * 2;
						var bottomLeftChildY = currentParent.Y * 2;

						//top left
						if (current.X == bottomLeftChildX && current.Y == bottomLeftChildY)
						{
							offsetY = 0.5f + (offsetY/2);
							offsetX = offsetX / 2;
						}
						//top right
						else if (current.X == bottomLeftChildX + 1 && current.Y == bottomLeftChildY)
						{
							offsetX = 0.5f + (offsetX / 2);
							offsetY = 0.5f + (offsetY / 2);
						}
						//bottom left
						else if (current.X == bottomLeftChildX && current.Y == bottomLeftChildY + 1)
						{
							offsetX = offsetX / 2;
							offsetY = offsetY / 2;
						}
						//bottom right
						else if (current.X == bottomLeftChildX + 1 && current.Y == bottomLeftChildY + 1)
						{
							offsetX = 0.5f + (offsetX / 2);
							offsetY = offsetY / 2;
						}

						current = currentParent;
						currentParent = currentParent.Parent;
					}

					unityTile.MeshRenderer.sharedMaterial.SetTexture(ShaderElevationTextureFieldName, dataTile.Texture2D);
					unityTile.MeshRenderer.sharedMaterial.SetVector(ShaderElevationTextureScaleOffsetFieldName, new Vector4(scale, scale, offsetX, offsetY));
					unityTile.MeshRenderer.sharedMaterial.SetFloat("_TileScale", unityTile.TileScale);
					unityTile.MeshRenderer.sharedMaterial.SetFloat("_ElevationMultiplier", _elevationSettings.requiredOptions.exaggerationFactor);
					unityTile.SetHeightData(dataTile, _elevationSettings.requiredOptions.exaggerationFactor, _elevationSettings.modificationOptions.useRelativeHeight, _elevationSettings.colliderOptions.addCollider);
					TerrainStrategy.RegisterTile(unityTile, false);
				}
				else
				{
					unityTile.SetHeightData(dataTile, _elevationSettings.requiredOptions.exaggerationFactor, _elevationSettings.modificationOptions.useRelativeHeight, _elevationSettings.colliderOptions.addCollider, (tile) =>
					{
						TerrainStrategy.RegisterTile(unityTile, true);
					});
				}
			}
			else
			{
				if (_isUsingShaderSolution)
				{
					//unityTile.MeshRenderer.sharedMaterial.SetTexture(ShaderElevationTextureFieldName, null);
					unityTile.MeshRenderer.sharedMaterial.SetVector(ShaderElevationTextureScaleOffsetFieldName, new Vector4(1, 1, 0, 0));
					unityTile.MeshRenderer.sharedMaterial.SetFloat("_TileScale", unityTile.TileScale);
					unityTile.MeshRenderer.sharedMaterial.SetFloat("_ElevationMultiplier", 0);
					unityTile.SetHeightData(
						dataTile,
						_elevationSettings.requiredOptions.exaggerationFactor,
						_elevationSettings.modificationOptions.useRelativeHeight,
						_elevationSettings.colliderOptions.addCollider);
					TerrainStrategy.RegisterTile(unityTile, false);
				}
				else
				{
					unityTile.SetHeightData(
						dataTile,
						0,
						_elevationSettings.modificationOptions.useRelativeHeight,
						_elevationSettings.colliderOptions.addCollider,
						(tile) => {
						if (tile.CanonicalTileId == cachedTileIdForCallbackCheck)
						{
							TerrainStrategy.RegisterTile(unityTile, true);
						}
					});
				}
			}
		}

		public void PregenerateTileMesh(UnityTile tile)
		{
			TerrainStrategy.RegisterTile(tile, false);
		}

		protected override void ApplyParentTexture(UnityTile tile)
		{
			var parent = tile.UnwrappedTileId.Parent;
			for (int i = tile.CanonicalTileId.Z - 1; i > 0; i--)
			{
				var cacheItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(_sourceSettings.Id, parent.Canonical, true);
				if (cacheItem != null && cacheItem.Texture2D != null)
				{
					tile.SetParentTexture(parent, cacheItem.Texture2D, ShaderElevationTextureFieldName, ShaderElevationTextureScaleOffsetFieldName);

					if (_isUsingShaderSolution)
					{
						tile.MeshRenderer.sharedMaterial.SetFloat("_TileScale", tile.TileScale);
						tile.MeshRenderer.sharedMaterial.SetFloat("_ElevationMultiplier", _elevationSettings.requiredOptions.exaggerationFactor);
					}
					break;
				}

				parent = parent.Parent;
			}
		}
	}
}