using Mapbox.Map;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CustomImageLayerSample;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;


namespace Mapbox.Platform.Cache
{
	public class FileCache
	{
		public Action<string, CanonicalTileId, TextureCacheItem> FileSaved = (tilesetName, tileId, cacheItem) => { };

		private static string CacheRootFolderName = "FileCache";
		public static string PersistantCacheRootFolderPath = Path.Combine(Application.persistentDataPath, CacheRootFolderName);
		private static string FileExtension = "png";

		private FileDataFetcher _fileDataFetcher;
		private Dictionary<string, CacheItem> _cachedResponses;

		private Queue<InfoWrapper> _infosToSave;
		private HashSet<int> _infoKeys;

		private Dictionary<string, string> MapIdToFolderNameDictionary;

		public FileCache(IFileSource fileSource)
		{
			_fileDataFetcher = new FileDataFetcher(fileSource);
			_cachedResponses = new Dictionary<string, CacheItem>();
			_infosToSave = new Queue<InfoWrapper>();
			_infoKeys = new HashSet<int>();
			MapIdToFolderNameDictionary = new Dictionary<string, string>();
			Runnable.Run(FileScheduler());

			if (!Directory.Exists(PersistantCacheRootFolderPath))
			{
				Directory.CreateDirectory(PersistantCacheRootFolderPath);
			}
		}

		public bool Exists(string mapId, CanonicalTileId tileId)
		{
			string filePath = string.Format("{0}/{1}/{2}.{3}", PersistantCacheRootFolderPath, MapIdToFolderName(mapId), tileId.GenerateKey(mapId), FileExtension);
			return File.Exists(filePath);
		}

		public void Clear(string tilesetId)
		{
			List<string> toDelete = _cachedResponses.Keys.Where(k => k.Contains(tilesetId)).ToList();
			foreach (string key in toDelete)
			{
				_cachedResponses.Remove(key);
			}
		}

		public void Add(string tilesetId, CanonicalTileId tileId, TextureCacheItem textureCacheItem, bool forceInsert)
		{
			var key = tileId.GenerateKey(tilesetId);
			if (_infoKeys.Contains(key))
			{
				if(Debug.isDebugBuild) Debug.Log(string.Format("This image file ({0}) is already queued for saving. Removing (and destroying) first instance, adding new one.", key));
				//we can't find the first info object here in O(n) but we are removing it from _infoKeys
				//so we can find it and destroy the texture inside on update below
				_infoKeys.Remove(key);
			}

			_infoKeys.Add(key);
			var infoWrapper = new InfoWrapper(key, tilesetId, tileId, textureCacheItem);
			_infosToSave.Enqueue(infoWrapper);
		}

		public void GetAsync(string tilesetId, CanonicalTileId tileId, Action<TextureCacheItem> callback)
		{
			string filePath = string.Format("{0}/{1}/{2}", PersistantCacheRootFolderPath, MapIdToFolderName(tilesetId), tileId.GenerateKey(tilesetId));
			//Runnable.Run(LoadImageCoroutine(tileId, mapId, filePath, callback));

			var fullFilePath = string.Format("{0}.{1}", filePath, FileExtension);
			if (File.Exists(fullFilePath))
			{
				var tile = new FileImageTile(tileId, tilesetId, fullFilePath);
				_fileDataFetcher.FetchData(tile, tilesetId, tileId, false, callback);
			}
			else
			{
				Debug.Log("Requested file not found");
				callback(null);
			}
		}

		public void ClearStyle(string style)
		{
			ClearFolder(Path.Combine(PersistantCacheRootFolderPath, style));
		}

		public void ClearAll()
		{
			DirectoryInfo di = new DirectoryInfo(PersistantCacheRootFolderPath);

			foreach (DirectoryInfo folder in di.GetDirectories())
			{
				ClearFolder(folder.FullName);
			}
		}

		public void DeleteTileFile(string filePath)
		{
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}
		}

		public HashSet<string> GetFileList()
		{
			var pathList = new HashSet<string>();
			if (Directory.Exists(PersistantCacheRootFolderPath))
			{
				var dir = Directory.GetDirectories(FileCache.PersistantCacheRootFolderPath);
				foreach (var rasterDirectory in dir)
				{
					var directoryInfo = new DirectoryInfo(rasterDirectory);
					var files = directoryInfo.GetFiles();
					foreach (var fileInfo in files)
					{
						pathList.Add(fileInfo.FullName);
					}
				}
			}

			return pathList;
		}

		private IEnumerator FileScheduler()
		{
			while (true)
			{
				SaveFromQueue();
				yield return null;
			}
		}

