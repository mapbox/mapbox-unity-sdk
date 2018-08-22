namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Components;
	using Mapbox.Unity.MeshGeneration.Data;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Noise Offset Modifier")]
	public class NoiseOffsetModifier : GameObjectModifier
	{
		public override void Run(VectorEntity ve, UnityTile tile)
		{
			//create a very small random offset to avoid z-fighting
			Vector3 randomOffset = Random.insideUnitSphere;
			randomOffset *= 0.01f;

			ve.GameObject.transform.localPosition += randomOffset;
		}
	}
}