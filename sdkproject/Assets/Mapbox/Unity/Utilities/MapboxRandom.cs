namespace Mapbox.Unity.Utilities
{
	using System;

	public class MapboxRandom
	{

		Random random = new Random();

		public void Init(int seed)
		{
			random = new Random(seed);
		}

		public int Range(int min, int max, int? seed = null)
		{
			if (seed != null)
			{
				Init((int)seed);
			}
			return random.Next(min, max);
		}

		public float Range(float min, float max, int? seed = null)
		{
			if (seed != null)
			{
				Init((int)seed);
			}
			float t = (float)random.NextDouble();
			return UnityEngine.Mathf.Lerp(min, max, t);
		}
	}
}
