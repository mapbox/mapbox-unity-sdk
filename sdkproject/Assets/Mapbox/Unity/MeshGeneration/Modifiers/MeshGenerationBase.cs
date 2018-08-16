using UnityEngine;
using System.Collections;
using Mapbox.Unity.MeshGeneration.Modifiers;
using System.Collections.Generic;

public class MeshGenerationBase : MeshModifier, IReplaceable
{
	public HashSet<IReplacementCriteria> Criteria { get; set; }
	public override void Initialize()
	{
		base.Initialize();
		Criteria = new HashSet<IReplacementCriteria>();
	}
}
