using System;

namespace KdTree.Math
{
    [Serializable]
	public class FloatMath : TypeMath<float>
	{
		public override int Compare(float a, float b)
		{
			return a.CompareTo(b);
		}

		public override bool AreEqual(float a, float b)
		{
			return a == b;
		}

		public override float MinValue
		{
			get { return float.MinValue; }
		}

		public override float MaxValue
		{
			get { return float.MaxValue; }
		}

		public override float Zero
		{
			get { return 0; }
		}

		public override float NegativeInfinity { get { return float.NegativeInfinity; } }

		public override float PositiveInfinity { get { return float.PositiveInfinity; } }

		public override float Add(float a, float b)
		{
			return a + b;
		}

		public override float Subtract(float a, float b)
		{
			return a - b;
		}

		public override float Multiply(float a, float b)
		{
			return a * b;
		}

		public override float DistanceSquaredBetweenPoints(float[] a, float[] b)
		{
			float distance = Zero;
			int dimensions = a.Length;

			// Return the absolute distance bewteen 2 hyper points
			for (var dimension = 0; dimension < dimensions; dimension++)
			{
				float distOnThisAxis = Subtract(a[dimension], b[dimension]);
				float distOnThisAxisSquared = Multiply(distOnThisAxis, distOnThisAxis);

				distance = Add(distance, distOnThisAxisSquared);
			}

			return distance;
		}
	}
}
