using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace KdTree
{
    public enum AddDuplicateBehavior
    {
        Skip,
        Error,
        Update
    }

    public class DuplicateNodeError : Exception
    {
        public DuplicateNodeError()
            : base("Cannot Add Node With Duplicate Coordinates")
        {
        }
    }

    [Serializable]
    public class KdTree<TKey, TValue> : IKdTree<TKey, TValue>
    {
        public KdTree(int dimensions, ITypeMath<TKey> typeMath)
        {
            this.dimensions = dimensions;
            this.typeMath = typeMath;
            Count = 0;
        }

        public KdTree(int dimensions, ITypeMath<TKey> typeMath, AddDuplicateBehavior addDuplicateBehavior)
            : this(dimensions, typeMath)
        {
            AddDuplicateBehavior = addDuplicateBehavior;
        }

        private int dimensions;

        private ITypeMath<TKey> typeMath = null;

        private KdTreeNode<TKey, TValue> root = null;

        public AddDuplicateBehavior AddDuplicateBehavior { get; private set; }

        public bool Add(TKey[] point, TValue value)
        {
            var nodeToAdd = new KdTreeNode<TKey, TValue>(point, value);

            if (root == null)
            {
                root = new KdTreeNode<TKey, TValue>(point, value);
            }
            else
            {
                int dimension = -1;
                KdTreeNode<TKey, TValue> parent = root;

                do
                {
                    // Increment the dimension we're searching in
                    dimension = (dimension + 1) % dimensions;

                    // Does the node we're adding have the same hyperpoint as this node?
                    if (typeMath.AreEqual(point, parent.Point))
                    {
                        switch (AddDuplicateBehavior)
                        {
                            case AddDuplicateBehavior.Skip:
                                return false;

                            case AddDuplicateBehavior.Error:
                                throw new DuplicateNodeError();

                            case AddDuplicateBehavior.Update:
                                parent.Value = value;
                                return true;

                            default:
                                // Should never happen
                                throw new Exception("Unexpected AddDuplicateBehavior");
                        }
                    }

                    // Which side does this node sit under in relation to it's parent at this level?
                    int compare = typeMath.Compare(point[dimension], parent.Point[dimension]);

                    if (parent[compare] == null)
                    {
                        parent[compare] = nodeToAdd;
                        break;
                    }
                    else
                    {
                        parent = parent[compare];
                    }
                } while (true);
            }

            Count++;
            return true;
        }

        private void ReaddChildNodes(KdTreeNode<TKey, TValue> removedNode)
        {
            if (removedNode.IsLeaf)
                return;

            // The folllowing code might seem a little redundant but we're using 
            // 2 queues so we can add the child nodes back in, in (more or less) 
            // the same order they were added in the first place
            var nodesToReadd = new Queue<KdTreeNode<TKey, TValue>>();

            var nodesToReaddQueue = new Queue<KdTreeNode<TKey, TValue>>();

            if (removedNode.LeftChild != null)
                nodesToReaddQueue.Enqueue(removedNode.LeftChild);

            if (removedNode.RightChild != null)
                nodesToReaddQueue.Enqueue(removedNode.RightChild);

            while (nodesToReaddQueue.Count > 0)
            {
                var nodeToReadd = nodesToReaddQueue.Dequeue();

                nodesToReadd.Enqueue(nodeToReadd);

                for (int side = -1; side <= 1; side += 2)
                {
                    if (nodeToReadd[side] != null)
                    {
                        nodesToReaddQueue.Enqueue(nodeToReadd[side]);

                        nodeToReadd[side] = null;
                    }
                }
            }

            while (nodesToReadd.Count > 0)
            {
                var nodeToReadd = nodesToReadd.Dequeue();

                Count--;
                Add(nodeToReadd.Point, nodeToReadd.Value);
            }
        }

        public void RemoveAt(TKey[] point)
        {
            // Is tree empty?
            if (root == null)
                return;

            KdTreeNode<TKey, TValue> node;

            if (typeMath.AreEqual(point, root.Point))
            {
                node = root;
                root = null;
                Count--;
                ReaddChildNodes(node);
                return;
            }

            node = root;

            int dimension = -1;
            do
            {
                dimension = (dimension + 1) % dimensions;

                int compare = typeMath.Compare(point[dimension], node.Point[dimension]);

                if (node[compare] == null)
                    // Can't find node
                    return;

                if (typeMath.AreEqual(point, node[compare].Point))
                {
                    var nodeToRemove = node[compare];
                    node[compare] = null;
                    Count--;

                    ReaddChildNodes(nodeToRemove);
                }
                else
                    node = node[compare];
            } while (node != null);
        }

        public KdTreeNode<TKey, TValue>[] GetNearestNeighbours(TKey[] point, int count)
        {
            if (count > Count)
                count = Count;

            if (count < 0)
            {
                throw new ArgumentException("Number of neighbors cannot be negative");
            }

            if (count == 0)
                return new KdTreeNode<TKey, TValue>[0];

            var neighbours = new KdTreeNode<TKey, TValue>[count];

            var nearestNeighbours = new NearestNeighbourList<KdTreeNode<TKey, TValue>, TKey>(count, typeMath);

            var rect = HyperRect<TKey>.Infinite(dimensions, typeMath);

            AddNearestNeighbours(root, point, rect, 0, nearestNeighbours, typeMath.MaxValue);

            count = nearestNeighbours.Count;

            var neighbourArray = new KdTreeNode<TKey, TValue>[count];

            for (var index = 0; index < count; index++)
                neighbourArray[count - index - 1] = nearestNeighbours.RemoveFurtherest();

            return neighbourArray;
        }

        /*
         * 1. Search for the target
         * 
         *   1.1 Start by splitting the specified hyper rect
         *       on the specified node's point along the current
         *       dimension so that we end up with 2 sub hyper rects
         *       (current dimension = depth % dimensions)
         *   
         *	 1.2 Check what sub rectangle the the target point resides in
         *	     under the current dimension
         *	     
         *   1.3 Set that rect to the nearer rect and also the corresponding 
         *       child node to the nearest rect and node and the other rect 
         *       and child node to the further rect and child node (for use later)
         *       
         *   1.4 Travel into the nearer rect and node by calling function
         *       recursively with nearer rect and node and incrementing 
         *       the depth
         * 
         * 2. Add leaf to list of nearest neighbours
         * 
         * 3. Walk back up tree and at each level:
         * 
         *    3.1 Add node to nearest neighbours if
         *        we haven't filled our nearest neighbour
         *        list yet or if it has a distance to target less
         *        than any of the distances in our current nearest 
         *        neighbours.
         *        
         *    3.2 If there is any point in the further rectangle that is closer to
         *        the target than our furtherest nearest neighbour then travel into
         *        that rect and node
         * 
         *  That's it, when it finally finishes traversing the branches 
         *  it needs to we'll have our list!
         */

        private void AddNearestNeighbours(
            KdTreeNode<TKey, TValue> node,
            TKey[] target,
            HyperRect<TKey> rect,
            int depth,
            NearestNeighbourList<KdTreeNode<TKey, TValue>, TKey> nearestNeighbours,
            TKey maxSearchRadiusSquared)
        {
            if (node == null)
                return;

            // Work out the current dimension
            int dimension = depth % dimensions;

            // Split our hyper-rect into 2 sub rects along the current 
            // node's point on the current dimension
            var leftRect = rect.Clone();
            leftRect.MaxPoint[dimension] = node.Point[dimension];

            var rightRect = rect.Clone();
            rightRect.MinPoint[dimension] = node.Point[dimension];

            // Which side does the target reside in?
            int compare = typeMath.Compare(target[dimension], node.Point[dimension]);

            var nearerRect = compare <= 0 ? leftRect : rightRect;
            var furtherRect = compare <= 0 ? rightRect : leftRect;

            var nearerNode = compare <= 0 ? node.LeftChild : node.RightChild;
            var furtherNode = compare <= 0 ? node.RightChild : node.LeftChild;

            // Let's walk down into the nearer branch
            if (nearerNode != null)
            {
                AddNearestNeighbours(
                    nearerNode,
                    target,
                    nearerRect,
                    depth + 1,
                    nearestNeighbours,
                    maxSearchRadiusSquared);
            }

            TKey distanceSquaredToTarget;

            // Walk down into the further branch but only if our capacity hasn't been reached 
            // OR if there's a region in the further rect that's closer to the target than our
            // current furtherest nearest neighbour
            TKey[] closestPointInFurtherRect = furtherRect.GetClosestPoint(target, typeMath);
            distanceSquaredToTarget = typeMath.DistanceSquaredBetweenPoints(closestPointInFurtherRect, target);

            if (typeMath.Compare(distanceSquaredToTarget, maxSearchRadiusSquared) <= 0)
            {
                if (nearestNeighbours.IsCapacityReached)
                {
                    if (typeMath.Compare(distanceSquaredToTarget, nearestNeighbours.GetFurtherestDistance()) < 0)
                        AddNearestNeighbours(
                            furtherNode,
                            target,
                            furtherRect,
                            depth + 1,
                            nearestNeighbours,
                            maxSearchRadiusSquared);
                }
                else
                {
                    AddNearestNeighbours(
                        furtherNode,
                        target,
                        furtherRect,
                        depth + 1,
                        nearestNeighbours,
                        maxSearchRadiusSquared);
                }
            }

            // Try to add the current node to our nearest neighbours list
            distanceSquaredToTarget = typeMath.DistanceSquaredBetweenPoints(node.Point, target);

            if (typeMath.Compare(distanceSquaredToTarget, maxSearchRadiusSquared) <= 0)
                nearestNeighbours.Add(node, distanceSquaredToTarget);
        }

        /// <summary>
        /// Performs a radial search.
        /// </summary>
        /// <param name="center">Center point</param>
        /// <param name="radius">Radius to find neighbours within</param>
        public KdTreeNode<TKey, TValue>[] RadialSearch(TKey[] center, TKey radius)
        {
            var nearestNeighbours = new NearestNeighbourList<KdTreeNode<TKey, TValue>, TKey>(typeMath);
            return RadialSearch(center, radius, nearestNeighbours);
        }

        /// <summary>
        /// Performs a radial search up to a maximum count.
        /// </summary>
        /// <param name="center">Center point</param>
        /// <param name="radius">Radius to find neighbours within</param>
        /// <param name="count">Maximum number of neighbours</param>
        public KdTreeNode<TKey, TValue>[] RadialSearch(TKey[] center, TKey radius, int count)
        {
            var nearestNeighbours = new NearestNeighbourList<KdTreeNode<TKey, TValue>, TKey>(count, typeMath);
            return RadialSearch(center, radius, nearestNeighbours);
        }

        private KdTreeNode<TKey, TValue>[] RadialSearch(TKey[] center, TKey radius,
            NearestNeighbourList<KdTreeNode<TKey, TValue>, TKey> nearestNeighbours)
        {
            AddNearestNeighbours(
                root,
                center,
                HyperRect<TKey>.Infinite(dimensions, typeMath),
                0,
                nearestNeighbours,
                typeMath.Multiply(radius, radius));

            var count = nearestNeighbours.Count;

            var neighbourArray = new KdTreeNode<TKey, TValue>[count];

            for (var index = 0; index < count; index++)
                neighbourArray[count - index - 1] = nearestNeighbours.RemoveFurtherest();

            return neighbourArray;
        }

        public int Count { get; private set; }

        private TValue _cachedValue;
        public bool TryFindValueAt(TKey[] point)
        {
            var parent = root;
            int dimension = -1;
            do
            {
                if (parent == null)
                {
                    _cachedValue = default(TValue);
                    return false;
                }
                else if (typeMath.AreEqual(point, parent.Point))
                {
                    _cachedValue = parent.Value;
                    return true;
                }

                // Keep searching
                dimension = (dimension + 1) % dimensions;
                int compare = typeMath.Compare(point[dimension], parent.Point[dimension]);
                parent = parent[compare];
            } while (true);
        }

        public TValue FindValueAt(TKey[] point)
        {
            if (TryFindValueAt(point))
                return _cachedValue;
            else
                return default(TValue);
        }

        public TKey[] TryFindValue(TValue value)
        {
            if (root == null)
            {
                return null;
            }

            // First-in, First-out list of nodes to search
            var nodesToSearch = new Queue<KdTreeNode<TKey, TValue>>();

            nodesToSearch.Enqueue(root);

            while (nodesToSearch.Count > 0)
            {
                var nodeToSearch = nodesToSearch.Dequeue();

                if (nodeToSearch.Value.Equals(value))
                {
                    return nodeToSearch.Point;
                }
                else
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        var childNode = nodeToSearch[side];

                        if (childNode != null)
                            nodesToSearch.Enqueue(childNode);
                    }
                }
            }

            return null;
        }

        public TKey[] FindValue(TValue value)
        {
            var point = TryFindValue(value);
            return point;
        }

        private void AddNodeToStringBuilder(KdTreeNode<TKey, TValue> node, StringBuilder sb, int depth)
        {
            sb.AppendLine(node.ToString());

            for (var side = -1; side <= 1; side += 2)
            {
                for (var index = 0; index <= depth; index++)
                    sb.Append("\t");

                sb.Append(side == -1 ? "L " : "R ");

                if (node[side] == null)
                    sb.AppendLine("");
                else
                    AddNodeToStringBuilder(node[side], sb, depth + 1);
            }
        }

        public override string ToString()
        {
            if (root == null)
                return "";

            var sb = new StringBuilder();
            AddNodeToStringBuilder(root, sb, 0);
            return sb.ToString();
        }

        private void AddNodesToList(KdTreeNode<TKey, TValue> node, List<KdTreeNode<TKey, TValue>> nodes)
        {
            if (node == null)
                return;

            nodes.Add(node);

            for (var side = -1; side <= 1; side += 2)
            {
                if (node[side] != null)
                {
                    AddNodesToList(node[side], nodes);
                    node[side] = null;
                }
            }
        }

        private void SortNodesArray(KdTreeNode<TKey, TValue>[] nodes, int byDimension, int fromIndex, int toIndex)
        {
            for (var index = fromIndex + 1; index <= toIndex; index++)
            {
                var newIndex = index;

                while (true)
                {
                    var a = nodes[newIndex - 1];
                    var b = nodes[newIndex];
                    if (typeMath.Compare(b.Point[byDimension], a.Point[byDimension]) < 0)
                    {
                        nodes[newIndex - 1] = b;
                        nodes[newIndex] = a;
                    }
                    else
                        break;
                }
            }
        }

        private void AddNodesBalanced(KdTreeNode<TKey, TValue>[] nodes, int byDimension, int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex)
            {
                Add(nodes[fromIndex].Point, nodes[fromIndex].Value);
                nodes[fromIndex] = null;
                return;
            }

            // Sort the array from the fromIndex to the toIndex
            SortNodesArray(nodes, byDimension, fromIndex, toIndex);

            // Find the splitting point
            int midIndex = fromIndex + (int) System.Math.Round((toIndex + 1 - fromIndex) / 2f) - 1;

            // Add the splitting point
            Add(nodes[midIndex].Point, nodes[midIndex].Value);
            nodes[midIndex] = null;

            // Recurse
            int nextDimension = (byDimension + 1) % dimensions;

            if (fromIndex < midIndex)
                AddNodesBalanced(nodes, nextDimension, fromIndex, midIndex - 1);

            if (toIndex > midIndex)
                AddNodesBalanced(nodes, nextDimension, midIndex + 1, toIndex);
        }

        public void Balance()
        {
            var nodeList = new List<KdTreeNode<TKey, TValue>>();
            AddNodesToList(root, nodeList);

            Clear();

            AddNodesBalanced(nodeList.ToArray(), 0, 0, nodeList.Count - 1);
        }

        private void RemoveChildNodes(KdTreeNode<TKey, TValue> node)
        {
            for (var side = -1; side <= 1; side += 2)
            {
                if (node[side] != null)
                {
                    RemoveChildNodes(node[side]);
                    node[side] = null;
                }
            }
        }

        public void Clear()
        {
            if (root != null)
                RemoveChildNodes(root);
        }

        public void SaveToFile(string filename)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = File.Create(filename))
            {
                formatter.Serialize(stream, this);
                stream.Flush();
            }
        }

        public static KdTree<TKey, TValue> LoadFromFile(string filename)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = File.Open(filename, FileMode.Open))
            {
                return (KdTree<TKey, TValue>) formatter.Deserialize(stream);
            }
        }

        public IEnumerator<KdTreeNode<TKey, TValue>> GetEnumerator()
        {
            var left = new Stack<KdTreeNode<TKey, TValue>>();
            var right = new Stack<KdTreeNode<TKey, TValue>>();

            if (root != null)
            {
                yield return root;

                if (root.LeftChild != null)
                {
                    left.Push(root.LeftChild);
                }

                if (root.RightChild != null)
                {
                    right.Push(root.RightChild);
                }

                while (true)
                {
                    if (left.Any())
                    {
                        var item = left.Pop();

                        if (item.LeftChild != null)
                        {
                            left.Push(item.LeftChild);
                        }

                        if (item.RightChild != null)
                        {
                            right.Push(item.RightChild);
                        }

                        yield return item;
                    }
                    else if (right.Any())
                    {
                        var item = right.Pop();

                        if (item.LeftChild != null)
                        {
                            left.Push(item.LeftChild);
                        }

                        if (item.RightChild != null)
                        {
                            right.Push(item.RightChild);
                        }

                        yield return item;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}