namespace Mapbox.Unity.Map
{
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using System;

	[Serializable]
	public class ColliderOptions : ModifierProperties
	{
		public override Type ModifierType
		{
			get
			{
				return typeof(ColliderModifier);
			}
		}

		public ColliderType colliderType = ColliderType.None;

	}
}
