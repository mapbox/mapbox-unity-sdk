using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.MeshModifiers
{
	[Serializable]
	public class MeshOnPointModifierCore
	{
		public MeshOnPointSettings _meshOnPointSettings;
		private System.Random _random;

		public MeshOnPointModifierCore(MeshOnPointSettings meshOnPointSettings)
		{
			_meshOnPointSettings = meshOnPointSettings;
			_random = new System.Random();
		}

		public void Run(VectorFeatureUnity feature, MeshData md, IMapInformation mapInfo)
		{
			var tileScale = (float) (1 / Conversions.TileBoundsInWebMercator(feature.TileId).Size.x);
			md.Vertices = new List<Vector3>();
			md.Normals = new List<Vector3>();
			md.Tangents = new List<Vector4>();

			var prefabSet = _meshOnPointSettings.PrefabSet;
			var position = feature.Points[0][0];
			var randomMeshData = prefabSet.MeshDatas[_random.Next(0, prefabSet.MeshDatas.Count)];
			for (int i = 0; i < randomMeshData.Triangles.Count; i++)
			{
				md.Triangles.Add(new List<int>());
			}


			var start = md.Vertices.Count;

			var rotY = 0f;
			if (_meshOnPointSettings.RotateObjects)
			{
				rotY = Mathf.Lerp(Mathf.Max(-360, prefabSet.RotationVariety.x), Mathf.Min(360, prefabSet.RotationVariety.y), (float) _random.NextDouble());
			}
			var rot = Quaternion.Euler(0, rotY, 0);


			var scale = tileScale;
			if(_meshOnPointSettings.ScaleObjects)
			{
				scale *= Mathf.Lerp(prefabSet.ScaleVariety.x, prefabSet.ScaleVariety.y, (float) _random.NextDouble());
			}

			for (int i = 0; i < randomMeshData.Vertices.Count; i++)
			{
				md.Vertices.Add(position +
				                rot * (randomMeshData.Vertices[i]) //rot
				                    * scale); //scale
				md.Normals.Add(rot * randomMeshData.Normals[i]);
				md.Tangents.Add(rot * (randomMeshData.Vertices[i]) //rot
				                    * scale);
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

			md.UV[0].AddRange(randomMeshData.UV[0]);
		}
	}

	/// <summary>
	/// Places a prefab mesh on a point feature.
	///
	/// UNFINISHED, and deliberately kept rather than deleted. Like <see cref="MeshOnLineModifier"/>
	/// it has no ScriptableObject wrapper in MeshModifiers/ScriptableObjects, so it cannot be added
	/// to a modifier stack from the Inspector, and nothing in the package constructs it. Adding the
	/// wrapper is the whole of what it needs.
	/// </summary>
	public class MeshOnPointModifier : MeshModifier<MeshOnPointModifierCore>
	{
		private MeshOnPointSettings _meshOnPointSettings;
	
		public MeshOnPointModifier(MeshOnPointSettings meshOnPointSettings)
		{
			_meshOnPointSettings = meshOnPointSettings;
		}
	
		protected override MeshOnPointModifierCore CreateCore() => new MeshOnPointModifierCore(_meshOnPointSettings);

		public override void Run(VectorFeatureUnity feature, MeshData md, IMapInformation mapInfo)
		{
			Core.Run(feature, md, mapInfo);
		}
	}

	[Serializable]
	public class MeshOnPointSettings
	{
		public PrefabSet PrefabSet;
		public bool ScaleObjects;
		public bool RotateObjects;
	}
}