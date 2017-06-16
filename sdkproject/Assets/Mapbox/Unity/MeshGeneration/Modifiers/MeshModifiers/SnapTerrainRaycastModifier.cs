namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Data;
	using UnityEngine;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Snap Terrain Raycast Modifier")]
	public class SnapTerrainRaycastModifier : MeshModifier
	{
		private const int RAY_LENGTH = 50;

		[SerializeField]
		private LayerMask _terrainMask;

		public override ModifierType Type
		{
			get { return ModifierType.Preprocess; }
		}

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			// TODO: Get this from tile as IMapReadable.
			float worldScale = FindObjectOfType<AbstractMap>().WorldRelativeScale;

			if (md.Vertices.Count > 0)
			{
				for (int i = 0; i < md.Vertices.Count; i++)
				{
					var h = tile.QueryHeightData((float)((md.Vertices[i].x + tile.Rect.Size.x / 2) / tile.Rect.Size.x),
						(float)((md.Vertices[i].z + tile.Rect.Size.y / 2) / tile.Rect.Size.y));

					RaycastHit hit;
					Vector3 rayCenter =
						new Vector3(md.Vertices[i].x * worldScale + tile.transform.position.x,
							h * worldScale + RAY_LENGTH / 2,
							md.Vertices[i].z * worldScale + tile.transform.position.z);

					if (Physics.Raycast(rayCenter, Vector3.down, out hit, RAY_LENGTH, _terrainMask))
					{
						md.Vertices[i] += new Vector3(0, hit.point.y / worldScale, 0);
					}
					else
					{
						// Raycasting sometimes fails at terrain boundaries, fallback to tile height data.
						md.Vertices[i] += new Vector3(0, h, 0);
					}
				}
			}
			else
			{
				foreach (var sub in feature.Points)
				{
					for (int i = 0; i < sub.Count; i++)
					{
						var h = tile.QueryHeightData((float)((sub[i].x + tile.Rect.Size.x / 2) / tile.Rect.Size.x),
							(float)((sub[i].z + tile.Rect.Size.y / 2) / tile.Rect.Size.y));

						RaycastHit hit;
						Vector3 rayCenter =
							new Vector3(sub[i].x * worldScale + tile.transform.position.x,
								h * worldScale + RAY_LENGTH / 2,
								sub[i].z * worldScale + tile.transform.position.z);

						if (Physics.Raycast(rayCenter, Vector3.down, out hit, RAY_LENGTH, _terrainMask))
						{
							sub[i] += new Vector3(0, hit.point.y / worldScale, 0);
						}
						else
						{
							// Raycasting sometimes fails at terrain boundaries, fallback to tile height data.
							sub[i] += new Vector3(0, h, 0);
						}
					}
				}
			}
		}
	}
}