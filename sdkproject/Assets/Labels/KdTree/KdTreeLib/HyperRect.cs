namespace KdTree
{
    public struct HyperRect<T>
	{
		private T[] minPoint;
		public T[] MinPoint
		{
			get
			{
				return minPoint;
			}
			set
			{
				minPoint = new T[value.Length];
				value.CopyTo(minPoint, 0);
			}
		}

		private T[] maxPoint;
		public T[] MaxPoint
		{
			get
			{
				return maxPoint;
			}
			set
			{
				maxPoint = new T[value.Length];
				value.CopyTo(maxPoint, 0);
			}
		}

		public static HyperRect<T> Infinite(int dimensions, ITypeMath<T> math)
		{
            var rect = new HyperRect<T>
            {
                MinPoint = new T[dimensions],
                MaxPoint = new T[dimensions]
            };

            for (var dimension = 0; dimension < dimensions; dimension++)
			{
				rect.MinPoint[dimension] = math.NegativeInfinity;
				rect.MaxPoint[dimension] = math.PositiveInfinity;
			}

			return rect;
		}

		public T[] GetClosestPoint(T[] toPoint, ITypeMath<T> math)
		{
			T[] closest = new T[toPoint.Length];

			for (var dimension = 0; dimension < toPoint.Length; dimension++)
			{
				if (math.Compare(minPoint[dimension], toPoint[dimension]) > 0)
				{
					closest[dimension] = minPoint[dimension];
				}
				else if (math.Compare(maxPoint[dimension], toPoint[dimension]) < 0)
				{
					closest[dimension] = maxPoint[dimension];
				}
				else
					// Point is within rectangle, at least on this dimension
					closest[dimension] = toPoint[dimension];
			}

			return closest;
		}

		public HyperRect<T> Clone()
		{
            var rect = new HyperRect<T>
            {
                MinPoint = MinPoint,
                MaxPoint = MaxPoint
            };
            return rect;
		}
	}
}
