using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Factories.TerrainStrategies;
using UnityEngine;

namespace Mapbox.Unity.CustomLayer
{
	//important note
	//relationship between Factory manager and data fetcher can be a little messy.
	//factory manager creates a dataTile object and passes it to dataFetcher
	//initial idea was that dataFetcher would fill&complete this object and returns it
	//but we also use same dataTile for caching so IF dataFetcher finds same data (in another dataTile object)
	//in memory, it returns that instance.
	//So factory manager should be ready to handle situations where it sends one dataTile instance and returns whole another
	//but with same tileId, tilesetId etc of course.
	//shortly, you will not always get the same item you send, this is why it's using (int)Key instead of RasterTile references in tracker lists
	//(see MapboxTerrainFactoryManager)

	//_tileTracker uses RasterTile reference as these are the returned instances, in other words the instances used in cache/memory etc.
	//so at that point, it's final object/instance.
	public sealed class MapboxTerrainFactoryManager : ImageFactoryManager
	{
		public TerrainStrategy TerrainStrategy;
		public string ShaderElevationTextureFieldName = "_HeightTexture";
		public string ShaderElevationTextureScaleOffsetFieldName = "_HeightTexture_ST";

		private ElevationLayerProperties _elevationSettings;
		private bool _isUsingShaderSolution = true;

		//this is in-use unity tile to raster tile
		private Dictionary<UnityTile, RasterTile> _tileTracker = new Dictionary<UnityTile, RasterTile>();
		private Dictionary<int, HashSet<UnityTile>> _tileUserTracker = new Dictionary<int, HashSet<UnityTile>>();
		//this is grand parent id to raster tile
		//so these two are vastly separate, don't try to optimize
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

			if (DownloadFallbackImagery)
			{
				DownloadAndCacheBaseTiles(_sourceSettings.Id, true);
			}
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
				var parentId = tile.UnwrappedTileId.Z > 4
					? tile.UnwrappedTileId.Parent.Parent.Canonical
					: tile.UnwrappedTileId.ParentAt(2).Canonical;


				if (_requestedTiles.ContainsKey(parentId))
				{
					dataTile = _requestedTiles[parentId];
					dataTile.Logs.Add("data tile reused " + tile.CanonicalTileId);
					if (tile != null)
					{
						tile.AddTile(dataTile);
						dataTile.AddUser(tile.CanonicalTileId);
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
					dataTile.Logs.Add("data tile created " + tile.CanonicalTileId);
					_requestedTiles.Add(parentId, dataTile);
					if (tile != null)
					{
						tile.AddTile(dataTile);
						dataTile.AddUser(tile.CanonicalTileId);
					}
					if (!_tileWaitingList.ContainsKey(dataTile.Key))
					{
						_tileWaitingList.Add(dataTile.Key, new HashSet<UnityTile>());
					}
					_tileWaitingList[dataTile.Key].Add(tile);
					_tileTracker.Add(tile, dataTile);
					_fetcher.FetchData(dataTile, _sourceSettings.Id, tile.CanonicalTileId, tile);
				}

				tile.MeshRenderer.sharedMaterial.SetFloat("_TileScale", tile.TileScale);
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
				if (!_tileUserTracker.ContainsKey(dataTile.Key))
				{
					_tileUserTracker.Add(dataTile.Key, new HashSet<UnityTile>());
				}

				foreach (var utile in _tileWaitingList[dataTile.Key])
				{
					SetTexture(utile, dataTile);
					_tileUserTracker[dataTile.Key].Add(utile);
				}
				_tileWaitingList.Remove(dataTile.Key);

				_requestedTiles.Remove(dataTile.Id);
				TextureReceived(unityTile, dataTile);
			}
			else
			{
				//this means tile is unregistered during fetching... but somehow it didn't get cancelled?
			}
		}

		protected override void OnFetcherError(UnityTile unityTile, RasterTile dataTile, TileErrorEventArgs errorEventArgs)
		{
			FetchingError(unityTile, dataTile, errorEventArgs);
		}

		public override void UnregisterTile(UnityTile tile, bool clearData = true)
		{
			if (_tileTracker.ContainsKey(tile))
			{
				var noTileIsWaitingForIt = false;
				var requestedTile = _tileTracker[tile];
				requestedTile.Logs.Add("cancelling " + tile.CanonicalTileId);
				if (_tileWaitingList.ContainsKey(requestedTile.Key))
				{
					if (_tileWaitingList[requestedTile.Key].Contains(tile))
					{
						_tileWaitingList[requestedTile.Key].Remove(tile);
					}

					if (_tileWaitingList[requestedTile.Key].Count == 0)
					{
						_tileWaitingList.Remove(requestedTile.Key);
						noTileIsWaitingForIt = true;
					}
				}
				else
				{
					noTileIsWaitingForIt = true;
				}

				if (_tileUserTracker.ContainsKey(requestedTile.Key))
				{
					_tileUserTracker[requestedTile.Key].Remove(tile);
					if (_tileUserTracker[requestedTile.Key].Count == 0 && noTileIsWaitingForIt)
					{
						requestedTile.Logs.Add("disposing 1 " + tile.CanonicalTileId);
						_tileUserTracker.Remove(requestedTile.Key);
						_fetcher.CancelFetching(requestedTile, _sourceSettings.Id);
						_requestedTiles.Remove(requestedTile.Id);
						MapboxAccess.Instance.CacheManager.TileDisposed(requestedTile, _sourceSettings.Id);
					}
				}
				else if(noTileIsWaitingForIt)
				{
					requestedTile.Logs.Add("disposing 2 " + tile.CanonicalTileId);
					_fetcher.CancelFetching(requestedTile, _sourceSettings.Id);
					_requestedTiles.Remove(requestedTile.Id);
					MapboxAccess.Instance.CacheManager.TileDisposed(requestedTile, _sourceSettings.Id);
				}

				tile.RemoveTile(_tileTracker[tile]);
				_tileTracker[tile].RemoveUser(tile.CanonicalTileId);
				TerrainStrategy.UnregisterTile(tile);
				_tileTracker.Remove(tile);
			}
			else
			{
				if (_tileUserTracker.ContainsKey(tile.TerrainData.Key))
				{
					Debug.Log("here");
				}
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