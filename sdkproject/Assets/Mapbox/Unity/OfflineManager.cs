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

	public Action<string> NewLog = (log) => { };
	public Action<float> ProgressUpdated = (progress) => { };
	public Action<OfflineMapDownloadInfo> DownloadFinished = (tileCount) => { };
	public float Progress = 0;

	private int _currentCoroutine;

	//public List<UnwrappedTileId> EstimatedTileList = new List<UnwrappedTileId>();
	private int _currentlyDownloadedFileCount;
	private int _currentlyRequestedFileCount;
	private int _totalTileCount;
	private bool _isDownloading;

	private readonly int OfflineTileLimit = 6000;
	private OfflineMapDownloadInfo _offlineMapDownloadInfo;

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

	public int GetOfflineDataSize(int mapId)
	{
		return _offlineCache.GetOfflineDataSize(mapId);
	}

	public int GetOfflineDataSize(string mapName)
	{
		return _offlineCache.GetOfflineDataSize(mapName);
	}

	public Dictionary<string, int> GetOfflineMapList()
	{
		return _offlineCache.GetOfflineMapList();
	}

	//OPERATIONS
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
			_offlineMapDownloadInfo = new OfflineMapDownloadInfo(cacheName, capacityLeft);
			_currentCoroutine = Runnable.Run(DownloadTileListCoroutine(cacheName, tiles.Take(capacityLeft).ToList()));
			return new OfflineMapResponse()
			{
				HasErrors = true,
				ErrorMessage = "Not enough capacity to download whole region. Downloading  first " + capacityLeft + " tiles."
			};
		}
		else
		{
			_offlineMapDownloadInfo = new OfflineMapDownloadInfo(cacheName, tiles.Count);
			_currentCoroutine = Runnable.Run(DownloadTileListCoroutine(cacheName, tiles));
			return new OfflineMapResponse()
			{
				HasErrors = false,
				ErrorMessage = "Downloading " + tiles.Count + " tiles."
			};
		}
	}

	public void DeleteOfflineMap(string offlineMapName)
	{
		NewLog("Starting to delete resources of offline map: " + offlineMapName);
		_offlineCache.DeleteOfflineMap(offlineMapName);
		NewLog("Finished deleting the resources of offline map: " + offlineMapName);
	}


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
					string eTag = string.Empty;
					if (!r.Headers.ContainsKey("ETag"))
					{
						Debug.LogWarningFormat("no 'ETag' header present in response for {0}", requestUrl);
					}
					else
					{
						eTag = r.Headers["ETag"];
					}

					_offlineMapDownloadInfo.SuccesfulTileDownloads++;
					_offlineCache.Add(
						tilesetId
						, tileId
						, new CacheItem()
						{
							Data = r.Data,
							ETag = eTag
						}
						, true
					);
					_offlineCache.MarkOffline(offlineMapId, tilesetId, tileId);
				}
				else
				{
					_offlineMapDownloadInfo.FailedTileDownloads++;
					_offlineMapDownloadInfo.FailedDownloadLogs.Add(string.Format("Download Failed. TileId: {0}, Tileset:{1}, Error:{2}",
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

	public List<OfflineTileInfo> EstimatedTileList(OfflineRegion region)
	{
		var estimatedTileList = new List<OfflineTileInfo>();
		try
		{
			if (string.IsNullOrEmpty(region.MinLatLng) || string.IsNullOrEmpty(region.MaxLatLng))
				return null;

			estimatedTileList.Clear();
			for (int currentZoom = region.MinZoom; currentZoom <= region.MaxZoom; currentZoom++)
			{
				foreach (var tileId in GetWithWebMerc(Conversions.StringToLatLon(region.MinLatLng), Conversions.StringToLatLon(region.MaxLatLng), currentZoom))
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

	public int EstimatedOfflineTileCount(OfflineRegion region)
	{
		var tileCount = 0;
		var tileDataCount = 0;
		try
		{
			if (string.IsNullOrEmpty(region.MinLatLng) || string.IsNullOrEmpty(region.MaxLatLng))
				return tileCount;

			tileCount = 0;
			for (int currentZoom = region.MinZoom; currentZoom <= region.MaxZoom; currentZoom++)
			{
				foreach (var tileId in GetWithWebMerc(Conversions.StringToLatLon(region.MinLatLng), Conversions.StringToLatLon(region.MaxLatLng), currentZoom))
				{
					tileCount++;
				}
			}

			if (!string.IsNullOrWhiteSpace(region.ElevationTilesetId))
			{
				tileDataCount += tileCount;
			}

			if (!string.IsNullOrWhiteSpace(region.ImageTilesetId))
			{
				tileDataCount += tileCount;
			}

			if (!string.IsNullOrWhiteSpace(region.VectorTilesetId))
			{
				tileDataCount += tileCount;
			}

			return tileDataCount;
		}
		catch (Exception e)
		{
			// ignored
			return tileCount;
		}
	}

	public void Stop()
	{
		Runnable.Stop(_currentCoroutine);
	}

	//PRIVATE
	private IEnumerator DownloadTileListCoroutine(string cacheName, List<OfflineTileInfo> tiles)
	{
		_currentlyDownloadedFileCount = 0;
		_currentlyRequestedFileCount = 0;
		_totalTileCount = tiles.Count;

		_isDownloading = true;
		NewLog(string.Format("Starting download of offline map '{0}' ({1} tiles)", cacheName, _totalTileCount));

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
			DownloadFinished(_offlineMapDownloadInfo);
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

public enum OfflineTileType
{
	Elevation,
	Imagery,
	RetinaImagery,
	Vector
}

public class OfflineMapResponse
{
	public bool HasErrors = false;
	public string ErrorMessage;
}

public class OfflineTileInfo
{
	public CanonicalTileId CanonicalTileId;
	public OfflineTileType Type;
	public string TilesetId;

	public OfflineTileInfo(UnwrappedTileId tileId, OfflineTileType type, string tilesetId)
	{
		CanonicalTileId = tileId.Canonical;
		Type = type;
		TilesetId = tilesetId;
	}
}

public class OfflineMapDownloadInfo
{
	public string MapName;
	public int InitializedTileCount;
	public int SuccesfulTileDownloads = 0;
	public int FailedTileDownloads = 0;
	public List<string> FailedDownloadLogs;

	public OfflineMapDownloadInfo(string name, int tilesCount)
	{
		MapName = name;
		InitializedTileCount = tilesCount;
		FailedDownloadLogs = new List<string>();
	}
}