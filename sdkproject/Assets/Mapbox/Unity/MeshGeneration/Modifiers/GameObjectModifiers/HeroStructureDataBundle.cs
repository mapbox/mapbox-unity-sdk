using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Mapbox.Editor;
using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using Mapbox.Unity.Utilities;
using Mapbox.VectorTile.Geometry;
using Mapbox.Unity.MeshGeneration.Interfaces;

[System.Serializable]
public class HeroStructureDataBundle
{
	public bool active = true;
	public GameObject prefab;
	[Geocode]
	public string latLon;
	public Vector2d latLon_vector2d;
	public double radius;

	private UnwrappedTileId _tileId;
	private bool _tileIdCached;

	private bool _spawned = false;

	public UnwrappedTileId TileId
	{
		get
		{
			return _tileId;
		}
		set
		{
			_tileId = value;
		}
	}

	public bool TileIdCached
	{
		get
		{
			return _tileIdCached;
		}
		set
		{
			_tileIdCached = value;
		}
	}

	public bool Spawned
	{
		get
		{
			return _spawned;
		}
		set
		{
			_spawned = value;
		}
	}

}
