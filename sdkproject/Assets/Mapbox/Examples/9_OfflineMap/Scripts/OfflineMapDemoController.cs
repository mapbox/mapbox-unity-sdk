using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Mapbox.Unity;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Environment = System.Environment;

public class OfflineMapDemoController : MonoBehaviour
{
    public AbstractMap Map;

    public GameObject MinLatLngGo;
    public GameObject MaxLatLngGo;

    public Transform OfflineMapListContainer;
    public GameObject OfflineMapListPrefab;

    public Text MinLatLngText;
    public Text MaxLatLngText;
    public Text TileCountText;
    public InputField ReportField;

    public InputField MapName;

    public Slider MinZoomSlider;
    public Slider MaxZoomSlider;

    public Toggle ElevationToggle;
    public Toggle ImageryToggle;
    public Toggle VectorToggle;

    public Button DownloadButton;
    public Button CancelButton;

    public GameObject ProgressPanel;
    public Slider ProgressBar;

    private bool _isDownloading = false;
    private Camera _camera;
    private Plane _plane;
    private Vector2d _minLatLng;
    private Vector2d _maxLatLng;
    private int _minZoom;
    private int _maxZoom;

    void Start()
    {
        _camera = Camera.main;
        _plane = new Plane(Vector3.up, Vector3.zero);

        DownloadButton.onClick.AddListener(DownloadMap);
        CancelButton.onClick.AddListener(CancelDownload);
        MapboxAccess.Instance.OfflineManager.ProgressUpdated += DownloadProgressChanged;
        MapboxAccess.Instance.OfflineManager.DownloadFinished += DownloadFinished;

        UpdateMapList();
    }

    void Update()
    {
        if (Map != null)
        {
            Ray ray = _camera.ScreenPointToRay(MinLatLngGo.transform.position);
            var distance = 0f;
            if (_plane.Raycast(ray, out distance))
            {
                _minLatLng = Map.WorldToGeoPosition(ray.GetPoint(distance));
                MinLatLngText.text = _minLatLng.ToString();
            }

            ray = _camera.ScreenPointToRay(MaxLatLngGo.transform.position);
            if (_plane.Raycast(ray, out distance))
            {
                _maxLatLng = Map.WorldToGeoPosition(ray.GetPoint(distance));
                MaxLatLngText.text = _maxLatLng.ToString();
            }
        }

        _minZoom = (int) MinZoomSlider.value;
        _maxZoom = Mathf.Max(_minZoom, (int) MaxZoomSlider.value);
        _minZoom = Mathf.Min(_minZoom, _maxZoom);
        MinZoomSlider.value = _minZoom;
        MaxZoomSlider.value = _maxZoom;

        var tileCountInRegion = MapboxAccess.Instance.OfflineManager.EstimatedTileCount(_minLatLng, _maxLatLng, (int) MinZoomSlider.value, (int) MaxZoomSlider.value);

        var tilesetCount = 0;
        if (ElevationToggle.isOn) tilesetCount++;
        if (ImageryToggle.isOn) tilesetCount++;
        if (VectorToggle.isOn) tilesetCount++;

        TileCountText.text = string.Format("Total of {0} tiles", tileCountInRegion * tilesetCount);

        ProgressPanel.SetActive(_isDownloading);
        DownloadButton.interactable = !_isDownloading;
    }

    private void UpdateMapList()
    {
        foreach (Transform button in OfflineMapListContainer.transform)
        {
            Destroy(button.gameObject);
        }

        var maps = MapboxAccess.Instance.OfflineManager.GetOfflineMapList();
        foreach (var map in maps)
        {
            var go = Instantiate(OfflineMapListPrefab, OfflineMapListContainer);
            go.GetComponentInChildren<Text>().text = string.Format("{0} ({1} tiles)", map.Key, map.Value);
            go.GetComponentInChildren<Button>().onClick.AddListener(() =>
            {
                MapboxAccess.Instance.OfflineManager.DeleteOfflineMap(map.Key);
                UpdateMapList();
            });
        }
    }

    private void DownloadProgressChanged(float value)
    {
        ProgressBar.value = value;
    }

    private void DownloadFinished(OfflineMapDownloadInfo info)
    {
        _isDownloading = false;
        ReportField.text = string.Format(@"Finished download of offline map '{0}'.
Succesful tiles:{1}
Failed tiles: {2}
Logs: {3}",
            info.MapName,
            info.SuccesfulTileDownloads,
            info.FailedTileDownloads,
            string.Join(Environment.NewLine, info.FailedDownloadLogs));
        UpdateMapList();
    }

    //This is how offline map procedure should work
    //Define a region, set a name, start download. Then you can track progress and state using events
    private void DownloadMap()
    {
        if (string.IsNullOrEmpty(MapName.text))
        {
            ReportField.text = "Invalid map name.";
            return;
        }

        //null or empty tilesetId means that data type will be skipped
        var elevationTilesetId = ElevationToggle.isOn ? "mapbox.terrain-rgb" : null;
        var imageryTilesetId = ImageryToggle.isOn ? "mapbox.satellite" : null;
        var vectorTilesetId = VectorToggle.isOn ? "mapbox.mapbox-streets-v8" : null;

        var region = new OfflineRegion(
            MapName.text,
            _minLatLng,
            _maxLatLng,
            _minZoom,
            _maxZoom,
            elevationTilesetId,
            imageryTilesetId,
            vectorTilesetId);

        var response = MapboxAccess.Instance.OfflineManager.CreateOfflineMap(region.Name, region);

        if (response.HasErrors) //download haven't started for some reason
        {
            ReportField.text = response.ErrorMessage;
            _isDownloading = false;
        }
        else //download started, you can use events to track progress and state
        {
            _isDownloading = true;
        }

        ReportField.text = string.Format("Started downloading {0} tiles under name {1}.", MapboxAccess.Instance.OfflineManager.CurrentMapDownloadInfo.InitializedTileCount, MapboxAccess.Instance.OfflineManager.CurrentMapDownloadInfo.MapName);
    }

    public void CancelDownload()
    {
        var name = MapboxAccess.Instance.OfflineManager.CurrentMapDownloadInfo.MapName;
        MapboxAccess.Instance.OfflineManager.Stop();
        _isDownloading = false;
        ReportField.text = string.Format("{0} download cancelled", name);
        UpdateMapList();
    }
}
