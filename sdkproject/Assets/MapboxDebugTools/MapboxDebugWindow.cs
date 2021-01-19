using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Platform.Cache;
using Mapbox.Unity;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;
using UnityEditor;

public class MapboxDebugWindow : EditorWindow
{
	private int _currentTab;
	private string[] _tabList = new string[] {"DataFetcher", "UnityTiles" , "Memory Cache", "File Cache"};
	private static DataFetcherTabController _dataFetcherTabController;
	private static UnityTilesTabController _unityTilesTabController;
	private static MemoryTabController _memoryTabController;


	[MenuItem("Mapbox/Debug Window")]
	public static void ShowWindow()
	{
		ReadyObjects();
		EditorWindow.GetWindow(typeof(MapboxDebugWindow));
	}

	private static void ReadyObjects()
	{
		if (_dataFetcherTabController == null)
		{
			_dataFetcherTabController = new DataFetcherTabController();
		}
		if (_unityTilesTabController == null)
		{
			_unityTilesTabController = new UnityTilesTabController();
		}
		if (_memoryTabController == null)
		{
			_memoryTabController = new MemoryTabController();
		}
	}

	void OnGUI()
	{
		ReadyObjects();
		_currentTab = GUILayout.Toolbar (_currentTab, _tabList);
		switch (_currentTab)
		{
			case 0 : 
				_dataFetcherTabController.Draw();
				break;
			case 1 :
				_unityTilesTabController.Draw();
				break;
			case 2 :
				_memoryTabController.Draw();
				break;
		}
	}

	public void OnInspectorUpdate()
	{
		// This will only get called 10 times per second.
		Repaint();
	}
}

public class MemoryTabController
{
	private MemoryCache _memoryCache;
	private bool _cachedFold;
	private bool _fixedFold;
	private bool[] _cachedItemFolds;
	private bool[] _fixedItemFolds;
	private Vector2 _cachedScrollPos;
	private Vector2 _fixedScrollPos;
	private int _currentTab;
	private string[] _tabList = new string[2] {"Group By Tile", "Group By Tileset"};

	private bool[] _cacheByTileFolds;
	private bool[] _cacheByTilesetFolds;

	public MemoryTabController()
	{
		_memoryCache = MapboxAccess.Instance.CacheManager.GetMemoryCache();
	}

	public void Draw()
	{
		var _cachedList = _memoryCache.GetCachedItems;
		var _fixedList = _memoryCache.GetFixedItems;

		_cachedFold = EditorGUILayout.Foldout(_cachedFold, string.Format("Cached Items ({0})", _cachedList.Count));
		if (_cachedFold)
		{
			EditorGUI.indentLevel++;
			var index = 0;

			var _groupById = new Dictionary<CanonicalTileId, List<CacheItem>>();
			var _groupByTileset = new Dictionary<string, List<CacheItem>>();

			foreach (var item in _cachedList)
			{
				if (!_groupById.ContainsKey(item.Value.TileId))
				{
					_groupById.Add(item.Value.TileId, new List<CacheItem>() {item.Value});
				}
				else
				{
					_groupById[item.Value.TileId].Add(item.Value);
				}

				if (!_groupByTileset.ContainsKey(item.Value.TilesetId))
				{
					_groupByTileset.Add(item.Value.TilesetId, new List<CacheItem>() {item.Value});
				}
				else
				{
					_groupByTileset[item.Value.TilesetId].Add(item.Value);
				}
			}

			GUILayout.Label(string.Format("{0} tiles {1} tilesets", _groupById.Count, _groupByTileset), EditorStyles.miniLabel);

			_currentTab = GUILayout.Toolbar (_currentTab, _tabList);
			if (_currentTab == 0)
			{
				DrawCachesByTile(_groupById);
			}
			else if (_currentTab == 1)
			{
				DrawCachesByTileset(_groupByTileset);
			}


			EditorGUI.indentLevel--;
		}

		FixedFold(_fixedList);
	}

