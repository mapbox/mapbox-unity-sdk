using System;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.SourceLayers;

namespace Mapbox.Unity.DataContainers
{
	[Serializable]
	public class ColliderOptions : ModifierProperties, ISubLayerColliderOptions
	{
		public override Type ModifierType
		{
			get
			{
				return typeof(ColliderModifier);
			}
		}

		public ColliderType colliderType = ColliderType.None;

		/// <summary>
		/// Enable/Disable feature colliders and sets the type of colliders to use.
		/// </summary>
		/// <param name="colliderType">Type of the collider to use on features.</param>
		public virtual void SetFeatureCollider(ColliderType colType)
		{
			if (colliderType != colType)
			{
				colliderType = colType;
				HasChanged = true;
			}
		}
	}
}
