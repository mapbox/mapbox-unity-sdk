using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Platform;
using Mapbox.Platform.Cache;
using Mapbox.Unity;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using NUnit;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Console = System.Console;
using Constants = Mapbox.Utils.Constants;

public class OfflineModeWindow : EditorWindow
{
	[SerializeField] public OfflineRegion OfflineRegion;

	private static bool _waitingToLoad;
	private Color _horizontalLineColor = new Color(.9f, .9f, .9f);
	private List<UnwrappedTileId> _estimatedTileList = new List<UnwrappedTileId>();

	private bool _downloadElevation;

	private bool _downloadImagery;
	private ImagerySourceType _selectedImageEnum;

	private bool _downloadVector;
	private VectorSourceType _selectedVectorEnum;

	private int _currentlyRequestedFileCount;
	private int _currentlyDownloadedFileCount = 0;
	private int _totalTileCount = 0;
	private bool _isDownloading = false;

	private int _currentCoroutine = 0;
	private string _logs;
	private Vector2 _scroll;
	private ListView listView;
	private string _offlineCacheName;
	private int _estimateTileCount;

	[MenuItem("Mapbox/Offline Maps")]
	static void Init()
	{
		var window = (OfflineModeWindow) EditorWindow.GetWindow(typeof(OfflineModeWindow));
		window.Show();
	}

	void OnGUI()
	{
		AreaInfos();
		DrawUILine(_horizontalLineColor, 1, 10);
		GUILayout.Space(10);
		TilesetInfos();

		if (GUI.changed)
		{
			_estimateTileCount = MapboxAccess.Instance.OfflineManager.EstimatedOfflineTileCount(OfflineRegion);
		}

		EditorGUILayout.LabelField("Estimate: " + _estimateTileCount + " tiles");

		DrawUILine(_horizontalLineColor, 1, 10);
		_offlineCacheName = EditorGUILayout.TextField("Offline Map Name", _offlineCacheName);

		if (GUILayout.Button("Download") && !string.IsNullOrEmpty(_offlineCacheName))
		{
			MapboxAccess.Instance.OfflineManager.CreateOfflineMap(_offlineCacheName, OfflineRegion);
		}

		if (GUILayout.Button("Remove"))
		{
			MapboxAccess.Instance.OfflineManager.DeleteOfflineMap(_offlineCacheName);
		}

		if (GUILayout.Button("Get Map List"))
		{
			var maps = MapboxAccess.Instance.OfflineManager.GetOfflineMapList();
			foreach (var map in maps)
			{
				NewLog(string.Format("Map named {0} has {1} tiles" + Environment.NewLine, map.Key, map.Value));
			}
		}

		if (GUILayout.Button("Ambient Tile Count"))
		{
			NewLog("Ambient cache tile count: " + MapboxAccess.Instance.OfflineManager.GetAmbientTileCount());
		}

		if (GUILayout.Button("Offline Tile Count"))
		{
			NewLog("Offline cache tile count: " + MapboxAccess.Instance.OfflineManager.GetOfflineTileCount());
		}

		if (GUILayout.Button("Offline Tile Count and Size"))
		{
			NewLog(string.Format("Offline cache tile count: {0} tiles", MapboxAccess.Instance.OfflineManager.GetOfflineTileCount(_offlineCacheName)));
			NewLog(string.Format("Offline cache data size: {0} bytes", MapboxAccess.Instance.OfflineManager.GetOfflineDataSize(_offlineCacheName)));
		}



		_scroll = EditorGUILayout.BeginScrollView(_scroll);
		_logs = EditorGUILayout.TextArea(_logs, GUILayout.Height(500));
		EditorGUILayout.EndScrollView();
	}

	public void Test()
	{


		var offlineRegion = new OfflineRegion()
		{
			MinLatLng = "37.7402769,-122.5984467",
			MaxLatLng = "37.9327687,-122.2179695",
			MinZoom = 6,
			MaxZoom = 11,
			ElevationTilesetId = "mapbox.terrain-rgb",
			ImageTilesetId = "mapbox://styles/mapbox/streets-v10"
		};
		MapboxAccess.Instance.OfflineManager.CreateOfflineMap("San Francisco", offlineRegion);


	}

	private void UpdateProgressBar(float f)
	{
		EditorUtility.DisplayProgressBar("Downloading", "Downloading " + (f * 100).ToString("F1") + "%", f);
	}

	private void DownloadFinished(OfflineMapDownloadInfo offlineMapDownloadInfo)
	{
		NewLog(string.Format(@"Finished download of offline map '{0}'.
Succesful tiles:{1}
Failed tiles: {2}
Logs: {3}",
			offlineMapDownloadInfo.MapName,
			offlineMapDownloadInfo.SuccesfulTileDownloads,
			offlineMapDownloadInfo.FailedTileDownloads,
			string.Join(Environment.NewLine, offlineMapDownloadInfo.FailedDownloadLogs)));
		EditorUtility.ClearProgressBar();
		_isDownloading = false;
	}

	private void AreaInfos()
	{
		GUILayout.Label("Area Settings", EditorStyles.boldLabel);
		GUILayout.Space(10);
		EditorGUILayout.LabelField("Minimum Longitude Latitude");
		OfflineRegion.MinLatLng = EditorGUILayout.TextField(OfflineRegion.MinLatLng);
		EditorGUILayout.LabelField("Maximum Longitude Latitude");
		OfflineRegion.MaxLatLng = EditorGUILayout.TextField(OfflineRegion.MaxLatLng);
		EditorGUILayout.LabelField("Minimum Zoom");
		OfflineRegion.MinZoom = EditorGUILayout.IntSlider(OfflineRegion.MinZoom, 0, 22);
		EditorGUILayout.LabelField("Maximum Zoom");
		OfflineRegion.MaxZoom = EditorGUILayout.IntSlider(OfflineRegion.MaxZoom, 0, 22);
		GUILayout.Space(10);
	}

