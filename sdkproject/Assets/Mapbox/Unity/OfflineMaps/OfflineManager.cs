using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Mapbox.Map;
using Mapbox.Platform;
using Mapbox.Platform.Cache;
using Mapbox.Unity;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;
using Constants = Mapbox.Utils.Constants;

public class OfflineManager
{
	private string _accessToken;
	private Func<string> _getMapsSkuToken;
	private SQLiteCache _offlineCache;

	public Action<float> ProgressUpdated = (progress) => { };
	public Action<OfflineMapDownloadInfo> DownloadFinished = (tileCount) => { };
	public float Progress = 0;
	public OfflineMapDownloadInfo CurrentMapDownloadInfo;

	private int _currentCoroutine;

	//public List<UnwrappedTileId> EstimatedTileList = new List<UnwrappedTileId>();
	private int _currentlyDownloadedFileCount;
	private int _currentlyRequestedFileCount;
	private int _totalTileCount;
	private bool _isDownloading;
	private readonly int OfflineTileLimit = 6000;

	//INITIALIZE
	public OfflineManager(string configurationAccessToken, Func<string> getMapsSkuToken)
	{
		_accessToken = configurationAccessToken;
		_getMapsSkuToken = getMapsSkuToken;
	}

	public void SetOfflineCache(SQLiteCache sqliteCache)
	{
		_offlineCache = sqliteCache;
	}

	//QUERY
	public int GetAmbientTileCount()
	{
		return _offlineCache.GetAmbientTileCount();
	}

	public int GetOfflineTileCount()
	{
		return _offlineCache.GetOfflineTileCount();
	}

	public int GetOfflineTileCount(string offlineMapName)
	{
		return _offlineCache.GetOfflineTileCount(offlineMapName);
	}

	public int GetOfflineDataSize(string mapName)
	{
		return _offlineCache.GetOfflineDataSize(mapName);
	}

	public Dictionary<string, int> GetOfflineMapList()
	{
		return _offlineCache.GetOfflineMapList();
	}

	//Information of TMS tiles in given region and zoom range
	//This Info includes tile coordinates and tilesetid
	public List<OfflineTileInfo> EstimatedTileList(OfflineRegion region)
	{
		var estimatedTileList = new List<OfflineTileInfo>();
		try
		{
			estimatedTileList.Clear();
			for (int currentZoom = region.MinZoom; currentZoom <= region.MaxZoom; currentZoom++)
			{
				foreach (var tileId in GetWithWebMerc(region.MinLatLng, region.MaxLatLng, currentZoom))
				{
					if (!string.IsNullOrWhiteSpace(region.ElevationTilesetId))
					{
						estimatedTileList.Add(new OfflineTileInfo(tileId, OfflineTileType.Elevation, region.ElevationTilesetId));
					}

					if (!string.IsNullOrWhiteSpace(region.ImageTilesetId))
					{
						estimatedTileList.Add(new OfflineTileInfo(tileId, OfflineTileType.Imagery, region.ImageTilesetId));
					}

					if (!string.IsNullOrWhiteSpace(region.VectorTilesetId))
					{
						estimatedTileList.Add(new OfflineTileInfo(tileId, OfflineTileType.Vector, region.VectorTilesetId));
					}
				}
			}

			return estimatedTileList;
		}
		catch (Exception e)
		{
			// ignored
			return null;
		}
	}

	//Number of TMS tiles in given region and zoom range
	public int EstimatedTileCount(OfflineRegion region)
	{
		return EstimatedTileCount(region.MinLatLng, region.MaxLatLng, region.MinZoom, region.MaxZoom);
	}

	//Number of TMS tiles in given region and zoom range
	public int EstimatedTileCount(string minLatlng, string maxLatlng, int minZoom, int maxZoom)
	{
		if (string.IsNullOrEmpty(minLatlng) || string.IsNullOrEmpty(maxLatlng))
			return 0;

		return EstimatedTileCount(Conversions.StringToLatLon(minLatlng), Conversions.StringToLatLon(maxLatlng), minZoom, maxZoom);
	}

	//Number of TMS tiles in given region and zoom range
	public int EstimatedTileCount(Vector2d minLatlng, Vector2d maxLatlng, int minZoom, int maxZoom)
	{
		var tileCount = 0;
		try
		{
			for (int currentZoom = minZoom; currentZoom <= maxZoom; currentZoom++)
			{
				tileCount += GetWithWebMerc(minLatlng, maxLatlng, currentZoom).Count();
			}
		}
		catch (Exception e)
		{
			// ignored
			return 0;
		}

		return tileCount;
	}

