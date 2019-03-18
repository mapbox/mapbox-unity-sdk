using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.UI;

public class TileCacher : MonoBehaviour
{
    [Header("Area Data")]
    public List<string> Points;
    public string TerrainMapId;
    public string ImageMapId;
    public string VectorMapId;
    public int ZoomLevel;

    [Header("Output")]
    public float Progress;
    [TextArea(10,20)]
    public string Log;

    private TerrainDataFetcher TerrainFetcher;
    private ImageDataFetcher ImageFetcher;
    private VectorDataFetcher VectorFetcher;
    private int _tileCountToFetch;
    [SerializeField] private int _currentProgress;
    private Vector2 _anchor;
    [SerializeField] private Transform _canvas;

    private void Start()
    {
        TerrainFetcher = new TerrainDataFetcher();
        TerrainFetcher.DataRecieved += TerrainDataReceived;
        TerrainFetcher.FetchingError += TerrainDataError;

        ImageFetcher = new ImageDataFetcher();
        ImageFetcher.DataRecieved += ImageDataReceived;
        ImageFetcher.FetchingError += ImageDataError;

        VectorFetcher = new VectorDataFetcher();
        VectorFetcher.DataRecieved += VectorDataReceived;
        VectorFetcher.FetchingError += VectorDataError;
    }


    [ContextMenu("Download Tiles")]
    public void PullTiles()
    {
	    Progress = 0;
	    _tileCountToFetch = 0;
	    _currentProgress = 0;

        var pointMeters = new List<UnwrappedTileId>();
        foreach (var point in Points)
        {
            var pointVector = Conversions.StringToLatLon(point);
            var pointMeter = Conversions.LatitudeLongitudeToTileId(pointVector.x, pointVector.y, ZoomLevel);
            pointMeters.Add(pointMeter);
        }

        var minx = int.MaxValue;
        var maxx = int.MinValue;
        var miny = int.MaxValue;
        var maxy = int.MinValue;

        foreach (var meter in pointMeters)
        {
            if (meter.X < minx)
            {
                minx = meter.X;
            }

            if (meter.X > maxx)
            {
                maxx = meter.X;
            }

            if (meter.Y < miny)
            {
                miny = meter.Y;
            }

            if (meter.Y > maxy)
            {
                maxy = meter.Y;
            }
        }

        _tileCountToFetch = (maxx - minx) * (maxy - miny) * 3;
        _anchor = new Vector2((maxx + minx) / 2, (maxy + miny) / 2);
        Log += string.Format("{0}, {1}, {2}, {3}", minx, maxx, miny, maxy);
        StartCoroutine(StartPulling(minx, maxx, miny, maxy));
    }

    private IEnumerator StartPulling(int minx, int maxx, int miny, int maxy)
    {
        for (int i = minx; i < maxx; i++)
        {
            for (int j = miny; j < maxy; j++)
            {
                TerrainFetcher.FetchData(new TerrainDataFetcherParameters()
                {
                    canonicalTileId = new CanonicalTileId(ZoomLevel, i, j),
                    mapid = TerrainMapId,
                    tile = null
                });

                ImageFetcher.FetchData(new ImageDataFetcherParameters()
                {
                    canonicalTileId = new CanonicalTileId(ZoomLevel, i, j),
                    mapid = ImageMapId,
                    tile = null
                });

                VectorFetcher.FetchData(new VectorDataFetcherParameters()
                {
                    canonicalTileId = new CanonicalTileId(ZoomLevel, i, j),
                    mapid = VectorMapId,
                    tile = null
                });

                yield return null;
            }
        }
    }

    #region Fetcher Events
    private void VectorDataError(UnityTile arg1, VectorTile arg2, TileErrorEventArgs arg3)
    {
        Log += (string.Format("Vector data fetching failed for {0}\r\n",  arg2.Id));
    }

    private void VectorDataReceived(UnityTile arg1, VectorTile arg2)
    {
        _currentProgress++;
	    Progress = (float)_currentProgress / _tileCountToFetch * 100 ;
    }

    private void ImageDataError(UnityTile arg1, RasterTile arg2, TileErrorEventArgs arg3)
    {
        Log += (string.Format("Image data fetching failed for {0}\r\n",  arg2.Id));
    }

    private void ImageDataReceived(UnityTile arg1, RasterTile arg2)
    {
        _currentProgress++;
	    Progress = (float)_currentProgress / _tileCountToFetch * 100;
        RenderImagery(arg2);
    }

    private void TerrainDataError(UnityTile arg1, RawPngRasterTile arg2, TileErrorEventArgs arg3)
    {
        Log += (string.Format("Vector data fetching failed for {0}\r\n",  arg2.Id));
    }

    private void TerrainDataReceived(UnityTile arg1, RawPngRasterTile arg2)
    {
        _currentProgress++;
	    Progress = (float)_currentProgress / _tileCountToFetch * 100;
    }
    #endregion

    private void RenderImagery(RasterTile rasterTile)
    {
        var go = new GameObject("image");
        go.transform.SetParent(_canvas);
        var img = go.AddComponent<RawImage>();
        img.rectTransform.sizeDelta = new Vector2(10,10);
        var txt = new Texture2D(256,256);
        txt.LoadImage(rasterTile.Data);
        img.texture = txt;
        (go.transform as RectTransform).anchoredPosition = new Vector2((float)(rasterTile.Id.X - _anchor.x) * 10, (float)-(rasterTile.Id.Y - _anchor.y) * 10);
    }
}
