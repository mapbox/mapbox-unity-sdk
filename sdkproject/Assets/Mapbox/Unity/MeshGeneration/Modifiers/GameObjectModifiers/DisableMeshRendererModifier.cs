namespace Mapbox.Unity.MeshGeneration.Modiiers
{
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using UnityEngine;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Disable Mesh Renderer Modifier")]
	public class DisableMeshRendererModifier : GameObjectModifier
	{
		public override void Run(VectorEntity ve, UnityTile tile)
		{
			ve.MeshRenderer.enabled = false;
		}
	}
}