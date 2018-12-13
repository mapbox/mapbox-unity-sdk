using System;

namespace KdTree
{
    public interface INearestNeighbourList<TItem, TDistance>
	{
		bool Add(TItem item, TDistance distance);
		TItem GetFurtherest();
		TItem RemoveFurtherest();

		int MaxCapacity { get; }
		int Count { get; }
	}

	public class NearestNeighbourList<TItem, TDistance> : INearestNeighbourList<TItem, TDistance>
	{
		public NearestNeighbourList(int maxCapacity, ITypeMath<TDistance> distanceMath)
		{
			this.maxCapacity = maxCapacity;
			this.distanceMath = distanceMath;

			queue = new PriorityQueue<TItem, TDistance>(maxCapacity, distanceMath);
		}

		public NearestNeighbourList(ITypeMath<TDistance> distanceMath)
		{
			this.maxCapacity = int.MaxValue;
			this.distanceMath = distanceMath;

			queue = new PriorityQueue<TItem, TDistance>(distanceMath);
		}

		private PriorityQueue<TItem, TDistance> queue;

		private ITypeMath<TDistance> distanceMath;

		private int maxCapacity;
		public int MaxCapacity { get { return maxCapacity; } }

		public int Count { get { return queue.Count; } }

		public bool Add(TItem item, TDistance distance)
		{
			if (queue.Count >= maxCapacity)
			{
				// If the distance of this item is less than the distance of the last item
				// in our neighbour list then pop that neighbour off and push this one on
				// otherwise don't even bother adding this item
				if (distanceMath.Compare(distance, queue.GetHighestPriority()) < 0)
				{
					queue.Dequeue();
					queue.Enqueue(item, distance);
					return true;
				}
				else
					return false;
			}
			else
			{
				queue.Enqueue(item, distance);
				return true;
			}
		}

		public TItem GetFurtherest()
		{
			if (Count == 0)
				throw new Exception("List is empty");
			else
				return queue.GetHighest();
		}

		public TDistance GetFurtherestDistance()
		{
			if (Count == 0)
				throw new Exception("List is empty");
			else
				return queue.GetHighestPriority();
		}

		public TItem RemoveFurtherest()
		{
			return queue.Dequeue();
		}

		public bool IsCapacityReached
		{
			get { return Count == MaxCapacity; }
		}
	}
}
