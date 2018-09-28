using UnityEngine;

namespace Mapbox.Unity.Map
{
	public interface ITerrainLayer : ILayer
	{

		ElevationSourceType LayerSource { get; }
		ElevationLayerType ElevationType { get; set; }
		float ExxagerationFactor { get; set; }

		/// <summary>
		/// Sets the data source for Terrain Layer.
		/// Defaults to MapboxTerrain.
		/// Use <paramref name="terrainSource"/> = None, to disable the Terrain Layer. 
		/// </summary>
		/// <param name="terrainSource">Terrain source.</param>
		void SetLayerSource(ElevationSourceType terrainSource = ElevationSourceType.MapboxTerrain);

		///// <summary>
		///// Sets the main strategy for terrain mesh generation.
		///// Flat terrain doesn't pull data from servers and just uses a quad as terrain.
		///// </summary>
		///// <param name="elevationType">Type of the elevation strategy</param>
		//void SetElevationType(ElevationLayerType elevationType);

		/// <summary>
		/// Add/Remove terrain collider. Terrain uses mesh collider.
		/// </summary>
		/// <param name="enable">Boolean for enabling/disabling mesh collider</param>
		void EnableCollider(bool enable);

		/// <summary>
		/// Sets the elevation multiplier for terrain. It'll regenerate terrain mesh, multiplying each point elevation by provided value.
		/// </summary>
		/// <param name="factor">Elevation multiplier</param>
		void SetExaggerationFactor(float factor);

		/// <summary>
		/// Turn on terrain side walls.
		/// </summary>
		/// <param name="wallHeight">Wall height.</param>
		/// <param name="wallMaterial">Wall material.</param>
		void EnableSideWalls(float wallHeight, Material wallMaterial);

		/// <summary>
		/// Turn off terrain side walls.
		/// </summary>
		void DisableSideWalls();


		/// <summary>
		/// Adds Terrain GameObject to Unity layer.
		/// </summary>
		/// <param name="layerId">Layer identifier.</param>
		void AddToUnityLayer(int layerId);

		/// <summary>
		/// Remove Terrain GameObject to Unity layer.
		/// </summary>
		/// <param name="layerId">Layer identifier.</param>
		void RemoveFromUnityLayer(int layerId);

		/// <summary>
		/// Change terrain layer settings.
		/// </summary>
		/// <param name="dataSource">The data source for the terrain height map.</param>
		/// <param name="elevationType">Mesh generation strategy for the tile/height.</param>
		/// <param name="enableCollider">Enable/Disable collider component for the tile game object.</param>
		/// <param name="factor">Multiplier for the height data.</param>
		/// <param name="layerId">Unity Layer for the tile game object.</param>
		void SetProperties(ElevationSourceType dataSource = ElevationSourceType.MapboxTerrain, ElevationLayerType elevationType = ElevationLayerType.TerrainWithElevation, bool enableCollider = false, float factor = 1, int layerId = 0);
	}


	public interface IGlobeTerrainLayer : ITerrainLayer
	{
		float EarthRadius { get; set; }
	}

}


