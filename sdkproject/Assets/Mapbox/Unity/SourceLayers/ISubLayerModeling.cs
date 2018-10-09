using Mapbox.Unity.Map;

namespace Mapbox.Unity.SourceLayers
{
	public interface ISubLayerModeling
	{
		ISubLayerCoreOptions CoreOptions { get; }
		ISubLayerExtrusionOptions ExtrusionOptions { get; }
		ISubLayerColliderOptions ColliderOptions { get; }
		ISubLayerLineGeometryOptions LineOptions { get; }
		
		/// <summary>
		/// Enable terrain snapping for features which sets vertices to terrain
		/// elevation before extrusion.
		/// </summary>
		/// <param name="isEnabled">Enabled terrain snapping</param>
		void EnableSnapingTerrain(bool isEnabled);

		/// <summary>
		/// Enable combining individual features meshes into one to minimize gameobject
		/// count and draw calls.
		/// </summary>
		/// <param name="isEnabled"></param>
		void EnableCombiningMeshes(bool isEnabled);
	}
}