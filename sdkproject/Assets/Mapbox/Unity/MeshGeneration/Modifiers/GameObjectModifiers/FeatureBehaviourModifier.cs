namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Components;
	using UnityEngine;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Add Feature Behaviour Modifier")]
	public class FeatureBehaviourModifier : GameObjectModifier
	{
		public override void Run(VectorEntity ve, UnityTile tile)
		{
			ve.GameObject.AddComponent<FeatureBehaviour>().Initialize(ve);
		}
	}
}