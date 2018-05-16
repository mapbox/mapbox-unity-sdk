using UnityEngine;
using System.Collections;
using Mapbox.Unity.MeshGeneration.Modifiers;
using System;
using Mapbox.Unity.Map;

[Serializable]
public class ReplacementOptions : ModifierProperties
{
	public override Type ModifierType
	{
		get
		{
			return typeof(ReplacementOptions);
		}
	}

	public PrefabItemOptions prefabItemOptions = new PrefabItemOptions();
}
