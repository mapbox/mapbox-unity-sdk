using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.MeshGeneration.Data;

[System.Serializable]
public class MaterialAtlasPaletteBundle 
{
	[HideInInspector]
	private string _name;

	public Material m_material;
	public AtlasInfo m_atlasInfo;
	public ScriptablePalette m_palette;

	public string Name{ get { return _name; }}

	public MaterialAtlasPaletteBundle(string name)
	{
		_name = name;
	}
}
