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
using Mapbox.Unity.CustomLayer;
using Mapbox.Unity.DataFetching;
using Mapbox.Unity;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;


namespace Mapbox.Platform.Cache
{
	public interface IFileCache
	{
		event Action<CanonicalTileId, string, TextureCacheItem> FileSaved;
		void Add(CanonicalTileId tileId, string tilesetId, TextureCacheItem textureCacheItem, bool forceInsert);
		bool GetAsync(CanonicalTileId tileId, string tilesetId, bool isTextureNonreadable, Action<TextureCacheItem> callback);
		void TileDisposed(UnityTile tile, string tilesetId);
		bool Exists(CanonicalTileId tileId, string mapId);
		void Clear(string tilesetId);
		void ClearAll();
		void DeleteTileFile(string filePath);
		HashSet<string> GetFileList();
		void Clear();
	}

	public class FileCache : IFileCache
	{
		public event Action<CanonicalTileId, string, TextureCacheItem> FileSaved = (tileId, tilesetName, cacheItem) => { };

		private static string CacheRootFolderName = "FileCache";
		public static string PersistantCacheRootFolderPath = Path.Combine(Application.persistentDataPath, CacheRootFolderName);
		private static string FileExtension = "png";

		protected FileDataFetcher _fileDataFetcher;
		protected Dictionary<string, CacheItem> _cachedResponses;
		protected Queue<InfoWrapper> _infosToSave;
		protected HashSet<int> _infoKeys;
		protected Dictionary<string, string> MapIdToFolderNameDictionary;

		public FileCache()
		{
			_fileDataFetcher = new FileDataFetcher();
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

		public virtual bool Exists(CanonicalTileId tileId, string mapId)
		{
			string filePath = string.Format("{0}/{1}/{2}.{3}", PersistantCacheRootFolderPath, MapIdToFolderName(mapId), tileId.GenerateKey(mapId), FileExtension);
			return File.Exists(filePath);
		}

		public virtual void Clear(string tilesetId)
		{
			List<string> toDelete = _cachedResponses.Keys.Where(k => k.Contains(tilesetId)).ToList();
			foreach (string key in toDelete)
			{
				_cachedResponses.Remove(key);
			}
		}

		public virtual void Add(CanonicalTileId tileId, string tilesetId, TextureCacheItem textureCacheItem, bool forceInsert)
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

		public virtual bool GetAsync(CanonicalTileId tileId, string tilesetId, bool isTextureNonreadable, Action<TextureCacheItem> callback)
		{
			string filePath = string.Format("{0}/{1}/{2}", PersistantCacheRootFolderPath, MapIdToFolderName(tilesetId), tileId.GenerateKey(tilesetId));
			//Runnable.Run(LoadImageCoroutine(tileId, mapId, filePath, callback));

			var fullFilePath = string.Format("{0}.{1}", filePath, FileExtension);
			var fileExists = File.Exists(fullFilePath);
			if (fileExists)
			{

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
				fullFilePath = fullFilePath.Insert(0, "file://");
#endif
				fullFilePath = new Uri(fullFilePath).ToString();
				var tile = new FileImageTile(tileId, tilesetId, fullFilePath, isTextureNonreadable);
				_fileDataFetcher.FetchData(tile, tilesetId, tileId, false, callback);
			}

			return fileExists;
		}

		public void TileDisposed(UnityTile tile, string tilesetId)
		{
			_fileDataFetcher.CancelFetching(tile.UnwrappedTileId, tilesetId);
		}

		public virtual void ClearAll()
		{
			DirectoryInfo di = new DirectoryInfo(PersistantCacheRootFolderPath);

			foreach (DirectoryInfo folder in di.GetDirectories())
			{
				ClearFolder(folder.FullName);
			}
		}

		public virtual void DeleteTileFile(string filePath)
		{
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}
		}

		public virtual HashSet<string> GetFileList()
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

		public virtual void Clear()
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

		protected virtual void SaveInfo(InfoWrapper info)
		{
			if (info.TextureCacheItem == null || info.TextureCacheItem.Data == null)
			{
				return;
			}

			string folderPath = string.Format("{0}/{1}", PersistantCacheRootFolderPath, MapIdToFolderName(info.TilesetId));

			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}


