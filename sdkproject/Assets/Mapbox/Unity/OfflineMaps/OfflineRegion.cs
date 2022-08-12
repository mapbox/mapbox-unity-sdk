using System;
using Mapbox.Utils;
using UnityEngine;

[Serializable]
public class OfflineRegion
{
	public string Name;

	public Vector2d MinLatLng;
	public Vector2d MaxLatLng;

	[Range(0, 16)] public int MinZoom;
	[Range(0, 16)] public int MaxZoom;

	public string ElevationTilesetId;
	public string ImageTilesetId;
	public string VectorTilesetId;

	public OfflineRegion()
	{

	}

	public OfflineRegion(string mapName, Vector2d minLatLng, Vector2d maxLatLng, int minZoom, int maxZoom, string elevationTileset = "", string imageryTileset = "", string vectorTileset = "")
	{
		MinLatLng = minLatLng;
		MaxLatLng = maxLatLng;
		MinZoom = minZoom;
		MaxZoom = maxZoom;
		ElevationTilesetId = elevationTileset;
		ImageTilesetId = imageryTileset;
		VectorTilesetId = vectorTileset;
		Name = mapName;
	}

}