	private void FixedFold(Dictionary<int, CacheItem> _fixedList)
	{
		Array.Resize(ref _fixedItemFolds, _fixedList.Count);
		_fixedFold = EditorGUILayout.Foldout(_fixedFold, string.Format("Fixed Items ({0})", _fixedList.Count));
		if (_fixedFold)
		{
			EditorGUI.indentLevel++;
			var index = 0;

			EditorGUILayout.BeginHorizontal();
			_fixedScrollPos = EditorGUILayout.BeginScrollView(_fixedScrollPos, GUILayout.Height(500));

			foreach (var item in _fixedList)
			{
				_fixedItemFolds[index] = EditorGUILayout.Foldout(
					_fixedItemFolds[index],
					string.Format("{0} - {1}", item.Value.TileId, item.Value.TilesetId));
				if (_fixedItemFolds[index])
				{
					EditorGUI.indentLevel++;
					DrawCacheItem(item.Value);
					EditorGUI.indentLevel--;
				}

				index++;
			}

			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndHorizontal();
			EditorGUI.indentLevel--;
		}
	}

	private void DrawCachesByTile(Dictionary<CanonicalTileId,List<CacheItem>> groupById)
	{
		Array.Resize(ref _cachedItemFolds, groupById.Count + 1);
		EditorGUILayout.BeginHorizontal();
		_cachedScrollPos = EditorGUILayout.BeginScrollView(_cachedScrollPos, GUILayout.Height(500), GUILayout.ExpandWidth(true));

		var index = 0;
		EditorGUILayout.BeginVertical();
		foreach (var tile in groupById)
		{
			_cachedItemFolds[index] = EditorGUILayout.Foldout(
				_cachedItemFolds[index],
				string.Format("{0} ({1})", tile.Key, tile.Value.Count));
			if (_cachedItemFolds[index])
			{
				EditorGUI.indentLevel++;
				Array.Resize(ref _cacheByTileFolds, tile.Value.Count + 1);
				var index2 = 0;
				foreach (var item in tile.Value)
				{
					_cacheByTileFolds[index2] = EditorGUILayout.Foldout(
						_cacheByTileFolds[index2],
						string.Format("{0}", item.TilesetId));
					if (_cacheByTileFolds[index2])
					{
						EditorGUI.indentLevel++;
						DrawCacheItem(item);
						EditorGUI.indentLevel--;
					}
					index2++;
				}
				//DrawCacheItem(item);
				EditorGUI.indentLevel--;
			}
			index++;
		}
		EditorGUILayout.EndVertical();
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndHorizontal();
	}

	private void DrawCachesByTileset(Dictionary<string,List<CacheItem>> groupByTileset)
	{
		Array.Resize(ref _cachedItemFolds, groupByTileset.Count + 1);
		EditorGUILayout.BeginHorizontal();
		_cachedScrollPos = EditorGUILayout.BeginScrollView(_cachedScrollPos, GUILayout.Height(500), GUILayout.ExpandWidth(true));

		var index = 0;
		EditorGUILayout.BeginVertical();
		foreach (var tile in groupByTileset)
		{
			_cachedItemFolds[index] = EditorGUILayout.Foldout(
				_cachedItemFolds[index],
				string.Format("{0} ({1})", tile.Key, tile.Value.Count));
			if (_cachedItemFolds[index])
			{
				EditorGUI.indentLevel++;
				var index2 = 0;
				Array.Resize(ref _cacheByTileFolds, tile.Value.Count + 1);
				foreach (var item in tile.Value)
				{
					_cacheByTileFolds[index2] = EditorGUILayout.Foldout(
						_cacheByTileFolds[index2],
						string.Format("{0}", item.TileId));
					if (_cacheByTileFolds[index2])
					{
						EditorGUI.indentLevel++;
						DrawCacheItem(item);
						EditorGUI.indentLevel--;
					}
					index2++;
				}
				//DrawCacheItem(item);
				EditorGUI.indentLevel--;
			}
			index++;
		}
		EditorGUILayout.EndVertical();
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndHorizontal();
	}

