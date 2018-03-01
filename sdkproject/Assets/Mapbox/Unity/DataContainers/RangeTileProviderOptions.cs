namespace Mapbox.Unity.Map
{
	using System;
	[Serializable]
	public class RangeTileProviderOptions : ITileProviderOptions
	{
		public int west;
		public int north;
		public int east;
		public int south;


		public void SetOptions(int northRange, int southRange, int eastRange, int westRange)
		{
			west = westRange;
			north = northRange;
			east = eastRange;
			south = southRange;
		}
	}
}
