using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mapbox.Unity.Map;
using Mapbox.Utils;
using Mapbox.Unity.Utilities;

public enum MapType
{
	AtTileCenter,
	AtLocationCenter,
	Zoomable,
	WorldScale,
}



public enum MapDrawType
{
	// Camera bounds will mean 
	// CameraBoundsTile Provider for all cases except Zoomable MapType
	// For Zoomable map - QuadTreeTileProvider.
	CameraBounds,
	RangeAroundCenter,
	RangeAroundTransform,
	VirtualGlobe,
}

[System.Serializable]
public class MapOptions
{
	public MapType type = MapType.AtTileCenter;
	public MapDrawType tileProviderType = MapDrawType.RangeAroundCenter;
}

public enum MapLayerType
{
	Imagery,
	Elevation,
	Vector
}

public enum VectorPrimitiveType
{
	Point,
	Line,
	Polygon,
	Polyline,
}

public abstract class LayerProperties
{

}


// Layer Interfaces
public interface ILayer
{
	MapLayerType LayerType { get; }
	bool IsLayerActive { get; set; }
	string LayerSource { get; set; }

	LayerProperties LayerProperty { get; set; }
}

public interface ITerrainLayer : ILayer
{

}

public interface IImageryLayer : ILayer
{

}

public interface IVectorDataLayer : ILayer
{
	VectorPrimitiveType PrimitiveType { get; set; }
}

// Layer Concrete Implementation. 

public class TerrainLayer : ITerrainLayer
{
	public MapLayerType LayerType
	{
		get
		{
			return MapLayerType.Elevation;
		}
	}

	public bool IsLayerActive
	{
		get;
		set;
	}
	public string LayerSource
	{
		get;
		set;
	}
	public LayerProperties LayerProperty
	{
		get;
		set;
	}
}

public class ImageryLayer : IImageryLayer
{
	public MapLayerType LayerType
	{
		get
		{
			return MapLayerType.Imagery;
		}
	}

	public bool IsLayerActive
	{
		get;
		set;
	}
	public string LayerSource
	{
		get;
		set;
	}
	public LayerProperties LayerProperty
	{
		get;
		set;
	}
}

public class VectorLayer : IVectorDataLayer
{
	public MapLayerType LayerType
	{
		get
		{
			return MapLayerType.Vector;
		}
	}

	public bool IsLayerActive
	{
		get;
		set;
	}

	public string LayerSource
	{
		get;
		set;
	}

	public LayerProperties LayerProperty
	{
		get;
		set;
	}

	public VectorPrimitiveType PrimitiveType
	{
		get;
		set;
	}
}


[System.Serializable]
public class MapLocationOptions
{
	[Geocode]
	public string latitudeLongitude;
	[Range(0, 22)]
	public float zoom;
}

public class MapAPI : MonoBehaviour
{
	protected AbstractMap _map;
	protected AbstractTileProvider _tileProvider;

	[SerializeField]
	MapLocationOptions _mapLocation;

	[SerializeField]
	MapOptions _mapOptions;

	void SetUpMap()
	{
		// Setup map based on type. 
		switch (_mapOptions.type)
		{
			case MapType.AtTileCenter:
				_map = gameObject.AddComponent<BasicMap>();
				break;
			case MapType.AtLocationCenter:
				_map = gameObject.AddComponent<MapAtSpecificLocation>();
				break;
			case MapType.WorldScale:
				_map = gameObject.AddComponent<MapAtWorldScale>();
				break;
			case MapType.Zoomable:
				_map = gameObject.AddComponent<QuadTreeBasicMap>();
				break;
			default:
				break;
		}
		// Setup tileprovider based on type. 
		switch (_mapOptions.tileProviderType)
		{
			case MapDrawType.CameraBounds:
				if (_mapOptions.type == MapType.Zoomable)
				{
					_tileProvider = gameObject.AddComponent<QuadTreeTileProvider>();
				}
				else
				{
					_tileProvider = gameObject.AddComponent<CameraBoundsTileProvider>();
				}
				break;
			case MapDrawType.RangeAroundCenter:
				_tileProvider = gameObject.AddComponent<RangeTileProvider>();
				break;
			case MapDrawType.RangeAroundTransform:
				_tileProvider = gameObject.AddComponent<RangeAroundTransformTileProvider>();
				break;
			case MapDrawType.VirtualGlobe:
				_tileProvider = gameObject.AddComponent<GlobeTileProvider>();
				break;
			default:
				break;
		}
		// Setup a visualizer to get a "Starter" map. 

		_map.TileProvider = _tileProvider;

	}




	// Use this for initialization
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{

	}
}
