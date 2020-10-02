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
	public class FileCache : ITextureCache
	{
		private static string PersistantDataPath = Application.persistentDataPath;
		private static string CacheRootFolderName = "FileCache";
		public static string PersistantCacheRootFolderPath = Path.Combine(Application.persistentDataPath, CacheRootFolderName);
		private static string FileExtension = ".png";
		private Queue<string> _cacheFiles = new Queue<string>();
		
		public FileCache(uint maxCacheSize)
		{
#if MAPBOX_DEBUG_CACHE
			_className = this.GetType().Name;
#endif
			_maxCacheSize = maxCacheSize;
			_cachedResponses = new Dictionary<string, CacheItem>();
			_infosToSave = new Queue<InfoWrapper>();
			Runnable.Run(FileScheduler());

			if (!Directory.Exists(PersistantCacheRootFolderPath))
			{
				Directory.CreateDirectory(PersistantCacheRootFolderPath);
			}

			DirectoryInfo di = new DirectoryInfo(PersistantCacheRootFolderPath);	
			var _files = new List<FileInfo>();
			foreach (DirectoryInfo folder in di.GetDirectories())
			{
				foreach (var fileInfo in folder.GetFiles())
				{
					_files.Add(fileInfo);
				}
			}

			foreach (var file in _files.OrderBy(x => x.CreationTime))
			{
				_cacheFiles.Enqueue(file.FullName);
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

		public void Add(string mapId, CanonicalTileId tileId, CacheItem item, bool forceInsert)
		{

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

		// public void Add(string tilesetId, CanonicalTileId tileId, float[] heightData, bool forceInsert)
		// {
		// 	string folderPath = Path.Combine(PersistantCacheRootFolderPath, "ElevationData");
		// 	string filePath = Path.Combine(folderPath, tileId.ToString('_') + FileExtension);
		//
		// 	if (!Directory.Exists(folderPath))
		// 	{
		// 		Directory.CreateDirectory(folderPath);
		// 	}
		//
		// 	StringBuilder sb = new StringBuilder();
		//
		// 	for (int i = 0; i < heightData.Length; i++)
		// 	{
		// 		sb.Append(heightData[i] + ";");
		// 	}
		// 	File.WriteAllText(filePath, sb.ToString());
		// }

		// public void GetAsync(string tilesetId, CanonicalTileId tileId, Action<float[]> callback)
		// {
		// 	string folderPath = Path.Combine(PersistantCacheRootFolderPath, "ElevationData");
		// 	string filePath = Path.Combine(folderPath, tileId.ToString('_') + FileExtension);
		//
		// 	var elevations = new float[256 * 256];
		// 	var text = File.ReadAllText(filePath).Split(';');
		// 	for (int i = 0; i < text.Length; i++)
		// 	{
		// 		elevations[i] = float.Parse(text[i]);
		// 	}
		//
		// 	callback(elevations);
		// }

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
			string filePath = Path.Combine(folderPath, TileIdToFileName(info.TileId) + FileExtension);

			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			//byte[] bytes = info.TextureCacheItem.Texture2D.EncodeToPNG();
			
			using (FileStream sourceStream = new FileStream(filePath,
				FileMode.Create, FileAccess.Write, FileShare.Read,
				bufferSize: 4096, useAsync: false))
			{
				sourceStream.Write(info.TextureCacheItem.Data, 0, info.TextureCacheItem.Data.Length);
			}
			_cacheFiles.Enqueue(filePath);

			var txtFullName = Path.Combine(folderPath, TileIdToFileName(info.TileId) + ".txt");
			File.WriteAllLines(txtFullName,
				new string[]
				{
					info.TextureCacheItem.ETag,
					info.TextureCacheItem.ExpirationDate != null ? UnixTimestampUtils.To(info.TextureCacheItem.ExpirationDate.Value).ToString(CultureInfo.GetCultureInfo(DataSerializationCulture)) : string.Empty,
					info.TextureCacheItem.AddedToCacheTicksUtc.ToString()
				});
			_cacheFiles.Enqueue(txtFullName);

			CheckFileCoiuntLimit();

		}

		private void CheckFileCoiuntLimit()
		{
			if (_cacheFiles.Count > _maxCacheSize)
			{
				for (int i = 0; i < 20; i++)
				{
					var fileInfo = new FileInfo(_cacheFiles.Dequeue());
					fileInfo.Delete();
				}
			}
		}

		public void GetAsync(string mapId, CanonicalTileId tileId, Action<TextureCacheItem> callback)
		{
			string filePath = Path.Combine(PersistantCacheRootFolderPath, mapId + "/" + TileIdToFileName(tileId));

			if (File.Exists(filePath + FileExtension))
			{
				Runnable.Run(LoadImageCoroutine(filePath, callback));
			}
		}

		private IEnumerator LoadImageCoroutine(string filePath, Action<TextureCacheItem> callback)
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
					//we don't store metadata yet so only passing texture here
					//etag and last modified date should be added somewhere here
					string[] meta = File.ReadAllLines(filePath + ".txt");
					var textureCacheItem = new TextureCacheItem();
					textureCacheItem.Texture2D = DownloadHandlerTexture.GetContent(uwr);
					textureCacheItem.Texture2D.wrapMode = TextureWrapMode.Clamp;
					textureCacheItem.ETag = meta[0];
					textureCacheItem.ExpirationDate = UnixTimestampUtils.From(double.Parse(meta[1], CultureInfo.GetCultureInfo(DataSerializationCulture)));
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
