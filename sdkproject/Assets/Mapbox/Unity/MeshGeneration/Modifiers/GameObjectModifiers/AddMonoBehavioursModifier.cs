namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Components;
	using UnityEngine;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Add Monobehaviours Modifier")]
	public class AddMonoBehavioursModifier : GameObjectModifier
	{
		[SerializeField]
		AddMonoBehavioursModifierType[] _types;

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			foreach (var t in _types)
			{
				ve.GameObject.AddComponent(t.Type);
			}
		}
	}
}