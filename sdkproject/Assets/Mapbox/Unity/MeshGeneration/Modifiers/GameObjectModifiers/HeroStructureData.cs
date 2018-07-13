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
public class HeroStructureData
{
	public bool active = true;
	public GameObject prefab;
	[Geocode]
	public string latLon;
	[SerializeField]
	private Vector2d latLon_vector2d;
	[SerializeField]
	private double _radius;

	private bool _spawned = false;

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

	public Vector2d LatLonVector2d
	{
		get
		{
			return latLon_vector2d;
		}
	}

	public double Radius
	{
		get
		{
			return _radius;
		}
	}

	public void SetLatLonVector2d()
	{
		if (!string.IsNullOrEmpty(latLon))
		{
			latLon_vector2d = Conversions.StringToLatLon(latLon);
		}
	}

	public void SetRadius()
	{
		if (prefab == null)
		{
			return;
		}
		MeshRenderer meshRenderer = prefab.GetComponent<MeshRenderer>();
		if (meshRenderer == null)
		{
			return;
		}
		Vector3 size = meshRenderer.bounds.size;
		float radius = Mathf.Max(size.x, size.z);
		_radius = (double)System.Math.Pow(radius, 2f);
	}
}
