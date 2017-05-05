namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq;
    using Mapbox.Unity.MeshGeneration.Data;
    using Mapbox.Unity.MeshGeneration.Components;

    /// <summary>
    /// Modifier Stack creates a game object from a feature using given modifiers.
    /// It runs mesh modifiers, creates the game object and then run the game object modifiers.
    /// </summary>
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Modifier Stack")]
    public class ModifierStack : ModifierStackBase
    {
        public List<MeshModifier> MeshModifiers;
        public List<GameObjectModifier> GoModifiers;

        public override GameObject Execute(UnityTile tile, VectorFeatureUnity feature, MeshData meshData, GameObject parent = null, string type = "")
        {
            foreach (MeshModifier mod in MeshModifiers.Where(x => x.Active))
            {
                mod.Run(feature, meshData, tile);
            }

            var go = CreateGameObject(meshData, parent);
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
            for (int i = 0; i < data.Triangles.Count; i++)
            {
                var triangle = data.Triangles[i];
                mesh.SetTriangles(triangle, i);
            }

            for (int i = 0; i < data.UV.Count; i++)
            {
                var uv = data.UV[i];
                mesh.SetUVs(i, uv);
            }

            mesh.RecalculateNormals();
            go.transform.SetParent(main.transform, false);

            return go;
        }
    }
}