using System;
using UnityEngine;

namespace Mapbox.Examples.Voxels
{
	public class VoxelFetcher : MonoBehaviour
	{
		[SerializeField]
		VoxelColorMapper[] _voxels;

		public GameObject GetVoxelFromColor(Color color)
		{
			GameObject matchingVoxel = _voxels[0].Voxel;
			var minDistance = Mathf.Infinity;
			foreach (var voxel in _voxels)
			{
				var distance = GetDistanceBetweenColors(voxel.Color, color);
				if (distance < minDistance)
				{
					matchingVoxel = voxel.Voxel;
					minDistance = distance;
				}
			}
			return matchingVoxel;
		}

		public static float GetDistanceBetweenColors(Color color1, Color color2)
		{
			return Mathf.Sqrt(Mathf.Pow(color1.r - color2.r, 2f) + Mathf.Pow(color1.g - color2.g, 2f) + Mathf.Pow(color1.b - color2.b, 2f));
		}
	}

	[Serializable]
	public class VoxelColorMapper
	{
		public Color Color;
		public GameObject Voxel;
	}
}