	//OPERATIONS
	//This is the main method for creating offline maps.
	//It takes a region and zoom level range, then marks all tiles in there using the name you passed.
	//Tiles are stored in Sqlite tiles table as regular ambient cache tiles
	//Offline map name is stored in another table and this entry linked to tiles in a third connection table
	//So offline tiles are just regular tiles marked/flagged as offline by given name
	public OfflineMapResponse CreateOfflineMap(string cacheName, OfflineRegion region)
	{
		var tiles = EstimatedTileList(region);
		if (tiles.Count <= 0)
		{
			return new OfflineMapResponse()
			{
				HasErrors = true,
				ErrorMessage = "Region doesn't contain any tiles."
			};
		}
		else if (tiles.Count > OfflineTileLimit) //offline maps supports up to OfflineTileLimit-6000 tiles
		{
			return new OfflineMapResponse()
			{
				HasErrors = true,
				ErrorMessage = "You cannot store more than 6000 offline tiles."
			};
		}

		var currentOfflineTileCount = _offlineCache.GetOfflineTileCount();
		var capacityLeft = OfflineTileLimit - currentOfflineTileCount;
		if (capacityLeft <= 0)
		{
			return new OfflineMapResponse()
			{
				HasErrors = true,
				ErrorMessage = "Offline cache capacity is full."
			};
		}

		if (capacityLeft < tiles.Count)
		{
			//if requested region doesn't fit the capacity left, we do not download anything at all
			return new OfflineMapResponse()
			{
				HasErrors = true,
				ErrorMessage = string.Format("Not enough capacity to download whole region. Capacity left: {0} tiles )", capacityLeft)
			};
		}
		else
		{
			CurrentMapDownloadInfo = new OfflineMapDownloadInfo(cacheName, tiles.Count);
			_currentCoroutine = Runnable.Run(DownloadTileListCoroutine(cacheName, tiles));
			return new OfflineMapResponse()
			{
				HasErrors = false,
				ErrorMessage = "Downloading " + tiles.Count + " tiles."
			};
		}
	}

	//Clears all data marked by given name in the sqlite database.
	//It's not possible to delete elevation, imagery, vector separately at the moment.
	public void DeleteOfflineMap(string offlineMapName)
	{
		_offlineCache.DeleteOfflineMap(offlineMapName);
	}

	//Stops running download process and clears downloaded data afterwards.
	public void Stop()
	{
		Runnable.Stop(_currentCoroutine);
		if (CurrentMapDownloadInfo != null)
		{
			var cancelledName = CurrentMapDownloadInfo.MapName;
			DeleteOfflineMap(cancelledName);
		}
	}

	//PRIVATE
	private void RequestTile(OfflineTileType type, CanonicalTileId tileId, string tilesetId, int offlineMapId, Action<Response> callback)
	{
		var requestUrl = CreateTileRequestUrl(type, tileId, tilesetId);

		IAsyncRequestFactory.CreateRequest(
			requestUrl,
			(Response r) =>
			{
				// if the request was successful add tile to all caches
				if (!r.HasError && null != r.Data)
				{
					string eTag = r.GetETag();
					DateTime expirationDate = r.GetExpirationDate();

					CurrentMapDownloadInfo.SuccesfulTileDownloads++;
					_offlineCache.Add(
						tilesetId
						, tileId
						, new CacheItem()
						{
							Data = r.Data,
							ETag = eTag,
							ExpirationDate =expirationDate
						}
						, true
					);
					_offlineCache.MarkOffline(offlineMapId, tilesetId, tileId);
				}
				else
				{
					CurrentMapDownloadInfo.FailedTileDownloads++;
					CurrentMapDownloadInfo.FailedDownloadLogs.Add(string.Format("Download Failed. TileId: {0}, Tileset:{1}, Error:{2}",
						tileId,
						tilesetId,
						r.ExceptionsAsString));
				}

				if (null != callback)
				{
					r.IsUpdate = true;
					callback(r);
				}
			}, 10);
	}

