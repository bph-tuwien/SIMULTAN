using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Values
{
    [TestClass]
    public class SimMultiValueFunctionGraphTests
    {
        private static List<List<Point3D>> testPoints = new List<List<Point3D>>()
        {
            new List<Point3D>
            {
                new Point3D(0,0,0), new Point3D(0.5,0,0), new Point3D(1,0.5,0)
            },
            new List<Point3D>
            {
                new Point3D(0,1,0), new Point3D(0.25,0,0), new Point3D(0.5,1,0), new Point3D(1, 0.6, 0)
            },
            new List<Point3D>
            {
                new Point3D(0, 0, 0), new Point3D(1, 0, 0), new Point3D(0.75, 1, 0), new Point3D(0, 1, 0)
            },
            new List<Point3D>
            {
                new Point3D(0, 0, 0), new Point3D(1, 0.5, 0), new Point3D(0.75, 1, 0), new Point3D(0, 1, 0)
            },
            new List<Point3D>
            {

            },
            new List<Point3D>
            {
                new Point3D(0.5, 0.5, 0)
            }
        };

        public static (string name, List<Point3D> points) TestData(int pointSet, double sizeX, double SizeY, int z)
        {
            var points = testPoints[pointSet].Select(x => new Point3D(x.X * sizeX, x.Y * SizeY, z)).ToList();
            return (string.Format("graph_{0}_{1}", pointSet, z), points);
        }

        public static (SimMultiValueFunctionGraph graph, (string name, List<Point3D> points) data) TestDataGraph(int pointSet, double sizeX, double SizeY, int z)
        {
            var data = TestData(pointSet, sizeX, SizeY, z);
            return (new SimMultiValueFunctionGraph(data.name, data.points), data);
        }


        public static void CheckTestData(SimMultiValueFunctionGraph graph, (string name, List<Point3D> points) data)
        {
            Assert.AreEqual(data.name, graph.Name);

            Assert.AreEqual(data.points.Count, graph.Points.Count);
            for (int i = 0; i < data.points.Count; ++i)
            {
                AssertUtil.AreEqual(data.points[i], graph.Points[i]);
            }
        }

        public static void Check(SimMultiValueFunctionGraph expected, SimMultiValueFunctionGraph actual)
        {
            Assert.AreEqual(expected.Name, actual.Name);

            Assert.AreEqual(expected.Points.Count, actual.Points.Count);
            for (int i = 0; i < expected.Points.Count; ++i)
            {
                AssertUtil.AreEqual(expected.Points[i], actual.Points[i]);
            }
        }


        [TestMethod]
        public void Ctor()
        {
            var data = TestData(0, 2, 2, 0);

            Assert.ThrowsException<ArgumentNullException>(() => { new SimMultiValueFunctionGraph(null, data.points); });
            Assert.ThrowsException<ArgumentNullException>(() => { new SimMultiValueFunctionGraph(data.name, null); });

            var graph = new SimMultiValueFunctionGraph(data.name, data.points);
            CheckTestData(graph, data);
        }

        [TestMethod]
        public void Clone()
        {
            var data = TestDataGraph(0, 2, 2, 0);

            var copy = data.graph.Clone();
            CheckTestData(copy, data.data);
        }

        [TestMethod]
        public void PropertyChanged()
        {
            var data = TestDataGraph(0, 2, 2, 0);
            var events = new PropertyChangedEventCounter(data.graph);

            data.graph.Name = "newname";
            Assert.AreEqual("newname", data.graph.Name);
            events.AssertEventCount(1);
        }


        [TestMethod]
        public void AddPoint()
        {
            var data = TestDataGraph(0, 2, 2, 0);
            var eventCounter = new CollectionChangedEventCounter(data.graph.Points);

            data.data.points.Add(new Point3D(3, 3, 0));
            data.graph.Points.Add(new Point3D(3, 3, 0));
            CheckTestData(data.graph, data.data);
            eventCounter.AssertEventCount(1);
        }
        [TestMethod]
        public void InsertPoint()
        {
            var data = TestDataGraph(0, 2, 2, 0);
            var eventCounter = new CollectionChangedEventCounter(data.graph.Points);

            data.data.points.Insert(1, new Point3D(3, 3, 0));
            data.graph.Points.Insert(1, new Point3D(3, 3, 0));
            CheckTestData(data.graph, data.data);
            eventCounter.AssertEventCount(1);
        }
        [TestMethod]
        public void RemovePoint()
        {
            var data = TestDataGraph(0, 2, 2, 0);
            var eventCounter = new CollectionChangedEventCounter(data.graph.Points);

            data.data.points.RemoveAt(1);
            data.graph.Points.RemoveAt(1);
            CheckTestData(data.graph, data.data);
            eventCounter.AssertEventCount(1);
        }
        [TestMethod]
        public void ChangePoint()
        {
            var data = TestDataGraph(0, 2, 2, 0);
            var eventCounter = new CollectionChangedEventCounter(data.graph.Points);

            data.data.points[1] = new Point3D(3, 3, 0);
            data.graph.Points[1] = new Point3D(3, 3, 0);
            CheckTestData(data.graph, data.data);
            eventCounter.AssertEventCount(1);
        }


        [TestMethod]
        public void AddPointClamped()
        {
            var funcData = SimMultiValueFunctionTests.TestData(1);
            var graphData = TestDataGraph(0, 2, 2, 0);

            var func = new SimMultiValueFunction(funcData.name, funcData.unitX, funcData.unitY, funcData.unitZ, funcData.bounds, funcData.zs,
                new List<SimMultiValueFunctionGraph> { graphData.graph });

            //Not clamped
            graphData.data.points.Add(new Point3D(2, 3, 0));
            graphData.graph.Points.Add(new Point3D(2, 3, 0));
            CheckTestData(graphData.graph, graphData.data);

            //Clamped
            graphData.data.points.Add(new Point3D(3, 3, 0));
            graphData.graph.Points.Add(new Point3D(5, 6, 0));
            CheckTestData(graphData.graph, graphData.data);
        }
        [TestMethod]
        public void InsertPointClamped()
        {
            var funcData = SimMultiValueFunctionTests.TestData(1);
            var graphData = TestDataGraph(0, 2, 2, 0);

            var func = new SimMultiValueFunction(funcData.name, funcData.unitX, funcData.unitY, funcData.unitZ, funcData.bounds, funcData.zs,
                new List<SimMultiValueFunctionGraph> { graphData.graph });

            //Not clamped
            graphData.data.points.Insert(1, new Point3D(2, 3, 0));
            graphData.graph.Points.Insert(1, new Point3D(2, 3, 0));
            CheckTestData(graphData.graph, graphData.data);

            //Clamped
            graphData.data.points.Insert(1, new Point3D(3, 3, 0));
            graphData.graph.Points.Insert(1, new Point3D(5, 6, 0));
            CheckTestData(graphData.graph, graphData.data);
        }
        [TestMethod]
        public void ChangePointClamped()
        {
            var funcData = SimMultiValueFunctionTests.TestData(1);
            var graphData = TestDataGraph(0, 2, 2, 0);

            var func = new SimMultiValueFunction(funcData.name, funcData.unitX, funcData.unitY, funcData.unitZ, funcData.bounds, funcData.zs,
                new List<SimMultiValueFunctionGraph> { graphData.graph });

            //Not clamped
            graphData.data.points[1] = new Point3D(2, 3, 0);
            graphData.graph.Points[1] = new Point3D(2, 3, 0);
            CheckTestData(graphData.graph, graphData.data);

            //Clamped
            graphData.data.points[2] = new Point3D(3, 3, 0);
            graphData.graph.Points[2] = new Point3D(5, 6, 0);
            CheckTestData(graphData.graph, graphData.data);
        }


        [TestMethod]
        public void ClosestPoint()
        {
            var graphData = TestDataGraph(0, 2, 2, 0);

            //Sample left of leftmost point
            var sample = graphData.graph.ClosestPoint(new Point3D(-3, 0, 0));
            AssertUtil.AreEqual(new Point3D(0, 0, 0), sample.closestPoint);
            AssertUtil.AssertDoubleEqual(3, sample.distance);
            AssertUtil.AssertDoubleEqual(0, sample.value);

            //Sample right of rightmost point
            sample = graphData.graph.ClosestPoint(new Point3D(4, 0, 0));
            AssertUtil.AreEqual(new Point3D(2, 1, 0), sample.closestPoint);
            AssertUtil.AssertDoubleEqual(Math.Sqrt(2 * 2 + 1 * 1), sample.distance);
            AssertUtil.AssertDoubleEqual(1, sample.value);

            //Sample on graph
            sample = graphData.graph.ClosestPoint(new Point3D(1.5, 0.5, 0));
            AssertUtil.AreEqual(new Point3D(1.5, 0.5, 0), sample.closestPoint);
            AssertUtil.AssertDoubleEqual(0, sample.distance);
            AssertUtil.AssertDoubleEqual(0.5, sample.value);

            //Sample next to graph
            sample = graphData.graph.ClosestPoint(new Point3D(1.4, 0.6, 0));
            AssertUtil.AreEqual(new Point3D(1.5, 0.5, 0), sample.closestPoint);
            AssertUtil.AssertDoubleEqual(Math.Sqrt(0.1 * 0.1 + 0.1 * 0.1), sample.distance);
            AssertUtil.AssertDoubleEqual(0.5, sample.value);
        }
        [TestMethod]
        public void ClosestPointEmptyGraph()
        {
            var graphData = TestDataGraph(4, 2, 2, 0);

            //Sample left of leftmost point
            var sample = graphData.graph.ClosestPoint(new Point3D(0, 0, 0));
            AssertUtil.AreEqual(new Point3D(double.NaN, double.NaN, double.NaN), sample.closestPoint);
            AssertUtil.AssertDoubleEqual(double.NaN, sample.distance);
            AssertUtil.AssertDoubleEqual(double.NaN, sample.value);
        }
        [TestMethod]
        public void ClosestPointWrongZ()
        {
            var graphData = TestDataGraph(0, 2, 2, 0);

            //Sample left of leftmost point
            var sample = graphData.graph.ClosestPoint(new Point3D(0, 0, 2.0));
            AssertUtil.AreEqual(new Point3D(double.NaN, double.NaN, double.NaN), sample.closestPoint);
            AssertUtil.AssertDoubleEqual(double.NaN, sample.distance);
            AssertUtil.AssertDoubleEqual(double.NaN, sample.value);
        }
        [TestMethod]
        public void ClosestPointSinglePoint()
        {
            var graphData = TestDataGraph(5, 2, 2, 0);

            //Sample left of leftmost point
            var sample = graphData.graph.ClosestPoint(new Point3D(0, 0, 0));
            AssertUtil.AreEqual(new Point3D(1, 1, 0), sample.closestPoint);
            AssertUtil.AssertDoubleEqual(Math.Sqrt(2), sample.distance);
            AssertUtil.AssertDoubleEqual(1, sample.value);
        }

        [TestMethod]
        public void GetValueFromX()
        {
            var graphData = TestDataGraph(0, 2, 2, 0);

            //Left of graph
            var sample = graphData.graph.GetValueFromX(-2, 0);
            Assert.IsTrue(double.IsNaN(sample));

            //Right of graph
            sample = graphData.graph.GetValueFromX(4, 0);
            Assert.IsTrue(double.IsNaN(sample));

            //On Graph
            sample = graphData.graph.GetValueFromX(0.5, 0);
            AssertUtil.AssertDoubleEqual(0, sample);

            sample = graphData.graph.GetValueFromX(1.5, 0);
            AssertUtil.AssertDoubleEqual(0.5, sample);
        }
        [TestMethod]
        public void GetValueFromXNonUnique()
        {
            var graphData = TestDataGraph(2, 2, 2, 0);

            //On Graph
            var sample = graphData.graph.GetValueFromX(0.5, 0);
            AssertUtil.AssertDoubleEqual(0, sample);

            sample = graphData.graph.GetValueFromX(0.5, 0.95);
            AssertUtil.AssertDoubleEqual(0, sample);

            sample = graphData.graph.GetValueFromX(0.5, 1.05);
            AssertUtil.AssertDoubleEqual(2, sample);

            sample = graphData.graph.GetValueFromX(0.5, 2.5);
            AssertUtil.AssertDoubleEqual(2, sample);
        }
        [TestMethod]
        public void Sample()
        {
            var graphData = TestDataGraph(0, 2, 2, 0);

            var samples = graphData.graph.Sample(-1, 3, 0.5);
            List<double> expected = new List<double>() { 0, 0, 0, 0, 0, 0.5, 1, 0, 0 };
            Assert.AreEqual(expected.Count, samples.Count);
            for (int i = 0; i < expected.Count; ++i)
                AssertUtil.AssertDoubleEqual(expected[i], samples[i]);
        }
        [TestMethod]
        public void SampleNonUnique()
        {
            var graphData = TestDataGraph(3, 2, 2, 0);

            var samples = graphData.graph.Sample(-1, 3, 0.5);
            List<double> expected = new List<double>() { 0, 0, 0, 0.25, 0.5, 0.75, 1, 0, 0 };
            Assert.AreEqual(expected.Count, samples.Count);
            for (int i = 0; i < expected.Count; ++i)
                AssertUtil.AssertDoubleEqual(expected[i], samples[i]);
        }
    }
}
