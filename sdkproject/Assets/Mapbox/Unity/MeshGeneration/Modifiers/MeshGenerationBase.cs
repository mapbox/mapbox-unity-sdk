using UnityEngine;
using System.Collections;
using Mapbox.Unity.MeshGeneration.Modifiers;
using System.Collections.Generic;

public class MeshGenerationBase : MeshModifier, IReplacable
{
	public List<IReplacementCriteria> Criteria{get; set;}
	public virtual void Initialize(List<IReplacementCriteria> criteria)
	{
		Criteria = criteria;
	}
}
