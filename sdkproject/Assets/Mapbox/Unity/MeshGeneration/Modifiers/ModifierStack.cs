namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq;
    using Mapbox.Unity.MeshGeneration.Data;
    using Mapbox.Unity.MeshGeneration.Components;

	public enum PositionTargetType
	{
		TileCenter,
		FirstVertex,
		CenterOfVertices
	}

	/// <summary>
	/// Modifier Stack creates a game object from a feature using given modifiers.
	/// It runs mesh modifiers, creates the game object and then run the game object modifiers.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Modifier Stack")]
    public class ModifierStack : ModifierStackBase
    {
		[SerializeField]
		private PositionTargetType _moveFeaturePositionTo;
		private Vector3 _center = Vector3.zero;
		private int vertexIndex = 1;
        public List<MeshModifier> MeshModifiers;
        public List<GameObjectModifier> GoModifiers;

        public override GameObject Execute(UnityTile tile, VectorFeatureUnity feature, MeshData meshData, GameObject parent = null, string type = "")
        {
			_center = Vector3.zero;
			if (_moveFeaturePositionTo != PositionTargetType.TileCenter)
			{
				var f = Constants.Math.Vector3Zero;
				if (_moveFeaturePositionTo == PositionTargetType.FirstVertex)
				{
					f = feature.Points[0][0];
				}
				else if(_moveFeaturePositionTo == PositionTargetType.CenterOfVertices)
				{
					//this is not precisely the center because of the duplicates  (first/last vertex) but close to center
					f = feature.Points[0][0];
					vertexIndex = 1;
					for (int i = 0; i < feature.Points.Count; i++)
					{
						for (int j = 0; j < feature.Points[i].Count; j++)
						{
							f += feature.Points[i][j];
							vertexIndex++;
						}
					}
					f /= vertexIndex;
				}

				foreach (var item in feature.Points)
				{
					for (int i = 0; i < item.Count; i++)
					{
						item[i] = new Vector3(item[i].x - f.x, 0, item[i].z - f.z);
					}
				}
				_center = f;
			}

            foreach (MeshModifier mod in MeshModifiers.Where(x => x.Active))
            {
                mod.Run(feature, meshData, tile);
            }

            var go = CreateGameObject(meshData, parent);
			go.transform.localPosition = _center;
            go.name = type + " - " + feature.Data.Id;
            var bd = go.AddComponent<FeatureBehaviour>();
            bd.Init(feature);

            foreach (GameObjectModifier mod in GoModifiers.Where(x => x.Active))
            {
                mod.Run(bd);
            }

            return go;
        }

        private GameObject CreateGameObject(MeshData data, GameObject main)
        {
            var go = new GameObject();
            var mesh = go.AddComponent<MeshFilter>().mesh;
            mesh.subMeshCount = data.Triangles.Count;
            mesh.SetVertices(data.Vertices);
			mesh.SetNormals(data.Normals);
            for (int i = 0; i < data.Triangles.Count; i++)
            {
                mesh.SetTriangles(data.Triangles[i], i);
            }

            for (int i = 0; i < data.UV.Count; i++)
            {
                mesh.SetUVs(i, data.UV[i]);
            }

            go.transform.SetParent(main.transform, false);

            return go;
        }
    }
}