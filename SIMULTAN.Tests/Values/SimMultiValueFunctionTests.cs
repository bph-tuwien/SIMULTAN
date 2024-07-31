using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Projects;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace SIMULTAN.Tests.Values
{
    [TestClass]
    public class SimMultiValueFunctionTests
    {
        public static (string name, string unitX, string unitY, string unitZ, SimRect bounds, List<double> zs, List<SimMultiValueFunctionGraph> graphs)
            TestData(int zCount)
        {
            List<double> zs = new List<double>();
            List<SimMultiValueFunctionGraph> graphs = new List<SimMultiValueFunctionGraph>();
            for (int i = 0; i < zCount; ++i)
            {
                zs.Add(i);

                graphs.Add(SimMultiValueFunctionGraphTests.TestDataGraph(0, 2, 2, i).graph);
                graphs.Add(SimMultiValueFunctionGraphTests.TestDataGraph(1, 2, 2, i).graph);
            }

            return ("func", "funcunitX", "funcunitY", "funcUnitZ", new SimRect(-1, -1, 4, 4), zs, graphs);
        }

        public static (SimMultiValueFunction function,
            (string name, string unitX, string unitY, string unitZ, SimRect bounds, List<double> zs, List<SimMultiValueFunctionGraph> graphs) data,
            ExtendedProjectData projectData)
            TestDataFunction(int zCount)
        {
            var data = TestData(zCount);
            var func = new SimMultiValueFunction(data.name, data.unitX, data.unitY, data.unitZ, data.bounds, data.zs, data.graphs);

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.ValueManager.Add(func);

            return (func, data, projectData);
        }

        public void CheckTestData(SimMultiValueFunction function,
            (string name, string unitX, string unitY, string unitZ, SimRect bounds, List<double> zs, List<SimMultiValueFunctionGraph> graphs) data)
        {
            Assert.AreEqual(data.name, function.Name);
            Assert.AreEqual(true, function.CanInterpolate);
            Assert.AreEqual(SimMultiValueType.Function, function.MVType);

            Assert.AreEqual(data.unitX, function.UnitX);
            Assert.AreEqual(data.unitY, function.UnitY);
            Assert.AreEqual(data.unitZ, function.UnitZ);

            Assert.AreEqual(data.bounds.Left, function.Range.Minimum.X);
            Assert.AreEqual(data.bounds.Top, function.Range.Minimum.Y);
            Assert.AreEqual(data.zs.Min(), function.Range.Minimum.Z);

            Assert.AreEqual(data.bounds.Right, function.Range.Maximum.X);
            Assert.AreEqual(data.bounds.Bottom, function.Range.Maximum.Y);
            Assert.AreEqual(data.zs.Max(), function.Range.Maximum.Z);

            Assert.AreEqual(data.zs.Count, function.ZAxis.Count);
            for (int i = 0; i < data.zs.Count; ++i)
            {
                Assert.AreEqual(data.zs[i], function.ZAxis[i]);
            }

            Assert.AreEqual(data.graphs.Count, function.Graphs.Count);
            for (int i = 0; i < data.graphs.Count; ++i)
            {
                SimMultiValueFunctionGraphTests.Check(data.graphs[i], function.Graphs[i]);
                Assert.AreEqual(function, function.Graphs[i].Function);
            }
        }

        [TestMethod]
        public void Ctor()
        {
            var testData = TestData(2);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                new SimMultiValueFunction(null, testData.unitX, testData.unitY, testData.unitZ, testData.bounds, testData.zs, testData.graphs);
            });
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                new SimMultiValueFunction(testData.name, testData.unitX, testData.unitY, testData.unitZ, testData.bounds, null, testData.graphs);
            });
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                new SimMultiValueFunction(testData.name, testData.unitX, testData.unitY, testData.unitZ, testData.bounds, testData.zs, null);
            });

            var func = new SimMultiValueFunction(testData.name, testData.unitX, testData.unitY, testData.unitZ, testData.bounds, testData.zs, testData.graphs);

            CheckTestData(func, testData);

            //Data outside of range
            var outsidePoints = new List<SimPoint3D> { new SimPoint3D(-2, -2, 0), new SimPoint3D(1, 1, 0), new SimPoint3D(5, 6, 0) };
            var clampedPoints = new List<SimPoint3D> { new SimPoint3D(-1, -1, 0), new SimPoint3D(1, 1, 0), new SimPoint3D(3, 3, 0) };

            var graph = new SimMultiValueFunctionGraph("outsideGraph", outsidePoints);
            func = new SimMultiValueFunction(testData.name, testData.unitX, testData.unitY, testData.unitZ, testData.bounds, testData.zs,
                new List<SimMultiValueFunctionGraph> { graph });

            for (int i = 0; i < clampedPoints.Count; ++i)
                AssertUtil.AreEqual(clampedPoints[i], graph.Points[i]);
        }
        [TestMethod]
        public void CtorParsing()
        {
            var testData = TestData(2);
            long id = 99;

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                new SimMultiValueFunction(id, null, testData.unitX, testData.unitY, testData.unitZ, testData.bounds, testData.zs, testData.graphs);
            });
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                new SimMultiValueFunction(id, testData.name, testData.unitX, testData.unitY, testData.unitZ, testData.bounds, null, testData.graphs);
            });
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                new SimMultiValueFunction(id, testData.name, testData.unitX, testData.unitY, testData.unitZ, testData.bounds, testData.zs, null);
            });

            var func = new SimMultiValueFunction(id, testData.name, testData.unitX, testData.unitY, testData.unitZ, testData.bounds, testData.zs, testData.graphs);

            CheckTestData(func, testData);
            Assert.AreEqual(id, func.LocalID);

            //Data outside of range
            var outsidePoints = new List<SimPoint3D> { new SimPoint3D(-2, -2, 0), new SimPoint3D(1, 1, 0), new SimPoint3D(5, 6, 0) };
            var clampedPoints = new List<SimPoint3D> { new SimPoint3D(-1, -1, 0), new SimPoint3D(1, 1, 0), new SimPoint3D(3, 3, 0) };

            var graph = new SimMultiValueFunctionGraph("outsideGraph", outsidePoints);
            func = new SimMultiValueFunction(id, testData.name, testData.unitX, testData.unitY, testData.unitZ, testData.bounds, testData.zs,
                new List<SimMultiValueFunctionGraph> { graph });

            for (int i = 0; i < clampedPoints.Count; ++i)
                AssertUtil.AreEqual(clampedPoints[i], graph.Points[i]);
            Assert.AreEqual(id, func.LocalID);
        }

        [TestMethod]
        public void Clone()
        {
            var data = TestDataFunction(2);

            var cloned = (SimMultiValueFunction)data.function.Clone();

            CheckTestData(cloned, data.data);
        }

        [TestMethod]
        public void Range()
        {
            var data = TestDataFunction(2);
            var events = new PropertyChangedEventCounter(data.function);

            data.function.Range = new Range3D(new SimPoint3D(0, 0, -1), new SimPoint3D(1, 1, 0));
            AssertUtil.AreEqual(new SimPoint3D(0, 0, 0), data.function.Range.Minimum);
            AssertUtil.AreEqual(new SimPoint3D(1, 1, 1), data.function.Range.Maximum);
            events.AssertEventCount(1);

            foreach (var graph in data.function.Graphs)
            {
                foreach (var p in graph.Points)
                {
                    Assert.IsTrue(p.X >= 0 && p.X <= 1);
                    Assert.IsTrue(p.Y >= 0 && p.Y <= 1);
                }
            }
        }
        [TestMethod]
        public void CanInterpolate()
        {
            var data = TestDataFunction(2);

            Assert.IsTrue(data.function.CanInterpolate);

            data.function.CanInterpolate = false; //No changes allowed
            Assert.IsTrue(data.function.CanInterpolate);
        }


        [TestMethod]
        public void GetValue()
        {
            var data = TestDataFunction(2);

            AssertUtil.AssertDoubleEqual(2.0, data.function.GetValue(new SimPoint3D(1, 2, 0)));

            AssertUtil.AssertDoubleEqual(2.0, data.function.GetValue(new SimPoint3D(1, 2, 0), 0.01));
            AssertUtil.AssertDoubleEqual(double.NaN, data.function.GetValue(new SimPoint3D(1, 1.5, 0), 0.01));
            AssertUtil.AssertDoubleEqual(1.52941, data.function.GetValue(new SimPoint3D(1, 1.5, 0), 2.0));

            AssertUtil.AssertDoubleEqual(2.0, data.function.GetValue(new SimPoint3D(1, 2, 0), 0.01, out var isValid, out var closestPoint, out var closestGraph));
            Assert.AreEqual(true, isValid);
            AssertUtil.AreEqual(new SimPoint3D(1, 2, 0), closestPoint);
            Assert.AreEqual(data.data.graphs.First(x => x.Name == "graph_1_0"), closestGraph);

            AssertUtil.AssertDoubleEqual(double.NaN, data.function.GetValue(new SimPoint3D(1, 1.5, 0), 0.01, out isValid, out closestPoint, out closestGraph));
            Assert.AreEqual(false, isValid);
            AssertUtil.AreEqual(new SimPoint3D(double.NaN, double.NaN, double.NaN), closestPoint);
            Assert.AreEqual(null, closestGraph);
        }

        [TestMethod]
        public void AddGraph()
        {
            var data = TestDataFunction(2);

            //No clamping
            var insertGraph = SimMultiValueFunctionGraphTests.TestDataGraph(2, 2, 2, 1);
            data.function.Graphs.Add(insertGraph.graph);

            Assert.IsTrue(data.function.Graphs.Contains(insertGraph.graph));
            Assert.AreEqual(data.function, insertGraph.graph.Function);
            SimMultiValueFunctionGraphTests.CheckTestData(insertGraph.graph, insertGraph.data);

            //With clamping
            var insertGraphData = SimMultiValueFunctionGraphTests.TestData(2, 5, 5, 1);
            insertGraphData.points[0] = new SimPoint3D(-2, -2, 1);
            var clampGraph = new SimMultiValueFunctionGraph(insertGraphData.name, insertGraphData.points);
            data.function.Graphs.Add(clampGraph);

            insertGraphData.points = new List<SimPoint3D>
            {
                new SimPoint3D(0, 0, 1),
                new SimPoint3D(3, 0, 1),
                new SimPoint3D(3, 3, 1),
                new SimPoint3D(0, 3, 1)
            };
            Assert.IsTrue(data.function.Graphs.Contains(insertGraph.graph));
            Assert.AreEqual(data.function, insertGraph.graph.Function);
            SimMultiValueFunctionGraphTests.CheckTestData(insertGraph.graph, insertGraph.data);
        }
        [TestMethod]
        public void RemoveGraph()
        {
            var data = TestDataFunction(2);

            var removeGraph = data.function.Graphs[2];
            data.function.Graphs.Remove(removeGraph);

            Assert.AreEqual(null, removeGraph.Function);
            Assert.IsFalse(data.function.Graphs.Contains(removeGraph));
        }


        private WeakReference RemoveGraphMemoryLeakTest_Action(SimMultiValueFunction function)
        {
            var removeGraph = function.Graphs[2];
            function.Graphs.Remove(removeGraph);

            //Check for memory leak
            var weakGraph = new WeakReference(removeGraph);
            return weakGraph;
        }

        [TestMethod]
        public void RemoveGraphMemoryLeakTest()
        {
            var data = TestDataFunction(2);
            data.data.graphs.Clear();
            data.data.graphs = null;

            var weakGraph = RemoveGraphMemoryLeakTest_Action(data.function);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(weakGraph.IsAlive);
        }
        [TestMethod]
        public void ClearGraphs()
        {
            var data = TestDataFunction(2);

            var removedGraphs = data.function.Graphs.ToList();
            data.function.Graphs.Clear();

            for (int i = 0; i < removedGraphs.Count; ++i)
                Assert.AreEqual(null, removedGraphs[i].Function);
            Assert.AreEqual(0, data.function.Graphs.Count);
        }

        [TestMethod]
        public void ClearGraphsMemoryLeakTest()
        {
            var data = TestDataFunction(2);
            List<WeakReference> weakGraphs = data.function.Graphs.Select(x => new WeakReference(x)).ToList();
            data.function.Graphs.Clear();

            //Check for memory leak
            data.data.graphs.Clear();
            data.data.graphs = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            foreach (var wg in weakGraphs)
                Assert.IsFalse(wg.IsAlive);
        }

        [TestMethod]
        public void ZAxisAdd()
        {
            var data = TestDataFunction(2);
            var propertyEventCounter = new PropertyChangedEventCounter(data.function);
            var zsCollectionCounter = new CollectionChangedEventCounter(data.function.ZAxis);

            //No resorting
            data.function.ZAxis.Add(100.0);
            Assert.AreEqual(3, data.function.ZAxis.Count);
            Assert.AreEqual(100.0, data.function.ZAxis.Last());
            propertyEventCounter.AssertEventCount(1); zsCollectionCounter.AssertEventCount(1);
            Assert.AreEqual(nameof(SimMultiValueFunction.Range), propertyEventCounter.PropertyChangedArgs[0]);

            AssertUtil.AssertDoubleEqual(100.0, data.function.Range.Maximum.Z);
            AssertUtil.AssertDoubleEqual(0.0, data.function.Range.Minimum.Z);

            //With resorting
            data.function.ZAxis.Add(-100.0);
            Assert.AreEqual(4, data.function.ZAxis.Count);
            Assert.AreEqual(-100.0, data.function.ZAxis.First());
            propertyEventCounter.AssertEventCount(2); zsCollectionCounter.AssertEventCount(2);
            Assert.AreEqual(nameof(SimMultiValueFunction.Range), propertyEventCounter.PropertyChangedArgs[1]);

            AssertUtil.AssertDoubleEqual(100.0, data.function.Range.Maximum.Z);
            AssertUtil.AssertDoubleEqual(-100.0, data.function.Range.Minimum.Z);
        }
        //[TestMethod]
        //public void ZAxisChange()
        //{
        //	var data = TestDataFunction(2);
        //	var propertyEventCounter = new PropertyChangedEventCounter(data.function);
        //	var zsCollectionCounter = new CollectionChangedEventCounter(data.function.Zs);
        //}
        [TestMethod]
        public void ZAxisRemove()
        {
            var data = TestDataFunction(3);
            var propertyEventCounter = new PropertyChangedEventCounter(data.function);
            var zsCollectionCounter = new CollectionChangedEventCounter(data.function.ZAxis);

            var graphsToRemove = data.function.Graphs.Where(x => x.Points[0].Z.InRange(0.99, 1.01)).ToList();

            data.function.ZAxis.RemoveAt(1);
            Assert.AreEqual(2, data.function.ZAxis.Count);
            Assert.AreEqual(0, data.function.ZAxis[0]);
            Assert.AreEqual(2, data.function.ZAxis[1]);
            propertyEventCounter.AssertEventCount(1); zsCollectionCounter.AssertEventCount(1);
            Assert.AreEqual(nameof(SimMultiValueFunction.Range), propertyEventCounter.PropertyChangedArgs[0]);

            Assert.AreEqual(4, data.function.Graphs.Count);
            foreach (var g in graphsToRemove)
            {
                Assert.IsFalse(data.function.Graphs.Contains(g));
                Assert.AreEqual(null, g.Function);
            }
        }
        [TestMethod]
        public void ZAxisClear()
        {
            var data = TestDataFunction(3);
            var propertyEventCounter = new PropertyChangedEventCounter(data.function);
            var zsCollectionCounter = new CollectionChangedEventCounter(data.function.ZAxis);

            var graphsToRemove = data.function.Graphs.ToList();

            data.function.ZAxis.Clear();
            Assert.AreEqual(1, data.function.ZAxis.Count);
            Assert.AreEqual(0, data.function.ZAxis[0]);
            propertyEventCounter.AssertEventCount(2); zsCollectionCounter.AssertEventCount(2);
            Assert.AreEqual(nameof(SimMultiValueFunction.Range), propertyEventCounter.PropertyChangedArgs[0]);
            Assert.AreEqual(nameof(SimMultiValueFunction.Range), propertyEventCounter.PropertyChangedArgs[1]);

            Assert.AreEqual(0, data.function.Graphs.Count);
            foreach (var g in graphsToRemove)
            {
                Assert.IsFalse(data.function.Graphs.Contains(g));
                Assert.AreEqual(null, g.Function);
            }
        }

        [TestMethod]
        public void SampleAll()
        {
            var data = TestDataFunction(2);

            var sampled = data.function.Sample(-2, 4, 0.5);

            Assert.AreEqual(4, sampled.columnNames.Count);
            Assert.AreEqual(13, sampled.graphsSamples.Count);

            List<List<double>> expectedSamples = new List<List<double>>()
            {
                new List<double> { 0, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0, 0, 0 },
                new List<double> { 0, 0, 0, 0, 2, 0, 2, 0.8, 0.6, 0, 0, 0, 0 },
                new List<double> { 0, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0, 0, 0 },
                new List<double> { 0, 0, 0, 0, 2, 0, 2, 0.8, 0.6, 0, 0, 0, 0 }
            };

            Assert.AreEqual("graph_0_0", sampled.columnNames[0]);
            Assert.AreEqual("graph_1_0", sampled.columnNames[1]);
            Assert.AreEqual("graph_0_1", sampled.columnNames[2]);
            Assert.AreEqual("graph_1_1", sampled.columnNames[3]);

            for (int graph = 0; graph < 4; graph++)
            {
                for (int i = 0; i < sampled.graphsSamples[graph].Count; ++i)
                {
                    AssertUtil.AssertDoubleEqual(expectedSamples[graph][i], sampled.graphsSamples[i][graph]);
                }
            }
        }
        [TestMethod]
        public void SampleZLayer()
        {
            var data = TestDataFunction(3);

            var sampled = data.function.Sample(-2, 4, 0.5, 1);

            Assert.AreEqual(2, sampled.columnNames.Count);
            Assert.AreEqual(13, sampled.graphsSamples.Count);

            List<List<double>> expectedSamples = new List<List<double>>()
            {
                new List<double> { 0, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0, 0, 0 },
                new List<double> { 0, 0, 0, 0, 2, 0, 2, 0.8, 0.6, 0, 0, 0, 0 }
            };

            Assert.AreEqual("graph_0_1", sampled.columnNames[0]);
            Assert.AreEqual("graph_1_1", sampled.columnNames[1]);

            for (int graph = 0; graph < 2; graph++)
            {
                for (int i = 0; i < sampled.graphsSamples[graph].Count; ++i)
                {
                    AssertUtil.AssertDoubleEqual(expectedSamples[graph][i], sampled.graphsSamples[i][graph]);
                }
            }
        }
    }
}
