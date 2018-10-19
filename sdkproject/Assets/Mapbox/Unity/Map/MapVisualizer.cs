using Mapbox.Unity.Map.Interfaces;

namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Map;

	public enum ModuleState
	{
		Initialized,
		Working,
		Finished
	}

	public class AssignmentTypeAttribute : PropertyAttribute
	{
		public System.Type Type;

		public AssignmentTypeAttribute(System.Type t)
		{
			Type = t;
		}
	}

	/// <summary>
	/// Map Visualizer
	/// Represents a map.Doesn't contain much logic and at the moment, it creates requested tiles and relays them to the factories 
	/// under itself.It has a caching mechanism to reuse tiles and does the tile positioning in unity world.
	/// Later we'll most likely keep track of map features here as well to allow devs to query for features easier 
	/// (i.e.query all buildings x meters around any restaurant etc).
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/MapVisualizer/BasicMapVisualizer")]
	public class MapVisualizer : AbstractMapVisualizer
	{
		protected override void PlaceTile(UnwrappedTileId tileId, UnityTile tile, IMapReadable map)
		{
			var rect = tile.Rect;

			// TODO: this is constant for all tiles--cache.
			var scale = tile.TileScale;
			var scaleFactor = Mathf.Pow(2, (map.InitialZoom - map.AbsoluteZoom));

			var position = new Vector3(
				(float)(rect.Center.x - map.CenterMercator.x) * scale * scaleFactor,
				0,
				(float)(rect.Center.y - map.CenterMercator.y) * scale * scaleFactor);
			tile.transform.localPosition = position;
		}
	}
}
