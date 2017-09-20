namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Snap Terrain Modifier")]
	public class SnapTerrainModifier : MeshModifier
	{
		public override ModifierType Type { get { return ModifierType.Preprocess; } }

		private double scaledX;
		private double scaledY;

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			scaledX = tile.Rect.Size.x * tile.TileScale;
			scaledY = tile.Rect.Size.y * tile.TileScale;
			if (md.Vertices.Count > 0)
			{
				for (int i = 0; i < md.Vertices.Count; i++)
				{
					var h = tile.QueryHeightData((float)((md.Vertices[i].x + scaledX / 2) / scaledX), (float)((md.Vertices[i].z + scaledY / 2) / scaledY));
					md.Vertices[i] += new Vector3(0, h, 0);
				}
			}
			else
			{
				foreach (var sub in feature.Points)
				{
					for (int i = 0; i < sub.Count; i++)
					{
						var h = tile.QueryHeightData((float)((sub[i].x + scaledX / 2) / scaledX), (float)((sub[i].z + scaledY / 2) / scaledY));
						sub[i] += new Vector3(0, h, 0);
					}
				}
			}
		}
	}
}