	private IEnumerator DownloadTileListCoroutine(string cacheName, List<OfflineTileInfo> tiles)
	{
		_currentlyDownloadedFileCount = 0;
		_currentlyRequestedFileCount = 0;
		_totalTileCount = tiles.Count;

		_isDownloading = true;

		var batchSize = 5;
		var currentBatchSize = 0;

		var offlineMapId = _offlineCache.GetOrAddOfflineMapId(cacheName);

		while (_currentlyRequestedFileCount < _totalTileCount)
		{
			foreach (var tile in tiles)
			{
				if (!_offlineCache.TileExists(tile.TilesetId, tile.CanonicalTileId))
				{
					RequestTile(tile.Type, tile.CanonicalTileId, tile.TilesetId, offlineMapId, HandleOfflineRequestResponse);
				}
				else
				{
					_offlineCache.MarkOffline(offlineMapId, tile.TilesetId, tile.CanonicalTileId);
				}

				_currentlyRequestedFileCount++;
				currentBatchSize++;

				if (currentBatchSize > batchSize)
				{
					currentBatchSize = 0;
					yield return new WaitForSeconds(1f);
				}
			}
		}
	}

	private string CreateTileRequestUrl(OfflineTileType type, CanonicalTileId tileId, string tilesetId)
	{
		var uri = GetUriForDataType(type, tileId, tilesetId);
		if (string.IsNullOrEmpty(tilesetId))
		{
			throw new Exception("Cannot cache without a tileset id");
		}

		var uriBuilder = new UriBuilder(uri);
		if (!string.IsNullOrEmpty(_accessToken))
		{
			string accessTokenQuery = "access_token=" + _accessToken;
			string mapsSkuToken = "sku=" + _getMapsSkuToken();
			string offlineRequest = "events=true";
			if (uriBuilder.Query.Length > 1)
			{
				uriBuilder.Query = uriBuilder.Query.Substring(1) + "&" + accessTokenQuery + "&" + mapsSkuToken + "&" + offlineRequest;
			}
			else
			{
				uriBuilder.Query = accessTokenQuery + "&" + mapsSkuToken + "&" + offlineRequest;
			}
		}

		string url = uriBuilder.ToString();
		return url;
	}

	private string GetUriForDataType(OfflineTileType type, CanonicalTileId tileId, string tilesetId)
	{
		switch (type)
		{
			case OfflineTileType.Imagery when tilesetId.StartsWith("mapbox://", StringComparison.Ordinal):
				return string.Format("{0}/{1}", MapUtils.NormalizeStaticStyleURL(tilesetId), tileId);
			case OfflineTileType.Imagery:
				return string.Format("{0}/{1}.png", MapUtils.TilesetIdToUrl(tilesetId), tileId);
			case OfflineTileType.RetinaImagery when tilesetId.StartsWith("mapbox://", StringComparison.Ordinal):
				return string.Format("{0}/{1}@2x", MapUtils.NormalizeStaticStyleURL(tilesetId), tileId);
			case OfflineTileType.RetinaImagery:
				return string.Format("{0}/{1}@2x.png", MapUtils.TilesetIdToUrl(tilesetId), tileId);
			case OfflineTileType.Elevation:
				return string.Format("{0}/{1}.pngraw", MapUtils.TilesetIdToUrl(tilesetId), tileId);
			case OfflineTileType.Vector:
				return string.Format("{0}/{1}.vector.pbf", MapUtils.TilesetIdToUrl(tilesetId), tileId);
			default:
				return "";
		}
	}

	private void HandleOfflineRequestResponse(Response obj)
	{
		_currentlyDownloadedFileCount++;
		Progress = (float) _currentlyDownloadedFileCount / _totalTileCount;
		ProgressUpdated(Progress);

		if (_currentlyDownloadedFileCount >= _totalTileCount)
		{
			_isDownloading = false;
			DownloadFinished(CurrentMapDownloadInfo);
		}
	}

	private IEnumerable<UnwrappedTileId> GetWithWebMerc(Vector2d min, Vector2d max, int zoom)
	{
		var swWebMerc = new Vector2d(Math.Max(min.x, -Constants.WebMercMax), Math.Max(min.y, -Constants.WebMercMax));
		var neWebMerc = new Vector2d(Math.Min(max.x, Constants.WebMercMax), Math.Min(max.y, Constants.WebMercMax));

		var swTile = Conversions.LatitudeLongitudeToTileId(swWebMerc.x, swWebMerc.y, zoom);
		var neTile = Conversions.LatitudeLongitudeToTileId(neWebMerc.x, neWebMerc.y, zoom);

		for (int x = swTile.X; x <= neTile.X; x++)
		{
			for (int y = neTile.Y; y <= swTile.Y; y++)
			{
				yield return new UnwrappedTileId(zoom, x, y);
			}
		}
	}
}