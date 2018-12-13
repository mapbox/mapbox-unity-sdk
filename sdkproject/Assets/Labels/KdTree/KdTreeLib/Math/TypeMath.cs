using System;

namespace KdTree.Math
{
    // Algebraic!
    [Serializable]
	public abstract class TypeMath<T> : ITypeMath<T>
	{
		#region ITypeMath<T> members

		public abstract int Compare(T a, T b);

		public abstract bool AreEqual(T a, T b);

		public virtual bool AreEqual(T[] a, T[] b)
		{
			if (a.Length != b.Length)
				return false;

			for (var index = 0; index < a.Length; index++)
			{
				if (!AreEqual(a[index], b[index]))
					return false;
			}

			return true;
		}

		public abstract T MinValue { get; }

		public abstract T MaxValue { get; }

		public T Min(T a, T b)
		{
			if (Compare(a, b) < 0)
				return a;
			else
				return b;
		}

		public T Max(T a, T b)
		{
			if (Compare(a, b) > 0)
				return a;
			else
				return b;
		}

		public abstract T Zero { get; }

		public abstract T NegativeInfinity { get; }

		public abstract T PositiveInfinity { get; }

		public abstract T Add(T a, T b);

		public abstract T Subtract(T a, T b);

		public abstract T Multiply(T a, T b);

		public abstract T DistanceSquaredBetweenPoints(T[] a, T[] b);

		#endregion
	}
}
