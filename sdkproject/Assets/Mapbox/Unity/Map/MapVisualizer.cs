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

	[CreateAssetMenu(menuName = "Mapbox/MapVisualizer/BasicMapVisualizer")]
	public class MapVisualizer : AbstractMapVisualizer
	{
		protected override void PlaceTile(UnwrappedTileId tileId, UnityTile tile, IMapReadable map)
		{
			var rect = tile.Rect;

			// TODO: this is constant for all tiles--cache.
			var scale = tile.TileScale;

			var position = new Vector3(
				(float)(rect.Center.x - map.CenterMercator.x) * scale, 
				0, 
				(float)(rect.Center.y - map.CenterMercator.y) * scale);
			tile.transform.localPosition = position;
		}
	}
}