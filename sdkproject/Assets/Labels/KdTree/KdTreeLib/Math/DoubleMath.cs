using System;

namespace KdTree.Math
{
    [Serializable]
    public class DoubleMath : TypeMath<double>
    {
        public override int Compare(double a, double b)
        {
            return a.CompareTo(b);
        }

        public override bool AreEqual(double a, double b)
        {
            return a == b;
        }

        public override double MinValue
        {
            get { return double.MinValue; }
        }

        public override double MaxValue
        {
            get { return double.MaxValue; }
        }

        public override double Zero
        {
            get { return 0; }
        }

        public override double NegativeInfinity { get { return double.NegativeInfinity; } }

        public override double PositiveInfinity { get { return double.PositiveInfinity; } }

        public override double Add(double a, double b)
        {
            return a + b;
        }

        public override double Subtract(double a, double b)
        {
            return a - b;
        }

        public override double Multiply(double a, double b)
        {
            return a * b;
        }

        public override double DistanceSquaredBetweenPoints(double[] a, double[] b)
        {
            double distance = Zero;
            int dimensions = a.Length;

            // Return the absolute distance bewteen 2 hyper points
            for (var dimension = 0; dimension < dimensions; dimension++)
            {
                double distOnThisAxis = Subtract(a[dimension], b[dimension]);
                double distOnThisAxisSquared = Multiply(distOnThisAxis, distOnThisAxis);

                distance = Add(distance, distOnThisAxisSquared);
            }

            return distance;
        }
    }
}
