using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.Map;
using UnityEngine;

public class ApiTest : MonoBehaviour
{
	private AbstractMap _abstractMap;

	void Start()
	{
		_abstractMap = FindObjectOfType<AbstractMap>();
	}

	[ContextMenu("EnableColliders")]
	public void EnableColliders()
	{
		_abstractMap.Terrain.LayerProperty.SetCollider(true);
	}

	[ContextMenu("DisableColliders")]
	public void DisableColliders()
	{
		_abstractMap.Terrain.LayerProperty.SetCollider(true);
	}
}