			info.TextureCacheItem.FilePath = Path.GetFullPath(string.Format("{0}/{1}/{2}.{3}", PersistantCacheRootFolderPath, MapIdToFolderName(info.TilesetId), info.TileId.GenerateKey(info.TilesetId), FileExtension));

			MapboxAccess.Instance.TaskManager.AddTask(
				new TaskWrapper(info.TileId.GenerateKey(info.TilesetId, "FileCache"))
				{
					TileId = info.TileId,
					TilesetId = info.TilesetId,
					Action = () =>
					{
						FileStream sourceStream = new FileStream(info.TextureCacheItem.FilePath,
							FileMode.Create, FileAccess.Write, FileShare.Read,
							bufferSize: 4096, useAsync: false);

						sourceStream.Write(info.TextureCacheItem.Data, 0, info.TextureCacheItem.Data.Length);
						sourceStream.Close();

//this is not a good way to do it
// #if UNITY_EDITOR
// 					FileCacheDebugView.AddToLogs(string.Format("Saved {0, 20} - {1, -20}", info.TilesetId, info.TileId));
// #endif
					},
					ContinueWith = (t) =>
					{
						OnFileSaved(info.TileId, info.TilesetId, info.TextureCacheItem);
					},
#if UNITY_EDITOR
					Info = "FileCache.SaveInfo"
#endif
				}, 4);
		}

		protected virtual void OnFileSaved(CanonicalTileId infoTileId, string infoTilesetId, TextureCacheItem infoTextureCacheItem)
		{
			FileSaved(infoTileId, infoTilesetId, infoTextureCacheItem);
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

		private void ClearFolder(string folderPath)
		{
			DirectoryInfo di = new DirectoryInfo(folderPath);

			foreach (FileInfo file in di.GetFiles())
			{
				file.Delete();
			}

			di.Delete();
		}

		protected class InfoWrapper
		{
			public int Key;
			public string TilesetId;
			public CanonicalTileId TileId;
			public TextureCacheItem TextureCacheItem;

			public InfoWrapper(int key, string tilesetId, CanonicalTileId tileId, TextureCacheItem textureCacheItem)
			{
				Key = key;
				TilesetId = tilesetId;
				TileId = tileId;
				TextureCacheItem = textureCacheItem;
			}
		}

#if UNITY_EDITOR
		public int GetInfosToSaveListCount()
		{
			return _infosToSave.Count;
		}
#endif
	}

	public class EditorFileCache : FileCache
	{
		public Action<CanonicalTileId, string, TextureCacheItem, bool> TileAdded = (id, s, arg3, arg4) => { };
		public Action<CanonicalTileId, string> TileRequested = (id, s) => { };
		public Action<CanonicalTileId, string> SavingInfo = (id, s) => { };
		public Action<CanonicalTileId, string, TextureCacheItem> FileSaved = (s, id, item) => { };

		public int GetInfosToSaveListCount()
		{
			return _infosToSave.Count;
		}

		public override void Add(CanonicalTileId tileId, string tilesetId, TextureCacheItem textureCacheItem, bool forceInsert)
		{
			TileAdded(tileId, tilesetId, textureCacheItem, forceInsert);
			base.Add(tileId, tilesetId, textureCacheItem, forceInsert);
		}

		public override bool GetAsync(CanonicalTileId tileId, string tilesetId, bool isTextureNonreadable, Action<TextureCacheItem> callback)
		{
			TileRequested(tileId, tilesetId);
			return base.GetAsync(tileId, tilesetId, isTextureNonreadable, callback);
		}

		protected override void SaveInfo(InfoWrapper info)
		{
			SavingInfo(info.TileId, info.TilesetId);
			base.SaveInfo(info);
		}

		protected override void OnFileSaved(CanonicalTileId infoTileId, string infoTilesetId, TextureCacheItem infoTextureCacheItem)
		{
			FileSaved(infoTileId, infoTilesetId, infoTextureCacheItem);
			base.OnFileSaved(infoTileId, infoTilesetId, infoTextureCacheItem);
		}
	}
}
