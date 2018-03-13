namespace Mapbox.Unity.Ar
{
	using System;
	using UnityEngine;

	public interface ISynchronizationContext
	{
		event Action<Alignment> OnAlignmentAvailable;
	}

	public struct Alignment
	{
		public Vector3 Position;
		public float Rotation;
	}
}
