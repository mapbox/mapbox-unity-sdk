using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Components;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    /// <summary>
    /// Merged Modifier Stack, just like regular Modifier stack, creates a game object from features. But the difference is, regular modifier stack creates a game object for each given faeture meanwhile Merged Modifier Stack merges meshes and creates one game object for all features (until the 65k vertex limit).
    /// It has extremely higher performance compared to regular modifier stack but since it merged all entities together, it also loses all individual entity data & makes it harder to interact with them.
    /// It pools and merges objects based on the tile contains them.
    /// </summary>
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Merged Modifier Stack")]
    public class MergedModifierStack : ModifierStackBase
    {
        public List<MeshModifier> MeshModifiers;
        public List<GameObjectModifier> GoModifiers;

        private Dictionary<UnityTile, int> _cacheVertexCount = new Dictionary<UnityTile, int>();
        private Dictionary<UnityTile, List<MeshData>> _cached = new Dictionary<UnityTile, List<MeshData>>();
        private Dictionary<UnityTile, int> _buildingCount = new Dictionary<UnityTile, int>();

        public override GameObject Execute(UnityTile tile, VectorFeatureUnity feature, MeshData meshData, GameObject parent = null, string type = "")
        {
            base.Execute(tile, feature, meshData, parent, type);

            if (!_cacheVertexCount.ContainsKey(tile))
            {
                _cacheVertexCount.Add(tile, 0);
                _cached.Add(tile, new List<MeshData>());
                _buildingCount.Add(tile, 0);
            }

            _buildingCount[tile]++;
            foreach (MeshModifier mod in MeshModifiers.Where(x => x.Active))
            {
                mod.Run(feature, meshData, tile);
            }

            GameObject go = null;
            if (_cacheVertexCount[tile] < 60000)
            {
                _cacheVertexCount[tile] += meshData.Vertices.Count();
                _cached[tile].Add(meshData);
            }
            else
            {
                go = End(tile, parent);
            }

            return go;
        }

        private GameObject CreateGameObject(MeshData data, GameObject main)
        {
            var go = new GameObject();
            var mesh = go.AddComponent<MeshFilter>().mesh;
            var rend = go.AddComponent<MeshRenderer>();
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

            var fb = go.AddComponent<FeatureBehaviour>();
            foreach (GameObjectModifier mod in GoModifiers.Where(x => x.Active))
            {
                mod.Run(fb);
            }

            return go;
        }

        public GameObject End(UnityTile tile, GameObject parent)
        {
            var md = new MeshData();
            md.UV = new List<List<Vector2>>() { new List<Vector2>(), new List<Vector2>() };
            md.Triangles = new List<List<int>>() { new List<int>(), new List<int>() };
            if (_cached.ContainsKey(tile))
            {
                foreach (var item in _cached[tile].Where(x => x.Vertices.Count > 3))
                {
                    var st = md.Vertices.Count;
                    md.Vertices.AddRange(item.Vertices);
                    md.UV[0].AddRange(item.UV[0]);
                    if (item.UV.Count > 1)
                        md.UV[1].AddRange(item.UV[1]);
                    for (int i = 0; i < item.Triangles.Count; i++)
                    {
                        md.Triangles[i].AddRange(item.Triangles[i].Select(x => x + st));
                    }
                }

                if (md.Vertices.Count > 3)
                {
                    GameObject go = null;
                    go = CreateGameObject(md, parent);
                    _cacheVertexCount[tile] = 0;
                    _cached[tile].Clear();
                    return go;
                }
            }
            return null;
        }
    }
}