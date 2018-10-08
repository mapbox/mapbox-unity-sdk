using UnityEngine;

namespace Mapbox.Unity.Map
{
	public interface ITerrainLayer : ILayer
	{
		/// <summary>
		/// Gets the `Data Source` for the `TERRAIN` section.
		/// </summary>
		ElevationSourceType LayerSource { get; }
		/// <summary>
		/// Gets the `Elevation Layer Type` for the `TERRAIN` section.
		/// </summary>
		ElevationLayerType ElevationType { get; set; }
		/// <summary>
		/// Gets the `Exaggeration Factor` for the `TERRAIN` section.
		/// </summary>
		float ExaggerationFactor { get; set; }

		/// <summary>
		/// Sets the Data Source for `TERRAIN`. By default this is set to
		/// `Mapbox Terrain`. Currenly, only terrain-rgb is supported.
		/// Use <paramref name="terrainSource"/> = `None`, to disable the terrain.
		/// </summary>
		/// <param name="terrainSource">`Data Source` for `TERRAIN`</param>
		void SetLayerSource(ElevationSourceType terrainSource = ElevationSourceType.MapboxTerrain);

		/// <summary>
		/// Sets the `Elevation Layer Type` which is the main strategy for terrain
		/// mesh generation. `Flat Terrain` doesn't pull data from servers, it
		/// uses a quad as the terrain. </summary>
		/// <param name="elevationType">Type of the elevation. Can be set to `Terrain with Elevation`,
		/// `Flat Terrain`, `Globe`, or `Low Polygon Terrain`. Note: low poly doesn't
		/// improve performance.</param>
		void SetElevationType(ElevationLayerType elevationType);

		/// <summary>
		/// Enables or disables the `Add Collider` settings for adding a collider
		/// to the terrain. The collider type is a mesh collider.
		/// </summary>
		/// <param name="enable">Boolean for toggling `Add Collider`. </param>
		void EnableCollider(bool enable);

		/// <summary>
		/// Sets the `Exaggeration Factor` for the terrain. This acts as a multiplier
		/// for elevation. Use this setting to better highlight elevation in your scene.
		/// Each elevation point will be multiplied by the float value.
		/// </summary>
		/// <param name="factor">Elevation multiplier for `Exaggeration Factor` settings. </param>
		void SetExaggerationFactor(float factor);

		/// <summary>
		/// Enables the settings for `Show Sidewalls`.
		/// </summary>
		/// <param name="wallHeight">Wall height.</param>
		/// <param name="wallMaterial">Wall material.</param>
		void EnableSideWalls(float wallHeight, Material wallMaterial);

		/// <summary>
		/// Disables the settings for `Show Sidewalls`.
		/// </summary>
		void DisableSideWalls();


		/// <summary>
		/// Adds the terrain mesh GameObject to a Unity layer.
		/// </summary>
		/// <param name="layerId">Layer identifier. You may need to add the layer in
		/// the Tags and Layers manager.</param>
		void AddToUnityLayer(int layerId);

		/// <summary>
		/// Removes the terrain GameObject from a Unity layer.
		/// </summary>
		/// <param name="layerId">Layer identifier.</param>
		void RemoveFromUnityLayer(int layerId);

		/// <summary>
		/// Change the `TERRAIN` layer settings.
		/// </summary>
		/// <param name="dataSource">The `Data Source` for the terrain.</param>
		/// <param name="elevationType">`Elevation Layer Type` setting to define elevation strategy.</param>
		/// <param name="enableCollider">Enables or disables `Use Collider` settings for a mesh collider on the terrain.</param>
		/// <param name="factor">`Exaggertion Factor` for a multiplier of the height data.</param>
		/// <param name="layerId">`Add to Unity Layer` settings which adds terrrain to a layer.</param>
		void SetProperties(ElevationSourceType dataSource = ElevationSourceType.MapboxTerrain, ElevationLayerType elevationType = ElevationLayerType.TerrainWithElevation, bool enableCollider = false, float factor = 1, int layerId = 0);
	}


	public interface IGlobeTerrainLayer : ITerrainLayer
	{
		float EarthRadius { get; set; }
	}

}
