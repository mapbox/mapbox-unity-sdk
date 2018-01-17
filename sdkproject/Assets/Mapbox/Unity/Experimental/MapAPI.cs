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

[System.Serializable]
public class MapOptions
{
	public MapType type;
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

	// Use this for initialization
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{

	}
}
