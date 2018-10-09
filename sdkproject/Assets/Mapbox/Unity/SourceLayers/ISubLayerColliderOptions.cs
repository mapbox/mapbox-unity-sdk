using Mapbox.Unity.Map;

namespace Mapbox.Unity.SourceLayers
{
	public interface ISubLayerColliderOptions
	{
		/// <summary>
		/// Enable/Disable feature colliders and sets the type of colliders to use.
		/// </summary>
		/// <param name="colliderType">Type of the collider to use on features.</param>
		void SetFeatureCollider(ColliderType colliderType);
	}
}