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
		private int _counter;

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			scaledX = tile.Rect.Size.x * tile.TileScale;
			scaledY = tile.Rect.Size.y * tile.TileScale;
			_counter = md.Vertices.Count;
			if (_counter > 0)
			{
				for (int i = 0; i < _counter; i++)
				{
					var h = tile.QueryHeightData(
						(float)((md.Vertices[i].x + md.PositionInTile.x + scaledX / 2) / scaledX),
						(float)((md.Vertices[i].z + md.PositionInTile.z + scaledY / 2) / scaledY));
					md.Vertices[i] += new Vector3(0, h, 0);
				}
			}
			else
			{
				foreach (var sub in feature.Points)
				{
					_counter = sub.Count;
					for (int i = 0; i < _counter; i++)
					{
						var h = tile.QueryHeightData(
							(float)((sub[i].x + md.PositionInTile.x + scaledX / 2) / scaledX), 
							(float)((sub[i].z + md.PositionInTile.z + scaledY / 2) / scaledY));
						sub[i] += new Vector3(0, h, 0);
					}
				}
			}
		}
	}
}
