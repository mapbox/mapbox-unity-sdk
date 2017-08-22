namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Snap Terrain Modifier")]
	public class SnapTerrainModifier : MeshModifier
	{
		public override ModifierType Type { get { return ModifierType.Preprocess; } }

		internal override void Initialize(WorldProperties wp)
		{
			base.Initialize(wp);
		}

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			foreach (var sub in feature.Points)
			{
				for (int i = 0; i < sub.Count; i++)
				{
					var h = tile.QueryHeightData((float)(((sub[i].x / _worldProperties.WorldRelativeScale) + tile.Rect.Size.x / 2) / tile.Rect.Size.x), (float)(((sub[i].z / _worldProperties.WorldRelativeScale) + tile.Rect.Size.y / 2) / tile.Rect.Size.y)) * _worldProperties.WorldRelativeScale;
					sub[i] += new Vector3(0, h, 0);
				}
			}
		}
	}
}