	private void TilesetInfos()
	{
		GUILayout.Label("Tileset Settings", EditorStyles.boldLabel);
		GUILayout.Space(10);
		//ELEVATION DATA
		EditorGUILayout.BeginHorizontal();
		_downloadElevation = EditorGUILayout.Toggle(_downloadElevation, GUILayout.Width(16));
		OfflineRegion.ElevationTilesetId = _downloadElevation ? "mapbox.terrain-rgb" : "";
		EditorGUILayout.LabelField("Download Elevation Data");
		EditorGUILayout.EndHorizontal();

		GUILayout.Space(4);
		//IMAGE DATA
		EditorGUILayout.BeginHorizontal();
		_downloadImagery = EditorGUILayout.Toggle(_downloadImagery, GUILayout.Width(16));
		EditorGUILayout.LabelField("Download Image Data");
		EditorGUILayout.EndHorizontal();
		if (_downloadImagery)
		{
			EditorGUILayout.LabelField("Data Source");
			_selectedImageEnum = (ImagerySourceType) EditorGUILayout.EnumPopup(_selectedImageEnum);

			switch (_selectedImageEnum)
			{
				case ImagerySourceType.MapboxStreets:
				case ImagerySourceType.MapboxOutdoors:
				case ImagerySourceType.MapboxDark:
				case ImagerySourceType.MapboxLight:
				case ImagerySourceType.MapboxSatellite:
				case ImagerySourceType.MapboxSatelliteStreet:
					var sourcePropertyValue = MapboxDefaultImagery.GetParameters(_selectedImageEnum);
					OfflineRegion.ImageTilesetId = sourcePropertyValue.Id;
					GUI.enabled = false;
					EditorGUILayout.TextField(OfflineRegion.ImageTilesetId);
					GUI.enabled = true;
					break;
				case ImagerySourceType.Custom:
					EditorGUILayout.TextField(OfflineRegion.ImageTilesetId);
					break;
				case ImagerySourceType.None:
					break;
				default:
					break;
			}

			GUILayout.Space(10);
		}
		else
		{
			OfflineRegion.ImageTilesetId = "";
		}

		GUILayout.Space(4);
		//VECTOR DATA
		EditorGUILayout.BeginHorizontal();
		_downloadVector = EditorGUILayout.Toggle(_downloadVector, GUILayout.Width(16));
		EditorGUILayout.LabelField("Download Vector Data");
		EditorGUILayout.EndHorizontal();
		if (_downloadVector)
		{
			EditorGUILayout.LabelField("Data Source");
			_selectedVectorEnum = (VectorSourceType) EditorGUILayout.EnumPopup(_selectedVectorEnum);

			switch (_selectedVectorEnum)
			{
				case VectorSourceType.MapboxStreets:
				case VectorSourceType.MapboxStreetsV8:
				case VectorSourceType.MapboxStreetsWithBuildingIds:
				case VectorSourceType.MapboxStreetsV8WithBuildingIds:
					var sourcePropertyValue = MapboxDefaultVector.GetParameters(_selectedVectorEnum);
					OfflineRegion.VectorTilesetId = sourcePropertyValue.Id;
					GUI.enabled = false;
					EditorGUILayout.TextField(OfflineRegion.VectorTilesetId);
					GUI.enabled = true;
					break;
				case VectorSourceType.Custom:
					EditorGUILayout.TextField(OfflineRegion.VectorTilesetId);
					break;
				case VectorSourceType.None:
					break;
				default:
					break;
			}

			GUILayout.Space(20);
		}
		else
		{
			OfflineRegion.VectorTilesetId = "";
		}
	}

	public static void DrawUILine(Color color, int thickness = 1, int padding = 10)
	{
		Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
		r.height = thickness;
		r.y += padding / 2;
		r.x -= 2;
		r.width += 6;
		EditorGUI.DrawRect(r, color);
	}


	protected void OnEnable()
	{
		var data = EditorPrefs.GetString("MapboxOfflineMaps", JsonUtility.ToJson(this, false));
		JsonUtility.FromJsonOverwrite(data, this);
		_estimateTileCount = MapboxAccess.Instance.OfflineManager.EstimatedOfflineTileCount(OfflineRegion);
		MapboxAccess.Instance.OfflineManager.ProgressUpdated += UpdateProgressBar;
		MapboxAccess.Instance.OfflineManager.DownloadFinished += DownloadFinished;
		MapboxAccess.Instance.OfflineManager.NewLog += NewLog;
	}

	protected void OnDisable()
	{
		MapboxAccess.Instance.OfflineManager.ProgressUpdated -= UpdateProgressBar;
		MapboxAccess.Instance.OfflineManager.DownloadFinished -= DownloadFinished;
		MapboxAccess.Instance.OfflineManager.NewLog -= NewLog;
		MapboxAccess.Instance.OfflineManager.Stop();
		var data = JsonUtility.ToJson(this, false);
		EditorPrefs.SetString("MapboxOfflineMaps", data);

		_isDownloading = false;
		EditorUtility.ClearProgressBar();

		if (_currentCoroutine > 0)
		{
			Runnable.Stop(_currentCoroutine);
		}
	}

	private void NewLog(string log)
	{
		_logs += log + Environment.NewLine;
		_logs += "-------------------------" + Environment.NewLine;
	}

}

[Serializable]
public class OfflineRegion
{
	public string MinLatLng = "";
	public string MaxLatLng = "";

	[Range(0, 16)] public int MinZoom;
	[Range(0, 16)] public int MaxZoom;

	public string ElevationTilesetId;
	public string ImageTilesetId;
	public string VectorTilesetId;
}