		private void SaveFromQueue()
		{
			if (_infosToSave.Count > 0)
			{
				var info = _infosToSave.Dequeue();
				if (_infoKeys.Contains(info.Key))
				{
					_infoKeys.Remove(info.Key);
					SaveInfo(info);
				}
				else
				{
					//this object was removed from queue, probably updates
					//texture inside should be destroyed
					GameObject.Destroy(info.TextureCacheItem.Texture2D);
					info.TextureCacheItem.Data = null;
					if (Debug.isDebugBuild) Debug.Log("Destroying the first copy of the same tile in file save queue");
				}
			}
		}

		private void SaveInfo(InfoWrapper info)
		{
			if (info.TextureCacheItem == null || info.TextureCacheItem.Data == null)
			{
				return;
			}

			string folderPath = string.Format("{0}/{1}", PersistantCacheRootFolderPath, MapIdToFolderName(info.MapId));

			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}


			info.TextureCacheItem.FilePath = Path.GetFullPath(string.Format("{0}/{1}/{2}.{3}", PersistantCacheRootFolderPath, MapIdToFolderName(info.MapId), info.TileId.GenerateKey(info.MapId), FileExtension));

			FileStream sourceStream = new FileStream(info.TextureCacheItem.FilePath,
				FileMode.Create, FileAccess.Write, FileShare.Read,
				bufferSize: 4096, useAsync: false);

			Task t = sourceStream
				.WriteAsync(info.TextureCacheItem.Data, 0, info.TextureCacheItem.Data.Length)
				.ContinueWith((task) =>
				{
					sourceStream.Close();
					FileSaved(info.MapId, info.TileId, info.TextureCacheItem);
				});
		}

		private string MapIdToFolderName(string mapId)
		{
			if (MapIdToFolderNameDictionary.ContainsKey(mapId))
			{
				return MapIdToFolderNameDictionary[mapId];
			}
			var folderName = mapId;
			var chars = Path.GetInvalidFileNameChars();
			foreach (Char c in chars)
			{
				folderName = folderName.Replace(c, '-');
			}
			MapIdToFolderNameDictionary.Add(mapId, folderName);
			return folderName;
		}

		public void Clear()
		{
			if (_cachedResponses != null)
			{
				_cachedResponses.Clear();
			}
			else
			{
				_cachedResponses = new Dictionary<string, CacheItem>();
			}
		}

		private void ClearFolder(string folderPath)
		{
			DirectoryInfo di = new DirectoryInfo(folderPath);

			foreach (FileInfo file in di.GetFiles())
			{
				file.Delete();
			}

			di.Delete();
		}

		private class InfoWrapper
		{
			public int Key;
			public string MapId;
			public CanonicalTileId TileId;
			public TextureCacheItem TextureCacheItem;

			public InfoWrapper(int key, string mapId, CanonicalTileId tileId, TextureCacheItem textureCacheItem)
			{
				Key = key;
				MapId = mapId;
				TileId = tileId;
				TextureCacheItem = textureCacheItem;
			}
		}
	}

	public class FileDataFetcher : ImageDataFetcher
	{
		public FileDataFetcher(IFileSource fileSource) : base(fileSource)
		{

		}

		public void FetchData(FileImageTile tile, string tilesetId, CanonicalTileId tileId, bool useRetina, Action<TextureCacheItem> callback)
		{
			EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, String.Empty)
			{
				Callback = () =>
				{
					FetchingCallback(tileId, tile, null);

					//file cache is using datafetcher queue and items in queue might get cancelled at any time
					//cancelled counts as an error, if a file fetching order is cancelled
					//setting error flag here is important so cache manager won't follow up with
					//sql queries for file details like etag

					var textureCacheItem = new TextureCacheItem
					{
						TileId = tileId,
						TilesetId = tilesetId,
						Texture2D = tile.Texture2D,
						FilePath = tile.FilePath,
						HasError = tile.CurrentState == Tile.State.Canceled
					};

					tile.Clear();
					callback(textureCacheItem);
				}
			});
		}

		protected override void FetchingCallback(CanonicalTileId tileId, RasterTile rasterTile, UnityTile unityTile = null)
		{
			if (unityTile != null && !unityTile.ContainsDataTile(rasterTile))
			{
				return;
			}

			if (rasterTile.HasError)
			{
				FetchingError(unityTile, rasterTile, new TileErrorEventArgs(tileId, rasterTile.GetType(), unityTile, rasterTile.Exceptions));
			}
			else
			{

				rasterTile.ExtractTextureFromRequest();

#if UNITY_EDITOR
				if (rasterTile.Texture2D != null)
				{
					rasterTile.Texture2D.name = string.Format("{0}_{1}", tileId.ToString(), rasterTile.TilesetId);
				}
#endif

				if (rasterTile.StatusCode != 304) //NOT MODIFIED
				{
					TextureReceived(unityTile, rasterTile);
				}
			}
		}
	}
}
