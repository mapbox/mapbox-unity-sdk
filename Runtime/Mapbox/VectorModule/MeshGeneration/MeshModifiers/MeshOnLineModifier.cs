using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorTile.Geometry;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mapbox.VectorModule.MeshGeneration.MeshModifiers
{
	/// <summary>
	/// Scatters prefab meshes along a line feature at a fixed spacing.
	///
	/// UNFINISHED, and deliberately kept rather than deleted. It has no ScriptableObject wrapper in
	/// MeshModifiers/ScriptableObjects, unlike every other mesh modifier, so there is no
	/// CreateAssetMenu entry and no way to add it to a stack from the Inspector; nothing in the
	/// package constructs it either. To finish it: add the wrapper, and give the spacing a home -
	/// <see cref="MeshOnLineModifierCore"/> reads a _distance field that nothing ever assigns, so
	/// spacing is zero and <c>OnLine</c> divides by it and places nothing. PrefabSet has no such
	/// setting today.
	/// </summary>
	[Serializable]
	public class MeshOnLineModifier : MeshModifier<MeshOnLineModifierCore>
	{
		private PrefabSet _prefabSet;

		public MeshOnLineModifier(PrefabSet prefabset)
		{
			_prefabSet = prefabset;
			
			var tileScale = (float) (1 / Conversions.TileBoundsInWebMercator(new UnwrappedTileId(16, 0, 0)).Size.x);
			
			_prefabSet.MeshDatas = new List<MeshData>(_prefabSet.Prefabs.Count);
			foreach (var prefab in _prefabSet.Prefabs)
			{
				var meshData = new MeshData();
				meshData.Vertices = new List<Vector3>();
				meshData.Normals = new List<Vector3>();

				var currentVertexOffset = 0;
				foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>())
				{
					var mesh = mf.sharedMesh;
					var meshVertes = mesh.vertices;
					var meshNormals = mesh.normals;
					var rotation = prefab.transform.rotation;
					var pos = prefab.transform.position;

					for (int i = 0; i < meshVertes.Length; i++)
					{
						meshData.Vertices.Add(rotation * ((pos + meshVertes[i]) * prefab.transform.localScale.x) * tileScale);
						meshData.Normals.Add(rotation * meshNormals[i]);
					}
					
					var newTris = new List<int>();
					foreach (var triIndex in mesh.GetTriangles(0))
					{
						newTris.Add(currentVertexOffset + triIndex);
					}
					meshData.Triangles.Add(newTris);

					currentVertexOffset = meshData.Vertices.Count;
				}

				_prefabSet.MeshDatas.Add(meshData);
			}
		}
		
		protected override MeshOnLineModifierCore CreateCore() => new MeshOnLineModifierCore(_prefabSet);

		public override void Run(VectorFeatureUnity feature, MeshData md, IMapInformation mapInfo)
		{
			Core.Run(feature, md, mapInfo);
		}
	}

	[Serializable]
	public class MeshOnLineModifierCore
	{
		// Never assigned; see the note on MeshOnLineModifier. Until it has a source, unityDistance
		// below is zero and OnLine's count calculation divides by it.
		private float _distance;
		private PrefabSet _prefabSet;
		private System.Random _random;

		private bool _doScale;
		private bool _doRotate;

		public MeshOnLineModifierCore(PrefabSet prefabSet)
		{
			_prefabSet = prefabSet;
			_random = new System.Random();

			if (_prefabSet.ScaleVariety.x != 1 || _prefabSet.ScaleVariety.y != 1)
				_doScale = true;

			if (_prefabSet.RotationVariety.x != 0 || _prefabSet.RotationVariety.y != 0)
				_doRotate = true;
		}

		public void Run(VectorFeatureUnity feature, MeshData md, IMapInformation mapInfo)
		{
			if (_prefabSet?.MeshDatas == null) return;
			
			var unityDistance = _distance * (1 / mapInfo.Scale);
			md.Vertices = new List<Vector3>();
			md.Normals = new List<Vector3>();
			md.Tangents = new List<Vector4>();

			var rotation = feature.Properties.ContainsKey("angle")
				? float.Parse(feature.Properties["angle"].ToString())
				: 0;

			if (feature.Data.GeometryType == GeomType.POINT)
			{
				var randomMeshData = _prefabSet.MeshDatas[_random.Next(0, _prefabSet.MeshDatas.Count)];
				md.Triangles = randomMeshData.Triangles;
				var position = feature.Points[0][0];
				var rotY = 0f;
				if (_doRotate)
				{
					rotY = Mathf.Lerp(Mathf.Max(-360, _prefabSet.RotationVariety.x), Mathf.Min(360, _prefabSet.RotationVariety.y), (float) _random.NextDouble());
				}
				var rot = Quaternion.Euler(0, rotation + rotY, 0);
				if(_doScale)
				{
					var scale = Mathf.Lerp(_prefabSet.ScaleVariety.x, _prefabSet.ScaleVariety.y, (float) _random.NextDouble());
					for (int i = 0; i < randomMeshData.Vertices.Count; i++)
					{
						md.Vertices.Add(position +
						                rot * (randomMeshData.Vertices[i]) //rot
						                    * scale); //scale
						md.Normals.Add(rot * randomMeshData.Normals[i]);
					}
				}
				else
				{
					for (int i = 0; i < randomMeshData.Vertices.Count; i++)
					{
						md.Vertices.Add(position +
						                rot * (randomMeshData.Vertices[i])); //scale
						md.Normals.Add(rot * randomMeshData.Normals[i]);
					}
				}
			}
			else if(feature.Data.GeometryType == GeomType.LINESTRING)
			{
				OnLine(feature, md, unityDistance, rotation);
			}
			
			
		}

		private void OnLine(VectorFeatureUnity feature, MeshData md, float unityDistance, float rotation)
		{
			var leftover = 0f;
			foreach (var subfeature in feature.Points)
			{
				for (int j = 0; j < subfeature.Count - 1; j++)
				{
					var first = subfeature[j];
					var second = subfeature[j + 1];
					var lineDistance = (second - first).magnitude;
					var dir = (second - first).normalized;
					var count = Mathf.FloorToInt((lineDistance + leftover) / unityDistance);
					var lineRot = Quaternion.LookRotation(dir, Vector3.up);
					var firstPosition = first + (dir * (unityDistance - leftover));
					leftover = (lineDistance - (unityDistance - leftover)) - ((count-1) * unityDistance);

					for (int u = 0; u < count; u++)
					{
						var position = firstPosition + (u * dir * unityDistance);
						var rotY = 0f;
						if (_doRotate)
						{
							rotY = Mathf.Lerp(Mathf.Max(-360, _prefabSet.RotationVariety.x), Mathf.Min(360, _prefabSet.RotationVariety.y), (float) _random.NextDouble());
						}
						var rot = Quaternion.Euler(0, rotation + (lineRot.eulerAngles.y + rotY), 0);

						var randomMeshData = _prefabSet.MeshDatas[_random.Next(0, _prefabSet.MeshDatas.Count)];
						var start = md.Vertices.Count;
						var itemDelay = (Vector4)position;
						itemDelay.y = _random.Next(0, 10) / 3f;
						if(_doScale)
						{
							var scale = Mathf.Lerp(_prefabSet.ScaleVariety.x, _prefabSet.ScaleVariety.y, (float) _random.NextDouble());
							for (int i = 0; i < randomMeshData.Vertices.Count; i++)
							{
								md.Vertices.Add(position +
								                rot * (randomMeshData.Vertices[i]) //rot
								                    * scale); //scale
								md.Tangents.Add(itemDelay);
								md.Normals.Add(rot * randomMeshData.Normals[i]);
							}
						}
						else
						{
							for (int i = 0; i < randomMeshData.Vertices.Count; i++)
							{
								md.Vertices.Add(position +
								                rot * (randomMeshData.Vertices[i])); //scale
								md.Tangents.Add(itemDelay);
								md.Normals.Add(rot * randomMeshData.Normals[i]);
							}
						}



						for (var submeshIndex = 0; submeshIndex < randomMeshData.Triangles.Count; submeshIndex++)
						{
							var submesh = randomMeshData.Triangles[submeshIndex];
							for (var index = 0; index < submesh.Count; index++)
							{
								var i = submesh[index];
								md.Triangles[submeshIndex].Add(start + i);
							}
						}

						
					}

				}
			}
		}
	}
}