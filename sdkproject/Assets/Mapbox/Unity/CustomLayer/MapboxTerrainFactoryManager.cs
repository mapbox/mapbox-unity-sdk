using System;
using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Factories.TerrainStrategies;
using UnityEngine;
using UnityEngine.Rendering;

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
		private bool _shaderIdsInitialized = false;
		private ElevationLayerProperties _elevationSettings;
		private bool _isUsingShaderSolution = true;

		protected Dictionary<int, HashSet<UnityTile>> _elevationWaitingList = new Dictionary<int, HashSet<UnityTile>>();

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

		public override void RegisterTile(UnityTile tile, bool loadParent)
		{

			if (TerrainStrategy is IElevationBasedTerrainStrategy)
			{
				if (_isUsingShaderSolution && loadParent)
				{
					ApplyParentTexture(tile);
				}

				var parentId = tile.UnwrappedTileId.Z > 4
					? tile.UnwrappedTileId.Parent.Parent.Canonical
					: tile.UnwrappedTileId.ParentAt(2).Canonical;

				var memoryCacheItem = _fetcher.FetchDataInstant(parentId, _sourceSettings.Id);
				if (memoryCacheItem != null)
				{
					var dataTile = (RasterTile) memoryCacheItem.Tile;
					ConnectTiles(tile, dataTile, false);
					memoryCacheItem.Tile.AddLog("data tile instant ", tile.CanonicalTileId);
					SetTexture(tile, dataTile);
					TextureReceived(dataTile);
				}
				else
				{
					RasterTile dataTile = null;
					if (_requestedTiles.ContainsKey(parentId))
					{
						dataTile = _requestedTiles[parentId];
						dataTile.AddLog("data tile reused ", tile.CanonicalTileId);
						ConnectTiles(tile, dataTile);
					}
					else
					{
						dataTile = CreateTile(parentId, _sourceSettings.Id);
						ConnectTiles(tile, dataTile);
						dataTile.AddLog("data tile created ", tile.CanonicalTileId);
						_requestedTiles.Add(parentId, dataTile);
						_fetcher.FetchData(dataTile, _sourceSettings.Id, tile.CanonicalTileId);
					}
				}

				//tile.MeshRenderer.sharedMaterial.SetFloat(_tileScaleFieldNameID, tile.TileScale);
			}
			else
			{
				//reseting height data
				tile.SetHeightData( null);
				TerrainStrategy.RegisterTile(tile, false);
			}
		}

		public override void UnregisterTile(UnityTile tile, bool clearData = true)
		{
			base.UnregisterTile(tile, clearData);
			TerrainStrategy.UnregisterTile(tile);
		}

		protected override void OnTextureReceived(RasterTile dataTile)
		{
			if (_tileWaitingList.ContainsKey(dataTile.Key))
			{
				if (!_tileUserTracker.ContainsKey(dataTile.Key))
				{
					_tileUserTracker.Add(dataTile.Key, new HashSet<UnityTile>());
				}

				if (SystemInfo.supportsAsyncGPUReadback && _isUsingShaderSolution)
				{
					AsyncExtractElevationArray((RawPngRasterTile) dataTile);
				}
				else
				{
					SyncExtractElevationArray((RawPngRasterTile) dataTile);
				}

				foreach (var unityTile in _tileWaitingList[dataTile.Key])
				{
					SetTexture(unityTile, dataTile);
					_tileUserTracker[dataTile.Key].Add(unityTile);
				}
				_tileWaitingList.Remove(dataTile.Key);
				TextureReceived(dataTile);
			}
			else
			{
				//this means tile is unregistered during fetching... but somehow it didn't get cancelled?
				dataTile.AddLog("tile is unregistered during fetching?");
			}

			_requestedTiles.Remove(dataTile.Id);
		}

		private void SyncExtractElevationArray(RawPngRasterTile dataTile)
		{
			var _heightDataResolution = 514;
			byte[] rgbData = dataTile.Texture2D.GetRawTextureData();
			var width = dataTile.Texture2D.width;

			if (dataTile.HeightData == null || dataTile.HeightData.Length != _heightDataResolution * _heightDataResolution)
			{
				dataTile.ExtractedDataResolution = _heightDataResolution;
				dataTile.HeightData = new float[_heightDataResolution * _heightDataResolution];
			}

			for (float y = 0; y < _heightDataResolution; y++)
			{
				for (float x = 0; x < _heightDataResolution; x++)
				{
					var xx = (x / _heightDataResolution) * width;
					var yy = (y / _heightDataResolution) * width;
					var index = ((int) yy * width) + (int) xx;

					float r = rgbData[index * 4 + 1];
					float g = rgbData[index * 4 + 2];
					float b = rgbData[index * 4 + 3];
					//var color = rgbData[index];
					// float r = color.g;
					// float g = color.b;
					// float b = color.a;
					//the formula below is the same as Conversions.GetAbsoluteHeightFromColor but it's inlined for performance
					dataTile.HeightData[(int) (y * _heightDataResolution + x)] = (-10000f + ((r * 65536f + g * 256f + b) * 0.1f));
					//678 ==> 012345678
					//345
					//012
				}
			}

			foreach (var unityTile in _tileWaitingList[dataTile.Key])
			{
				unityTile.ElevationDataParsingCompleted(dataTile);
			}
		}

		private void AsyncExtractElevationArray(RawPngRasterTile dataTile)
		{
			var _heightDataResolution = 100;

			if (!_elevationWaitingList.ContainsKey(dataTile.Key))
			{
				_elevationWaitingList.Add(dataTile.Key, new HashSet<UnityTile>());
			}

			foreach (var utile in _tileWaitingList[dataTile.Key])
			{
				_elevationWaitingList[dataTile.Key].Add(utile);
			}

			AsyncGPUReadback.Request(dataTile.Texture2D, 0, (t) =>
			{
				var width = t.width;
				var data = t.GetData<Color32>().ToArray();

				if (dataTile.HeightData == null || dataTile.HeightData.Length != _heightDataResolution * _heightDataResolution)
				{
					dataTile.ExtractedDataResolution = _heightDataResolution;
					dataTile.HeightData = new float[_heightDataResolution * _heightDataResolution];
				}

				for (float y = 0; y < _heightDataResolution; y++)
				{
					for (float x = 0; x < _heightDataResolution; x++)
					{
						var xx = (x / _heightDataResolution) * width;
						var yy = (y / _heightDataResolution) * width;
						var index = ((int) yy * width) + (int) xx;

						float r = data[index].g;
						float g = data[index].b;
						float b = data[index].a;
						//the formula below is the same as Conversions.GetAbsoluteHeightFromColor but it's inlined for performance
						dataTile.HeightData[(int) (y * _heightDataResolution + x)] = (-10000f + ((r * 65536f + g * 256f + b) * 0.1f));
						//678 ==> 012345678
						//345
						//012
					}
				}

				foreach (var unityTile in _elevationWaitingList[dataTile.Key])
				{
					unityTile.ElevationDataParsingCompleted(dataTile);
				}

				_elevationWaitingList.Remove(dataTile.Key);
			});
		}

		// protected override void OnFetcherError(RasterTile dataTile, TileErrorEventArgs errorEventArgs)
		// {
		// 	dataTile.AddLog("OnFetcherError TerrainFactoryManager");
		// 	if (_requestedTiles.ContainsKey(dataTile.Id))
		// 	{
		// 		_requestedTiles.Remove(dataTile.Id);
		// 		_tileWaitingList.Remove(dataTile.Key);
		// 	}
		// 	else
		// 	{
		// 		Debug.Log("fetching failed but it was requested?");
		// 	}
		// 	FetchingError(dataTile, errorEventArgs);
		// }

		protected override RasterTile CreateTile(CanonicalTileId tileId, string tilesetId)
		{
			RasterTile rasterTile;


			// //TODO fix this obviously
			if (tilesetId == "mapbox.mapbox-terrain-dem-v1")
			{
				if (SystemInfo.supportsAsyncGPUReadback)
				{
					rasterTile = new DemRasterTile(tileId, tilesetId, true);
				}
				else
				{
					rasterTile = new DemRasterTile(tileId, tilesetId, false);
				}

			}
			else
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
				// var tileZoom = unityTile.UnwrappedTileId.Z;
				// var parentZoom = dataTile.Id.Z;
				// var scale = 1f;
				// var offsetX = 0f;
				// var offsetY = 0f;
				//
				// var current = unityTile.UnwrappedTileId;
				// var currentParent = current.Parent;
				//
				// for (int i = tileZoom - 1; i >= parentZoom; i--)
				// {
				// 	scale /= 2;
				//
				// 	var bottomLeftChildX = currentParent.X * 2;
				// 	var bottomLeftChildY = currentParent.Y * 2;
				//
				// 	//top left
				// 	if (current.X == bottomLeftChildX && current.Y == bottomLeftChildY)
				// 	{
				// 		offsetY = 0.5f + (offsetY/2);
				// 		offsetX = offsetX / 2;
				// 	}
				// 	//top right
				// 	else if (current.X == bottomLeftChildX + 1 && current.Y == bottomLeftChildY)
				// 	{
				// 		offsetX = 0.5f + (offsetX / 2);
				// 		offsetY = 0.5f + (offsetY / 2);
				// 	}
				// 	//bottom left
				// 	else if (current.X == bottomLeftChildX && current.Y == bottomLeftChildY + 1)
				// 	{
				// 		offsetX = offsetX / 2;
				// 		offsetY = offsetY / 2;
				// 	}
				// 	//bottom right
				// 	else if (current.X == bottomLeftChildX + 1 && current.Y == bottomLeftChildY + 1)
				// 	{
				// 		offsetX = 0.5f + (offsetX / 2);
				// 		offsetY = offsetY / 2;
				// 	}
				//
				// 	current = currentParent;
				// 	currentParent = currentParent.Parent;
				// }
				// //if collider is disabled, we switch to a shader based solution
				// //no elevated mesh is generated
				//
				// var scaleOffset = new Vector4(scale, scale, offsetX, offsetY);
				if (_isUsingShaderSolution)
				{
					unityTile.SetHeightData(
						dataTile,
						_elevationSettings.requiredOptions.exaggerationFactor,
						_elevationSettings.modificationOptions.useRelativeHeight,
						_elevationSettings.colliderOptions.addCollider,
						(tile) =>
						{
							ElevationUpdated(tile);
						});
					TerrainStrategy.RegisterTile(unityTile, false);
				}
				else
				{
					unityTile.SetHeightData(dataTile,
						_elevationSettings.requiredOptions.exaggerationFactor,
						_elevationSettings.modificationOptions.useRelativeHeight,
						_elevationSettings.colliderOptions.addCollider,
						(tile) =>
						{
							if (tile.CanonicalTileId == cachedTileIdForCallbackCheck)
							{
								TerrainStrategy.RegisterTile(unityTile, true);
								ElevationUpdated(tile);
							}
						});
				}
			}
			else
			{
				if (_isUsingShaderSolution)
				{
					//unityTile.MeshRenderer.sharedMaterial.SetTexture(ShaderElevationTextureFieldName, null);
					//unityTile.MeshRenderer.sharedMaterial.SetVector(ShaderElevationTextureScaleOffsetFieldName, new Vector4(1, 1, 0, 0));
					// var probBlock = unityTile.PropertyBlock;
					// unityTile.MeshRenderer.GetPropertyBlock(probBlock);
					// probBlock.SetFloat(_tileScaleFieldNameID, unityTile.TileScale);
					// probBlock.SetFloat(_elevationMultiplierFieldNameID, 0);
					// unityTile.MeshRenderer.SetPropertyBlock(probBlock);

					unityTile.SetHeightData(dataTile,
						_elevationSettings.requiredOptions.exaggerationFactor,
						_elevationSettings.modificationOptions.useRelativeHeight,
						_elevationSettings.colliderOptions.addCollider,
						(tile) => { ElevationUpdated(tile); });

					TerrainStrategy.RegisterTile(unityTile, false);
				}
				else
				{
					unityTile.SetHeightData(dataTile, 0,
						_elevationSettings.modificationOptions.useRelativeHeight,
						_elevationSettings.colliderOptions.addCollider,
						(tile) =>
						{
							if (tile.CanonicalTileId == cachedTileIdForCallbackCheck)
							{
								TerrainStrategy.RegisterTile(unityTile, true);
								ElevationUpdated(tile);
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
			var parentFound = false;
			var parent = tile.UnwrappedTileId.Z > 4
				? tile.UnwrappedTileId.Parent.Parent
				: tile.UnwrappedTileId.ParentAt(2);

			parent = parent.Parent;
			for (int i = tile.CanonicalTileId.Z - 1; i > 0; i--)
			{
				var cacheItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(_sourceSettings.Id, parent.Canonical, true);
				if (cacheItem != null && cacheItem.Texture2D != null)
				{
					tile.SetParentElevationTexture(parent, (RawPngRasterTile) cacheItem.Tile, _isUsingShaderSolution);

					parentFound = true;
					break;
				}

				parent = parent.Parent;
			}

			if (!parentFound)
			{
				Debug.Log("no parent? " + tile.CanonicalTileId);
			}
		}

		public Action<UnityTile> ElevationUpdated = (s) => { };
	}
}