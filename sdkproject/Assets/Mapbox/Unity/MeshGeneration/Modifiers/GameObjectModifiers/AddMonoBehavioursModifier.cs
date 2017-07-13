namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using Mapbox.Unity.MeshGeneration.Components;
	using UnityEngine;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Add Monobehaviours Modifier")]
	public class AddMonoBehavioursModifier : GameObjectModifier
	{
		[SerializeField]
		AddMonoBehavioursModifierType[] _types;

		public override void Run(FeatureBehaviour fb)
		{
			foreach (var t in _types)
			{
				fb.gameObject.AddComponent(t.Type);
			}
		}
	}
}