	private void DrawCacheItem(CacheItem item)
	{
		var cacheItem = (TextureCacheItem) item;
		if (cacheItem != null)
		{
			EditorGUILayout.LabelField(string.Format("Tile Id: {0}", cacheItem.TileId), EditorStyles.label);
			EditorGUILayout.LabelField(string.Format("Tileset {0}", cacheItem.TilesetId), EditorStyles.label);
			EditorGUILayout.LabelField(string.Format("From {0}", cacheItem.From), EditorStyles.label);
			EditorGUILayout.LabelField(string.Format("Expiration {0}", cacheItem.ExpirationDate), EditorStyles.label);
			if (cacheItem.Texture2D != null)
			{
				EditorGUILayout.ObjectField(cacheItem.Texture2D, typeof(Texture2D));
			}
		}
	}
}

public class UnityTilesTabController
{
	private AbstractMap _map;
	public UnityTilesTabController()
	{
		_map = GameObject.FindObjectOfType<AbstractMap>();
	}

	private UnityTile _openTile;
	private Tile _openDataTile;

	public void Draw()
	{
		GUILayout.Label ("Unity Tiles", EditorStyles.boldLabel);
		var tiles = _map.MapVisualizer.ActiveTiles;
		GUILayout.Label (string.Format("{0} : {1}", "Unity Tile Count", tiles.Count), EditorStyles.miniLabel);

		GUILayout.Space(10);
		var zoomLevelDictionary = new Dictionary<int, int>();
		var dataTileDictionary = new Dictionary<Type, Tuple<int, int>>();
		foreach (var unityTile in tiles)
		{
			var zoom = unityTile.Value.CurrentZoom;
			if (!zoomLevelDictionary.ContainsKey(zoom))
			{
				zoomLevelDictionary.Add(zoom, 1);
			}
			else
			{
				zoomLevelDictionary[zoom]++;
			}

			var dataErrors = new List<string>();
			foreach (var tile in unityTile.Value.Tiles)
			{
				if (tile.HasError)
				{
					foreach (var exception in tile.Exceptions)
					{
						dataErrors.Add(exception.ToString());
					}
				}

				var type = tile.GetType();
				var isFromCacheAdd = tile.FromCache != CacheType.NoCache ? 1 : 0;
				if (!dataTileDictionary.ContainsKey(type))
				{
					dataTileDictionary.Add(type, new Tuple<int, int>(1, isFromCacheAdd));
				}
				else
				{
					dataTileDictionary[type] = new Tuple<int, int>(dataTileDictionary[type].Item1 + 1, dataTileDictionary[type].Item2 + isFromCacheAdd);
				}
			}

			var unityTileFold = EditorGUILayout.Foldout(_openTile == unityTile.Value, unityTile.Value.CanonicalTileId.ToString());
			if (unityTileFold)
			{
				EditorGUI.indentLevel++;
				_openTile = unityTile.Value;
				foreach (var dataTile in unityTile.Value.Tiles)
				{
					var dataTileFold = EditorGUILayout.Foldout(_openDataTile == dataTile, dataTile.GetType().ToString());
					if (dataTileFold)
					{
						EditorGUI.indentLevel++;
						_openDataTile = dataTile;
						if(dataTile is RasterTile)
						{
							if ((dataTile as RasterTile).Texture2D != null)
							{
								EditorGUILayout.ObjectField(
								(dataTile as RasterTile).Texture2D,
								typeof(Texture2D));
							}
						}
						EditorGUILayout.LabelField(string.Format("Tileset : {0}", dataTile.TilesetId), EditorStyles.miniLabel);
						EditorGUILayout.LabelField(string.Format("State : {0}", dataTile.CurrentState), EditorStyles.miniLabel);
						EditorGUILayout.LabelField(string.Format("Is Mapbox : {0}", dataTile.IsMapboxTile), EditorStyles.miniLabel);
						EditorGUILayout.LabelField(string.Format("From : {0}", dataTile.FromCache), EditorStyles.miniLabel);
						if (dataTile.HasError)
						{
							EditorGUILayout.LabelField(string.Format("Error : {0}", dataTile.Exceptions[0].Message), EditorStyles.miniLabel);
						}
						EditorGUI.indentLevel--;
					}
				}
				EditorGUI.indentLevel--;
			}
		}
		foreach (var entry in zoomLevelDictionary)
		{
			GUILayout.Label (string.Format("{1} tiles at zoom level {0}", entry.Key, entry.Value), EditorStyles.miniLabel);
		}
		foreach (var entry in dataTileDictionary)
		{
			GUILayout.Label (string.Format("{1} data tile of type {0}", entry.Key, entry.Value), EditorStyles.miniLabel);
		}
	}
}

