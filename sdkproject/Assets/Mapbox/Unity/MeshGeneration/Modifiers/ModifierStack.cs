namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using System.Collections.Generic;
	using System.Linq;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Components;
	using System;

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
		[NodeEditorElement("Mesh Modifiers")]
		public List<MeshModifier> MeshModifiers;
		[NodeEditorElement("Game Object Modifiers")]
		public List<GameObjectModifier> GoModifiers;

		[NonSerialized]
		private Dictionary<UnityTile, List<VectorEntity>> _activeObjects;
		[NonSerialized]
		private Queue<VectorEntity> _pool;

		private void OnEnable()
		{
			_pool = new Queue<VectorEntity>();
			_activeObjects = new Dictionary<UnityTile, List<VectorEntity>>();
		}

		public override void OnUnregisterTile(UnityTile tile)
		{
			if(_activeObjects.ContainsKey(tile))
			{
				foreach (var ve in _activeObjects[tile])
				{
					ve.GameObject.SetActive(false);
					_pool.Enqueue(ve);
				}
				_activeObjects[tile].Clear();
				_activeObjects.Remove(tile);
			}
		}

		public override void Initialize()
		{
			base.Initialize();
			
			foreach (var mmod in MeshModifiers)
			{
				mmod.Initialize();
			}

			foreach (var gmod in GoModifiers)
			{
				gmod.Initialize();
			}
		}

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
				else if (_moveFeaturePositionTo == PositionTargetType.CenterOfVertices)
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

			foreach (MeshModifier mod in MeshModifiers.Where(x => x != null && x.Active))
			{
				mod.Run(feature, meshData, tile);
			}


			VectorEntity ve = null;
			if (_pool.Count > 0)
			{
				ve = _pool.Dequeue();
				ve.GameObject.SetActive(true);
				ve.Mesh.Clear();
			}
			else
			{
				var go = new GameObject();
				var mf = go.AddComponent<MeshFilter>();
				var mr = go.AddComponent<MeshRenderer>();
				ve = new VectorEntity() {
					GameObject = go,
					Transform = go.transform,
					MeshFilter = mf,
					MeshRenderer = mr,
					Mesh = mf.mesh,
					Feature = feature };
			}

			ve.GameObject.name = type + " - " + feature.Data.Id;
			ve.Mesh.subMeshCount = meshData.Triangles.Count;
			ve.Mesh.SetVertices(meshData.Vertices);
			ve.Mesh.SetNormals(meshData.Normals);
			for (int i = 0; i < meshData.Triangles.Count; i++)
			{
				ve.Mesh.SetTriangles(meshData.Triangles[i], i);
			}

			for (int i = 0; i < meshData.UV.Count; i++)
			{
				ve.Mesh.SetUVs(i, meshData.UV[i]);
			}

			ve.Transform.SetParent(parent.transform, false);

			if (!_activeObjects.ContainsKey(tile))
				_activeObjects.Add(tile, new List<VectorEntity>());
			_activeObjects[tile].Add(ve);


			ve.Transform.localPosition = _center;

			foreach (GameObjectModifier mod in GoModifiers.Where(x => x.Active))
			{
				mod.Run(ve, tile);
			}

			return ve.GameObject;
		}
	}
}