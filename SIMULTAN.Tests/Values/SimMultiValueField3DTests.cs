using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Projects;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace SIMULTAN.Tests.Values
{
    [TestClass]
    public class SimMultiValueField3DTests
    {
        internal class MultiValueTableEventCounter
        {
            internal MultiValueTableEventCounter(SimMultiValueField3D table)
            {
                table.PropertyChanged += (s, e) => PropertyChangedArgs.Add(e.PropertyName);
                table.ValueChanged += (s, e) => ValueChangedArgs.Add(e);
                table.AxisChanged += (s, e) => AxisChangedArgs.Add(e);
            }

            internal List<string> PropertyChangedArgs { get; } = new List<string>();

            internal List<SimMultiValueField3D.ValueChangedEventArgs> ValueChangedArgs { get; } = new List<SimMultiValueField3D.ValueChangedEventArgs>();

            internal List<EventArgs> AxisChangedArgs { get; } = new List<EventArgs>();

            public void AssertCount(int propertyChangedCount, int valueChangedCount, int axisChangedCount)
            {
                Assert.AreEqual(propertyChangedCount, PropertyChangedArgs.Count);
                Assert.AreEqual(valueChangedCount, ValueChangedArgs.Count);
                Assert.AreEqual(axisChangedCount, AxisChangedArgs.Count);
            }
        }

        private void AssertData(SimMultiValueField3D table, List<double> data, int xCount, int yCount, int zCount)
        {
            for (int z = 0; z < zCount; ++z)
            {
                for (int y = 0; y < yCount; ++y)
                {
                    for (int x = 0; x < xCount; ++x)
                    {
                        Assert.AreEqual(data[z * yCount * xCount + y * xCount + x], table[x, y, z]);
                    }
                }
            }
        }

        //values order: z, y, x
        private void AssertField(SimMultiValueField3D table, double[,,] values)
        {
            for (int x = 0; x < values.GetLength(2); ++x)
            {
                for (int y = 0; y < values.GetLength(1); ++y)
                {
                    for (int z = 0; z < values.GetLength(0); ++z)
                    {
                        Assert.IsTrue(Math.Abs(table[x, y, z] - values[z, y, x]) < 0.0001);
                    }
                }
            }
        }



        internal static (string name, List<double> xaxis, List<double> yaxis, List<double> zaxis, List<double> data,
            Dictionary<SimPoint3D, double> dataDict,
            string unitX, string unitY, string unitZ)
            TestData(int xCount, int yCount, int zCount)
        {
            var xaxis = new List<double>(xCount);
            var yaxis = new List<double>(yCount);
            var zaxis = new List<double>(zCount);

            for (int i = 0; i < xCount; ++i)
                xaxis.Add(i);
            for (int i = 0; i < yCount; ++i)
                yaxis.Add(i / 100.0);
            for (int i = 0; i < zCount; ++i)
                zaxis.Add(i * 100.0);

            List<double> data = new List<double>();
            Dictionary<SimPoint3D, double> dataDict = new Dictionary<SimPoint3D, double>();
            int val = 0;
            for (int z = 0; z < zaxis.Count; ++z)
            {
                for (int y = 0; y < yaxis.Count; ++y)
                {
                    for (int x = 0; x < xaxis.Count; ++x)
                    {
                        data.Add(val);
                        dataDict.Add(new SimPoint3D(x, y, z), val);
                        val++;
                    }
                }
            }

            return
                (
                "test1",
                xaxis, yaxis, zaxis,
                data, dataDict,
                "ux", "uy", "uz"
                );

        }

        internal static ((string name, List<double> xaxis, List<double> yaxis, List<double> zaxis, List<double> data,
            Dictionary<SimPoint3D, double> dataDict,
            string unitX, string unitY, string unitZ) data, SimMultiValueField3D table, ExtendedProjectData projectData)
            TestDataTable(int xCount, int yCount, int zCount)
        {
            ExtendedProjectData projectData = new ExtendedProjectData();

            var data = TestData(xCount, yCount, zCount);
            var mvt = new SimMultiValueField3D(data.name, data.xaxis, data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ, data.data, true);

            projectData.ValueManager.Add(mvt);
            return (data, mvt, projectData);
        }

        [TestMethod]
        public void Ctor()
        {
            var data = TestData(3, 4, 5);

            //Null arguments
            Assert.ThrowsException<ArgumentNullException>(() =>
                { var mvte = new SimMultiValueField3D(null, data.xaxis, data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ, data.data, true); });
            Assert.ThrowsException<ArgumentNullException>(() =>
            { var mvte = new SimMultiValueField3D(data.name, data.xaxis, data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ, null, true); });

            //To less data
            Assert.ThrowsException<ArgumentException>(() =>
            {
                var mvte = new SimMultiValueField3D(data.name, data.xaxis, data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ,
                                                new List<double> { 1, 2, 3 }
                                                , true);
            });

            //Default case
            var mvt = new SimMultiValueField3D(data.name, data.xaxis, data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ, data.data, true);
            Assert.AreEqual(data.name, mvt.Name);
            Assert.AreEqual(SimMultiValueType.Field3D, mvt.MVType);
            AssertUtil.ContainEqualValues(data.xaxis, mvt.XAxis);
            AssertUtil.ContainEqualValues(data.yaxis, mvt.YAxis);
            AssertUtil.ContainEqualValues(data.zaxis, mvt.ZAxis);
            Assert.AreEqual(data.unitX, mvt.UnitX);
            Assert.AreEqual(data.unitY, mvt.UnitY);
            Assert.AreEqual(data.unitZ, mvt.UnitZ);
            AssertData(mvt, data.data, data.xaxis.Count, data.yaxis.Count, data.zaxis.Count);
            Assert.AreEqual(true, mvt.CanInterpolate);
            Assert.IsTrue(mvt.Id.LocalId >= 0);
        }

        [TestMethod]
        public void CtorDimensionNullOrEmpty()
        {
            var data = TestData(3, 4, 5);
            var location = new DummyReferenceLocation(Guid.Empty);

            //Case xaxiss == null
            var mvt = new SimMultiValueField3D(data.name, null, data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ, data.data, true);
            Assert.AreEqual(SimMultiValueType.Field3D, mvt.MVType);
            Assert.AreEqual(1, mvt.XAxis.Count);
            Assert.AreEqual(data.yaxis.Count, mvt.YAxis.Count);
            Assert.AreEqual(data.zaxis.Count, mvt.ZAxis.Count);
            Assert.AreEqual(0.0, mvt.XAxis[0]);

            //xaxis is empty
            mvt = new SimMultiValueField3D(data.name, new List<double>(), data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ, data.data, true);
            Assert.AreEqual(SimMultiValueType.Field3D, mvt.MVType);
            Assert.AreEqual(1, mvt.XAxis.Count);
            Assert.AreEqual(data.yaxis.Count, mvt.YAxis.Count);
            Assert.AreEqual(data.zaxis.Count, mvt.ZAxis.Count);
            Assert.AreEqual(0.0, mvt.XAxis[0]);

            //yaxis == null
            mvt = new SimMultiValueField3D(data.name, data.xaxis, data.unitX, null, data.unitY, data.zaxis, data.unitZ, data.data, true);
            Assert.AreEqual(SimMultiValueType.Field3D, mvt.MVType);
            Assert.AreEqual(data.xaxis.Count, mvt.XAxis.Count);
            Assert.AreEqual(1, mvt.YAxis.Count);
            Assert.AreEqual(0.0, mvt.YAxis[0]);
            Assert.AreEqual(data.zaxis.Count, mvt.ZAxis.Count);

            //yaxis is empty
            mvt = new SimMultiValueField3D(data.name, data.xaxis, data.unitX, new List<double>(), data.unitY, data.zaxis, data.unitZ, data.data, true);
            Assert.AreEqual(SimMultiValueType.Field3D, mvt.MVType);
            Assert.AreEqual(data.xaxis.Count, mvt.XAxis.Count);
            Assert.AreEqual(1, mvt.YAxis.Count);
            Assert.AreEqual(0.0, mvt.YAxis[0]);
            Assert.AreEqual(data.zaxis.Count, mvt.ZAxis.Count);

            //zaxis == null
            mvt = new SimMultiValueField3D(data.name, data.xaxis, data.unitX, data.yaxis, data.unitY, null, data.unitZ, data.data, true);
            Assert.AreEqual(SimMultiValueType.Field3D, mvt.MVType);
            Assert.AreEqual(data.xaxis.Count, mvt.XAxis.Count);
            Assert.AreEqual(data.yaxis.Count, mvt.YAxis.Count);
            Assert.AreEqual(1, mvt.ZAxis.Count);
            Assert.AreEqual(0.0, mvt.ZAxis[0]);

            //zaxis is empty
            mvt = new SimMultiValueField3D(data.name, data.xaxis, data.unitX, data.yaxis, data.unitY, new List<double>(), data.unitZ, data.data, true);
            Assert.AreEqual(SimMultiValueType.Field3D, mvt.MVType);
            Assert.AreEqual(data.xaxis.Count, mvt.XAxis.Count);
            Assert.AreEqual(data.yaxis.Count, mvt.YAxis.Count);
            Assert.AreEqual(1, mvt.ZAxis.Count);
            Assert.AreEqual(0.0, mvt.ZAxis[0]);
        }

        [TestMethod]
        public void CtorTooMuchData()
        {
            var data = TestData(3, 4, 5);
            var location = new DummyReferenceLocation(Guid.Empty);

            data.dataDict.Add(new SimPoint3D(1000, 1000, 1000), 99.9);

            //Case xaxis == null
            var mvt = new SimMultiValueField3D(55, data.name, data.xaxis, data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ, data.dataDict, true);

            Assert.AreEqual(3 * 4 * 5, mvt.Count(0) * mvt.Count(1) * mvt.Count(2));
        }

        [TestMethod]
        public void CtorAxisSorting()
        {
            var data = TestData(2, 2, 2);

            //x-axis sorting
            var mvt = new SimMultiValueField3D(data.name, new List<double> { 100, 99 }, data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ, data.data, true);
            Assert.AreEqual(2, mvt.XAxis.Count);
            Assert.AreEqual(99, mvt.XAxis[0]);
            Assert.AreEqual(100, mvt.XAxis[1]);
            AssertField(mvt, new double[,,]
            {
                {
                    {1, 0 },
                    {3, 2 }
                },
                {
                    {5, 4 },
                    {7, 6 }
                }
            });

            //y-axis sorting
            mvt = new SimMultiValueField3D(data.name, data.xaxis, data.unitX, new List<double> { 100, 99 }, data.unitY, data.zaxis, data.unitZ, data.data, true);
            Assert.AreEqual(2, mvt.YAxis.Count);
            Assert.AreEqual(99, mvt.YAxis[0]);
            Assert.AreEqual(100, mvt.YAxis[1]);

            AssertField(mvt, new double[,,]
            {
                { { 2, 3 }, { 0, 1 } },
                { { 6, 7 }, { 4, 5 } }
            });

            //z-axis sorting
            mvt = new SimMultiValueField3D(data.name, data.xaxis, data.unitX, data.yaxis, data.unitY, new List<double> { 100, 99 }, data.unitZ, data.data, true);
            Assert.AreEqual(2, mvt.ZAxis.Count);
            Assert.AreEqual(99, mvt.ZAxis[0]);
            Assert.AreEqual(100, mvt.ZAxis[1]);

            AssertField(mvt, new double[,,]
            {
                { { 4, 5 }, { 6, 7 } },
                { { 0, 1 }, { 2, 3 } }
            });
        }

        [TestMethod]
        public void CtorParsing()
        {
            var data = TestData(3, 4, 5);

            var datadict = new Dictionary<SimPoint3D, double>
            {
                { new SimPoint3D(0,0,0), 0 },
                { new SimPoint3D(1,0,0), 1 },
                { new SimPoint3D(0,1,0), 2 },
                { new SimPoint3D(1,1,0), 3 },

                { new SimPoint3D(0,0,1), 4 },
                { new SimPoint3D(1,0,1), 5 },
                { new SimPoint3D(0,1,1), 6 },
                { new SimPoint3D(1,1,1), 7 },
            };

            //Check data exception
            Assert.ThrowsException<ArgumentNullException>(() =>
            { var tabex = new SimMultiValueField3D(0, data.name, data.xaxis, data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ, null, false); });

            var table1 = new SimMultiValueField3D(99, data.name, null, data.unitX, null, data.unitY, null, data.unitZ, new Dictionary<SimPoint3D, double>(), true);
            Assert.AreEqual(SimMultiValueType.Field3D, table1.MVType);
            Assert.AreEqual(99, table1.LocalID);
            Assert.AreEqual(data.name, table1.Name);
            Assert.AreEqual(1, table1.XAxis.Count);
            Assert.AreEqual(0, table1.XAxis[0]);
            Assert.AreEqual(1, table1.YAxis.Count);
            Assert.AreEqual(0, table1.YAxis[0]);
            Assert.AreEqual(1, table1.ZAxis.Count);
            Assert.AreEqual(0, table1.ZAxis[0]);
            Assert.AreEqual(1, table1.Length);
            Assert.AreEqual(0, table1.GetValue(new SimPoint3D(0, 0, 0)));
            Assert.AreEqual(data.unitX, table1.UnitX);
            Assert.AreEqual(data.unitY, table1.UnitY);
            Assert.AreEqual(data.unitZ, table1.UnitZ);
            Assert.AreEqual(true, table1.CanInterpolate);

            var table2 = new SimMultiValueField3D(100, data.name, data.xaxis, data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ, data.dataDict, false);
            Assert.AreEqual(100, table2.LocalID);
            Assert.AreEqual(SimMultiValueType.Field3D, table2.MVType);
            Assert.AreEqual(data.name, table2.Name);
            Assert.AreEqual(data.xaxis.Count, table2.XAxis.Count);
            AssertUtil.ContainEqualValues(data.xaxis, table2.XAxis);
            Assert.AreEqual(data.yaxis.Count, table2.YAxis.Count);
            AssertUtil.ContainEqualValues(data.yaxis, table2.YAxis);
            Assert.AreEqual(data.zaxis.Count, table2.ZAxis.Count);
            AssertUtil.ContainEqualValues(data.zaxis, table2.ZAxis);
            Assert.AreEqual(data.xaxis.Count * data.yaxis.Count * data.zaxis.Count, table2.Length);

            for (int x = 0; x < data.xaxis.Count; x++)
                for (int y = 0; y < data.yaxis.Count; y++)
                    for (int z = 0; z < data.zaxis.Count; z++)
                        Assert.AreEqual(data.dataDict[new SimPoint3D(x, y, z)], table2.GetValue(new SimPoint3D(x, y, z)));
            Assert.AreEqual(data.unitX, table2.UnitX);
            Assert.AreEqual(data.unitY, table2.UnitY);
            Assert.AreEqual(data.unitZ, table2.UnitZ);
            Assert.AreEqual(false, table2.CanInterpolate);
        }



        [TestMethod]
        public void Clone()
        {
            var data = TestDataTable(3, 4, 5);

            var mvtcopy = (SimMultiValueField3D)data.table.Clone();

            Assert.AreEqual(data.data.name, mvtcopy.Name);
            AssertUtil.ContainEqualValues(data.data.xaxis, mvtcopy.XAxis);
            AssertUtil.ContainEqualValues(data.data.yaxis, mvtcopy.YAxis);
            AssertUtil.ContainEqualValues(data.data.zaxis, mvtcopy.ZAxis);
            Assert.AreEqual(data.data.unitX, mvtcopy.UnitX);
            Assert.AreEqual(data.data.unitY, mvtcopy.UnitY);
            Assert.AreEqual(data.data.unitZ, mvtcopy.UnitZ);
            AssertData(mvtcopy, data.data.data, data.data.xaxis.Count, data.data.yaxis.Count, data.data.zaxis.Count);
            Assert.AreEqual(true, mvtcopy.CanInterpolate);
            Assert.AreEqual(SimId.Empty, mvtcopy.Id);
            Assert.AreEqual(null, mvtcopy.Factory);
        }

        [TestMethod]
        public void GetValue()
        {
            var data = TestData(3, 4, 5);
            var mvt = new SimMultiValueField3D(data.name, data.xaxis, data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ, data.data, false);

            //Hit on point
            Assert.AreEqual(16, mvt.GetValue(new SimPoint3D(1, 1, 1)));

            //Hit exactly in middle of four points
            Assert.AreEqual(16, mvt.GetValue(new SimPoint3D(0.5, 0.5, 0.5)));

            //Hit at 1/4
            Assert.AreEqual(0, mvt.GetValue(new SimPoint3D(0.25, 0.25, 0.25)));

            //Hit at 3/4
            Assert.AreEqual(16, mvt.GetValue(new SimPoint3D(0.75, 0.75, 0.75)));

            //Hit outside
            Assert.AreEqual(59, mvt.GetValue(new SimPoint3D(30, 30, 30)));
        }

        [TestMethod]
        public void Indexer()
        {
            var data = TestData(3, 4, 5);
            var mvt = new SimMultiValueField3D(data.name, data.xaxis, data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ, data.data, false);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { var test = mvt[-1, 1, 1]; });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { var test = mvt[1, -1, 1]; });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { var test = mvt[1, 1, -1]; });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { var test = mvt[10, 1, 1]; });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { var test = mvt[1, 10, 1]; });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { var test = mvt[1, 1, 10]; });

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { mvt[-1, 1, 1] = 100.0; });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { mvt[1, -1, 1] = 100.0; });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { mvt[1, 1, -1] = 100.0; });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { mvt[10, 1, 1] = 100.0; });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { mvt[1, 10, 1] = 100.0; });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { mvt[1, 1, 10] = 100.0; });

            Assert.AreEqual(16, mvt[new IntIndex3D(1, 1, 1)]);
            mvt[new IntIndex3D(1, 1, 1)] = 99;
            Assert.AreEqual(99, mvt[new IntIndex3D(1, 1, 1)]);

            Assert.AreEqual(17, mvt[2, 1, 1]);
            mvt[new IntIndex3D(2, 1, 1)] = 999;
            Assert.AreEqual(999, mvt[2, 1, 1]);
        }

        [TestMethod]
        public void GetValueInterpolated()
        {
            var data = TestData(3, 4, 5);
            var mvt = new SimMultiValueField3D(data.name, data.xaxis, data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ, data.data, true);

            //Hit on point
            Assert.AreEqual(16.0, mvt.GetValue(new SimPoint3D(1, 1, 1)));

            //Hit exactly in middle of four points
            Assert.AreEqual(8.0, mvt.GetValue(new SimPoint3D(0.5, 0.5, 0.5)));

            //Hit at 1/4
            Assert.AreEqual(4.0, mvt.GetValue(new SimPoint3D(0.25, 0.25, 0.25)));

            //Hit at 3/4
            Assert.AreEqual(12.0, mvt.GetValue(new SimPoint3D(0.75, 0.75, 0.75)));

            //Hit outside
            Assert.AreEqual(59, mvt.GetValue(new SimPoint3D(30, 30, 30)));
        }



        [TestMethod]
        public void XAxisAdd()
        {
            var data = TestDataTable(2, 2, 2);
            var eventCounter = new MultiValueTableEventCounter(data.table);

            data.table.XAxis.Add(-50);
            Assert.AreEqual(3, data.table.XAxis.Count);
            AssertUtil.ContainEqualValues(new double[] { -50, 0, 1 }, data.table.XAxis);
            Assert.AreEqual(12, data.table.Length);

            AssertField(data.table, new double[,,] {
                { { 0, 0, 1 }, { 0, 2, 3 } },
                { { 0, 4, 5 }, { 0, 6, 7 } }
            });

            eventCounter.AssertCount(0, 0, 1);

            data.table.XAxis.Add(1000);
            Assert.AreEqual(4, data.table.XAxis.Count);
            AssertUtil.ContainEqualValues(new double[] { -50, 0, 1, 1000 }, data.table.XAxis);
            Assert.AreEqual(16, data.table.Length);

            AssertField(data.table, new double[,,] {
                { { 0, 0, 1, 0 }, { 0, 2, 3, 0 } },
                { { 0, 4, 5, 0 }, { 0, 6, 7, 0 } }
            });
            eventCounter.AssertCount(0, 0, 2);
        }

        [TestMethod]
        public void XAxisRemove()
        {
            var data = TestDataTable(3, 2, 2);
            var eventCounter = new MultiValueTableEventCounter(data.table);

            data.table.XAxis.RemoveAt(1);
            eventCounter.AssertCount(0, 0, 1);

            Assert.AreEqual(2, data.table.XAxis.Count);
            Assert.AreEqual(8, data.table.Length);

            AssertField(data.table, new double[,,]
            {
                { { 0, 2 }, { 3, 5 } },
                { { 6, 8 }, { 9, 11 } }
            });

            data.table.XAxis.RemoveAt(1);
            eventCounter.AssertCount(0, 0, 2);
            data.table.XAxis.RemoveAt(0);
            eventCounter.AssertCount(0, 0, 3);
            Assert.AreEqual(1, data.table.XAxis.Count);
            Assert.AreEqual(0, data.table.XAxis[0]);
            AssertField(data.table, new double[,,]
            {
                { { 0 }, { 0 } },
                { { 0 }, { 0 } }
            });
        }

        [TestMethod]
        public void XAxisReplace()
        {
            var data = TestDataTable(2, 2, 2);
            var eventCounter = new MultiValueTableEventCounter(data.table);

            data.table.XAxis[1] = -99;

            AssertField(data.table, new double[,,]
            {
                { {1, 0 }, {3, 2 } },
                { {5, 4 }, {7, 6 } }
            });

            Assert.AreEqual(2, data.table.XAxis.Count);
            Assert.AreEqual(-99, data.table.XAxis[0]);
            Assert.AreEqual(0, data.table.XAxis[1]);

            eventCounter.AssertCount(0, 0, 1);
        }

        [TestMethod]
        public void XAxisClear()
        {
            var data = TestDataTable(2, 2, 2);
            var eventCounter = new MultiValueTableEventCounter(data.table);

            data.table.XAxis.Clear();

            AssertField(data.table, new double[,,]
            {
                { { 0 }, { 0 } },
                { { 0 }, { 0 } }
            });

            Assert.AreEqual(1, data.table.XAxis.Count);
            Assert.AreEqual(0, data.table.XAxis[0]);

            eventCounter.AssertCount(0, 0, 1);
        }


        [TestMethod]
        public void YAxisAdd()
        {
            var data = TestDataTable(2, 2, 2);
            var eventCounter = new MultiValueTableEventCounter(data.table);

            data.table.YAxis.Add(-50);
            Assert.AreEqual(3, data.table.YAxis.Count);
            AssertUtil.ContainEqualValues(new double[] { -50, 0, 0.01 }, data.table.YAxis);
            Assert.AreEqual(12, data.table.Length);

            AssertField(data.table, new double[,,] {
                { { 0, 0 }, { 0, 1 }, { 2, 3 } },
                { { 0, 0 }, { 4, 5 }, { 6, 7 } },
            });

            eventCounter.AssertCount(0, 0, 1);

            data.table.YAxis.Add(1000);
            Assert.AreEqual(4, data.table.YAxis.Count);
            AssertUtil.ContainEqualValues(new double[] { -50, 0, 0.01, 1000 }, data.table.YAxis);
            Assert.AreEqual(16, data.table.Length);

            AssertField(data.table, new double[,,] {
                { { 0, 0 }, { 0, 1 }, { 2, 3 }, {0, 0 } },
                { { 0, 0 }, { 4, 5 }, { 6, 7 }, {0, 0 } },
            });

            eventCounter.AssertCount(0, 0, 2);
        }

        [TestMethod]
        public void YAxisRemove()
        {
            var data = TestDataTable(2, 3, 2);
            var eventCounter = new MultiValueTableEventCounter(data.table);

            data.table.YAxis.RemoveAt(1);
            eventCounter.AssertCount(0, 0, 1);
            Assert.AreEqual(2, data.table.YAxis.Count);
            Assert.AreEqual(8, data.table.Length);

            AssertField(data.table, new double[,,]
            {
                { { 0, 1 }, { 4, 5 } },
                { { 6, 7 }, { 10, 11 } }
            });

            data.table.YAxis.RemoveAt(1);
            eventCounter.AssertCount(0, 0, 2);
            data.table.YAxis.RemoveAt(0);
            eventCounter.AssertCount(0, 0, 3);
            Assert.AreEqual(1, data.table.YAxis.Count);
            Assert.AreEqual(0, data.table.YAxis[0]);
            AssertField(data.table, new double[,,]
            {
                { { 0, 0 } },
                { { 0, 0 } }
            });
        }

        [TestMethod]
        public void YAxisReplace()
        {
            var data = TestDataTable(2, 2, 2);
            var eventCounter = new MultiValueTableEventCounter(data.table);

            data.table.YAxis[1] = -99;

            AssertField(data.table, new double[,,]
            {
                { {2, 3 }, {0, 1 } },
                { {6, 7 }, {4, 5 } }
            });

            Assert.AreEqual(2, data.table.YAxis.Count);
            Assert.AreEqual(-99, data.table.YAxis[0]);
            Assert.AreEqual(0, data.table.YAxis[1]);

            eventCounter.AssertCount(0, 0, 1);
        }

        [TestMethod]
        public void YAxisClear()
        {
            var data = TestDataTable(2, 2, 2);
            var eventCounter = new MultiValueTableEventCounter(data.table);

            data.table.YAxis.Clear();

            AssertField(data.table, new double[,,]
            {
                { { 0, 0 } },
                { { 0, 0 } },
            });

            Assert.AreEqual(1, data.table.YAxis.Count);
            Assert.AreEqual(0, data.table.YAxis[0]);

            eventCounter.AssertCount(0, 0, 1);
        }


        [TestMethod]
        public void ZAxisAdd()
        {
            var data = TestDataTable(2, 2, 2);
            var eventCounter = new MultiValueTableEventCounter(data.table);

            data.table.ZAxis.Add(-50);
            Assert.AreEqual(3, data.table.ZAxis.Count);
            AssertUtil.ContainEqualValues(new double[] { -50, 0, 100 }, data.table.ZAxis);
            Assert.AreEqual(12, data.table.Length);

            AssertField(data.table, new double[,,] {
                { { 0, 0 }, { 0, 0 } },
                { { 0, 1 }, { 2, 3 } },
                { { 4, 5 }, { 6, 7 } },
            });

            eventCounter.AssertCount(0, 0, 1);

            data.table.ZAxis.Add(1000);
            Assert.AreEqual(4, data.table.ZAxis.Count);
            AssertUtil.ContainEqualValues(new double[] { -50, 0, 100, 1000 }, data.table.ZAxis);
            Assert.AreEqual(16, data.table.Length);

            AssertField(data.table, new double[,,] {
                { { 0, 0 }, { 0, 0 } },
                { { 0, 1 }, { 2, 3 } },
                { { 4, 5 }, { 6, 7 } },
                { { 0, 0 }, { 0, 0 } },
            });

            eventCounter.AssertCount(0, 0, 2);
        }

        [TestMethod]
        public void ZAxisRemove()
        {
            var data = TestDataTable(2, 2, 3);
            var eventCounter = new MultiValueTableEventCounter(data.table);

            data.table.ZAxis.RemoveAt(1);
            eventCounter.AssertCount(0, 0, 1);
            Assert.AreEqual(2, data.table.ZAxis.Count);
            Assert.AreEqual(8, data.table.Length);

            AssertField(data.table, new double[,,]
            {
                { { 0, 1 }, { 2, 3 } },
                { { 8, 9 }, { 10, 11 } }
            });

            data.table.ZAxis.RemoveAt(1);
            eventCounter.AssertCount(0, 0, 2);
            data.table.ZAxis.RemoveAt(0);
            eventCounter.AssertCount(0, 0, 3);
            Assert.AreEqual(1, data.table.ZAxis.Count);
            Assert.AreEqual(0, data.table.ZAxis[0]);
            AssertField(data.table, new double[,,]
            {
                { { 0, 0 }, { 0, 0 } },
            });
        }

        [TestMethod]
        public void ZAxisReplace()
        {
            (var data, var mvt, _) = TestDataTable(2, 2, 2);
            var eventCounter = new MultiValueTableEventCounter(mvt);

            mvt.ZAxis[1] = -99;

            AssertField(mvt, new double[,,]
            {
                { {4, 5 }, {6, 7 } },
                { {0, 1 }, {2, 3 } }
            });

            Assert.AreEqual(2, mvt.ZAxis.Count);
            Assert.AreEqual(-99, mvt.ZAxis[0]);
            Assert.AreEqual(0, mvt.ZAxis[1]);

            eventCounter.AssertCount(0, 0, 1);
        }

        [TestMethod]
        public void ZAxisClear()
        {
            (var data, var mvt, _) = TestDataTable(2, 2, 2);
            var eventCounter = new MultiValueTableEventCounter(mvt);

            mvt.ZAxis.Clear();

            AssertField(mvt, new double[,,]
            {
                { { 0, 0 }, { 0, 0 } },
            });

            Assert.AreEqual(1, mvt.ZAxis.Count);
            Assert.AreEqual(0, mvt.ZAxis[0]);

            eventCounter.AssertCount(0, 0, 1);
        }





        [TestMethod]
        public void AxisPositionFromValue()
        {
            var data = TestData(3, 4, 5);
            var mvt = new SimMultiValueField3D(data.name, data.xaxis, data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ, data.data, true);

            var idx = mvt.AxisPositionFromValue(SimMultiValueField3D.Axis.X, 1.5);
            AssertUtil.AssertDoubleEqual(1.5, idx);
            idx = mvt.AxisPositionFromValue(SimMultiValueField3D.Axis.X, -1000);
            AssertUtil.AssertDoubleEqual(0, idx);
            idx = mvt.AxisPositionFromValue(SimMultiValueField3D.Axis.X, 1000);
            AssertUtil.AssertDoubleEqual(2, idx);

            idx = mvt.AxisPositionFromValue(SimMultiValueField3D.Axis.Y, 0.017);
            AssertUtil.AssertDoubleEqual(1.7, idx);
            idx = mvt.AxisPositionFromValue(SimMultiValueField3D.Axis.Y, -1000);
            AssertUtil.AssertDoubleEqual(0, idx);
            idx = mvt.AxisPositionFromValue(SimMultiValueField3D.Axis.Y, 1000);
            AssertUtil.AssertDoubleEqual(3, idx);

            idx = mvt.AxisPositionFromValue(SimMultiValueField3D.Axis.Z, 355);
            AssertUtil.AssertDoubleEqual(3.55, idx);
            idx = mvt.AxisPositionFromValue(SimMultiValueField3D.Axis.Z, -1000);
            AssertUtil.AssertDoubleEqual(0, idx);
            idx = mvt.AxisPositionFromValue(SimMultiValueField3D.Axis.Z, 1000);
            AssertUtil.AssertDoubleEqual(4, idx);
        }

        [TestMethod]
        public void ValueFromAxisPosition()
        {
            var data = TestData(3, 4, 5);
            var mvt = new SimMultiValueField3D(data.name, data.xaxis, data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ, data.data, true);

            var val = mvt.ValueFromAxisPosition(SimMultiValueField3D.Axis.X, 1.5);
            AssertUtil.AssertDoubleEqual(1.5, val);
            val = mvt.ValueFromAxisPosition(SimMultiValueField3D.Axis.X, 1.5, out var isOutside);
            AssertUtil.AssertDoubleEqual(1.5, val);
            Assert.AreEqual(false, isOutside);
            val = mvt.ValueFromAxisPosition(SimMultiValueField3D.Axis.X, -1000, out isOutside);
            AssertUtil.AssertDoubleEqual(0, val);
            Assert.AreEqual(true, isOutside);
            val = mvt.ValueFromAxisPosition(SimMultiValueField3D.Axis.X, 1000, out isOutside);
            AssertUtil.AssertDoubleEqual(2, val);
            Assert.AreEqual(true, isOutside);

            val = mvt.ValueFromAxisPosition(SimMultiValueField3D.Axis.Y, 1.7);
            AssertUtil.AssertDoubleEqual(0.017, val);
            val = mvt.ValueFromAxisPosition(SimMultiValueField3D.Axis.Y, 1.7, out isOutside);
            AssertUtil.AssertDoubleEqual(0.017, val);
            Assert.AreEqual(false, isOutside);
            val = mvt.ValueFromAxisPosition(SimMultiValueField3D.Axis.Y, -1000, out isOutside);
            AssertUtil.AssertDoubleEqual(0, val);
            Assert.AreEqual(true, isOutside);
            val = mvt.ValueFromAxisPosition(SimMultiValueField3D.Axis.Y, 1000, out isOutside);
            AssertUtil.AssertDoubleEqual(0.03, val);
            Assert.AreEqual(true, isOutside);

            val = mvt.ValueFromAxisPosition(SimMultiValueField3D.Axis.Z, 2.3);
            AssertUtil.AssertDoubleEqual(230, val);
            val = mvt.ValueFromAxisPosition(SimMultiValueField3D.Axis.Z, 2.3, out isOutside);
            AssertUtil.AssertDoubleEqual(230, val);
            Assert.AreEqual(false, isOutside);
            val = mvt.ValueFromAxisPosition(SimMultiValueField3D.Axis.Z, -1000, out isOutside);
            AssertUtil.AssertDoubleEqual(0, val);
            Assert.AreEqual(true, isOutside);
            val = mvt.ValueFromAxisPosition(SimMultiValueField3D.Axis.Z, 1000, out isOutside);
            AssertUtil.AssertDoubleEqual(400, val);
            Assert.AreEqual(true, isOutside);
        }

        [TestMethod]
        public void CreateValuePointer()
        {
            var data = TestData(3, 4, 5);
            var mvt = new SimMultiValueField3D(data.name, data.xaxis, data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ, data.data, true);

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.ValueManager.Add(mvt);

            var mvp = mvt.CreateNewPointer() as SimMultiValueField3DParameterSource;

            AssertUtil.AssertDoubleEqual(0, mvp.AxisValueX);
            AssertUtil.AssertDoubleEqual(0, mvp.AxisValueY);
            AssertUtil.AssertDoubleEqual(0, mvp.AxisValueZ);
            Assert.AreEqual(mvt, mvp.ValueField);
            Assert.AreEqual(null, mvp.TargetParameter);
        }

        [TestMethod]
        public void ValueChangedEvent()
        {
            var data = TestData(3, 4, 5);
            var mvt = new SimMultiValueField3D(data.name, data.xaxis, data.unitX, data.yaxis, data.unitY, data.zaxis, data.unitZ, data.data, true);
            var eventCounter = new MultiValueTableEventCounter(mvt);

            mvt[1, 2, 3] = 77.77;

            eventCounter.AssertCount(0, 1, 0);
            AssertUtil.AreEqual(new SimPoint3D(0, 0.01, 200), eventCounter.ValueChangedArgs[0].Range.Minimum);
            AssertUtil.AreEqual(new SimPoint3D(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity), eventCounter.ValueChangedArgs[0].Range.Maximum);

            mvt[0, 0, 0] = -99;
            eventCounter.AssertCount(0, 2, 0);
            AssertUtil.AreEqual(new SimPoint3D(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity), eventCounter.ValueChangedArgs[1].Range.Minimum);
            AssertUtil.AreEqual(new SimPoint3D(1, 0.01, 100), eventCounter.ValueChangedArgs[1].Range.Maximum);

            mvt[0, 1, 1] = -99;
            eventCounter.AssertCount(0, 3, 0);
            AssertUtil.AreEqual(new SimPoint3D(double.NegativeInfinity, 0, 0), eventCounter.ValueChangedArgs[2].Range.Minimum);
            AssertUtil.AreEqual(new SimPoint3D(1, 0.02, 200), eventCounter.ValueChangedArgs[2].Range.Maximum);
        }

        [TestMethod]
        public void Count()
        {
            (var data, var mvt, _) = TestDataTable(3, 4, 5);

            Assert.AreEqual(3, mvt.Count(0));
            Assert.AreEqual(4, mvt.Count(1));
            Assert.AreEqual(5, mvt.Count(2));

            mvt.XAxis.Add(1000);
            Assert.AreEqual(4, mvt.Count(0));
            Assert.AreEqual(4, mvt.Count(1));
            Assert.AreEqual(5, mvt.Count(2));

            mvt.YAxis.Add(1000);
            Assert.AreEqual(4, mvt.Count(0));
            Assert.AreEqual(5, mvt.Count(1));
            Assert.AreEqual(5, mvt.Count(2));

            mvt.ZAxis.Add(1000);
            Assert.AreEqual(4, mvt.Count(0));
            Assert.AreEqual(5, mvt.Count(1));
            Assert.AreEqual(6, mvt.Count(2));


            Assert.AreEqual(mvt.Count(0), mvt.Count(SimMultiValueField3D.Axis.X));
            Assert.AreEqual(mvt.Count(1), mvt.Count(SimMultiValueField3D.Axis.Y));
            Assert.AreEqual(mvt.Count(2), mvt.Count(SimMultiValueField3D.Axis.Z));


            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { mvt.Count(-1); });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { mvt.Count(3); });
        }

        [TestMethod]
        public void CanInterpolateChanged()
        {
            (var data, var mvt, _) = TestDataTable(3, 4, 5);
            var eventCounter = new MultiValueTableEventCounter(mvt);

            Assert.AreEqual(4.0, mvt.GetValue(new SimPoint3D(0.25, 0.25, 0.25)));

            mvt.CanInterpolate = false;
            eventCounter.AssertCount(1, 0, 0);
            Assert.AreEqual(nameof(SimMultiValue.CanInterpolate), eventCounter.PropertyChangedArgs[0]);
            Assert.AreEqual(0, mvt.GetValue(new SimPoint3D(0.25, 0.25, 0.25)));

            mvt.CanInterpolate = true;
            eventCounter.AssertCount(2, 0, 0);
            Assert.AreEqual(nameof(SimMultiValue.CanInterpolate), eventCounter.PropertyChangedArgs[1]);
            Assert.AreEqual(4.0, mvt.GetValue(new SimPoint3D(0.25, 0.25, 0.25)));
        }

        [TestMethod]
        public void PropertyChanged()
        {
            (var data, var mvt, _) = TestDataTable(3, 4, 5);
            var eventCounter = new MultiValueTableEventCounter(mvt);

            mvt.UnitX = "asdf";
            Assert.AreEqual("asdf", mvt.UnitX);
            eventCounter.AssertCount(1, 0, 0);
            Assert.AreEqual(nameof(SimMultiValueBigTable.UnitX), eventCounter.PropertyChangedArgs[0]);

            mvt.UnitY = "asdf";
            Assert.AreEqual("asdf", mvt.UnitY);
            eventCounter.AssertCount(2, 0, 0);
            Assert.AreEqual(nameof(SimMultiValueBigTable.UnitY), eventCounter.PropertyChangedArgs[1]);

            mvt.UnitZ = "asdf";
            Assert.AreEqual("asdf", mvt.UnitZ);
            eventCounter.AssertCount(3, 0, 0);
            Assert.AreEqual(nameof(SimMultiValueBigTable.UnitZ), eventCounter.PropertyChangedArgs[2]);

            mvt.Name = "asdf";
            Assert.AreEqual("asdf", mvt.Name);
            eventCounter.AssertCount(4, 0, 0);
            Assert.AreEqual(nameof(SimMultiValueBigTable.Name), eventCounter.PropertyChangedArgs[3]);
        }
    }
}