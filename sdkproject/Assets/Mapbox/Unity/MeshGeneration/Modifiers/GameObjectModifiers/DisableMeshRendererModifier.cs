namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using Mapbox.Unity.MeshGeneration.Data;
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