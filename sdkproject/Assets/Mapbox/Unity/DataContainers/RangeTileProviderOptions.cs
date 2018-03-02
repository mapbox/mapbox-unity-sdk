namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;

	[Serializable]
	public class RangeTileProviderOptions : ITileProviderOptions
	{
		[Range(0, 10)]
		public int west = 1;
		[Range(0, 10)]
		public int north = 1;
		[Range(0, 10)]
		public int east = 1;
		[Range(0, 10)]
		public int south = 1;


		public void SetOptions(int northRange, int southRange, int eastRange, int westRange)
		{
			west = westRange;
			north = northRange;
			east = eastRange;
			south = southRange;
		}
	}
}
