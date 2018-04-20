using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpawnLocation { Center, Side, Front, Top }
public enum SpawnAlignment { Outwards, Inwards, Normal }

[System.Serializable]
public class OrnamentBundle 
{
	[HideInInspector]
	private string _name;

	public GameObject m_prefab;
	public SpawnLocation m_spawnLocation;
	public SpawnAlignment m_spawnAlignment;
	[Range(0,100)]
	public float m_density;

	public string Name { get { return _name; } }

	public OrnamentBundle(string name)
	{
		_name = name;
	}
}
