namespace Mapbox.Unity.Utilities
{
	using System;

	public class MapboxRandom
	{

		Random random = new Random();

		/// <summary>
		/// Init the specified seed.
		/// </summary>
		/// <param name="seed">Seed.</param>
		public void Init(int seed)
		{
			random = new Random(seed);
		}

		/// <summary>
		/// Get a random int value between min and max; optionally provide a seed for randomization.
		/// </summary>
		/// <returns>The range.</returns>
		/// <param name="min">Minimum.</param>
		/// <param name="max">Max.</param>
		/// <param name="seed">Seed.</param>
		public int Range(int min, int max, int? seed = null)
		{
			if (seed != null)
			{
				Init((int)seed);
			}
			return random.Next(min, max);
		}

		/// <summary>
		/// Get a random float value between min and max; optionally provide a seed for randomization.
		/// </summary>
		/// <returns>The range.</returns>
		/// <param name="min">Minimum.</param>
		/// <param name="max">Max.</param>
		/// <param name="seed">Seed.</param>
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
