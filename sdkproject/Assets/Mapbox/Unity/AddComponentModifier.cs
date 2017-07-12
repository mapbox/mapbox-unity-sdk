using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.MeshGeneration.Components;
using ModuleMachine;
using UnityEngine;

[CreateAssetMenu(menuName = "Mapbox/Modifiers/Add Component Modifier")]
public class AddComponentModifier : GameObjectModifier
{
	[SerializeField]
	AddComponentModifierType[] _types;

	public override void Run(FeatureBehaviour fb)
	{
		foreach (var t in _types)
		{
			fb.gameObject.AddComponent(t.Type);
		}
	}
}