public class DataFetcherTabController
{
	private DebuggerDataFetcherWrapper _dataFetcher;

	public DataFetcherTabController()
	{
		_dataFetcher = new DebuggerDataFetcherWrapper();
	}

	public void Draw()
	{
		var order = _dataFetcher.GetTileOrderQueue();
		var items = _dataFetcher.GetFetchInfoQueue();
		var activeRequests = _dataFetcher.GetActiveRequests();
		var activeRequestsLimit = _dataFetcher.GetActiveRequestLimit();

		GUILayout.Label ("Data Fetcher", EditorStyles.boldLabel);
		GUILayout.Label (string.Format("{0} : {1}/{2}", "Active Request Count", activeRequests.Count.ToString(), activeRequestsLimit), EditorStyles.miniLabel);
		GUILayout.Label (string.Format("{0} : {1}", "Order Queue Size", order.Count.ToString()), EditorStyles.miniLabel);
		GUILayout.Label (string.Format("{0} : {1}", "Item List Size", items.Count.ToString()), EditorStyles.miniLabel);
		GUILayout.Label (string.Format("{0} : {1}", "Order - Item (Cancelled)", order.Count - items.Count), EditorStyles.miniLabel);

		GUILayout.Space(10);
		var mapboxTiles = 0;
		var customTiles = 0;
		var tilesetDictionary = new Dictionary<string, int>();
		foreach (var item in items)
		{
			if (item.Value.RasterTile.IsMapboxTile)
			{
				mapboxTiles++;
			}
			else
			{
				customTiles++;
			}

			if (!tilesetDictionary.ContainsKey(item.Value.TilesetId))
			{
				tilesetDictionary.Add(item.Value.TilesetId, 1);
			}
			else
			{
				tilesetDictionary[item.Value.TilesetId]++;
			}
		}

		GUILayout.Label (string.Format("{0} : {1}", "Mapbox tiles", mapboxTiles.ToString()), EditorStyles.miniLabel);
		GUILayout.Label (string.Format("{0} : {1}", "Custom tiles", customTiles.ToString()), EditorStyles.miniLabel);

		GUILayout.Space(10);
		GUILayout.Label (string.Format("{0}", "Tilesets in list"), EditorStyles.miniLabel);
		foreach (var entry in tilesetDictionary)
		{
			GUILayout.Label (string.Format("{0} : {1}", entry.Key, entry.Value), EditorStyles.miniLabel);
		}
	}
}

public class DebuggerDataFetcherWrapper : DataFetcher
{
	public override void FetchData(DataFetcherParameters parameters)
	{

	}

	public Queue<int> GetTileOrderQueue()
	{
		return _tileOrder;
	}

	public Dictionary<int, FetchInfo> GetFetchInfoQueue()
	{
		return _tileFetchInfos;
	}

	public int GetActiveRequestLimit()
	{
		return _activeRequestLimit;
	}

	public Dictionary<int, Tile> GetActiveRequests()
	{
		return _activeRequests;
	}
}