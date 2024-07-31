using SIMULTAN.Data.SimMath;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.MultiValues
{
    /// <summary>
    /// Stores data for one graph in a SimMultiValueFunction. Currently only linear interpolation is supported (aka polyline graphs)
    /// </summary>
    public class SimMultiValueFunctionGraph : INotifyPropertyChanged
    {
        /// <summary>
        /// Control points of the graph.
        /// </summary>
        public SimMultiValueFunctionPointList Points { get; }

        /// <summary>
        /// Stores the SimMultiValueFunction this graph belongs to. Returns null when the graph is not part of a SimMultiValueFunction
        /// </summary>
        public SimMultiValueFunction Function { get; internal set; }

        /// <summary>
        /// Gets or sets the name of the graph
        /// </summary>
        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    NotifyPropertyChanged(nameof(Name));
                    Function?.Factory?.NotifyChanged();
                }
            }
        }
        private string name;


        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Invokes the PropertyChanged event
        /// </summary>
        /// <param name="prop">Name of the property</param>
        protected void NotifyPropertyChanged(string prop)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }



        /// <summary>
        /// Initializes a new instance of the SimMultiValueFunctionGraph class
        /// </summary>
        /// <param name="name">Name of the graph</param>
        /// <param name="points">The control points of this graph</param>
        public SimMultiValueFunctionGraph(string name, IEnumerable<SimPoint3D> points)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            this.name = name;
            this.Points = new SimMultiValueFunctionPointList(this, points);
        }



        /// <summary>
        /// Creates a deep copy of this graph
        /// </summary>
        /// <returns>A new graph containing a copy of the data</returns>
        public SimMultiValueFunctionGraph Clone()
        {
            return new SimMultiValueFunctionGraph(this.name, this.Points);
        }

        /// <summary>
        /// Searches for the closest point on the graph to a given position
        /// </summary>
        /// <param name="position">The position towards which the closest point is searched</param>
        /// <returns>
        /// The closest point on the graph to position, 
        /// The distance between the two points and 
        /// the graph value at the closest point (Y value of the graph)
        /// </returns>
        public (SimPoint3D closestPoint, double distance, double value) ClosestPoint(SimPoint3D position)
        {
            if (this.Points.Count == 0) //No points
                return (new SimPoint3D(double.NaN, double.NaN, double.NaN), double.NaN, double.NaN);
            else if (Math.Abs(this.Points[0].Z - position.Z) > 0.0001) //Other z layer
                return (new SimPoint3D(double.NaN, double.NaN, double.NaN), double.NaN, double.NaN);
            else if (this.Points.Count == 1) //Exactly one point
                return (Points[0], (position - Points[0]).Length, Points[0].Y);

            double minDistance = double.PositiveInfinity;
            SimPoint3D minPoint = new SimPoint3D(double.NaN, double.NaN, double.NaN);
            int minIndex = -1;

            var pLast = Points[0];
            for (int i = 1; i < Points.Count; ++i)
            {
                var pNext = Points[i];
                var cp = ClosestPointOnLine(pLast, pNext, position);
                if (cp.distance < minDistance)
                {
                    minDistance = cp.distance;
                    minPoint = cp.p;
                    minIndex = i - 1;
                }
                pLast = pNext;
            }

            return (minPoint, minDistance, minPoint.Y);
        }

        /// <summary>
        /// Returns the closest value on a graph depending on a x-position
        /// This method searches for all intersections of the graph and the given x-position.
        /// When there are multiple hits, the value closest to previouseY is returned
        /// </summary>
        /// <param name="x">The x value</param>
        /// <param name="previouseY">The reference value for y. When multiple graphs cross the x-value, the returned value will be closest to this point</param>
        /// <returns>The y value at the given x-coordinate</returns>
        public double GetValueFromX(double x, double previouseY = 0.0)
        {
            double minValue = double.NaN;
            double minDistance = double.PositiveInfinity;
            bool found = false;

            for (int i = 0; i < Points.Count - 1; ++i)
            {
                var p1 = Points[i];
                var p2 = Points[i + 1];

                if (p1.X > p2.X)
                    (p2, p1) = (p1, p2);

                if (p1.X <= x && p2.X >= x)
                {
                    //Line found
                    double lin = (x - p1.X) / (p2.X - p1.X);
                    if (Math.Abs(p1.X - p2.X) < 0.00001)
                        lin = 0;

                    //Check if the new line is closer to the y value than others
                    var lineY = p1.Y + lin * (p2.Y - p1.Y);
                    var lineYDistance = Math.Abs(lineY - previouseY);

                    if (minDistance > lineYDistance)
                    {
                        minDistance = lineYDistance;
                        minValue = lineY;
                    }
                    found = true;
                }
            }

            if (found)
                return minValue;
            else
                return double.NaN;
        }

        private (SimPoint3D p, double distance) ClosestPointOnLine(SimPoint3D p1, SimPoint3D p2, SimPoint3D position)
        {
            var ap = position - p1;
            var ab = p2 - p1;

            double magnitudeAB = ab.LengthSquared;
            double ABAPproduct = ap.X * ab.X + ap.Y * ab.Y;
            double distance = ABAPproduct / magnitudeAB;

            if (distance <= 0)
                return (p1, (position - p1).Length);
            else if (distance >= 1)
                return (p2, (position - p2).Length);
            else
            {
                var ip = p1 + ab * distance;
                return (ip, (position - ip).Length);
            }
        }

        /// <summary>
        /// Samples the graph with a specific step size.
        /// Note, that the last step might be smaller than stepSize.
        /// Returns 0 when the graph is not defined at a specific location. 
        /// In ambigiouse cases, the value closest to the previouse sample is returned (starting with 0).
        /// </summary>
        /// <param name="start">Start of the sampling range on the X-Axis</param>
        /// <param name="end">End of the sampling range on the X-Axis</param>
        /// <param name="stepSize">Step-Size along the X-Axis</param>
        /// <returns>A list of sampled values. Returns 0 values outside of the graph area</returns>
        public List<double> Sample(double start, double end, double stepSize)
        {
            double length = end - start;
            int numSamples = (int)Math.Floor(length / stepSize) + 1;
            List<double> results = new List<double>(numSamples);

            var lastSample = 0.0;
            for (int i = 0; i < numSamples - 1; ++i)
            {
                double samplePos = start + i * stepSize;
                lastSample = GetValueFromX(samplePos, lastSample);

                if (double.IsNaN(lastSample))
                    lastSample = 0; //Prevents problems with closes point matching

                results.Add(lastSample);
            }

            lastSample = GetValueFromX(end, lastSample);
            if (double.IsNaN(lastSample))
                lastSample = 0;
            results.Add(lastSample);

            return results;
        }

        /// <summary>
        /// Makes sure that all points lie inside the definition space of the SimMultiValueFunction.
        /// Does nothing when the graph is not part of a SimMultiValueFunction.
        /// </summary>
        internal void ClampToValidRange()
        {
            this.Points.Clamp();
        }
    }
}
