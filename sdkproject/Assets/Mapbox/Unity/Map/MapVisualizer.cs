namespace Mapbox.Unity.MeshGeneration
{
	using Mapbox.Map;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Map;

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

	[CreateAssetMenu(menuName = "Mapbox/MapVisualizer")]
	public class MapVisualizer : AbstractMapVisualizer
	{
		protected override void PlaceTile(UnityTile tile, IMapReadable map)
		{
			var rect = tile.Rect;
			var scale = tile.TileScale;
			var position = new Vector3(
				(float)(rect.Center.x - map.CenterMercator.x) * scale, 
				0, 
				(float)(rect.Center.y - map.CenterMercator.y) * scale);
			tile.transform.localPosition = position;
		}
	}
}