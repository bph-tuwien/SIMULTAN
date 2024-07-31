using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Tests.TestUtils;
using System;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.Values
{
    [TestClass]
    public class MultiValueField3DPointerTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@"./Field3DTestsProject.simultan");

        private void CheckPointer(SimMultiValueField3DParameterSource ptr, SimMultiValueField3D table, double x, double y, double z)
        {
            Assert.AreEqual(table, ptr.ValueField);
            AssertUtil.AssertDoubleEqual(x, ptr.AxisValueX);
            AssertUtil.AssertDoubleEqual(y, ptr.AxisValueY);
            AssertUtil.AssertDoubleEqual(z, ptr.AxisValueZ);
        }

        #region Tests without Parameters

        [TestMethod]
        public void Ctor()
        {
            var data = SimMultiValueField3DTests.TestDataTable(3, 4, 5);

            Assert.ThrowsException<ArgumentNullException>(() => { new SimMultiValueField3DParameterSource(null, 0, 0, 0); });

            var dTable = SimMultiValueBigTableTests.DoubleTestDataTable(3, 4);
            var iTable = SimMultiValueBigTableTests.IntTestDataTable(3, 4);
            var sTable = SimMultiValueBigTableTests.StringTestDataTable(3, 4);
            var boolTable = SimMultiValueBigTableTests.BoolTestDataTable(3, 4);


            var ptr = new SimMultiValueField3DParameterSource(data.table, 0.25, 1.25, 2.25);
            CheckPointer(ptr, data.table, 0.25, 1.25, 2.25);
        }

        [TestMethod]
        public void CloneTest()
        {
            var data = SimMultiValueField3DTests.TestDataTable(3, 4, 5);

            var ptr = new SimMultiValueField3DParameterSource(data.table, 0.25, 1.25, 2.25);

            var ptr2 = ptr.Clone();
            Assert.IsTrue(ptr2 is SimMultiValueField3DParameterSource);
            CheckPointer((SimMultiValueField3DParameterSource)ptr2, data.table, 0.25, 1.25, 2.25);
        }

        [TestMethod]
        public void GetValueTests()
        {
            var data = SimMultiValueField3DTests.TestDataTable(3, 4, 5);

            var ptr = new SimMultiValueField3DParameterSource(data.table, 0.25, 1.25 / 100.0, 2.25 * 100.0);
            Assert.AreEqual(31.0, (double)ptr.GetValue());

            //Outside
            ptr = new SimMultiValueField3DParameterSource(data.table, -1, -1, -1);
            Assert.AreEqual(0.0, (double)ptr.GetValue());

            ptr = new SimMultiValueField3DParameterSource(data.table, 1000, -1, -1);
            Assert.AreEqual(2.0, (double)ptr.GetValue());

            ptr = new SimMultiValueField3DParameterSource(data.table, 1000, 1000, -1);
            Assert.AreEqual(11.0, (double)ptr.GetValue());

            ptr = new SimMultiValueField3DParameterSource(data.table, 1000, 1000, 1000);
            Assert.AreEqual(59.0, (double)ptr.GetValue());

            ptr.Dispose();
            Assert.ThrowsException<InvalidOperationException>(() => (double)ptr.GetValue());

            ptr = new SimMultiValueField3DParameterSource(data.table, double.NaN, -1, -1);
            Assert.IsTrue(double.IsNaN((double)ptr.GetValue()));

            ptr = new SimMultiValueField3DParameterSource(data.table, -1, double.NaN, -1);
            Assert.IsTrue(double.IsNaN((double)ptr.GetValue()));

            ptr = new SimMultiValueField3DParameterSource(data.table, -1, -1, double.NaN);
            Assert.IsTrue(double.IsNaN((double)ptr.GetValue()));
        }

        [TestMethod]
        public void IsSamePointerTest()
        {
            var data = SimMultiValueField3DTests.TestDataTable(3, 4, 5);

            var ptr = new SimMultiValueField3DParameterSource(data.table, 0.25, 1.25 / 100.0, 2.25 * 100.0);
            var ptr2 = new SimMultiValueField3DParameterSource(data.table, 0.25, 1.25 / 100.0, 2.25 * 100.0);
            Assert.IsTrue(ptr.IsSamePointer(ptr2));

            ptr2 = new SimMultiValueField3DParameterSource(data.table, 1.25, 1.25 / 100.0, 2.25 * 100.0);
            Assert.IsFalse(ptr.IsSamePointer(ptr2));

            ptr2 = new SimMultiValueField3DParameterSource(data.table, 0.25, 2.25 / 100.0, 2.25 * 100.0);
            Assert.IsFalse(ptr.IsSamePointer(ptr2));

            var data2 = SimMultiValueField3DTests.TestDataTable(2, 2, 2);
            ptr2 = new SimMultiValueField3DParameterSource(data2.table, 0.25, 1.25 / 100.0, 2.25 * 100.0);
            Assert.IsFalse(ptr.IsSamePointer(ptr2));

            //Test other pointer types
        }

        private WeakReference MemoryLeakTest_Action(SimMultiValueField3D field)
        {
            var ptrRef = new WeakReference(new SimMultiValueField3DParameterSource(field, 0.25, 1.25 / 100.0, 2.25 * 100.0));
            return ptrRef;
        }
        private void MemoryLeakTest_Action2(WeakReference ptrRef)
        {
            ((SimMultiValueField3DParameterSource)ptrRef.Target).Dispose();
        }

        [TestMethod]
        public void MemoryLeakTest()
        {
            var data = SimMultiValueField3DTests.TestDataTable(3, 4, 5);

            var ptrRef = MemoryLeakTest_Action(data.table);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsTrue(ptrRef.IsAlive);

            MemoryLeakTest_Action2(ptrRef);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(ptrRef.IsAlive);
        }

        [TestMethod]
        public void DefaultPointerTest()
        {
            var data = SimMultiValueField3DTests.TestDataTable(3, 4, 5);
            var ptr = data.table.DefaultPointer;

            CheckPointer((SimMultiValueField3DParameterSource)ptr, data.table, 0, 0, 0);
        }

        [TestMethod]
        public void CreateNewPointerTest()
        {
            var data = SimMultiValueField3DTests.TestDataTable(3, 4, 5);
            var ptr = data.table.CreateNewPointer();

            CheckPointer((SimMultiValueField3DParameterSource)ptr, data.table, 0, 0, 0);

            ptr = data.table.CreateNewPointer(null);
            CheckPointer((SimMultiValueField3DParameterSource)ptr, data.table, 0, 0, 0);

            var sourcePtr = new SimMultiValueField3DParameterSource(data.table, 1.5, 2.5, 3.5);
            ptr = data.table.CreateNewPointer(sourcePtr);
            CheckPointer((SimMultiValueField3DParameterSource)ptr, data.table, 1.5, 2.5, 3.5);
        }

        [TestMethod]
        public void ValueChangedTest()
        {
            var data = SimMultiValueField3DTests.TestDataTable(3, 4, 5);

            var ptr = new SimMultiValueField3DParameterSource(data.table, 0.25, 1.25 / 100.0, 2.25 * 100.0);
            Assert.AreEqual(31.0, (double)ptr.GetValue());

            int eventCount = 0;
            ptr.ValueChanged += (o, e) => eventCount++;

            //No modification since range not affected
            data.table[2, 3, 4] = 999.9;
            Assert.AreEqual(0, eventCount);

            //Inside range
            data.table[0, 1, 2] = 270.0;
            Assert.AreEqual(1, eventCount);
            AssertUtil.AssertDoubleEqual(133.5156, (double)ptr.GetValue(), 0.001);

            ptr.Dispose();

            //Inside range, but only due to not interpolating
            data.table.CanInterpolate = false;
            ptr = new SimMultiValueField3DParameterSource(data.table, 1.5, 0.017478, 200.0);
            eventCount = 0;
            ptr.ValueChanged += (o, e) => eventCount++;

            data.table[2, 2, 2] = 9977;
            Assert.AreEqual(1, eventCount);
            AssertUtil.AssertDoubleEqual(9977, (double)ptr.GetValue(), 0.001);
        }

        [TestMethod]
        public void XAxisChanged()
        {
            var data = SimMultiValueField3DTests.TestDataTable(3, 4, 5);

            var ptr = new SimMultiValueField3DParameterSource(data.table, 0.25, 1.25 / 100.0, 2.25 * 100.0);
            Assert.AreEqual(31.0, (double)ptr.GetValue());

            int eventCount = 0;
            ptr.ValueChanged += (o, e) => eventCount++;

            //No modification since range not affected
            data.table.XAxis.Insert(0, -1);
            Assert.AreEqual(1, eventCount);

            data.table.XAxis[1] = 10.0;
            Assert.AreEqual(2, eventCount);

            data.table.XAxis.Clear();
            Assert.AreEqual(4, eventCount); //Should be 3 after the rework
            Assert.IsTrue(double.IsNaN(ptr.AxisValueX));
            Assert.IsTrue(double.IsNaN(ptr.AxisValueY));
            Assert.IsTrue(double.IsNaN(ptr.AxisValueZ));
        }

        [TestMethod]
        public void YAxisChanged()
        {
            var data = SimMultiValueField3DTests.TestDataTable(3, 4, 5);

            var ptr = new SimMultiValueField3DParameterSource(data.table, 0.25, 1.25 / 100.0, 2.25 * 100.0);
            Assert.AreEqual(31.0, (double)ptr.GetValue());

            int eventCount = 0;
            ptr.ValueChanged += (o, e) => eventCount++;

            //No modification since range not affected
            data.table.YAxis.Insert(0, -1);
            Assert.AreEqual(1, eventCount);

            data.table.YAxis[1] = 10.0;
            Assert.AreEqual(2, eventCount);

            data.table.YAxis.Clear();
            Assert.AreEqual(4, eventCount); //Should be 3 after the rework
            Assert.IsTrue(double.IsNaN(ptr.AxisValueX));
            Assert.IsTrue(double.IsNaN(ptr.AxisValueY));
            Assert.IsTrue(double.IsNaN(ptr.AxisValueZ));
        }

        [TestMethod]
        public void ZAxisChanged()
        {
            var data = SimMultiValueField3DTests.TestDataTable(3, 4, 5);

            var ptr = new SimMultiValueField3DParameterSource(data.table, 0.25, 1.25 / 100.0, 2.25 * 100.0);
            Assert.AreEqual(31.0, (double)ptr.GetValue());

            int eventCount = 0;
            ptr.ValueChanged += (o, e) => eventCount++;

            //No modification since range not affected
            data.table.ZAxis.Insert(0, -1);
            Assert.AreEqual(1, eventCount);

            data.table.ZAxis[1] = 10.0;
            Assert.AreEqual(2, eventCount);

            data.table.ZAxis.Clear();
            Assert.AreEqual(4, eventCount); //Should be 3 after the rework
            Assert.IsTrue(double.IsNaN(ptr.AxisValueX));
            Assert.IsTrue(double.IsNaN(ptr.AxisValueY));
            Assert.IsTrue(double.IsNaN(ptr.AxisValueZ));
        }

        #endregion

        #region Tests with Parameters

        [TestMethod]
        public void PointerParameterTest()
        {
            LoadProject(testProject);

            //Find ptr
            var comp = projectData.Components.First(x => x.Name == "WithPointer");
            var param = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "Target");

            var ptr = (SimMultiValueField3DParameterSource)param.ValueSource;
            CheckPointer(ptr, ptr.Field, 1.5, 0.017478, 200.0);
            Assert.AreEqual(param, ptr.TargetParameter);
            Assert.AreEqual(38.0, (double)ptr.GetValue());
        }

        [TestMethod]
        public void PointerParameterChangedTest()
        {
            LoadProject(testProject);

            int eventCounter = 0;

            //Find ptr
            var comp = projectData.Components.First(x => x.Name == "WithPointer");
            var param = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "Target");
            var xParam = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "Target.ValuePointer.OffsetX");
            var yParam = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "Target.ValuePointer.OffsetY");
            var zParam = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "Target.ValuePointer.OffsetZ");

            var ptr = (SimMultiValueField3DParameterSource)param.ValueSource;
            ptr.ValueChanged += (s, e) => eventCounter++;
            Assert.AreEqual(38.0, (double)ptr.GetValue());

            xParam.Value = -1.1;
            Assert.AreEqual(1, eventCounter);
            Assert.AreEqual(36.0, (double)ptr.GetValue());

            yParam.Value = -0.011;
            Assert.AreEqual(2, eventCounter);
            Assert.AreEqual(42.0, (double)ptr.GetValue());

            zParam.Value = -100.1;
            Assert.AreEqual(3, eventCounter);
            Assert.AreEqual(6.0, (double)ptr.GetValue());
        }

        [TestMethod]
        public void RemovePointerParameterTest()
        {
            LoadProject(testProject);

            int eventCounter = 0;

            //Find ptr
            var comp = projectData.Components.First(x => x.Name == "WithPointer");
            var param = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "Target");
            var xParam = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "Target.ValuePointer.OffsetX");
            var yParam = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "Target.ValuePointer.OffsetY");
            var zParam = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "Target.ValuePointer.OffsetZ");

            var ptr = (SimMultiValueField3DParameterSource)param.ValueSource;
            ptr.ValueChanged += (s, e) => eventCounter++;

            comp.Parameters.Remove(xParam);
            Assert.AreEqual(1, eventCounter);
            Assert.AreEqual(38, (double)ptr.GetValue());

            comp.Parameters.Remove(yParam);
            Assert.AreEqual(2, eventCounter);
            Assert.AreEqual(41, (double)ptr.GetValue());

            comp.Parameters.Remove(zParam);
            Assert.AreEqual(3, eventCounter);
            Assert.AreEqual(29, (double)ptr.GetValue());

            //Make sure no further updates are called
            xParam.Value = 0;
            Assert.AreEqual(3, eventCounter);
        }

        [TestMethod]
        public void AddPointerParameterTest()
        {
            LoadProject(testProject);

            var comp = projectData.Components.First(x => x.Name == "NoParameters");
            var param = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "Target");

            ((SimMultiValueParameterSource)param.ValueSource).CreateValuePointerParameters(projectData.UsersManager.Users.First());

            var xParam = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "Target.ValuePointer.OffsetX");
            var yParam = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "Target.ValuePointer.OffsetY");
            var zParam = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "Target.ValuePointer.OffsetZ");

            Assert.IsNotNull(xParam);
            Assert.IsNotNull(yParam);
            Assert.IsNotNull(zParam);

            xParam.Value = 1.5;
            Assert.AreEqual(11, param.Value);

            yParam.Value = 0.015;
            Assert.AreEqual(5, param.Value);

            zParam.Value = 150.0;
            Assert.AreEqual(29, param.Value);
        }

        private WeakReference MemoryLeakRemoveFromParameterTest_Action()
        {
            var comp = projectData.Components.First(x => x.Name == "WithPointer");
            var param = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "Target");

            WeakReference ptrRef = new WeakReference(param.ValueSource);
            param.ValueSource = null;
            param = null;

            return ptrRef;
        }

        [TestMethod]
        public void MemoryLeakRemoveFromParameterTest()
        {
            LoadProject(testProject);

            var ptrRef = MemoryLeakRemoveFromParameterTest_Action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(ptrRef.IsAlive);
        }


        private WeakReference MemoryLeakRemoveParameterTest_Action()
        {
            var comp = projectData.Components.First(x => x.Name == "WithPointer");
            var param = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "Target");

            WeakReference ptrRef = new WeakReference(param.ValueSource);

            comp.Parameters.Remove(param);

            return ptrRef;
        }
        [TestMethod]
        public void MemoryLeakRemoveParameterTest()
        {
            LoadProject(testProject);

            WeakReference ptrRef = MemoryLeakRemoveParameterTest_Action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(ptrRef.IsAlive);
        }

        [TestMethod]
        public void ValueChangedWithOffsetParameters()
        {
            LoadProject(testProject);

            var comp = projectData.Components.First(x => x.Name == "WithPointer");
            var param = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "Target");

            var table = ((SimMultiValueField3DParameterSource)param.ValueSource).Field;
            table[2, 3, 3] = 9977;

            Assert.AreEqual(9977, param.Value);
        }

        #endregion
    }
}
