using Mapbox.Map;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
		private static string FileExtension = ".png";

		public FileCache()
		{
#if MAPBOX_DEBUG_CACHE
			_className = this.GetType().Name;
#endif
			_cachedResponses = new Dictionary<string, CacheItem>();
			_infosToSave = new Queue<InfoWrapper>();
			Runnable.Run(FileScheduler());

			if (!Directory.Exists(PersistantCacheRootFolderPath))
			{
				Directory.CreateDirectory(PersistantCacheRootFolderPath);
			}
		}

#if MAPBOX_DEBUG_CACHE
		private string _className;
#endif
		private uint _maxCacheSize;
		private object _lock = new object();
		private Dictionary<string, CacheItem> _cachedResponses;

		private Queue<InfoWrapper> _infosToSave;
		private int _lastFileSaveFrame = 0;
		private string DataSerializationCulture = "en-US";

		public uint MaxCacheSize
		{
			get { return _maxCacheSize; }
		}

		public void ReInit()
		{
			_cachedResponses = new Dictionary<string, CacheItem>();
		}

		public void CheckIntegrity(List<tiles> tiles)
		{
//			var filePathsToDelete = new List<string>();
//			foreach (var tile in tiles)
//			{
//				if (!File.Exists(tile.tile_path))
//				{
//					filePathsToDelete.Add(tile.tile_path);
//				}
//			}

			DirectoryInfo di = new DirectoryInfo(PersistantCacheRootFolderPath);
			var _files = new List<FileInfo>();
			foreach (DirectoryInfo folder in di.GetDirectories())
			{
				foreach (var fileInfo in folder.GetFiles())
				{
					_files.Add(fileInfo);
				}
			}
		}

		public CacheItem Get(string tilesetId, CanonicalTileId tileId)
		{
			string key = tilesetId + "||" + tileId;

#if MAPBOX_DEBUG_CACHE
			string methodName = _className + "." + new System.Diagnostics.StackFrame().GetMethod().Name;
			UnityEngine.Debug.LogFormat("{0} {1}", methodName, key);
#endif

			lock (_lock)
			{
				if (!_cachedResponses.ContainsKey(key))
				{
					return null;
				}

				return _cachedResponses[key];
			}
		}

		public bool Exists(string mapId, CanonicalTileId tileId)
		{
			string filePath = Path.Combine(PersistantCacheRootFolderPath, mapId + "/" + TileIdToFileName(tileId) + FileExtension);
			return File.Exists(filePath);
		}

		public void Clear()
		{
			lock (_lock)
			{
				_cachedResponses.Clear();
			}
		}

		public void Clear(string tilesetId)
		{
			lock (_lock)
			{
				tilesetId += "||";
				List<string> toDelete = _cachedResponses.Keys.Where(k => k.Contains(tilesetId)).ToList();
				foreach (string key in toDelete)
				{
					_cachedResponses.Remove(key);
				}
			}
		}

		public void Add(string mapId, CanonicalTileId tileId, TextureCacheItem textureCacheItem, bool forceInsert)
		{
			var infoWrapper = new InfoWrapper(mapId, tileId, textureCacheItem);
			_infosToSave.Enqueue(infoWrapper);
		}

		private IEnumerator FileScheduler()
		{
			while (true)
			{
				if (_infosToSave.Count > 0)
				{
					SaveInfo(_infosToSave.Dequeue());
				}

				yield return null;
			}
		}

		private void SaveInfo(InfoWrapper info)
		{
			string folderPath = Path.Combine(PersistantCacheRootFolderPath, info.MapId);
			info.TextureCacheItem.FilePath = Path.Combine(folderPath, TileIdToFileName(info.TileId) + FileExtension);

			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			//byte[] bytes = info.TextureCacheItem.Texture2D.EncodeToPNG();
			
			using (FileStream sourceStream = new FileStream(info.TextureCacheItem.FilePath,
				FileMode.Create, FileAccess.Write, FileShare.Read,
				bufferSize: 4096, useAsync: false))
			{
				sourceStream.Write(info.TextureCacheItem.Data, 0, info.TextureCacheItem.Data.Length);
			}

			//We probably shouldn't delay this. It will only cause problems and it should be fast enough anyway
			FileSaved(info.MapId, info.TileId, info.TextureCacheItem);
		}

		public void GetAsync(string mapId, CanonicalTileId tileId, Action<TextureCacheItem> callback)
		{
			string filePath = Path.Combine(PersistantCacheRootFolderPath, mapId + "/" + TileIdToFileName(tileId));

			if (File.Exists(filePath + FileExtension))
			{
				Runnable.Run(LoadImageCoroutine(mapId, tileId, filePath, callback));
			}
		}

		private IEnumerator LoadImageCoroutine(string mapId, CanonicalTileId tileId, string filePath, Action<TextureCacheItem> callback)
		{
			using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture("file:///" + filePath + FileExtension))
			{
				yield return uwr.SendWebRequest();

				if (uwr.isNetworkError || uwr.isHttpError)
				{
					UnityEngine.Debug.LogErrorFormat(uwr.error);
				}
				else
				{
					var textureCacheItem = new TextureCacheItem();
					textureCacheItem.Texture2D = DownloadHandlerTexture.GetContent(uwr);
					textureCacheItem.Texture2D.wrapMode = TextureWrapMode.Clamp;
					textureCacheItem.FilePath = filePath;
					
					callback(textureCacheItem);
				}
			}
		}

		private string TileIdToFileName(CanonicalTileId tileId)
		{
			return tileId.Z.ToString() + "_" + tileId.X + "_" + tileId.Y;
		}

		public static void ClearFolder(string folderPath)
		{
			DirectoryInfo di = new DirectoryInfo(folderPath);

			foreach (FileInfo file in di.GetFiles())
			{
				file.Delete(); 
			}
		}
		
		public static void ClearStyle(string style)
		{
			ClearFolder(Path.Combine(PersistantCacheRootFolderPath, style));
		}
		
		public static void ClearAll()
		{
			DirectoryInfo di = new DirectoryInfo(PersistantCacheRootFolderPath);

			foreach (DirectoryInfo folder in di.GetDirectories())
			{
				ClearFolder(folder.FullName);
			}
		}

		private class InfoWrapper
		{
			public string MapId;
			public CanonicalTileId TileId;
			public TextureCacheItem TextureCacheItem;

			public InfoWrapper(string mapId, CanonicalTileId tileId, TextureCacheItem textureCacheItem)
			{
				MapId = mapId;
				TileId = tileId;
				TextureCacheItem = textureCacheItem;
			}
		}
	}
}
