using UnityEngine;
using System.Collections;
using Mapbox.Unity.MeshGeneration.Modifiers;
using System.Collections.Generic;

public class MeshGenerationBase : MeshModifier, IReplaceable
{
	public List<IReplacementCriteria> Criteria{get; set;}
	public override void Initialize()
	{
		base.Initialize();
		Criteria = new List<IReplacementCriteria>();
	}
}
